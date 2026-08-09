Option Strict Off
Option Explicit On

Imports System.Runtime.InteropServices
Imports MECMOD
Imports PARTITF
Imports KnowledgewareTypeLib
Imports HybridShapeTypeLib
Imports ProductStructureTypeLib

Public Class BuildSolidFuselage

#Region "Fields"

    Private oCATIA As Object
    Private oDoc As Object
    Private oPart As Object
    Private oSF As Object
    Private oHSF As Object
    Private oBodies As Object
    Private _log As Action(Of String)

    ' === BULKHEAD PARAMETERS ===
    Private ReadOnly BulkheadWidth As Double = 200.0
    Private ReadOnly BulkheadThickness As Double = 20.0
    Private ReadOnly BulkheadFrameThickness As Double = 10.0
    Private ReadOnly BulkheadPositions() As Double = {150.0, 450.0, 850.0, 1200.0}

    ' === LONGERON PARAMETERS ===
    Private ReadOnly LongeronDiameter As Double = 2.0
    Private ReadOnly LongeronOffset As Double = 65.0

    ' === AERO NOSE PARAMETERS ===
    Private ReadOnly AeroNoseLength As Double = 250.0 ' Length of the egg shape

    ' === TAIL CONE PARAMETERS ===
    Private ReadOnly TailLength As Double = 200.0

    ' === FUSELAGE SKIN PARAMETERS (DYNAMIC) ===
    Public Property SegmentThicknesses As Double() = {5.0, 4.0, 3.0, 2.0}

#End Region

    Public Sub Run(Optional logger As Action(Of String) = Nothing)
        _log = If(logger, New Action(Of String)(AddressOf DebugLog))
        Try
            Log("== Findus Aero UAV : COMPLETE FUSELAGE BUILD ==")
            ConnectAndCreatePart()

            Log("Stage 2: Building frame structure (5 bulkheads)...")
            BuildSolidWithSketch()

            Log("Stage 3: Building longerons (X=0 to X=1220mm)...")
            BuildLongerons()

            Log("Stage 4: Building Aero Nose (Revolved Egg Profile)...")
            BuildAeroNoseEgg()

            Log("Stage 5: Building tail cone (Pad Extrusion)...")
            BuildTailCone()

            Log("Stage 6: Building segmented fuselage skin...")
            BuildFuselageSkin()

            Try : oPart.Update() : Catch : End Try   ' Final refresh
            Log("== BUILD COMPLETE ==")

        Catch ex As Exception
            Log("[FATAL] " & ex.Message)
            Throw
        End Try
    End Sub

    Private Sub ConnectAndCreatePart()
        Try
            oCATIA = GetObject("", "CATIA.Application")
        Catch
            Throw New Exception("CATIA is not running.")
        End Try
        oCATIA.Visible = True
        oDoc = oCATIA.Documents.Add("Part")
        oPart = oDoc.Part
        Try : oPart.Name = "Findus_AeroUAV_Fuselage" : Catch : End Try
        oSF = oPart.ShapeFactory
        oHSF = oPart.HybridShapeFactory
        oBodies = oPart.Bodies
    End Sub

    Private Sub BuildSolidWithSketch()
        Dim oPartBody As Object = oBodies.Item("PartBody")
        Dim oOE As Object = oPart.OriginElements
        Dim oPlaneRef As Object = oOE.PlaneYZ

        Dim oSkMain As Object = oPartBody.Sketches.Add(oPlaneRef)
        oSkMain.OpenEdition()
        Dim fMain As Object = oSkMain.Factory2D
        Dim outerR As Double = BulkheadWidth / 2.0
        fMain.CreateClosedCircle(0, 0, outerR)
        fMain.CreateClosedCircle(0, 0, outerR - BulkheadFrameThickness)
        oSkMain.CloseEdition()
        oPart.Update()
        oSF.AddNewPad(oSkMain, BulkheadThickness).Name = "Bulkhead_Frame"
        oPart.Update()

        Dim oHBodies As Object = oPart.HybridBodies
        Dim oHBody As Object = oHBodies.Add()
        oHBody.Name = "Offset_Planes"
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oPlaneRef)

        For idx As Integer = 0 To UBound(BulkheadPositions)
            Dim xOff As Double = BulkheadPositions(idx)
            Dim oOff As Object = oHSF.AddNewPlaneOffset(oYZRef, xOff, False)
            oOff.Name = "Plane_" & CInt(xOff)
            oHBody.AppendHybridShape(oOff)
            oPart.Update()

            Dim oNewBody As Object = oBodies.Add()
            oNewBody.Name = "Body_Bulkhead_" & CInt(xOff)
            Dim oSk As Object = oNewBody.Sketches.Add(oPart.CreateReferenceFromObject(oOff))
            oSk.OpenEdition()
            Dim f As Object = oSk.Factory2D
            f.CreateClosedCircle(0, 0, outerR)
            f.CreateClosedCircle(0, 0, outerR - BulkheadFrameThickness)
            oSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oSk, BulkheadThickness).Name = "Bulkhead_" & CInt(xOff)
            oPart.Update()
        Next
    End Sub

    Private Sub BuildLongerons()
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oPart.OriginElements.PlaneYZ)
        Dim corners() As Double = {LongeronOffset, -LongeronOffset}
        Dim longeronLen As Double = BulkheadPositions(UBound(BulkheadPositions)) + BulkheadThickness

        For yIdx As Integer = 0 To 1
            For zIdx As Integer = 0 To 1
                Dim yPos As Double = corners(yIdx)
                Dim zPos As Double = corners(zIdx)
                Dim lName As String = "Longeron_Y" & CInt(yPos) & "_Z" & CInt(zPos)

                Dim oBody As Object = oBodies.Add()
                oBody.Name = "Body_" & lName
                Dim oSk As Object = oBody.Sketches.Add(oYZRef)
                oSk.OpenEdition()
                oSk.Factory2D.CreateClosedCircle(yPos, zPos, LongeronDiameter / 2.0)
                oSk.CloseEdition()
                oPart.Update()
                oSF.AddNewPad(oSk, longeronLen).Name = lName
                oPart.Update()
            Next
        Next
    End Sub

    ' =========================================================================
    '  AERO NOSE  — Revolved Smooth Spline Profile (Egg Shape)
    ' =========================================================================

    Private Sub BuildAeroNoseEgg()
        Dim baseRadius As Double = BulkheadWidth / 2.0
        Log("   [AERO NOSE] Revolving smooth solid egg profile...")

        Try
            Dim oBody As Object = oBodies.Add()
            oBody.Name = "Body_AeroNose_Solid"

            Dim oOE As Object = oPart.OriginElements
            Dim oXYPlane As Object = oOE.PlaneXY
            Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)

            Dim oSk As Object = oBody.Sketches.Add(oXYRef)
            oSk.Name = "Sketch_AeroNoseProfile"
            oSk.OpenEdition()
            Dim f As Object = oSk.Factory2D

            ' 1. Create a 4-point smooth spline to mimic the egg curve
            Dim pts(7) As Object
            pts(0) = 0.0 : pts(1) = baseRadius ' Start at the top edge of the bulkhead
            pts(2) = -AeroNoseLength * 0.4 : pts(3) = baseRadius * 0.9 ' Slight bulge/taper
            pts(4) = -AeroNoseLength * 0.8 : pts(5) = baseRadius * 0.45 ' Steeper curve towards nose
            pts(6) = -AeroNoseLength : pts(7) = 0.0 ' Tip of the egg on the center axis

            Dim oSpline As Object = f.CreateSpline(pts)

            ' 2. Close the profile with straight lines
            f.CreateLine(-AeroNoseLength, 0.0, 0.0, 0.0) ' Center axis line
            f.CreateLine(0.0, 0.0, 0.0, baseRadius) ' Vertical base line

            oSk.CloseEdition()
            oPart.Update()

            ' 3. Extract the absolute horizontal axis to use as the revolution center
            Dim oGeo As Object = oSk.GeometricElements
            Dim oAbsAx As Object = oGeo.Item("AbsoluteAxis")
            Dim oHDir As Object = oAbsAx.GetItem("HDirection")
            Dim oHDirRef As Object = oPart.CreateReferenceFromObject(oHDir)

            ' 4. Revolve the closed profile 360 degrees
            Dim oShaft As Object = oSF.AddNewShaft(oSk)
            oShaft.Name = "AeroNose_Egg_Revolve"
            oShaft.RevoluteAxis = oHDirRef

            oPart.Update()
            Log("      [✓] Solid aero egg created successfully.")

        Catch ex As Exception
            Log("   [ERROR] Aero egg: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildTailCone()
        Dim tailStartX As Double = BulkheadPositions(UBound(BulkheadPositions)) + BulkheadThickness
        Dim baseRadius As Double = BulkheadWidth / 2.0
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oPart.OriginElements.PlaneYZ)

        Dim oBody As Object = oBodies.Add()
        oBody.Name = "Body_TailCone_Pad"

        Dim oTailPlanes As Object = oPart.HybridBodies.Add()
        oTailPlanes.Name = "Tail_Planes"

        Dim oPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, tailStartX, False)
        oPlane.Name = "Plane_TailStart"
        oTailPlanes.AppendHybridShape(oPlane)
        oPart.Update()

        Dim oSk As Object = oBody.Sketches.Add(oPart.CreateReferenceFromObject(oPlane))
        oSk.OpenEdition()
        oSk.Factory2D.CreateClosedCircle(0.0, 0.0, baseRadius)
        oSk.CloseEdition()
        oPart.Update()

        oSF.AddNewPad(oSk, TailLength).Name = "Tail_Extrusion"
        oPart.Update()
    End Sub

    Private Sub BuildFuselageSkin()
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oPart.OriginElements.PlaneYZ)
        Dim oSkinPlanesBody As Object = oPart.HybridBodies.Add()
        oSkinPlanesBody.Name = "Skin_Segment_Planes"

        Dim stations() As Double = {0.0, 150.0, 450.0, 850.0, 1200.0 + BulkheadThickness}

        For i As Integer = 0 To stations.Length - 2
            Dim startX As Double = stations(i)
            Dim endX As Double = stations(i + 1)
            Dim segmentLength As Double = endX - startX
            Dim currentThickness As Double = If(i < SegmentThicknesses.Length, SegmentThicknesses(i), 2.0)

            Dim outerR As Double = BulkheadWidth / 2.0
            Dim innerR As Double = outerR - currentThickness

            Dim oBody As Object = oBodies.Add()
            oBody.Name = "Body_Fuselage_Skin_Seg_" & i + 1

            Dim oPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, startX, False)
            oPlane.Name = "Plane_SkinSeg_" & i + 1
            oSkinPlanesBody.AppendHybridShape(oPlane)
            oPart.Update()

            Dim oSk As Object = oBody.Sketches.Add(oPart.CreateReferenceFromObject(oPlane))
            oSk.OpenEdition()
            Dim f As Object = oSk.Factory2D
            f.CreateClosedCircle(0.0, 0.0, outerR)
            f.CreateClosedCircle(0.0, 0.0, innerR)
            oSk.CloseEdition()
            oPart.Update()

            oSF.AddNewPad(oSk, segmentLength).Name = "Fuselage_Skin_Seg_" & i + 1
            oPart.Update()
        Next
    End Sub

    Private Sub Log(msg As String)
        If _log IsNot Nothing Then _log.Invoke(msg) Else System.Diagnostics.Debug.WriteLine(msg)
    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

End Class