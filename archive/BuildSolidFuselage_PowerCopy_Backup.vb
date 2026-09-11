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

    ' ============================================================
    ' === LEGACY FUSELAGE PARAMETERS (commented out) ===
    ' ============================================================

    ' Parametric fuselage dimensions (currently disabled)
    'Private ReadOnly FuselageLength As Double = 1400.0  ' Total length in mm
    'Private ReadOnly SectionPositions() As Double = {0.0, 150.0, 700.0, 850.0, 1400.0}
    'Private ReadOnly SectionWidths() As Double = {10.0, 80.0, 150.0, 150.0, 100.0}
    'Private ReadOnly SectionHeights() As Double = {10.0, 60.0, 100.0, 100.0, 70.0}
    'Private ReadOnly BulkheadPositions() As Double = {150.0, 450.0, 850.0, 1200.0}

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

            ' --- Build main fuselage body (commented out - bulkhead only) ---
            'Dim oSketches As Object = oPartBody.Sketches
            'Dim aSketches(UBound(SectionPositions)) As Object

            'For i As Integer = 0 To UBound(SectionPositions)
            '    Dim oSketch As Object = oSketches.Add(oPlaneRef)
            '    Try
            '        oSketch.OpenEdition()
            '        Dim oFactory2D As Object = oSketch.Factory2D
            '        Dim w As Double = SectionWidths(i)
            '        Dim h As Double = SectionHeights(i)
            '        oFactory2D.CreateLine(-w / 2, -h / 2, w / 2, -h / 2)
            '        oFactory2D.CreateLine(w / 2, -h / 2, w / 2, h / 2)
            '        oFactory2D.CreateLine(w / 2, h / 2, -w / 2, h / 2)
            '        oFactory2D.CreateLine(-w / 2, h / 2, -w / 2, -h / 2)
            '        oSketch.CloseEdition()
            '        aSketches(i) = oSketch
            '    Catch ex As Exception
            '        aSketches(i) = oSketch
            '    End Try
            '    oPart.Update()
            'Next

            'Dim oPad As Object = oSF.AddNewPad(aSketches(0), FuselageLength)
            'oPad.Name = "Fuselage_Solid"
            'oPart.Update()
            'Log("   SOLID fuselage    : Multi-section [OK]")

            ' --- Build single bulkhead as hollow rectangular frame on YZ plane ---
            Try
                BuildBulkhead(oPlaneRef, oPartBody)
                Log("   Bulkhead          : Created [OK]")
            Catch ex As Exception
                Log("   [WARN] Bulkhead failed: " & ex.Message)
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

    Private Sub Log(msg As String)
        If _log IsNot Nothing Then _log.Invoke(msg) Else System.Diagnostics.Debug.WriteLine(msg)
    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

End Class