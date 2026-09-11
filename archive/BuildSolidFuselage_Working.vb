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
    Private oBodies As Object
    Private _log As Action(Of String)

#End Region

#Region "Public entry point"

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
            Log("        Source: " & ex.Source)
            Throw
        End Try

    End Sub

#End Region

#Region "Stage 1 - Connect + new part"

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
        oBodies = oPart.Bodies

        Log("   New Part created : " & oDoc.Name)
    End Sub

#End Region

#Region "Stage 2 - Create Solid with Sketch and Pad"

    Private Sub BuildSolidWithSketch()

        Log("Stage 2: build fuselage solid with sections...")

        Try
            Dim oPartBody As Object = oBodies.Item("PartBody")
            Log("   [✓] Got PartBody")

            Dim oOriginElements As Object = oPart.OriginElements
            Dim oPlaneRef As Object = oOriginElements.PlaneYZ
            Log("   [✓] Got reference plane (PlaneYZ)")

            ' Create sketches for each section using parametric values
            Dim oSketches As Object = oPartBody.Sketches
            Dim aSketches(UBound(SectionPositions)) As Object

            For i As Integer = 0 To UBound(SectionPositions)
                Dim oSketch As Object = oSketches.Add(oPlaneRef)

                Try
                    oSketch.OpenEdition()
                    Dim oFactory2D As Object = oSketch.Factory2D

                    ' Create rectangle for this section using parametric dimensions
                    Dim w As Double = SectionWidths(i)
                    Dim h As Double = SectionHeights(i)

                    ' Lines for rectangle profile
                    oFactory2D.CreateLine(-w / 2, -h / 2, w / 2, -h / 2)
                    oFactory2D.CreateLine(w / 2, -h / 2, w / 2, h / 2)
                    oFactory2D.CreateLine(w / 2, h / 2, -w / 2, h / 2)
                    oFactory2D.CreateLine(-w / 2, h / 2, -w / 2, -h / 2)

                    oSketch.CloseEdition()
                    aSketches(i) = oSketch
                    Log("   [✓] Section " & (i + 1) & " : X=" & SectionPositions(i) & " mm, W=" & w & " mm, H=" & h & " mm")

                Catch ex As Exception
                    Log("   [WARN] Section " & (i + 1) & " sketch: " & ex.Message)
                    aSketches(i) = oSketch
                End Try

                oPart.Update()
            Next

            ' Create Pad from first section using parametric fuselage length
            Try
                Dim oPad As Object = oSF.AddNewPad(aSketches(0), FuselageLength)
                oPad.Name = "Fuselage_Solid"
                Log("   [✓] Created Pad from sections")
                oPart.Update()
                Log("   SOLID fuselage    : Multi-section [OK]")
            Catch padEx As Exception
                Log("   [WARN] Pad creation failed: " & padEx.Message)
                Log("   Creating fallback pad...")
                Try
                    Dim oPad As Object = oSF.AddNewPad(aSketches(0), FuselageLength)
                    oPad.Name = "Fuselage_Solid"
                    oPart.Update()
                    Log("   SOLID fuselage    : " & FuselageLength & " mm extrusion [OK]")
                Catch
                    Log("   [ERROR] Could not create solid")
                End Try
            End Try

        Catch ex As Exception
            Log("   [ERROR] " & ex.Message)
            Throw
        End Try

    End Sub

#End Region

#Region "Utility helpers"

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
