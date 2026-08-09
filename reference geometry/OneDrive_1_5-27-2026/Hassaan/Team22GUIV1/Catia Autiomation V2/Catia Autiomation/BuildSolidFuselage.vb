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

    ' === AERO NOSE & SKIN PARAMETERS ===
    Public Property AeroNoseLength As Double = 250.0
    Public Property TailLength As Double = 200.0
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
            BuildTailBoom()

            Log("Stage 6: Building segmented fuselage skin...")
            BuildFuselageSkin(BulkheadPositions)

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

            ' Only cut the inner circle if it's NOT the last bulkhead.
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

    Private Sub BuildAeroNoseEgg()
        Dim baseRadius As Double = BulkheadWidth / 2.0
        Log("   [AERO NOSE] Building Revolved Nose from Bulkhead 1 (X=0)...")

        Try
            Dim oBody As Object = oBodies.Add()
            oBody.Name = "Body_AeroNose_Solid"
            oPart.InWorkObject = oBody

            Dim oOE As Object = oPart.OriginElements
            Dim oXYPlane As Object = oOE.PlaneXY
            Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)

            Dim oSk As Object = oBody.Sketches.Add(oXYRef)
            oSk.Name = "Sketch_AeroNoseProfile"
            oSk.OpenEdition()
            Dim f As Object = oSk.Factory2D

            Dim numSegments As Integer = 30
            Dim L As Double = AeroNoseLength
            Dim R As Double = baseRadius
            Dim prevX As Double = -L
            Dim prevY As Double = 0.0

            ' 1. Draw the top aerodynamic arc
            For i As Integer = 1 To numSegments
                Dim t As Double = Convert.ToDouble(i) / Convert.ToDouble(numSegments)
                Dim theta As Double = (Math.PI / 2.0) * t
                Dim currX As Double = -L * Math.Cos(theta)
                Dim currY As Double = R * Math.Sin(theta)

                If i = numSegments Then
                    currX = 0.0
                    currY = R
                End If

                f.CreateLine(prevX, prevY, currX, currY)
                prevX = currX
                prevY = currY
            Next

            ' 2. Draw the vertical line matching the bulkhead face
            f.CreateLine(0.0, R, 0.0, 0.0)

            ' 3. Draw the bottom return line and explicitly set IT as the centerline.
            Dim axisLine As Object = f.CreateLine(0.0, 0.0, -L, 0.0)
            oSk.CenterLine = axisLine

            oSk.CloseEdition()
            oPart.Update()

            ' 4. Execute the Solid Shaft natively
            Dim oShaft As Object = oSF.AddNewShaft(oSk)
            oShaft.Name = "AeroNose_Shaft"
            oPart.Update()

            Log("      [✓] Aerodynamic nose created successfully and added to tree.")

        Catch ex As Exception
            Log("   [ERROR] Aero nose: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildTailBoom()
        Dim tailStartX As Double = BulkheadPositions(UBound(BulkheadPositions)) + BulkheadThickness

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
        oSk.Factory2D.CreateClosedCircle(0.0, 0.0, BoomRadius)
        oSk.CloseEdition()
        oPart.Update()

        oSF.AddNewPad(oSk, BoomLength).Name = "TailBoom_Extrusion"
        oPart.Update()

        Log("      [✓] Tail boom created successfully.")
    End Sub

    ' Update the method definition
    Private Sub BuildFuselageSkin(positions As Double())
        Dim oOE As Object = oPart.OriginElements
        Dim oYZPlane As Object = oOE.PlaneYZ
        Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

        Dim oSkinPlanesBody As Object = oPart.HybridBodies.Add()
        oSkinPlanesBody.Name = "Skin_Segment_Planes"

        ' Combine Start (0) + Bulkhead Positions + Final Tail end
        Dim stations As New List(Of Double)
        stations.Add(0.0)
        stations.AddRange(positions)
        stations.Add(positions.Last() + BulkheadThickness)

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
    Private Sub BuildSkinCutouts(positions As Double())
        Log("Stage 6b: Adding weight-reduction cutouts...")

        ' 1. Create a dedicated body for the cuts
        Dim oCutBody As Object = oBodies.Add()
        oCutBody.Name = "Body_Fuselage_Cutouts"

        ' 2. Define the plane for the cut (e.g., a longitudinal plane or tangent plane)
        ' For simplicity, we use the XY plane offset to the top of the cylinder
        Dim oOE As Object = oPart.OriginElements
        Dim oXYPlane As Object = oOE.PlaneXY
        Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)

        ' Offset to the top skin surface
        Dim oCutPlane As Object = oHSF.AddNewPlaneOffset(oXYRef, (BulkheadWidth / 2.0) - 2.0, False)
        oPart.HybridBodies.Add().AppendHybridShape(oCutPlane)
        oPart.Update()

        ' 3. Create the sketch for the cutouts
        Dim oSk As Object = oCutBody.Sketches.Add(oPart.CreateReferenceFromObject(oCutPlane))
        oSk.OpenEdition()
        Dim f As Object = oSk.Factory2D

        ' --- Draw your pattern here ---
        ' Example: A simple triangular pattern based on your image
        Dim startX As Double = 200, startY As Double = -20
        Dim size As Double = 50

        ' Triangle 1
        f.CreateLine(startX, startY, startX + size, startY)
        f.CreateLine(startX + size, startY, startX, startY + size)
        f.CreateLine(startX, startY + size, startX, startY)

        ' Triangle 2 (Diagonal mirror)
        f.CreateLine(startX + 10, startY + 10, startX + size + 10, startY + 10)
        f.CreateLine(startX + size + 10, startY + 10, startX + 10, startY + size + 10)
        f.CreateLine(startX + 10, startY + size + 10, startX + 10, startY + 10)

        oSk.CloseEdition()
        oPart.Update()

        ' 4. Perform the Pocket
        Dim oPocket As Object = oSF.AddNewPocket(oSk, 20.0) ' Depth 20mm
        oPocket.Direction.Compute()
        oPart.Update()

        Log("   [✓] Cutouts applied successfully.")
    End Sub
    Private Sub BuildNoseLandingGear()
        Try
            Dim noseX As Double = BulkheadPositions(1)
            Dim strutRadius As Double = 6.0
            Dim bulkheadRadius As Double = BulkheadWidth / 2.0
            Dim strutStartZ As Double = -bulkheadRadius - 0.0
            Dim bottomZ As Double = strutStartZ - NoseStrutLength

            Dim oOE As Object = oPart.OriginElements
            Dim oXYPlane As Object = oOE.PlaneXY
            Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)

            Dim oNoseGeom As Object = oPart.HybridBodies.Add()
            oNoseGeom.Name = "NoseGear_Geometry"

            Dim oStrutPlane As Object = oHSF.AddNewPlaneOffset(oXYRef, strutStartZ, False)
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
            oSF.AddNewPad(oStrutSk, -NoseStrutLength).Name = "NoseGear_Strut"
            oPart.Update()

            Dim oYZPlane As Object = oOE.PlaneYZ
            Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)
            Dim oWheelPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, noseX, False)
            oWheelPlane.Name = "Plane_NoseGear_Wheel"
            oNoseGeom.AppendHybridShape(oWheelPlane)
            oPart.Update()

            Dim oWheelPlaneRef As Object = oPart.CreateReferenceFromObject(oWheelPlane)
            Dim oWheelBody As Object = oBodies.Add()
            oWheelBody.Name = "Body_NoseGear_Wheel"
            Dim oWheelSk As Object = oWheelBody.Sketches.Add(oWheelPlaneRef)
            oWheelSk.OpenEdition()
            oWheelSk.Factory2D.CreateClosedCircle(0.0, bottomZ, NoseWheelRadius + NoseTireThickness)
            oWheelSk.Factory2D.CreateClosedCircle(0.0, bottomZ, NoseWheelRadius)
            oWheelSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oWheelSk, NoseTireThickness).Name = "NoseGear_Wheel_Tire"
            oPart.Update()

            Log("      [OK] Nose landing gear connected to bulkhead at X=" & CInt(noseX) & "mm")

        Catch ex As Exception
            Log("   [ERROR] Nose gear: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildMainLandingGear()
        Try
            Dim mainX As Double = BulkheadPositions(3)
            Dim strutRadius As Double = 8.0
            Dim wheelOffset As Double = -90.0
            Dim bulkheadRadius As Double = BulkheadWidth / 2.0
            Dim strutStartZ As Double = -bulkheadRadius + 70.0
            Dim bottomZ As Double = strutStartZ - MainStrutLength

            Dim oOE As Object = oPart.OriginElements
            Dim oXYPlane As Object = oOE.PlaneXY
            Dim oXYRef As Object = oPart.CreateReferenceFromObject(oXYPlane)

            Dim oMainGeom As Object = oPart.HybridBodies.Add()
            oMainGeom.Name = "MainGear_Geometry"

            Dim oStrutPlane As Object = oHSF.AddNewPlaneOffset(oXYRef, strutStartZ, False)
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
            oSF.AddNewPad(oLeftStrutSk, -MainStrutLength).Name = "MainGear_Left_Strut"
            oPart.Update()

            Dim oRightStrutBody As Object = oBodies.Add()
            oRightStrutBody.Name = "Body_MainGear_Right_Strut"
            Dim oRightStrutSk As Object = oRightStrutBody.Sketches.Add(oStrutPlaneRef)
            oRightStrutSk.OpenEdition()
            oRightStrutSk.Factory2D.CreateClosedCircle(mainX, wheelOffset, strutRadius)
            oRightStrutSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oRightStrutSk, -MainStrutLength).Name = "MainGear_Right_Strut"
            oPart.Update()

            Dim oYZPlane As Object = oOE.PlaneYZ
            Dim oYZRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

            Dim oLeftWheelPlane As Object = oHSF.AddNewPlaneOffset(oYZRef, mainX, False)
            oLeftWheelPlane.Name = "Plane_MainGear_Left_Wheel"
            oMainGeom.AppendHybridShape(oLeftWheelPlane)
            oPart.Update()
            Dim oLeftWheelPlaneRef As Object = oPart.CreateReferenceFromObject(oLeftWheelPlane)

            Dim oLeftWheelBody As Object = oBodies.Add()
            oLeftWheelBody.Name = "Body_MainGear_Left_Wheel"
            Dim oLeftWheelSk As Object = oLeftWheelBody.Sketches.Add(oLeftWheelPlaneRef)
            oLeftWheelSk.OpenEdition()
            oLeftWheelSk.Factory2D.CreateClosedCircle(-wheelOffset, bottomZ, MainWheelRadius + MainTireThickness)
            oLeftWheelSk.Factory2D.CreateClosedCircle(-wheelOffset, bottomZ, MainWheelRadius)
            oLeftWheelSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oLeftWheelSk, MainTireThickness).Name = "MainGear_Left_Wheel_Tire"
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
            oRightWheelSk.Factory2D.CreateClosedCircle(wheelOffset, bottomZ, MainWheelRadius + MainTireThickness)
            oRightWheelSk.Factory2D.CreateClosedCircle(wheelOffset, bottomZ, MainWheelRadius)
            oRightWheelSk.CloseEdition()
            oPart.Update()
            oSF.AddNewPad(oRightWheelSk, MainTireThickness).Name = "MainGear_Right_Wheel_Tire"
            oPart.Update()

            Log("      [OK] Main landing gear connected to bulkhead at X=" & CInt(mainX) & "mm")

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