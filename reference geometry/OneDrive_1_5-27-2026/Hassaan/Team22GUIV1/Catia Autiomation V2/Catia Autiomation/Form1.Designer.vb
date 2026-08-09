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
        Me.txtLog = New System.Windows.Forms.TextBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.txtBulkheadPositions = New System.Windows.Forms.TextBox()
        Me.txtBulkheadFrameThickness = New System.Windows.Forms.TextBox()
        Me.txtBulkheadThickness = New System.Windows.Forms.TextBox()
        Me.txtBulkheadWidth = New System.Windows.Forms.TextBox()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.txtLongeronOffset = New System.Windows.Forms.TextBox()
        Me.txtLongeronDiameter = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.txtBoomLength = New System.Windows.Forms.TextBox()
        Me.txtBoomRadius = New System.Windows.Forms.TextBox()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.GroupBox4 = New System.Windows.Forms.GroupBox()
        Me.txtNoseTireThickness = New System.Windows.Forms.TextBox()
        Me.txtNoseWheelRadius = New System.Windows.Forms.TextBox()
        Me.txtNoseStrutLength = New System.Windows.Forms.TextBox()
        Me.Label14 = New System.Windows.Forms.Label()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.Label16 = New System.Windows.Forms.Label()
        Me.GroupBox5 = New System.Windows.Forms.GroupBox()
        Me.txtMainTireThickness = New System.Windows.Forms.TextBox()
        Me.txtMainWheelRadius = New System.Windows.Forms.TextBox()
        Me.txtMainStrutLength = New System.Windows.Forms.TextBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.GroupBox3.SuspendLayout()
        Me.GroupBox4.SuspendLayout()
        Me.GroupBox5.SuspendLayout()
        Me.SuspendLayout()
        '
        'btnConnect
        '
        Me.btnConnect.Location = New System.Drawing.Point(26, 350)
        Me.btnConnect.Margin = New System.Windows.Forms.Padding(4)
        Me.btnConnect.Name = "btnConnect"
        Me.btnConnect.Size = New System.Drawing.Size(267, 49)
        Me.btnConnect.TabIndex = 0
        Me.btnConnect.Text = "1. Connect to CATIA"
        Me.btnConnect.UseVisualStyleBackColor = True
        '
        'txtLog
        '
        Me.txtLog.Location = New System.Drawing.Point(738, 1)
        Me.txtLog.Margin = New System.Windows.Forms.Padding(4)
        Me.txtLog.Multiline = True
        Me.txtLog.Name = "txtLog"
        Me.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.txtLog.Size = New System.Drawing.Size(330, 481)
        Me.txtLog.TabIndex = 7
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.txtBulkheadPositions)
        Me.GroupBox1.Controls.Add(Me.txtBulkheadFrameThickness)
        Me.GroupBox1.Controls.Add(Me.txtBulkheadThickness)
        Me.GroupBox1.Controls.Add(Me.txtBulkheadWidth)
        Me.GroupBox1.Controls.Add(Me.Label4)
        Me.GroupBox1.Controls.Add(Me.Label3)
        Me.GroupBox1.Controls.Add(Me.Label2)
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Location = New System.Drawing.Point(22, 137)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(295, 100)
        Me.GroupBox1.TabIndex = 11
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Bulkhead Parameters"
        '
        'txtBulkheadPositions
        '
        Me.txtBulkheadPositions.Location = New System.Drawing.Point(96, 70)
        Me.txtBulkheadPositions.Name = "txtBulkheadPositions"
        Me.txtBulkheadPositions.Size = New System.Drawing.Size(185, 22)
        Me.txtBulkheadPositions.TabIndex = 7
        Me.txtBulkheadPositions.Text = "150.0, 450.0, 850.0, 1200.0"
        '
        'txtBulkheadFrameThickness
        '
        Me.txtBulkheadFrameThickness.Location = New System.Drawing.Point(120, 54)
        Me.txtBulkheadFrameThickness.Name = "txtBulkheadFrameThickness"
        Me.txtBulkheadFrameThickness.Size = New System.Drawing.Size(100, 22)
        Me.txtBulkheadFrameThickness.TabIndex = 6
        Me.txtBulkheadFrameThickness.Text = "10.0"
        '
        'txtBulkheadThickness
        '
        Me.txtBulkheadThickness.Location = New System.Drawing.Point(120, 35)
        Me.txtBulkheadThickness.Name = "txtBulkheadThickness"
        Me.txtBulkheadThickness.Size = New System.Drawing.Size(100, 22)
        Me.txtBulkheadThickness.TabIndex = 5
        Me.txtBulkheadThickness.Text = "20.0"
        '
        'txtBulkheadWidth
        '
        Me.txtBulkheadWidth.Location = New System.Drawing.Point(120, 16)
        Me.txtBulkheadWidth.Name = "txtBulkheadWidth"
        Me.txtBulkheadWidth.Size = New System.Drawing.Size(100, 22)
        Me.txtBulkheadWidth.TabIndex = 4
        Me.txtBulkheadWidth.Text = "200.0"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(7, 70)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(126, 16)
        Me.Label4.TabIndex = 3
        Me.Label4.Text = "Positions (X coords)"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(7, 54)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(90, 16)
        Me.Label3.TabIndex = 2
        Me.Label3.Text = "Fram Th (mm)"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(5, 38)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(73, 16)
        Me.Label2.TabIndex = 1
        Me.Label2.Text = "Thick (mm)"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(7, 22)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(71, 16)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Width(mm)"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.Add(Me.txtLongeronOffset)
        Me.GroupBox2.Controls.Add(Me.txtLongeronDiameter)
        Me.GroupBox2.Controls.Add(Me.Label7)
        Me.GroupBox2.Controls.Add(Me.Label8)
        Me.GroupBox2.Location = New System.Drawing.Point(22, 243)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(200, 68)
        Me.GroupBox2.TabIndex = 12
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Longeron Parameters"
        '
        'txtLongeronOffset
        '
        Me.txtLongeronOffset.Location = New System.Drawing.Point(100, 33)
        Me.txtLongeronOffset.Name = "txtLongeronOffset"
        Me.txtLongeronOffset.Size = New System.Drawing.Size(100, 22)
        Me.txtLongeronOffset.TabIndex = 13
        Me.txtLongeronOffset.Text = "65.0"
        '
        'txtLongeronDiameter
        '
        Me.txtLongeronDiameter.Location = New System.Drawing.Point(100, 17)
        Me.txtLongeronDiameter.Name = "txtLongeronDiameter"
        Me.txtLongeronDiameter.Size = New System.Drawing.Size(100, 22)
        Me.txtLongeronDiameter.TabIndex = 12
        Me.txtLongeronDiameter.Text = "2.0"
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(7, 36)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(74, 16)
        Me.Label7.TabIndex = 9
        Me.Label7.Text = "Offset (mm)"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(7, 20)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(95, 16)
        Me.Label8.TabIndex = 8
        Me.Label8.Text = "Diameter (mm)"
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.txtBoomLength)
        Me.GroupBox3.Controls.Add(Me.txtBoomRadius)
        Me.GroupBox3.Controls.Add(Me.Label11)
        Me.GroupBox3.Controls.Add(Me.Label12)
        Me.GroupBox3.Location = New System.Drawing.Point(323, 137)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Size = New System.Drawing.Size(200, 100)
        Me.GroupBox3.TabIndex = 13
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "TailBoom Parameters"
        '
        'txtBoomLength
        '
        Me.txtBoomLength.Location = New System.Drawing.Point(109, 41)
        Me.txtBoomLength.Name = "txtBoomLength"
        Me.txtBoomLength.Size = New System.Drawing.Size(62, 22)
        Me.txtBoomLength.TabIndex = 13
        Me.txtBoomLength.Text = "800.0"
        '
        'txtBoomRadius
        '
        Me.txtBoomRadius.Location = New System.Drawing.Point(109, 22)
        Me.txtBoomRadius.Name = "txtBoomRadius"
        Me.txtBoomRadius.Size = New System.Drawing.Size(62, 22)
        Me.txtBoomRadius.TabIndex = 12
        Me.txtBoomRadius.Text = "22.5"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(23, 41)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(80, 16)
        Me.Label11.TabIndex = 9
        Me.Label11.Text = "Length (mm)"
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(23, 25)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(50, 16)
        Me.Label12.TabIndex = 8
        Me.Label12.Text = "Radius"
        '
        'GroupBox4
        '
        Me.GroupBox4.Controls.Add(Me.txtNoseTireThickness)
        Me.GroupBox4.Controls.Add(Me.txtNoseWheelRadius)
        Me.GroupBox4.Controls.Add(Me.txtNoseStrutLength)
        Me.GroupBox4.Controls.Add(Me.Label14)
        Me.GroupBox4.Controls.Add(Me.Label15)
        Me.GroupBox4.Controls.Add(Me.Label16)
        Me.GroupBox4.Location = New System.Drawing.Point(229, 243)
        Me.GroupBox4.Name = "GroupBox4"
        Me.GroupBox4.Size = New System.Drawing.Size(200, 100)
        Me.GroupBox4.TabIndex = 14
        Me.GroupBox4.TabStop = False
        Me.GroupBox4.Text = "Nose Gear Parameters"
        '
        'txtNoseTireThickness
        '
        Me.txtNoseTireThickness.Location = New System.Drawing.Point(142, 52)
        Me.txtNoseTireThickness.Name = "txtNoseTireThickness"
        Me.txtNoseTireThickness.Size = New System.Drawing.Size(55, 22)
        Me.txtNoseTireThickness.TabIndex = 14
        Me.txtNoseTireThickness.Text = "8.0"
        '
        'txtNoseWheelRadius
        '
        Me.txtNoseWheelRadius.Location = New System.Drawing.Point(142, 36)
        Me.txtNoseWheelRadius.Name = "txtNoseWheelRadius"
        Me.txtNoseWheelRadius.Size = New System.Drawing.Size(55, 22)
        Me.txtNoseWheelRadius.TabIndex = 13
        Me.txtNoseWheelRadius.Text = "25.0"
        '
        'txtNoseStrutLength
        '
        Me.txtNoseStrutLength.Location = New System.Drawing.Point(142, 17)
        Me.txtNoseStrutLength.Name = "txtNoseStrutLength"
        Me.txtNoseStrutLength.Size = New System.Drawing.Size(55, 22)
        Me.txtNoseStrutLength.TabIndex = 12
        Me.txtNoseStrutLength.Text = "250.0"
        '
        'Label14
        '
        Me.Label14.AutoSize = True
        Me.Label14.Location = New System.Drawing.Point(6, 52)
        Me.Label14.Name = "Label14"
        Me.Label14.Size = New System.Drawing.Size(129, 16)
        Me.Label14.TabIndex = 10
        Me.Label14.Text = "Tire Thickness (mm)"
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(6, 34)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(125, 16)
        Me.Label15.TabIndex = 9
        Me.Label15.Text = "Wheel Radius (mm)"
        '
        'Label16
        '
        Me.Label16.AutoSize = True
        Me.Label16.Location = New System.Drawing.Point(6, 18)
        Me.Label16.Name = "Label16"
        Me.Label16.Size = New System.Drawing.Size(109, 16)
        Me.Label16.TabIndex = 8
        Me.Label16.Text = "Strut Length (mm)"
        '
        'GroupBox5
        '
        Me.GroupBox5.Controls.Add(Me.Label5)
        Me.GroupBox5.Controls.Add(Me.txtMainStrutLength)
        Me.GroupBox5.Controls.Add(Me.txtMainWheelRadius)
        Me.GroupBox5.Controls.Add(Me.Label6)
        Me.GroupBox5.Controls.Add(Me.Label9)
        Me.GroupBox5.Location = New System.Drawing.Point(446, 243)
        Me.GroupBox5.Name = "GroupBox5"
        Me.GroupBox5.Size = New System.Drawing.Size(200, 100)
        Me.GroupBox5.TabIndex = 15
        Me.GroupBox5.TabStop = False
        Me.GroupBox5.Text = "Main Gear Parameters"
        '
        'txtMainTireThickness
        '
        Me.txtMainTireThickness.Location = New System.Drawing.Point(587, 300)
        Me.txtMainTireThickness.Name = "txtMainTireThickness"
        Me.txtMainTireThickness.Size = New System.Drawing.Size(53, 22)
        Me.txtMainTireThickness.TabIndex = 14
        Me.txtMainTireThickness.Text = "10.0"
        '
        'txtMainWheelRadius
        '
        Me.txtMainWheelRadius.Location = New System.Drawing.Point(141, 39)
        Me.txtMainWheelRadius.Name = "txtMainWheelRadius"
        Me.txtMainWheelRadius.Size = New System.Drawing.Size(53, 22)
        Me.txtMainWheelRadius.TabIndex = 13
        Me.txtMainWheelRadius.Text = "30.0"
        '
        'txtMainStrutLength
        '
        Me.txtMainStrutLength.Location = New System.Drawing.Point(141, 21)
        Me.txtMainStrutLength.Name = "txtMainStrutLength"
        Me.txtMainStrutLength.Size = New System.Drawing.Size(53, 22)
        Me.txtMainStrutLength.TabIndex = 12
        Me.txtMainStrutLength.Text = "320.0"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(6, 57)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(129, 16)
        Me.Label5.TabIndex = 18
        Me.Label5.Text = "Tire Thickness (mm)"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(6, 39)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(125, 16)
        Me.Label6.TabIndex = 17
        Me.Label6.Text = "Wheel Radius (mm)"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(6, 23)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(109, 16)
        Me.Label9.TabIndex = 16
        Me.Label9.Text = "Strut Length (mm)"
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1200, 646)
        Me.Controls.Add(Me.txtMainTireThickness)
        Me.Controls.Add(Me.GroupBox5)
        Me.Controls.Add(Me.GroupBox4)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.txtLog)
        Me.Controls.Add(Me.btnConnect)
        Me.Margin = New System.Windows.Forms.Padding(4)
        Me.Name = "Form1"
        Me.Text = "Findus Truss UAV Builder"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        Me.GroupBox4.ResumeLayout(False)
        Me.GroupBox4.PerformLayout()
        Me.GroupBox5.ResumeLayout(False)
        Me.GroupBox5.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnConnect As Button
    Friend WithEvents txtLog As TextBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Label3 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents Label1 As Label
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents GroupBox4 As GroupBox
    Friend WithEvents txtBulkheadPositions As TextBox
    Friend WithEvents txtBulkheadFrameThickness As TextBox
    Friend WithEvents txtBulkheadThickness As TextBox
    Friend WithEvents txtBulkheadWidth As TextBox
    Friend WithEvents Label4 As Label
    Friend WithEvents txtLongeronOffset As TextBox
    Friend WithEvents txtLongeronDiameter As TextBox
    Friend WithEvents Label7 As Label
    Friend WithEvents Label8 As Label
    Friend WithEvents txtBoomLength As TextBox
    Friend WithEvents txtBoomRadius As TextBox
    Friend WithEvents Label11 As Label
    Friend WithEvents Label12 As Label
    Friend WithEvents txtNoseTireThickness As TextBox
    Friend WithEvents txtNoseWheelRadius As TextBox
    Friend WithEvents txtNoseStrutLength As TextBox
    Friend WithEvents Label14 As Label
    Friend WithEvents Label15 As Label
    Friend WithEvents Label16 As Label
    Friend WithEvents GroupBox5 As GroupBox
    Friend WithEvents txtMainTireThickness As TextBox
    Friend WithEvents txtMainWheelRadius As TextBox
    Friend WithEvents txtMainStrutLength As TextBox
    Friend WithEvents Label5 As Label
    Friend WithEvents Label6 As Label
    Friend WithEvents Label9 As Label
End Class
