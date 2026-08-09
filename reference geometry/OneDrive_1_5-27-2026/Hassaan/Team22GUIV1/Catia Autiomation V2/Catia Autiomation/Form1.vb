Option Strict On
Option Explicit On
Imports System.Runtime.InteropServices

Public Class Form1

    ' Logger for UI textbox
    Public Sub Log(msg As String)
        If txtLog.InvokeRequired Then
            ' Change 'Function' to 'Sub'
            txtLog.Invoke(New Action(Sub() txtLog.AppendText(msg & vbCrLf)))
        Else
            txtLog.AppendText(msg & vbCrLf)
        End If
        Application.DoEvents()
    End Sub

    ' Connection button
    Private Sub btnConnect_Click(sender As Object, e As EventArgs) Handles btnConnect.Click
        Try
            Log("Initializing CATIA Fuselage Builder...")
            Dim builder As New BuildSolidFuselage()

            ' --- 1. PARSE BULKHEAD PARAMETERS ---
            builder.BulkheadWidth = Double.Parse(txtBulkheadWidth.Text)
            builder.BulkheadThickness = Double.Parse(txtBulkheadThickness.Text)
            builder.BulkheadFrameThickness = Double.Parse(txtBulkheadFrameThickness.Text)

            ' Parse comma-separated string array for Bulkhead Positions
            Dim posStrings = txtBulkheadPositions.Text.Split(","c)
            Dim posList As New List(Of Double)
            For Each p In posStrings
                posList.Add(Double.Parse(p.Trim()))
            Next
            builder.BulkheadPositions = posList.ToArray()

            ' --- 2. PARSE LONGERON PARAMETERS ---
            builder.LongeronDiameter = Double.Parse(txtLongeronDiameter.Text)
            builder.LongeronOffset = Double.Parse(txtLongeronOffset.Text)

            ' --- 3. PARSE TAILBOOM PARAMETERS ---
            builder.BoomRadius = Double.Parse(txtBoomRadius.Text)
            builder.BoomLength = Double.Parse(txtBoomLength.Text)

            ' --- 4. PARSE NOSE GEAR PARAMETERS ---
            builder.NoseStrutLength = Double.Parse(txtNoseStrutLength.Text)
            builder.NoseWheelRadius = Double.Parse(txtNoseWheelRadius.Text)
            builder.NoseTireThickness = Double.Parse(txtNoseTireThickness.Text)

            ' --- 5. PARSE MAIN GEAR PARAMETERS ---
            builder.MainStrutLength = Double.Parse(txtMainStrutLength.Text)
            builder.MainWheelRadius = Double.Parse(txtMainWheelRadius.Text)
            builder.MainTireThickness = Double.Parse(txtMainTireThickness.Text)

            ' --- RUN THE BUILDER ---
            Log("Parameters successfully loaded. Sending commands to CATIA...")
            builder.Run(AddressOf Log)

        Catch ex As FormatException
            Log("[ERROR] Invalid input. Please ensure all fields contain valid numbers.")
        Catch ex As Exception
            Log("[ERROR] " & ex.Message)
        End Try
    End Sub
End Class