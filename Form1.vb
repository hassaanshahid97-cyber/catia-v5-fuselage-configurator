Option Strict Off
Option Explicit On

Imports System.Runtime.InteropServices

Public Class Form1

    ' Logger for UI textbox
    Public Sub Log(msg As String)
        txtLog.AppendText(msg & vbCrLf)
        Application.DoEvents()
    End Sub

    ' Original single-Part builder
    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        Try
            Dim builder As New BuildSolidFuselage()
            builder.Run(AddressOf Log)
        Catch ex As Exception
            Log("[ERROR] " & ex.Message)
        End Try
    End Sub

    ' PowerCopy / parametric assembly builder (individual CATPart per component)
    Private Sub btnPowerCopy_Click(sender As Object, e As EventArgs) Handles btnPowerCopy.Click
        Try
            txtLog.Clear()
            Dim builder As New FuselagePowerCopyBuilder(AddressOf Log)
            builder.Run()
        Catch ex As Exception
            Log("[ERROR] " & ex.Message)
        End Try
    End Sub

End Class