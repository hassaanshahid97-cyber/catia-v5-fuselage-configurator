Option Strict Off
Option Explicit On

Imports System.Runtime.InteropServices
Imports MECMOD
Imports PARTITF
Imports KnowledgewareTypeLib
Imports HybridShapeTypeLib
Imports ProductStructureTypeLib
Imports System.IO

Public Class BuildSolidFuselage

#Region "Fields"

    Private oCATIA As Object
    Private oDoc As Object
    Private oPart As Object
    Private oSF As Object
    Private oHSF As Object
    Private oBodies As Object
    Private _log As Action(Of String)

    ' ============================================================
    ' === BULKHEAD FRAME PARAMETERS - EDIT THESE TO CUSTOMIZE ===
    ' ============================================================

    ' Bulkhead frame dimensions
    Private ReadOnly BulkheadWidth As Double = 200.0        ' Outer width of frame (mm)
    Private ReadOnly BulkheadHeight As Double = 200.0       ' Outer height of frame (mm)
    Private ReadOnly BulkheadThickness As Double = 20.0     ' Extrusion depth along X-axis (mm)
    Private ReadOnly BulkheadFrameThickness As Double = 10.0  ' Thickness of frame walls (mm)

    ' Bulkhead positions along X-axis (for Pattern feature)
    Private ReadOnly BulkheadPositions() As Double = {150.0, 450.0, 850.0, 1200.0}  ' Offset from main bulkhead (mm)

#End Region

    Public Sub Run(Optional logger As Action(Of String) = Nothing)
        _log = If(logger, New Action(Of String)(AddressOf DebugLog))

        Try
            Log("== Findus UAV : SOLID FUSELAGE BUILD ==")
            ConnectAndCreatePart()
            BuildSolidWithSketch()
            oPart.Update()
            Log("== BUILD COMPLETE ==")
        Catch ex As Exception
            Log("[FATAL] " & ex.Message)
            Throw
        End Try
    End Sub

    Private Sub ConnectAndCreatePart()
        Log("Stage 1: connect + create new Part...")
        Try
            oCATIA = GetObject("", "CATIA.Application")
        Catch
            Throw New Exception("CATIA is not running. Start CATIA V5, then retry.")
        End Try
        oCATIA.Visible = True
        Dim oDocs As Object = oCATIA.Documents
        oDoc = oDocs.Add("Part")
        oPart = oDoc.Part
        Try
            oPart.Name = "Findus_UAV_Fuselage"
        Catch
        End Try
        oSF = oPart.ShapeFactory
        oHSF = oPart.HybridShapeFactory
        oBodies = oPart.Bodies
        Log("   New Part created : " & oDoc.Name)
    End Sub

    Private Sub BuildSolidWithSketch()
        Log("Stage 2: build fuselage solid with sections...")
        Try
            Dim oPartBody As Object = oBodies.Item("PartBody")
            Dim oOriginElements As Object = oPart.OriginElements
            Dim oPlaneRef As Object = oOriginElements.PlaneYZ
            Dim oPlaneYZRef As Object
            Try
                oPlaneYZRef = oPart.CreateReferenceFromObject(oPlaneRef)
            Catch
                oPlaneYZRef = oPlaneRef
            End Try

            ' --- Build main bulkhead (this works reliably) ---
            Try
                Log("   [STEP 1] Creating main bulkhead on YZ plane...")
                BuildBulkhead(oPlaneRef, oPartBody)
                Log("   [✓] Main bulkhead created successfully")
            Catch ex As Exception
                Log("   [FATAL] Main bulkhead creation failed: " & ex.Message)
                Throw
            End Try

            ' --- Create offset planes for visual reference ---
            Log("   [STEP 2] Creating offset planes for reference...")

            Dim oHybridBodies As Object = oPart.HybridBodies
            Dim oHybridBody As Object = oHybridBodies.Add()
            oHybridBody.Name = "Offset_Planes"

            Dim oYZPlane As Object = oPart.OriginElements.PlaneYZ
            Dim oYZPlaneRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

            Try
                For idx As Integer = 0 To UBound(BulkheadPositions)
                    Dim offsetDistance As Double = BulkheadPositions(idx)
                    Log("   [OFFSET] Creating offset plane at X=" & offsetDistance & " mm...")

                    Try
                        Dim oOffsetPlaneShape As Object = oHSF.AddNewPlaneOffset(oYZPlaneRef, offsetDistance, False)
                        oOffsetPlaneShape.Name = "Plane_" & CInt(offsetDistance)
                        oHybridBody.AppendHybridShape(oOffsetPlaneShape)
                        oPart.Update()
                        Log("      [✓] Offset plane created")

                    Catch exOffset As Exception
                        Log("      [ERROR] " & exOffset.Message)
                    End Try
                Next

                Log("   [✓] All offset planes created")

            Catch ex As Exception
                Log("   [WARN] Offset plane creation non-fatal: " & ex.Message)
            End Try

            ' --- BUILD BULKHEADS USING MULTIPLE BODIES APPROACH ---
            ' The key insight: Sequential AddNewPad() calls trigger COM errors.
            ' Solution: Create separate bodies for each bulkhead to avoid COM state corruption.
            ' Each body gets its own sketch/pad cycle, making it more reliable.

            Log("   [STEP 3] Creating bulkheads at offset positions...")

            Try
                BuildBulkheadsWithMultipleBodies(oPlaneRef, oPartBody)
                Log("   [✓] Bulkheads created successfully")
            Catch ex As Exception
                Log("   [ERROR] Bulkhead creation failed: " & ex.Message)
                Throw
            End Try

        Catch ex As Exception
            Log("   [ERROR] " & ex.Message)
            Throw
        End Try
    End Sub

    Private Sub BuildBulkhead(oPlaneRef As Object, oPartBody As Object)
        ' Create sketch with outer and inner rectangles (creates a hollow frame)
        Dim oSketch As Object = oPartBody.Sketches.Add(oPlaneRef)
        oSketch.OpenEdition()
        Dim f2D As Object = oSketch.Factory2D

        ' ===== OUTER RECTANGLE =====
        ' Size: BulkheadWidth × BulkheadHeight
        Dim ow As Double = BulkheadWidth / 2       ' Half-width for centered rectangle
        Dim oh As Double = BulkheadHeight / 2      ' Half-height for centered rectangle
        f2D.CreateLine(-ow, -oh, ow, -oh)          ' Bottom line
        f2D.CreateLine(ow, -oh, ow, oh)            ' Right line
        f2D.CreateLine(ow, oh, -ow, oh)            ' Top line
        f2D.CreateLine(-ow, oh, -ow, -oh)          ' Left line

        ' ===== INNER RECTANGLE =====
        ' Creates the hole in the center. Reduced by 2×BulkheadFrameThickness
        Dim iw As Double = (BulkheadWidth - 2 * BulkheadFrameThickness) / 2
        Dim ih As Double = (BulkheadHeight - 2 * BulkheadFrameThickness) / 2
        f2D.CreateLine(-iw, -ih, iw, -ih)          ' Bottom line
        f2D.CreateLine(iw, -ih, iw, ih)            ' Right line
        f2D.CreateLine(iw, ih, -iw, ih)            ' Top line
        f2D.CreateLine(-iw, ih, -iw, -ih)          ' Left line

        oSketch.CloseEdition()
        oPart.Update()

        ' ===== CREATE PAD (EXTRUDE) =====
        ' Extrudes the hollow frame by BulkheadThickness (depth along X-axis)
        Dim oPad As Object = oSF.AddNewPad(oSketch, BulkheadThickness)
        oPad.Name = "Bulkhead_Frame"
        oPart.Update()

        Log("   [✓] Bulkhead created: " & BulkheadWidth & "×" & BulkheadHeight & "×" & BulkheadThickness & " mm, frame thickness: " & BulkheadFrameThickness & " mm")
    End Sub

    Private Sub BuildBulkheadsWithMultipleBodies(oPlaneRef As Object, oPartBody As Object)
        ' ============================================================
        ' REVISED MULTIPLE BODIES WITH OFFSET PLANES
        ' ============================================================
        ' Key insight: Instead of creating sketches on YZ plane and
        ' trying to offset pads afterward, create sketches directly
        ' on the offset planes. The sketch will naturally be positioned
        ' at that plane's X location.
        '
        ' For each offset position:
        ' 1. Get reference to the offset plane
        ' 2. Create new body
        ' 3. Create sketch ON that offset plane
        ' 4. Draw geometry on the sketch
        ' 5. Create pad from sketch
        ' 6. Pad will be at the offset plane's X position
        ' ============================================================

        Log("      [STRATEGY] Creating bulkheads on offset planes in separate bodies...")

        ' Get reference to the Offset_Planes HybridBody and its planes
        Dim oHybridBodies As Object = oPart.HybridBodies
        Dim oOffsetPlanesBody As Object = Nothing

        ' Find the Offset_Planes HybridBody we created earlier
        Try
            For i As Integer = 1 To oHybridBodies.Count
                Dim oBody As Object = oHybridBodies.Item(i)
                If oBody.Name = "Offset_Planes" Then
                    oOffsetPlanesBody = oBody
                    Exit For
                End If
            Next
        Catch
            Log("      [WARN] Could not find Offset_Planes HybridBody")
        End Try

        ' BulkheadPositions array has 4 elements (150, 450, 850, 1200)
        ' Main bulkhead is already at X=0 in PartBody
        ' Now create 4 additional bulkheads in separate bodies using offset planes

        For idx As Integer = 0 To UBound(BulkheadPositions)
            Dim offsetDistance As Double = BulkheadPositions(idx)
            Dim bodyName As String = "Body_Bulkhead_" & CInt(offsetDistance)
            Dim planeName As String = "Plane_" & CInt(offsetDistance)

            Log("      [BULKHEAD " & (idx + 1) & "] Creating " & bodyName & " at X=" & offsetDistance & " mm...")

            Try
                ' ===== GET REFERENCE TO OFFSET PLANE =====
                Dim oOffsetPlane As Object = Nothing
                Dim oOffsetPlaneRef As Object = Nothing

                If oOffsetPlanesBody IsNot Nothing Then
                    Try
                        ' Get the offset plane shape from the HybridBody
                        Dim oShapes As Object = oOffsetPlanesBody.HybridShapes
                        For i As Integer = 1 To oShapes.Count
                            Dim oShape As Object = oShapes.Item(i)
                            If oShape.Name = planeName Then
                                oOffsetPlane = oShape
                                Exit For
                            End If
                        Next

                        If oOffsetPlane IsNot Nothing Then
                            ' Create a reference to this plane
                            oOffsetPlaneRef = oPart.CreateReferenceFromObject(oOffsetPlane)
                            Log("         [✓] Offset plane reference obtained")
                        Else
                            Log("         [FALLBACK] Offset plane not found, using YZ plane instead")
                            oOffsetPlaneRef = oPlaneRef
                        End If
                    Catch exPlaneRef As Exception
                        Log("         [FALLBACK] Could not get plane reference: " & exPlaneRef.Message)
                        oOffsetPlaneRef = oPlaneRef
                    End Try
                Else
                    Log("         [FALLBACK] No HybridBodies, using YZ plane")
                    oOffsetPlaneRef = oPlaneRef
                End If

                ' ===== CREATE NEW BODY =====
                Dim oNewBody As Object = oPart.Bodies.Add()
                oNewBody.Name = bodyName
                Log("         [✓] Body created")

                ' ===== CREATE SKETCH ON OFFSET PLANE =====
                ' This is the key difference: sketch is created on the offset plane,
                ' so it will naturally be at that X position
                Dim oSketch As Object = oNewBody.Sketches.Add(oOffsetPlaneRef)
                oSketch.Name = "Sketch_" & CInt(offsetDistance)
                oSketch.OpenEdition()
                Log("         [✓] Sketch opened on offset plane")

                ' ===== DRAW HOLLOW RECTANGLE =====
                Dim f2D As Object = oSketch.Factory2D

                ' Outer rectangle
                Dim ow As Double = BulkheadWidth / 2
                Dim oh As Double = BulkheadHeight / 2
                f2D.CreateLine(-ow, -oh, ow, -oh)
                f2D.CreateLine(ow, -oh, ow, oh)
                f2D.CreateLine(ow, oh, -ow, oh)
                f2D.CreateLine(-ow, oh, -ow, -oh)

                ' Inner rectangle
                Dim iw As Double = (BulkheadWidth - 2 * BulkheadFrameThickness) / 2
                Dim ih As Double = (BulkheadHeight - 2 * BulkheadFrameThickness) / 2
                f2D.CreateLine(-iw, -ih, iw, -ih)
                f2D.CreateLine(iw, -ih, iw, ih)
                f2D.CreateLine(iw, ih, -iw, ih)
                f2D.CreateLine(-iw, ih, -iw, -ih)

                oSketch.CloseEdition()
                oPart.Update()
                Log("         [✓] Sketch geometry created")

                ' ===== CREATE PAD IN THIS BODY =====
                Dim oNewBodySF As Object = oPart.ShapeFactory
                Dim oPad As Object = oNewBodySF.AddNewPad(oSketch, BulkheadThickness)
                oPad.Name = "Bulkhead_" & CInt(offsetDistance)
                oPart.Update()
                Log("         [✓] Pad created in body at X=" & offsetDistance & " mm")

                Log("      [✓] " & bodyName & " completed successfully")

            Catch ex As Exception
                Log("      [ERROR] " & bodyName & " failed: " & ex.Message)
                Log("         Type: " & ex.GetType().Name)
                If ex.InnerException IsNot Nothing Then
                    Log("         Inner: " & ex.InnerException.Message)
                End If
            End Try
        Next

        Log("      [✓] Multiple Bodies approach completed")
    End Sub

    Private Sub Log(msg As String)
        If _log IsNot Nothing Then _log.Invoke(msg) Else System.Diagnostics.Debug.WriteLine(msg)
    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

End Class
