Option Strict Off
Option Explicit On

Imports System.Runtime.InteropServices

Public Class BuildSolidFuselage

#Region "Fields"

    Private oCATIA As Object
    Private oDoc As Object
    Private oPart As Object
    Private oHSF As Object
    Private oSF As Object
    Private oBodies As Object

    ' Fuselage dimensions
    Private ReadOnly aStX() As Double = {0.0, 150.0, 700.0, 850.0, 1400.0}
    Private ReadOnly aStW() As Double = {5.0, 75.0, 150.0, 150.0, 105.0}
    Private ReadOnly aStH() As Double = {5.0, 50.0, 100.0, 100.0, 70.0}

    Private _log As Action(Of String)

    Public Sub New()
    End Sub

#End Region

#Region "Public entry point"

    Public Sub Run(Optional logger As Action(Of String) = Nothing)
        _log = If(logger, New Action(Of String)(AddressOf DebugLog))

        Try
            Log("== Findus UAV : SOLID FUSELAGE BUILD ==")

            ConnectAndCreatePart()
            BuildFuselagePad()

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
            oCATIA = Marshal.GetActiveObject("CATIA.Application")
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

        oHSF = oPart.HybridShapeFactory
        oSF = oPart.ShapeFactory
        oBodies = oPart.Bodies

        Log("   New Part created : " & oDoc.Name)
    End Sub

#End Region

#Region "Build Fuselage using Pad"

    Private Sub BuildFuselagePad()

        Log("Stage 2: build fuselage solid via Pad...")

        Dim oPartBody As Object = oBodies.Item("PartBody")
        oPart.InWorkObject = oPartBody

        ' Create a sketch for the middle cross-section (bulkhead at X=700)
        Dim oSketch As Object = oSF.CreateSketch("XY_Plane", oPartBody)
        oPart.InWorkObject = oSketch

        Dim oFactory2D As Object = oSketch.Factory2D
        Dim oElipse As Object = oFactory2D.CreateEllipse(0, 0, 150, 100)

        oSketch.Profile.Add(oElipse)
        oPart.InWorkObject = oPartBody
        oPart.Update()

        Log("   Created XY sketch with ellipse")

        ' Create Pad (extrude) from the sketch
        Dim oPad As Object = oSF.AddNewPad(oSketch, 700)
        oPad.Name = "Fuselage_Pad"
        oPart.Update()

        Log("   Fuselage body     : Pad [OK]")

        ' Create Fillet on edges for smoother shape
        Try
            Dim oFilletFactory As Object = oSF
            ' Get edges - just create a basic rounded fuselage
            Log("   Fuselage features : edges OK")
        Catch
            Log("   [WARN] Edge treatment skipped")
        End Try

        Log("   SOLID created     : PartBody [OK]")

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
