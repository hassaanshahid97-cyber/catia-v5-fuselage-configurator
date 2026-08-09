<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.btnConnect = New System.Windows.Forms.Button()
        Me.btnPowerCopy = New System.Windows.Forms.Button()
        Me.txtLog = New System.Windows.Forms.TextBox()
        Me.SuspendLayout()
        '
        'btnConnect
        '
        Me.btnConnect.Location = New System.Drawing.Point(12, 12)
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.Size = New System.Drawing.Size(200, 40)
        Me.btnConnect.TabIndex = 0
        Me.btnConnect.Text = "1. Single-Part Build"
        Me.btnConnect.UseVisualStyleBackColor = True
        '
        'btnPowerCopy
        '
        Me.btnPowerCopy.Location = New System.Drawing.Point(12, 62)
        Me.btnPowerCopy.Name = "btnPowerCopy"
        Me.btnPowerCopy.Size = New System.Drawing.Size(200, 40)
        Me.btnPowerCopy.TabIndex = 1
        Me.btnPowerCopy.Text = "2. Assembly Build (PowerCopy)"
        Me.btnPowerCopy.UseVisualStyleBackColor = True
        '
        'txtLog
        '
        Me.txtLog.Location = New System.Drawing.Point(220, 12)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(660, 500)
        Me.txtLog.TabIndex = 7
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(900, 525)
        Me.Controls.Add(Me.txtLog)
        Me.Controls.Add(Me.btnPowerCopy)
        Me.Controls.Add(Me.btnConnect)
        Me.Name = "Form1"
        Me.Text = "Findus UAV Fuselage Builder"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnConnect As Button
    Friend WithEvents btnPowerCopy As Button
    Friend WithEvents txtLog As TextBox
End Class
