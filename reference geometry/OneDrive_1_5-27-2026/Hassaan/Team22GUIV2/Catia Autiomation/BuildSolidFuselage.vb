Option Strict Off
Option Explicit On

Imports System.Runtime.InteropServices
Imports MECMOD
Imports PARTITF
Imports KnowledgewareTypeLib
Imports HybridShapeTypeLib
Imports ProductStructureTypeLib
Imports System.Collections.Generic

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
    Public Property BulkheadWidth As Double = 200.0
    Public Property BulkheadThickness As Double = 20.0
    Public Property BulkheadFrameThickness As Double = 10.0
    Public Property BulkheadPositions As Double() = {150.0, 450.0, 850.0, 1200.0}

    ' === LONGERON PARAMETERS ===
    Public Property LongeronDiameter As Double = 2.0
    Public Property LongeronOffset As Double = 65.0

    ' === TAILBOOM PARAMETERS ===
    Public Property BoomRadius As Double = 22.5
    Public Property BoomLength As Double = 800.0

    ' === NOSE LANDING GEAR PARAMETERS ===
    Public Property NoseStrutLength As Double = 250.0
    Public Property NoseWheelRadius As Double = 25.0
    Public Property NoseTireThickness As Double = 8.0

    ' === MAIN LANDING GEAR PARAMETERS ===
    Public Property MainStrutLength As Double = 320.0
    Public Property MainWheelRadius As Double = 30.0
    Public Property MainTireThickness As Double = 10.0

    ' === AERO NOSE PARAMETERS ===
    Public Property AeroNoseLength As Double = 250.0

    ' === TAIL CONE PARAMETERS ===
    Public Property TailLength As Double = 200.0

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

            Log("Stage 3: Building longerons (X=0 to max bulkhead)...")
            BuildLongerons()

            Log("Stage 4: Building Aero Nose (Revolved Ogive Profile)...")
            BuildAeroNoseEgg()

            Log("Stage 5: Building tail cone (Pad Extrusion)...")
            BuildTailBoom()

            Log("Stage 6: Building segmented fuselage skin...")
            BuildFuselageSkin()

            Log("Stage 7: Building nose landing gear (2nd bulkhead)...")
            BuildNoseLandingGear()

            Log("Stage 8: Building main landing gear (4th bulkhead)...")
            BuildMainLandingGear()

            Try : oPart.Update() : Catch : End Try
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

        ' Bulkhead 1 (At X=0) - Hollow
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

        ' Remaining Bulkheads
        For idx As Integer = 0 To UBound(BulkheadPositions)
            Dim xOff As Double = BulkheadPositions(idx)
            Dim oOff As Object = oHSF.AddNewPlaneOffset(oYZRef, xOff, False)
            oOff.Name = "Plane_" & CInt(xOff)
            oHBody.AppendHybridShape(oOff)
            oPart.Update()

            Dim oNewBody As Object = oBodies.Add()
            oNewBody.Name = "Body_Bulkhead_" & CInt(xOff)

            Dim oOffRef As Object = oPart.CreateReferenceFromObject(oOff)
            Dim oSk As Object = oNewBody.Sketches.Add(oOffRef)

            oSk.OpenEdition()
            Dim f As Object = oSk.Factory2D

            ' Always draw the outer boundary
            f.CreateClosedCircle(0, 0, outerR)

            ' CRITICAL FIX: Only cut the inner circle if it's NOT the last bulkhead.
            ' This leaves the 5th bulkhead completely solid for the tail boom.
            If idx < UBound(BulkheadPositions) Then
                f.CreateClosedCircle(0, 0, outerR - BulkheadFrameThickness)
            End If

            oSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oSk, BulkheadThickness).Name = "Bulkhead_" & CInt(xOff)
            oPart.Update()
        Next
    End Sub

    Private Sub BuildLongerons()
        Dim oOE As Object = oPart.OriginElements
        Dim oYZPlane As Object = oOE.PlaneYZ
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

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
    '  AERO NOSE -- Smooth conical nose using lofted approach
    ' =========================================================================
    Private Sub BuildAeroNoseEgg()
        Dim L As Double = AeroNoseLength          ' Uses Public Property
        Dim R As Double = BulkheadWidth / 2.0     ' Uses Public Property

        Log("   [AERO NOSE] Smooth cone: L=" & CInt(L) & "mm  R=" & CInt(R) & "mm")

        Try
            Dim oOE As Object = oPart.OriginElements
            Dim oYZPlane As Object = oOE.PlaneYZ
            Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

            Dim oNoseGeom As Object = oPart.HybridBodies.Add()
            oNoseGeom.Name = "NoseGeometry"

            ' Create nose with 40 tapered sections for ultra-smooth blended profile
            Dim numSections As Integer = 40
            Dim segmentLength As Double = L / CDbl(numSections)

            For section As Integer = 0 To numSections - 1
                Dim currentX As Double = -L + (section * segmentLength)

                ' Ultra-smooth sine curve taper for seamless cone
                Dim t As Double = (section + 1.0) / CDbl(numSections)
                Dim currentRadius As Double = R * Math.Sin(t * Math.PI / 2.0)

                Dim oPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, currentX, False)
                oPlane.Name = "Plane_Nose_" & section
                oNoseGeom.AppendHybridShape(oPlane)
                oPart.Update()

                Dim oBody As Object = oBodies.Add()
                oBody.Name = "Body_Nose_Sec_" & section

                Dim oPlaneRef As Object = oPart.CreateReferenceFromObject(oPlane)
                Dim oSk As Object = oBody.Sketches.Add(oPlaneRef)
                oSk.OpenEdition()
                oSk.Factory2D.CreateClosedCircle(0.0, 0.0, currentRadius)
                oSk.CloseEdition()
                oPart.Update()

                oSF.AddNewPad(oSk, segmentLength).Name = "Nose_Sec_" & section
                oPart.Update()
            Next

            Log("      [OK] Smooth conical nose created successfully.")

        Catch ex As Exception
            Log("   [ERROR] Aero nose: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildTailBoom()
        Dim tailStartX As Double = BulkheadPositions(UBound(BulkheadPositions)) + BulkheadThickness

        ' Swapped local variables to use the properties passed by the GUI
        Dim activeBoomRadius As Double = BoomRadius
        Dim activeBoomLength As Double = BoomLength

        Dim oOE As Object = oPart.OriginElements
        Dim oYZPlane As Object = oOE.PlaneYZ
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

        Dim oTailPlanes As Object = oPart.HybridBodies.Add()
        oTailPlanes.Name = "Tail_Planes"

        Dim oPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, tailStartX, False)
        oPlane.Name = "Plane_TailBoomStart"
        oTailPlanes.AppendHybridShape(oPlane)
        oPart.Update()

        Dim oPlaneRef As Object = oPart.CreateReferenceFromObject(oPlane)

        Dim oBody As Object = oBodies.Add()
        oBody.Name = "Body_TailBoom_Pad"

        Dim oSk As Object = oBody.Sketches.Add(oPlaneRef)
        oSk.OpenEdition()
        oSk.Factory2D.CreateClosedCircle(0.0, 0.0, activeBoomRadius)
        oSk.CloseEdition()
        oPart.Update()

        oSF.AddNewPad(oSk, activeBoomLength).Name = "TailBoom_Extrusion"
        oPart.Update()

        Log("      [OK] Tail boom created successfully.")
    End Sub

    Private Sub BuildFuselageSkin()
        Dim oOE As Object = oPart.OriginElements
        Dim oYZPlane As Object = oOE.PlaneYZ
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

        Dim oSkinPlanesBody As Object = oPart.HybridBodies.Add()
        oSkinPlanesBody.Name = "Skin_Segment_Planes"

        ' Re-integrated dynamic list calculation so it responds to GUI parameters
        Dim stations As New List(Of Double)
        stations.Add(0.0)
        stations.AddRange(BulkheadPositions)
        stations.Add(BulkheadPositions(BulkheadPositions.Length - 1) + BulkheadThickness)

        For i As Integer = 0 To stations.Count - 2
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

            Dim oPlaneRef As Object = oPart.CreateReferenceFromObject(oPlane)
            Dim oSk As Object = oBody.Sketches.Add(oPlaneRef)

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

    Private Sub BuildNoseLandingGear()
        Try
            Dim noseX As Double = BulkheadPositions(1)

            ' Properties bound to GUI Form
            Dim strutLength As Double = NoseStrutLength
            Dim wheelRadius As Double = NoseWheelRadius
            Dim tireThickness As Double = NoseTireThickness
            Dim strutRadius As Double = 6.0
            Dim bulkheadRadius As Double = BulkheadWidth / 2.0
            Dim strutStartZ As Double = -bulkheadRadius
            Dim bottomZ As Double = strutStartZ - strutLength

            Dim oOE As Object = oPart.OriginElements
            Dim oXYPlane As Object = oOE.PlaneXY
            Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)
            Dim oYZPlane As Object = oOE.PlaneYZ
            Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

            Dim oNoseGeom As Object = oPart.HybridBodies.Add()
            oNoseGeom.Name = "NoseGear_Geometry"

            ' ---- Strut ----
            Dim oStrutPlane As Object = oHSF.AddNewPlaneOffset(oXYRef, bottomZ, False)
            oStrutPlane.Name = "Plane_NoseGear_Strut"
            oNoseGeom.AppendHybridShape(oStrutPlane)
            oPart.Update()

            Dim oStrutPlaneRef As Object = oPart.CreateReferenceFromObject(oStrutPlane)

            Dim oStrutBody As Object = oBodies.Add()
            oStrutBody.Name = "Body_NoseGear_Strut"
            Dim oStrutSk As Object = oStrutBody.Sketches.Add(oStrutPlaneRef)
            oStrutSk.OpenEdition()
            oStrutSk.Factory2D.CreateClosedCircle(noseX, 0.0, strutRadius)
            oStrutSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oStrutSk, strutLength).Name = "NoseGear_Strut"
            oPart.Update()

            ' ---- Wheel / tyre (on XZ plane at Y=0) ----
            Dim oWheelPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, noseX, False)
            oWheelPlane.Name = "Plane_NoseGear_Wheel_XZ"
            oNoseGeom.AppendHybridShape(oWheelPlane)
            oPart.Update()

            Dim oWheelPlaneRef As Object = oPart.CreateReferenceFromObject(oWheelPlane)
            Dim oWheelBody As Object = oBodies.Add()
            oWheelBody.Name = "Body_NoseGear_Wheel"
            Dim oWheelSk As Object = oWheelBody.Sketches.Add(oWheelPlaneRef)
            oWheelSk.OpenEdition()
            oWheelSk.Factory2D.CreateClosedCircle(0.0, bottomZ, wheelRadius + tireThickness)
            oWheelSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oWheelSk, tireThickness * 3.0).Name = "NoseGear_Wheel_Tire"
            oPart.Update()

            Log("      [OK] Nose landing gear at X=" & CInt(noseX) & "mm  " &
                "strut " & CInt(strutStartZ) & " to " & CInt(bottomZ) & "mm")

        Catch ex As Exception
            Log("   [ERROR] Nose gear: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildMainLandingGear()
        Try
            Dim mainX As Double = BulkheadPositions(3)

            ' Properties bound to GUI Form
            Dim strutLength As Double = MainStrutLength
            Dim wheelRadius As Double = MainWheelRadius
            Dim tireThickness As Double = MainTireThickness
            Dim strutRadius As Double = 8.0
            Dim wheelOffset As Double = -90.0
            Dim bulkheadRadius As Double = BulkheadWidth / 2.0
            Dim strutStartZ As Double = -bulkheadRadius + 70.0
            Dim bottomZ As Double = strutStartZ - strutLength

            Dim oOE As Object = oPart.OriginElements
            Dim oXYPlane As Object = oOE.PlaneXY
            Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)
            Dim oYZPlane As Object = oOE.PlaneYZ
            Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

            Dim oMainGeom As Object = oPart.HybridBodies.Add()
            oMainGeom.Name = "MainGear_Geometry"

            ' ---- Struts ----
            Dim oStrutPlane As Object = oHSF.AddNewPlaneOffset(oXYRef, bottomZ, False)
            oStrutPlane.Name = "Plane_MainGear_Strut"
            oMainGeom.AppendHybridShape(oStrutPlane)
            oPart.Update()

            Dim oStrutPlaneRef As Object = oPart.CreateReferenceFromObject(oStrutPlane)

            Dim oLeftStrutBody As Object = oBodies.Add()
            oLeftStrutBody.Name = "Body_MainGear_Left_Strut"
            Dim oLeftStrutSk As Object = oLeftStrutBody.Sketches.Add(oStrutPlaneRef)
            oLeftStrutSk.OpenEdition()
            oLeftStrutSk.Factory2D.CreateClosedCircle(mainX, -wheelOffset, strutRadius)
            oLeftStrutSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oLeftStrutSk, strutLength).Name = "MainGear_Left_Strut"
            oPart.Update()

            Dim oRightStrutBody As Object = oBodies.Add()
            oRightStrutBody.Name = "Body_MainGear_Right_Strut"
            Dim oRightStrutSk As Object = oRightStrutBody.Sketches.Add(oStrutPlaneRef)
            oRightStrutSk.OpenEdition()
            oRightStrutSk.Factory2D.CreateClosedCircle(mainX, wheelOffset, strutRadius)
            oRightStrutSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oRightStrutSk, strutLength).Name = "MainGear_Right_Strut"
            oPart.Update()

            ' ---- Wheels ----
            Dim oLeftWheelPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, mainX, False)
            oLeftWheelPlane.Name = "Plane_MainGear_Left_Wheel"
            oMainGeom.AppendHybridShape(oLeftWheelPlane)
            oPart.Update()

            Dim oLeftWheelPlaneRef As Object = oPart.CreateReferenceFromObject(oLeftWheelPlane)
            Dim oLeftWheelBody As Object = oBodies.Add()
            oLeftWheelBody.Name = "Body_MainGear_Left_Wheel"
            Dim oLeftWheelSk As Object = oLeftWheelBody.Sketches.Add(oLeftWheelPlaneRef)
            oLeftWheelSk.OpenEdition()
            oLeftWheelSk.Factory2D.CreateClosedCircle(-wheelOffset, bottomZ, wheelRadius + tireThickness)
            oLeftWheelSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oLeftWheelSk, tireThickness * 2.0).Name = "MainGear_Left_Wheel_Tire"
            oPart.Update()

            Dim oRightWheelPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, mainX, False)
            oRightWheelPlane.Name = "Plane_MainGear_Right_Wheel"
            oMainGeom.AppendHybridShape(oRightWheelPlane)
            oPart.Update()

            Dim oRightWheelPlaneRef As Object = oPart.CreateReferenceFromObject(oRightWheelPlane)
            Dim oRightWheelBody As Object = oBodies.Add()
            oRightWheelBody.Name = "Body_MainGear_Right_Wheel"
            Dim oRightWheelSk As Object = oRightWheelBody.Sketches.Add(oRightWheelPlaneRef)
            oRightWheelSk.OpenEdition()
            oRightWheelSk.Factory2D.CreateClosedCircle(wheelOffset, bottomZ, wheelRadius + tireThickness)
            oRightWheelSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oRightWheelSk, tireThickness * 2.0).Name = "MainGear_Right_Wheel_Tire"
            oPart.Update()

            Log("      [OK] Main landing gear at X=" & CInt(mainX) & "mm  " &
                "strut " & CInt(strutStartZ) & " to " & CInt(bottomZ) & "mm")

        Catch ex As Exception
            Log("   [ERROR] Main gear: " & ex.Message)
        End Try
    End Sub

    Private Sub Log(msg As String)
        If _log IsNot Nothing Then _log.Invoke(msg) Else System.Diagnostics.Debug.WriteLine(msg)
    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

End Class