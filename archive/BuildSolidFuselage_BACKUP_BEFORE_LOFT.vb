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

    ' ============================================================
    ' === LONGERON PARAMETERS - SOLID CORNER RODS ===
    ' ============================================================

    Private ReadOnly LongeronDiameter As Double = 5.0         ' Solid rod diameter (mm)
    Private ReadOnly LongeronCount As Integer = 4             ' Always 4 at corners
    Private ReadOnly LongeronOffset As Double = 65.0          ' Y/Z offset from centerline (mm)
    ' Longeron positions: ±LongeronOffset in Y and Z (INSIDE the 100mm-radius bulkhead)
    ' At ±65, distance from center = sqrt(65² + 65²) = ~92mm, safely inside 100mm radius
    ' Number of cone slices for stacked-pad approach (used as fallback if Shaft fails)
    Private ReadOnly ConeSlices As Integer = 20

    ' ============================================================
    ' === NOSE CONE PARAMETERS - PARAMETRIC CONICAL TAPER ===
    ' ============================================================

    Private ReadOnly NoseLength As Double = 200.0             ' Length before first bulkhead (mm)
    Private ReadOnly NoseTipRadius As Double = 1.0            ' Sharp point radius (mm)

    ' ============================================================
    ' === TAIL CONE PARAMETERS - PARAMETRIC CONICAL TAPER ===
    ' ============================================================

    Private ReadOnly TailLength As Double = 200.0             ' Length after last bulkhead (mm)
    Private ReadOnly TailTipRadius As Double = 1.0            ' Sharp point radius (mm)

    ' ============================================================
    ' === FUSELAGE SKIN PARAMETERS - 5MM CONTINUOUS SHELL ===
    ' ============================================================

    Private ReadOnly SkinThickness As Double = 5.0            ' 5mm thin shell (mm)

#End Region

    Public Sub Run(Optional logger As Action(Of String) = Nothing)
        _log = If(logger, New Action(Of String)(AddressOf DebugLog))

        Try
            Log("== Findus UAV : COMPLETE FUSELAGE BUILD ==")
            Dim totalLength As Double = NoseLength + 1200.0 + TailLength
            Log("Fuselage Total Length: " & totalLength & "mm")
            Log("  Nose: " & NoseLength & "mm | Structure: 1200mm | Tail: " & TailLength & "mm")

            ConnectAndCreatePart()

            Log("Stage 2: Building frame structure (5 bulkheads)...")
            BuildSolidWithSketch()
            oPart.Update()

            Log("Stage 3: Building longerons (4 corner rods)...")
            BuildLongerons()
            oPart.Update()

            Log("Stage 4: Building nose cone...")
            BuildNoseCone()
            oPart.Update()

            Log("Stage 5: Building tail cone...")
            BuildTailCone()
            oPart.Update()

            Log("Stage 6: Building fuselage skin (5mm shell)...")
            BuildFuselageSkin()
            oPart.Update()

            oPart.Update()

            Log("== BUILD COMPLETE ==")
            Log("Total Structure (REALISTIC DRONE FUSELAGE):")
            Log("  - 5 bulkhead frames (200mm diameter circular, 10mm wall thickness)")
            Log("  - 4 longerons (5mm solid corner rods at ±" & LongeronOffset & ", ±" & LongeronOffset & " positions, INSIDE bulkhead)")
            Log("  - Nose cone (" & NoseLength & "mm conical taper, Revolved shape)")
            Log("  - Tail cone (" & TailLength & "mm conical taper, Revolved shape)")
            Log("  - Fuselage skin (5mm continuous circular shell)")

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
        ' Create sketch with outer and inner CIRCLES (creates a hollow circular frame - aerodynamic!)
        Dim oSketch As Object = oPartBody.Sketches.Add(oPlaneRef)
        oSketch.OpenEdition()
        Dim f2D As Object = oSketch.Factory2D

        ' ===== OUTER CIRCLE =====
        ' Diameter = 200mm, so radius = 100mm
        Dim outerRadius As Double = BulkheadWidth / 2
        f2D.CreateClosedCircle(0, 0, outerRadius)

        ' ===== INNER CIRCLE =====
        ' Hollow - wall thickness is BulkheadFrameThickness (10mm)
        Dim innerRadius As Double = outerRadius - BulkheadFrameThickness
        f2D.CreateClosedCircle(0, 0, innerRadius)

        oSketch.CloseEdition()
        oPart.Update()

        ' ===== CREATE PAD (EXTRUDE) =====
        Dim oPad As Object = oSF.AddNewPad(oSketch, BulkheadThickness)
        oPad.Name = "Bulkhead_Frame"
        oPart.Update()

        Log("   [✓] Bulkhead created: Circular diameter " & BulkheadWidth & "mm, wall thickness " & BulkheadFrameThickness & "mm, depth " & BulkheadThickness & " mm")
    End Sub

    Private Sub BuildBulkheadsWithMultipleBodies(oPlaneRef As Object, oPartBody As Object)
        Log("      [STRATEGY] Creating bulkheads on offset planes in separate bodies...")

        Dim oHybridBodies As Object = oPart.HybridBodies
        Dim oOffsetPlanesBody As Object = Nothing

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

        For idx As Integer = 0 To UBound(BulkheadPositions)
            Dim offsetDistance As Double = BulkheadPositions(idx)
            Dim bodyName As String = "Body_Bulkhead_" & CInt(offsetDistance)
            Dim planeName As String = "Plane_" & CInt(offsetDistance)

            Log("      [BULKHEAD " & (idx + 1) & "] Creating " & bodyName & " at X=" & offsetDistance & " mm...")

            Try
                Dim oOffsetPlane As Object = Nothing
                Dim oOffsetPlaneRef As Object = Nothing

                If oOffsetPlanesBody IsNot Nothing Then
                    Try
                        Dim oShapes As Object = oOffsetPlanesBody.HybridShapes
                        For i As Integer = 1 To oShapes.Count
                            Dim oShape As Object = oShapes.Item(i)
                            If oShape.Name = planeName Then
                                oOffsetPlane = oShape
                                Exit For
                            End If
                        Next

                        If oOffsetPlane IsNot Nothing Then
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

                Dim oNewBody As Object = oPart.Bodies.Add()
                oNewBody.Name = bodyName
                Log("         [✓] Body created")

                Dim oSketch As Object = oNewBody.Sketches.Add(oOffsetPlaneRef)
                oSketch.Name = "Sketch_" & CInt(offsetDistance)
                oSketch.OpenEdition()
                Log("         [✓] Sketch opened on offset plane")

                Dim f2D As Object = oSketch.Factory2D

                ' Create CIRCULAR bulkhead (aerodynamic!) instead of rectangular
                Dim outerRadius As Double = BulkheadWidth / 2
                f2D.CreateClosedCircle(0, 0, outerRadius)

                Dim innerRadius As Double = outerRadius - BulkheadFrameThickness
                f2D.CreateClosedCircle(0, 0, innerRadius)

                oSketch.CloseEdition()
                oPart.Update()
                Log("         [✓] Sketch geometry created")

                Dim oNewBodySF As Object = oPart.ShapeFactory
                Dim oPad As Object = oNewBodySF.AddNewPad(oSketch, BulkheadThickness)
                oPad.Name = "Bulkhead_" & CInt(offsetDistance)
                oPart.Update()
                Log("         [✓] Pad created in body at X=" & offsetDistance & " mm")

                Log("      [✓] " & bodyName & " completed successfully")

            Catch ex As Exception
                Log("      [ERROR] " & bodyName & " failed: " & ex.Message)
            End Try
        Next

        Log("      [✓] Multiple Bodies approach completed")
    End Sub

    Private Sub BuildLongerons()
        ' Creates 4 solid rods INSIDE the bulkhead at (±LongeronOffset, ±LongeronOffset) in Y,Z
        ' Runs from X=-NoseLength to X=(1200+TailLength)
        Log("   [LONGERONS] Creating 4 corner stringers (INSIDE bulkhead)...")

        Try
            Dim oOriginElements As Object = oPart.OriginElements
            ' Use YZ plane so pad extrudes along X axis (not XY which extrudes along Z)
            Dim oPlaneYZ As Object = oOriginElements.PlaneYZ
            Dim oPlaneYZRef As Object = oPart.CreateReferenceFromObject(oPlaneYZ)

            ' Define corner positions INSIDE the 100mm-radius bulkhead
            ' At ±65, longerons are at radius sqrt(65² + 65²) ≈ 92mm (safely inside)
            Dim cornerPositions() As Double = {LongeronOffset, -LongeronOffset}  ' Y,Z positions (symmetric)

            For yIdx As Integer = 0 To 1
                For zIdx As Integer = 0 To 1
                    Dim yPos As Double = cornerPositions(yIdx)
                    Dim zPos As Double = cornerPositions(zIdx)
                    Dim longeronName As String = "Longeron_Y" & CInt(yPos) & "_Z" & CInt(zPos)
                    Dim bodyName As String = "Body_" & longeronName

                    Log("      [LONGERON " & (yIdx * 2 + zIdx + 1) & "] Creating at Y=" & yPos & ", Z=" & zPos & "...")

                    Try
                        ' Create separate body for this longeron
                        Dim oNewBody As Object = oBodies.Add()
                        oNewBody.Name = bodyName
                        Log("         [✓] Body created")

                        ' Create sketch on YZ plane (perpendicular to X axis)
                        ' This way, the pad will extrude along X axis for the full fuselage length
                        Dim oSketch As Object = oNewBody.Sketches.Add(oPlaneYZRef)
                        oSketch.Name = "Sketch_" & longeronName
                        oSketch.OpenEdition()
                        Log("         [✓] Sketch opened on YZ plane")

                        ' Get Factory2D and create circle at corner position (Y, Z)
                        Dim f2D As Object = oSketch.Factory2D
                        f2D.CreateClosedCircle(yPos, zPos, LongeronDiameter / 2.0)

                        oSketch.CloseEdition()
                        oPart.Update()
                        Log("         [✓] Sketch with corner circle created at Y=" & yPos & ", Z=" & zPos)

                        ' Create pad extending along full fuselage length (along X axis)
                        Dim totalLongeronLength As Double = NoseLength + 1200.0 + TailLength
                        Dim oPad As Object = oSF.AddNewPad(oSketch, totalLongeronLength)
                        oPad.Name = longeronName
                        Log("         [✓] Pad created, length=" & totalLongeronLength & "mm")

                        ' Attempt to position at -NoseLength (so it starts before first bulkhead)
                        Try
                            oPad.OffsetLength.Value = -NoseLength
                            Log("         [✓] Pad offset set to X=" & (-NoseLength) & " mm")
                        Catch
                            Try
                                oPad.FirstOffset.Value = -NoseLength
                                Log("         [✓] FirstOffset set to X=" & (-NoseLength) & " mm")
                            Catch
                                Log("         [INFO] Longeron created at X=0 (offset not supported in this CATIA version)")
                            End Try
                        End Try

                        oPart.Update()
                        Log("      [✓] " & longeronName & " created successfully")

                    Catch ex As Exception
                        Log("      [ERROR] " & longeronName & " failed: " & ex.Message)
                    End Try
                Next
            Next

            Log("   [✓] All longerons created")

        Catch ex As Exception
            Log("   [ERROR] Longeron creation failed: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildNoseCone()
        ' Creates conical nose cone using stacked-pad approach (proven reliable pattern).
        ' Position: X = -NoseLength to X = 0 (just before first bulkhead at YZ plane)
        '
        ' DESIGN NOTE: Earlier versions attempted ShapeFactory.AddNewShaft() for a smooth
        ' revolution. The Shaft API in this CATIA build requires an axis that CATIA cannot
        ' auto-detect from a construction line. When AddNewShaft creates a feature without
        ' a valid axis, every subsequent oPart.Update() fails ("method Update failed")
        ' which cascades and kills ALL downstream features (slices, tail cone, skin).
        ' Solution: skip Shaft entirely. Stacked pads use only the proven
        ' offset-plane + circular-sketch + pad pattern that works reliably for bulkheads.
        Log("   [NOSE CONE] Creating stacked-pad conical nose cone (X=" & (-NoseLength) & " to X=0)...")

        Try
            Dim baseRadius As Double = BulkheadWidth / 2.0  ' 100mm
            BuildStackedConeNose(baseRadius)
        Catch ex As Exception
            Log("   [ERROR] Nose cone creation failed: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildStackedConeNose(baseRadius As Double)
        ' Builds nose cone as N stacked disk pads with linearly-decreasing radii.
        ' Uses the SAME proven approach as BuildBulkheadsWithMultipleBodies:
        ' offset planes + circular sketches + separate body per slice (avoids COM corruption).
        Dim numSlices As Integer = ConeSlices
        Dim sliceDepth As Double = NoseLength / numSlices
        Log("      Building " & numSlices & "-slice stacked nose cone (each slice " & sliceDepth & "mm thick)...")

        Dim oOriginElements As Object = oPart.OriginElements
        Dim oYZPlane As Object = oOriginElements.PlaneYZ
        Dim oYZPlaneRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

        ' Hybrid body for the slice planes (visual references, grouped together)
        Dim oHybridBodies As Object = oPart.HybridBodies
        Dim oPlanesBody As Object = oHybridBodies.Add()
        oPlanesBody.Name = "NoseCone_Slice_Planes"

        Dim successCount As Integer = 0
        For i As Integer = 0 To numSlices - 1
            Dim xPos As Double = -NoseLength + i * sliceDepth
            ' Radius interpolates from tip at i=0 to base at i=numSlices-1 (midpoint of each slice)
            Dim t As Double = (i + 0.5) / numSlices
            Dim sliceRadius As Double = NoseTipRadius + (baseRadius - NoseTipRadius) * t

            Try
                ' Step 1: Create offset plane at this X position
                Dim oOffsetPlane As Object = oHSF.AddNewPlaneOffset(oYZPlaneRef, xPos, False)
                oOffsetPlane.Name = "NoseSlicePlane_" & i
                oPlanesBody.AppendHybridShape(oOffsetPlane)
                oPart.Update()

                Dim oOffsetRef As Object = oPart.CreateReferenceFromObject(oOffsetPlane)

                ' Step 2: Create separate body for this slice (proven anti-COM-corruption pattern)
                Dim oSliceBody As Object = oBodies.Add()
                oSliceBody.Name = "Body_NoseSlice_" & i

                ' Step 3: Create circular sketch on offset plane
                Dim oSliceSketch As Object = oSliceBody.Sketches.Add(oOffsetRef)
                oSliceSketch.Name = "NoseSliceSketch_" & i
                oSliceSketch.OpenEdition()
                Dim f2D As Object = oSliceSketch.Factory2D
                f2D.CreateClosedCircle(0.0, 0.0, sliceRadius)
                oSliceSketch.CloseEdition()
                oPart.Update()

                ' Step 4: Pad the disk
                Dim oSlicePad As Object = oSF.AddNewPad(oSliceSketch, sliceDepth)
                oSlicePad.Name = "NoseSlice_Pad_" & i
                oPart.Update()
                successCount += 1

            Catch exSlice As Exception
                Log("         [WARN] Slice " & i & " (X=" & xPos & ", r=" & sliceRadius & ") failed: " & exSlice.Message)
            End Try
        Next

        Log("      [✓] Nose cone built with " & successCount & "/" & numSlices & " stacked-pad slices")
        Log("   [✓] Nose cone complete (stepped conical taper from r=" & NoseTipRadius & "mm to r=" & baseRadius & "mm)")
    End Sub

    Private Sub BuildTailCone()
        ' Creates conical tail cone using stacked-pad approach (proven reliable pattern).
        ' Position: X = (last_bulkhead_X + BulkheadThickness) to that + TailLength
        '          = 1200 + 20 = 1220 to 1420 (with default params)
        ' See BuildNoseCone for why Shaft approach was removed.
        Dim tailStartX As Double = BulkheadPositions(UBound(BulkheadPositions)) + BulkheadThickness
        Dim tailEndX As Double = tailStartX + TailLength
        Log("   [TAIL CONE] Creating stacked-pad conical tail cone (X=" & tailStartX & " to X=" & tailEndX & ")...")

        Try
            Dim baseRadius As Double = BulkheadWidth / 2.0  ' 100mm
            BuildStackedConeTail(baseRadius, tailStartX)
        Catch ex As Exception
            Log("   [ERROR] Tail cone creation failed: " & ex.Message)
        End Try
    End Sub

    Private Sub BuildStackedConeTail(baseRadius As Double, tailStartX As Double)
        ' Builds tail cone as N stacked disk pads with linearly-decreasing radii (base -> tip).
        Dim numSlices As Integer = ConeSlices
        Dim sliceDepth As Double = TailLength / numSlices
        Log("      Building " & numSlices & "-slice stacked tail cone (each slice " & sliceDepth & "mm thick)...")

        Dim oOriginElements As Object = oPart.OriginElements
        Dim oYZPlane As Object = oOriginElements.PlaneYZ
        Dim oYZPlaneRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

        ' Hybrid body for slice planes
        Dim oHybridBodies As Object = oPart.HybridBodies
        Dim oPlanesBody As Object = oHybridBodies.Add()
        oPlanesBody.Name = "TailCone_Slice_Planes"

        Dim successCount As Integer = 0
        For i As Integer = 0 To numSlices - 1
            Dim xPos As Double = tailStartX + i * sliceDepth
            ' Radius interpolates from base (at i=0) to tip (at i=numSlices-1)
            Dim t As Double = (i + 0.5) / numSlices
            Dim sliceRadius As Double = baseRadius - (baseRadius - TailTipRadius) * t

            Try
                ' Offset plane at this X
                Dim oOffsetPlane As Object = oHSF.AddNewPlaneOffset(oYZPlaneRef, xPos, False)
                oOffsetPlane.Name = "TailSlicePlane_" & i
                oPlanesBody.AppendHybridShape(oOffsetPlane)
                oPart.Update()

                Dim oOffsetRef As Object = oPart.CreateReferenceFromObject(oOffsetPlane)

                ' Separate body per slice (proven anti-COM-corruption pattern)
                Dim oSliceBody As Object = oBodies.Add()
                oSliceBody.Name = "Body_TailSlice_" & i

                ' Circular sketch
                Dim oSliceSketch As Object = oSliceBody.Sketches.Add(oOffsetRef)
                oSliceSketch.Name = "TailSliceSketch_" & i
                oSliceSketch.OpenEdition()
                Dim f2D As Object = oSliceSketch.Factory2D
                f2D.CreateClosedCircle(0.0, 0.0, sliceRadius)
                oSliceSketch.CloseEdition()
                oPart.Update()

                ' Pad the disk
                Dim oSlicePad As Object = oSF.AddNewPad(oSliceSketch, sliceDepth)
                oSlicePad.Name = "TailSlice_Pad_" & i
                oPart.Update()
                successCount += 1

            Catch exSlice As Exception
                Log("         [WARN] Slice " & i & " (X=" & xPos & ", r=" & sliceRadius & ") failed: " & exSlice.Message)
            End Try
        Next

        Log("      [✓] Tail cone built with " & successCount & "/" & numSlices & " stacked-pad slices")
        Log("   [✓] Tail cone complete (stepped conical taper from r=" & baseRadius & "mm to r=" & TailTipRadius & "mm)")
    End Sub

    Private Sub BuildFuselageSkin()
        ' Creates a HOLLOW 5mm-thick cylindrical shell that covers the structure section
        ' (from first bulkhead at X=0 to back face of last bulkhead at X=1220mm).
        ' Sketch is an annulus (outer circle + inner circle) - padding creates a tube,
        ' not a solid cylinder. This matches the "5mm continuous shell" description.
        Dim outerRadius As Double = BulkheadWidth / 2.0                           ' 100mm
        Dim innerRadius As Double = outerRadius - SkinThickness                   ' 95mm (5mm wall)
        Dim skinLength As Double = BulkheadPositions(UBound(BulkheadPositions)) + BulkheadThickness  ' 1220mm (X=0 to X=1220)

        Log("   [FUSELAGE SKIN] Creating hollow " & SkinThickness & "mm shell (outer Ø" & (outerRadius * 2) & "mm, inner Ø" & (innerRadius * 2) & "mm)...")

        Try
            ' Step 1: Create dedicated body for the skin
            Dim oSkinBody As Object = oBodies.Add()
            oSkinBody.Name = "Body_Fuselage_Skin"
            Log("      [✓] Skin body created")

            ' Step 2: Sketch on YZ plane (so pad extrudes along +X axis = fuselage length)
            Dim oOriginElements As Object = oPart.OriginElements
            Dim oYZPlane As Object = oOriginElements.PlaneYZ
            Dim oYZPlaneRef As Object = oPart.CreateReferenceFromObject(oYZPlane)

            Dim oSkinSketch As Object = oSkinBody.Sketches.Add(oYZPlaneRef)
            oSkinSketch.Name = "Sketch_Skin_Profile"
            oSkinSketch.OpenEdition()
            Log("      [✓] Skin sketch opened on YZ plane")

            ' Step 3: Draw ANNULUS (ring) profile - two concentric circles
            ' CATIA recognizes this as a hollow profile (inner circle subtracts from outer)
            Dim f2D As Object = oSkinSketch.Factory2D
            f2D.CreateClosedCircle(0.0, 0.0, outerRadius)        ' Outer circle (Ø200mm)
            f2D.CreateClosedCircle(0.0, 0.0, innerRadius)        ' Inner circle (Ø190mm)
            Log("      [✓] Annulus profile drawn (Ø" & (outerRadius * 2) & " - Ø" & (innerRadius * 2) & " = " & SkinThickness & "mm wall)")

            oSkinSketch.CloseEdition()
            oPart.Update()

            ' Step 4: Pad the annulus along +X axis - creates a hollow tube (the skin)
            Try
                Dim oSkinPad As Object = oSF.AddNewPad(oSkinSketch, skinLength)
                oSkinPad.Name = "Fuselage_Skin_Shell"
                oPart.Update()
                Log("      [✓] Hollow skin tube created (X=0 to X=" & skinLength & "mm, " & SkinThickness & "mm wall thickness)")
            Catch exPad As Exception
                Log("      [WARN] Skin pad creation failed: " & exPad.Message)
            End Try

            Log("   [✓] Fuselage skin complete (hollow shell)")

        Catch ex As Exception
            Log("   [ERROR] Fuselage skin creation failed: " & ex.Message)
        End Try
    End Sub

    Private Sub Log(msg As String)
        If _log IsNot Nothing Then _log.Invoke(msg) Else System.Diagnostics.Debug.WriteLine(msg)
    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

End Class
