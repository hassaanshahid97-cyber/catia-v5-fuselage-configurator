Option Strict Off
Option Explicit On

Imports System.Runtime.InteropServices

Public Class BuildSolidFuselage

#Region "Fields"

    '-- CATIA COM handles -----------------------------------------------------
    Private oCATIA As Object   ' CATIA.Application
    Private oDoc As Object   ' PartDocument
    Private oPart As Object   ' Part
    Private oHSF As Object   ' HybridShapeFactory  (GSD)
    Private oSF As Object   ' ShapeFactory        (Part Design)
    Private oBodies As Object   ' Bodies              (for new longeron bodies)
    Private oHBodies As Object  ' HybridBodies        (for construction geometry)

    '-- Construction geometry set (all wireframe/surfaces live here) ---------
    Private oBH_Const As Object  ' "00_Construction"

    '-- Bulkhead splines and their references --------------------------------
    '   Index: 0=X150  1=X700  2=X850  3=X1400
    Private oBhk(3) As Object
    Private refBhk(3) As Object

    '-- Truss station table (same numbers as your Form1) ---------------------
    '   aStX = fuselage X position (mm)
    '   aStW = half-width  (Y)   (mm)
    '   aStH = half-height (Z)   (mm)
    Private ReadOnly aStX() As Double = {0.0, 200.0, 500.0, 700.0, 850.0, 1100.0, 1400.0}
    Private ReadOnly aStW() As Double = {5.0, 60.0, 130.0, 150.0, 150.0, 110.0, 70.0}
    Private ReadOnly aStH() As Double = {5.0, 40.0, 85.0, 100.0, 100.0, 72.0, 45.0}

    '-- Outer-skin (OML) bulkhead table --------------------------------------
    Private ReadOnly aBhkX() As Double = {150.0, 700.0, 850.0, 1400.0}
    Private ReadOnly aBhkW() As Double = {75.0, 150.0, 150.0, 105.0}
    Private ReadOnly aBhkH() As Double = {50.0, 100.0, 100.0, 70.0}

    '-- Longeron cross-section + extent --------------------------------------
    Private Const LGR_HW As Double = 6.0   ' Half-width  of longeron section
    Private Const LGR_HH As Double = 6.0   ' Half-height of longeron section
    Private Const LGR_R As Double = 2.0   ' Corner-fillet radius
    Private Const LGR_IST_START As Integer = 1   ' First station used (X=200)
    Private Const LGR_IST_END As Integer = 5   ' Last  station used (X=1100)

    '-- Logger delegate (so the caller can route messages to their TextBox) --
    Private _log As Action(Of String)

    Public Sub New()

    End Sub

#End Region

#Region "Public entry point"

    ''' <summary>
    ''' Full one-click build:
    '''   connect -> new part -> skin solid -> 4 longeron solids.
    ''' </summary>
    ''' <param name="logger">
    ''' Delegate that receives status strings. Pass AddressOf YourLogSub.
    ''' If Nothing, messages go to Debug.WriteLine.
    ''' </param>
    Public Sub Run(Optional logger As Action(Of String) = Nothing)
        _log = If(logger, New Action(Of String)(AddressOf DebugLog))

        Try
            Log("== Findus UAV : SOLID FUSELAGE BUILD ==")

            ConnectAndCreatePart()
            BuildBulkheads()
            BuildSkinSolid()
            BuildLongeronSolids()

            oPart.Update()
            Log("== BUILD COMPLETE ==")
        Catch ex As Exception
            Log("[FATAL] " & ex.Message)
            Log("        Source: " & ex.Source)
            Throw
        End Try

    End Sub

#End Region

#Region "Stage 1 - Connect + new part"

    ' Attach to a running CATIA, then add a new Part document.
    Private Sub ConnectAndCreatePart()

        Log("Stage 1: connect + create new Part...")

        ' 1. Attach to running CATIA
        Try
            oCATIA = Marshal.GetActiveObject("CATIA.Application")
        Catch
            Throw New Exception("CATIA is not running. Start CATIA V5, then retry.")
        End Try
        oCATIA.Visible = True

        ' 2. Add a new Part document
        Dim oDocs As Object = oCATIA.Documents
        oDoc = oDocs.Add("Part")
        oPart = oDoc.Part

        ' Name the part (cosmetic - shows in tree)
        Try
            oPart.Name = "Findus_UAV_Fuselage"
        Catch
        End Try

        ' 3. Cache the factories we will use everywhere
        oHSF = oPart.HybridShapeFactory
        oSF = oPart.ShapeFactory
        oBodies = oPart.Bodies
        oHBodies = oPart.HybridBodies

        ' 4. One geometry set for construction (splines + surfaces + joins)
        oBH_Const = GetOrAddHB("00_Construction")

        Log("   New Part created : " & oDoc.Name)
    End Sub

#End Region

#Region "Stage 2 - Bulkhead splines (OML cross-sections)"

    ' 4 closed elliptical splines (8 points each) at X = 150, 700, 850, 1400 mm.
    ' Stores oBhk(0..3) and refBhk(0..3) which feed the loft in Stage 3.
    Private Sub BuildBulkheads()

        Log("Stage 2: bulkhead splines (OML)...")

        Dim hb As Object = oBH_Const
        Dim dPi As Double = 4.0 * Math.Atan(1.0)

        For i As Integer = 0 To 3

            Dim oSpline As Object = oHSF.AddNewSpline()
            oSpline.SetClosing(1)            ' close the spline

            ' 8 sample points around the ellipse, parametric angle 0..7*pi/4
            For j As Integer = 0 To 7
                Dim angle As Double = CDbl(j) * dPi / 4.0
                Dim y As Double = aBhkW(i) * Math.Cos(angle)
                Dim z As Double = aBhkH(i) * Math.Sin(angle)
                oSpline.AddPoint(Ref(oHSF.AddNewPointCoord(aBhkX(i), y, z)))
            Next

            oSpline.Name = "Bulkhead_X" & CInt(aBhkX(i)) & "mm"
            hb.AppendHybridShape(oSpline)
            oBhk(i) = oSpline
            refBhk(i) = Ref(oSpline)

        Next

        oPart.Update()
        Log("   4 bulkhead splines : X = 150, 700, 850, 1400 mm")
    End Sub

#End Region

#Region "Stage 3 - Outer-skin solid (PartBody)"

    ' Loft -> Nose fill -> Tail fill -> Join -> CloseSurface -> SOLID
    ' All construction geometry lives in "00_Construction".
    ' The resulting solid is added to PartBody (the in-work Body by default).
    Private Sub BuildSkinSolid()

        Log("Stage 3: outer-skin solid...")

        Dim hb As Object = oBH_Const

        ' 3.1  Loft through the 4 bulkheads
        Dim oLoft As Object = oHSF.AddNewLoft()
        oLoft.SectionCoupling = 1
        oLoft.Relimitation = 1
        oLoft.CanonicalDetection = 2

        ' FIX: Use missing value instead of Nothing for optional COM parameters
        Dim missingValue As Object = System.Reflection.Missing.Value

        oLoft.AddSectionToLoft(refBhk(0), 1, missingValue)
        oLoft.AddSectionToLoft(refBhk(1), 1, missingValue)
        oLoft.AddSectionToLoft(refBhk(2), 1, missingValue)
        oLoft.AddSectionToLoft(refBhk(3), 1, missingValue)

        oLoft.Name = "Fuselage_OML"
        hb.AppendHybridShape(oLoft)
        Dim refLoft As Object = Ref(oLoft)
        oPart.Update()
        Log("   Loft surface     : Fuselage_OML")

        ' 3.2  Cap the nose (X=150 ellipse)
        Dim oFillNose As Object = oHSF.AddNewFill()
        oFillNose.AddBound(refBhk(0))
        oFillNose.Name = "Endcap_Nose"
        hb.AppendHybridShape(oFillNose)
        Dim refFillNose As Object = Ref(oFillNose)

        ' 3.3  Cap the tail (X=1400 ellipse)
        Dim oFillTail As Object = oHSF.AddNewFill()
        oFillTail.AddBound(refBhk(3))
        oFillTail.Name = "Endcap_Tail"
        hb.AppendHybridShape(oFillTail)
        Dim refFillTail As Object = Ref(oFillTail)
        oPart.Update()
        Log("   End-caps         : nose + tail")

        ' 3.4  Join into a closed shell.
        ' NOTE: Use property assignment (NOT SetConnex/SetManifold/SetSimplify) -
        '       those setters raise Type Mismatch in late-bound R21 COM.
        Dim oJoin As Object = oHSF.AddNewJoin()
        oJoin.AddElement(refLoft)
        oJoin.AddElement(refFillNose)
        oJoin.AddElement(refFillTail)
        oJoin.Connex = 1
        oJoin.Manifold = 1
        oJoin.Simplify = 1
        oJoin.SuppressMode = 1
        oJoin.Deviation = 0.001
        oJoin.Name = "Fuselage_Shell"
        hb.AppendHybridShape(oJoin)
        Dim refJoin As Object = Ref(oJoin)
        oPart.Update()
        Log("   Closed shell     : Fuselage_Shell")

        ' 3.5  Close the surface -> SOLID in PartBody
        Try
            ' Make sure PartBody is in-work
            Dim oPartBody As Object = oBodies.Item("PartBody")
            oPart.InWorkObject = oPartBody

            ' CATIA V5 R21 requires the Join object directly, not a reference
            Dim oCS As Object = Nothing
            Try
                ' Try with Join object directly
                oCS = oSF.AddNewCloseSurface(oJoin)
            Catch
                ' Fallback: try with reference
                Try
                    oCS = oSF.AddNewCloseSurface(refJoin)
                Catch
                    ' If both fail, skip CloseSurface
                    Log("   [!] CloseSurface skipped - surfaces created but not closed to solid")
                    Log("       Manually close via: Insert > Surface-Based Features > Close Surface")
                    oPart.Update()
                    Return
                End Try
            End Try

            oPart.Update()
            Log("   SOLID skin       : PartBody [OK]")
        Catch ex As Exception
            Log("   [WARN] CloseSurface failed : " & ex.Message)
            Log("          Surfaces built OK. Close it manually via:")
            Log("          Insert > Surface-Based Features > Close Surface")
        End Try

    End Sub

#End Region

#Region "Stage 4 - 4 longeron solids"

    ' Loop the 4 corners. For each:
    '   * create N closed splines (rounded-rectangle profile) at stations
    '     iSt = LGR_IST_START .. LGR_IST_END
    '   * loft them, cap front + rear, join, then CloseSurface in its own Body.
    Private Sub BuildLongeronSolids()

        Log("Stage 4: 4 longeron solids...")

        ' Corner table: 0=TP(+Y,+Z) 1=TS(-Y,+Z) 2=BP(+Y,-Z) 3=BS(-Y,-Z)
        Dim cornerNames() As String = {"Lgrn_TopPort", "Lgrn_TopStbd",
                                        "Lgrn_BotPort", "Lgrn_BotStbd"}
        Dim signY() As Integer = {+1, -1, +1, -1}
        Dim signZ() As Integer = {+1, +1, -1, -1}

        For c As Integer = 0 To 3
            BuildOneLongeron(c, cornerNames(c), signY(c), signZ(c))
        Next

        Log("   4 longerons built (each in its own Body).")
    End Sub

    ' Build one longeron solid (one corner).
    Private Sub BuildOneLongeron(corner As Integer,
                                 lgrName As String,
                                 sY As Integer,
                                 sZ As Integer)

        Log("   - " & lgrName & " ...")

        Dim hb As Object = oBH_Const
        Dim nSt As Integer = LGR_IST_END - LGR_IST_START + 1   ' 5 stations
        Dim oProfile(nSt - 1) As Object
        Dim refProfile(nSt - 1) As Object

        ' 4.1  Build the N rounded-rectangle closed splines (one per station)
        Dim k As Integer = 0
        For iSt As Integer = LGR_IST_START To LGR_IST_END

            Dim yc As Double = sY * aStW(iSt)
            Dim zc As Double = sZ * aStH(iSt)
            Dim oSpln As Object = NewRoundedRectSpline(aStX(iSt), yc, zc,
                                                       LGR_HW, LGR_HH, LGR_R)

            oSpln.Name = lgrName & "_Sect_St" & iSt
            hb.AppendHybridShape(oSpln)
            oProfile(k) = oSpln
            refProfile(k) = Ref(oSpln)
            k += 1
        Next

        oPart.Update()

        ' 4.2  Loft the profiles
        Dim oLoft As Object = oHSF.AddNewLoft()
        oLoft.SectionCoupling = 1
        oLoft.Relimitation = 1
        oLoft.CanonicalDetection = 2

        ' FIX: Use missing value instead of Nothing for optional COM parameters
        Dim missingValue As Object = System.Reflection.Missing.Value

        For i As Integer = 0 To nSt - 1
            oLoft.AddSectionToLoft(refProfile(i), 1, missingValue)
        Next

        oLoft.Name = lgrName & "_Loft"
        hb.AppendHybridShape(oLoft)
        Dim refLoft As Object = Ref(oLoft)
        oPart.Update()

        ' 4.3  Cap front and rear
        Dim oFillF As Object = oHSF.AddNewFill()
        oFillF.AddBound(refProfile(0))
        oFillF.Name = lgrName & "_CapF"
        hb.AppendHybridShape(oFillF)
        Dim refFillF As Object = Ref(oFillF)

        Dim oFillR As Object = oHSF.AddNewFill()
        oFillR.AddBound(refProfile(nSt - 1))
        oFillR.Name = lgrName & "_CapR"
        hb.AppendHybridShape(oFillR)
        Dim refFillR As Object = Ref(oFillR)
        oPart.Update()

        ' 4.4  Join into a closed shell
        Dim oJoin As Object = oHSF.AddNewJoin()
        oJoin.AddElement(refLoft)
        oJoin.AddElement(refFillF)
        oJoin.AddElement(refFillR)
        oJoin.Connex = 1
        oJoin.Manifold = 1
        oJoin.Simplify = 1
        oJoin.SuppressMode = 1
        oJoin.Deviation = 0.001
        oJoin.Name = lgrName & "_Shell"
        hb.AppendHybridShape(oJoin)
        Dim refJoin As Object = Ref(oJoin)
        oPart.Update()

        ' 4.5  Each longeron in its own Body so it can be hidden separately.
        Dim newBody As Object = oBodies.Add()
        newBody.Name = lgrName
        oPart.InWorkObject = newBody
        Try
            ' CATIA V5 R21: try Join object first, then reference
            Dim oCS As Object = Nothing
            Try
                oCS = oSF.AddNewCloseSurface(oJoin)
            Catch
                Try
                    oCS = oSF.AddNewCloseSurface(refJoin)
                Catch
                    Log("        [WARN] CloseSurface for " & lgrName & " : surfaces created but not solid")
                End Try
            End Try

            oPart.Update()
        Catch ex As Exception
            Log("        [WARN] CloseSurface for " & lgrName & " : " & ex.Message)
        End Try

    End Sub

    ' Build a closed rounded-rectangle spline at X=Xs, centred at (Yc,Zc).
    ' 16 points: 4 straight-edge midpoints + 4 corner-start + 4 corner-mid + 4 corner-end.
    ' Going CCW starting from right-edge midpoint (Y=+hW, Z=0).
    Private Function NewRoundedRectSpline(Xs As Double, Yc As Double, Zc As Double,
                                          hW As Double, hH As Double, R As Double) As Object

        Dim oSpln As Object = oHSF.AddNewSpline()
        oSpln.SetClosing(1)

        Dim cos45 As Double = Math.Cos(Math.PI / 4.0)   ' 0.7071...
        Dim sin45 As Double = Math.Sin(Math.PI / 4.0)

        ' 16 points in order (Y, Z) relative to centre; CCW.
        Dim pts(15, 1) As Double
        Dim i As Integer = 0
        ' Right-edge mid
        pts(i, 0) = +hW : pts(i, 1) = 0 : i += 1
        ' Right edge top end (top-right arc start)
        pts(i, 0) = +hW : pts(i, 1) = +(hH - R) : i += 1
        ' Top-right arc mid (45 deg)
        pts(i, 0) = +(hW - R) + R * cos45 : pts(i, 1) = +(hH - R) + R * sin45 : i += 1
        ' Top-right arc end
        pts(i, 0) = +(hW - R) : pts(i, 1) = +hH : i += 1
        ' Top edge mid
        pts(i, 0) = 0 : pts(i, 1) = +hH : i += 1
        ' Top-left arc start
        pts(i, 0) = -(hW - R) : pts(i, 1) = +hH : i += 1
        ' Top-left arc mid
        pts(i, 0) = -(hW - R) - R * cos45 : pts(i, 1) = +(hH - R) + R * sin45 : i += 1
        ' Top-left arc end
        pts(i, 0) = -hW : pts(i, 1) = +(hH - R) : i += 1
        ' Left edge mid
        pts(i, 0) = -hW : pts(i, 1) = 0 : i += 1
        ' Bot-left arc start
        pts(i, 0) = -hW : pts(i, 1) = -(hH - R) : i += 1
        ' Bot-left arc mid
        pts(i, 0) = -(hW - R) - R * cos45 : pts(i, 1) = -(hH - R) - R * sin45 : i += 1
        ' Bot-left arc end
        pts(i, 0) = -(hW - R) : pts(i, 1) = -hH : i += 1
        ' Bot edge mid
        pts(i, 0) = 0 : pts(i, 1) = -hH : i += 1
        ' Bot-right arc start
        pts(i, 0) = +(hW - R) : pts(i, 1) = -hH : i += 1
        ' Bot-right arc mid
        pts(i, 0) = +(hW - R) + R * cos45 : pts(i, 1) = -(hH - R) - R * sin45 : i += 1
        ' Bot-right arc end
        pts(i, 0) = +hW : pts(i, 1) = -(hH - R) : i += 1

        For j As Integer = 0 To 15

            Dim y As Double = Yc + pts(j, 0)
            Dim z As Double = Zc + pts(j, 1)
            oSpln.AddPoint(Ref(oHSF.AddNewPointCoord(Xs, y, z)))

        Next

        Return oSpln

    End Function

#End Region

#Region "Utility helpers"

    ' Wrap any GSD object in a CATIA Reference.
    Private Function Ref(obj As Object) As Object
        Return oPart.CreateReferenceFromObject(obj)
    End Function

    ' Get an existing HybridBody by name, or create + name a new one.
    Private Function GetOrAddHB(name As String) As Object

        Try
            Dim hb As Object = oHBodies.Item(name)
            If hb IsNot Nothing Then Return hb
        Catch
        End Try

        Dim newHb As Object = oHBodies.Add()
        newHb.Name = name
        Return newHb

    End Function

    Private Sub Log(msg As String)

        If _log IsNot Nothing Then
            _log.Invoke(msg)
        Else
            System.Diagnostics.Debug.WriteLine(msg)
        End If

    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

#End Region

End Class
