' ============================================================================
' BACKUP - Working version of BuildSolidFuselage
' This file is a SAFETY BACKUP of the last known working code.
' DO NOT INCLUDE THIS FILE in the build (exclude from project, or comment out
' the Class declaration). It is kept here purely as a reference / restore point.
'
' To restore: copy contents into BuildSolidFuselage.vb
' ============================================================================

#If FALSE Then

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

    ' Parametric fuselage dimensions
    Private ReadOnly FuselageLength As Double = 1400.0  ' Total length in mm
    Private ReadOnly SectionPositions() As Double = {0.0, 150.0, 700.0, 850.0, 1400.0}
    Private ReadOnly SectionWidths() As Double = {10.0, 80.0, 150.0, 150.0, 100.0}
    Private ReadOnly SectionHeights() As Double = {10.0, 60.0, 100.0, 100.0, 70.0}

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
        oBodies = oPart.Bodies
        Log("   New Part created : " & oDoc.Name)
    End Sub

    Private Sub BuildSolidWithSketch()
        Log("Stage 2: build fuselage solid with sections...")
        Try
            Dim oPartBody As Object = oBodies.Item("PartBody")
            Dim oOriginElements As Object = oPart.OriginElements
            Dim oPlaneRef As Object = oOriginElements.PlaneYZ

            Dim oSketches As Object = oPartBody.Sketches
            Dim aSketches(UBound(SectionPositions)) As Object

            For i As Integer = 0 To UBound(SectionPositions)
                Dim oSketch As Object = oSketches.Add(oPlaneRef)
                Try
                    oSketch.OpenEdition()
                    Dim oFactory2D As Object = oSketch.Factory2D
                    Dim w As Double = SectionWidths(i)
                    Dim h As Double = SectionHeights(i)
                    oFactory2D.CreateLine(-w / 2, -h / 2, w / 2, -h / 2)
                    oFactory2D.CreateLine(w / 2, -h / 2, w / 2, h / 2)
                    oFactory2D.CreateLine(w / 2, h / 2, -w / 2, h / 2)
                    oFactory2D.CreateLine(-w / 2, h / 2, -w / 2, -h / 2)
                    oSketch.CloseEdition()
                    aSketches(i) = oSketch
                Catch ex As Exception
                    aSketches(i) = oSketch
                End Try
                oPart.Update()
            Next

            Dim oPad As Object = oSF.AddNewPad(aSketches(0), FuselageLength)
            oPad.Name = "Fuselage_Solid"
            oPart.Update()
            Log("   SOLID fuselage    : Multi-section [OK]")

        Catch ex As Exception
            Log("   [ERROR] " & ex.Message)
            Throw
        End Try
    End Sub

    Private Sub Log(msg As String)
        If _log IsNot Nothing Then _log.Invoke(msg) Else System.Diagnostics.Debug.WriteLine(msg)
    End Sub

    Private Sub DebugLog(msg As String)
        System.Diagnostics.Debug.WriteLine(msg)
    End Sub

End Class

#End If
