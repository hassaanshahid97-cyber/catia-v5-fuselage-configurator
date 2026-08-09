Option Strict Off
Option Explicit On

Imports System
Imports System.Collections.Generic

''' <summary>
''' Parametric assembly builder for the Findus UAV fuselage.
'''
''' Architecture  — "Reference-Part + CATProduct":
'''   Each structural component type lives in its own CATPart embedded in a
'''   CATProduct.  Parameters are declared once at the top of this class;
'''   changing them drives a full parametric rebuild via Run().
'''
''' PowerCopy usage:
'''   InstantiateFromPowerCopy() shows the complete InstanceFactory pattern
'''   (9-step CATIA API).  Use it once you have hand-built reference .CATPart
'''   files whose PowerCopy templates have been defined interactively in CATIA
'''   (right-click feature → "Define PowerCopy").
'''
''' Fuselage components created:
'''   4 x Bulkhead (hollow annular discs at X stations)
'''   4 x Longeron  (solid rods at ±Y ±Z corners)
'''   4 x Skin segment (hollow cylinder sections, variable wall thickness)
'''   1 x Nose Cone (ogive revolved solid)
'''   1 x Tail Boom (cylindrical extrusion)
'''   1 x Nose Landing Gear (strut + wheel)
'''   1 x Main Landing Gear (dual struts + dual wheels)
''' </summary>
Public Class FuselagePowerCopyBuilder

    '-------------------------------------------------------------------------
    '  CATIA handles
    '-------------------------------------------------------------------------
    Private CATIA      As Object
    Private oDocuments As Object
    Private oProductDoc As Object
    Private oProduct   As Object
    Private oProducts  As Object

    '=========================================================================
    '  REFERENCE PART PATHS
    '  Place the pre-built .CATPart files here.  When a path is set, the
    '  builder opens it and instances it into the product instead of
    '  generating geometry from code.
    '=========================================================================
    Private Const REF_BULKHEAD_PATH As String =
        "D:\Study\software\Catia\Automation123 rep\CATIA VB code automation\Catia Autiomation\reference geometry\Para_bulkhead_V4.CATPart"

    '=========================================================================
    '  FUSELAGE PARAMETERS  (all dimensions in mm)
    '  Modify these constants to drive a full parametric rebuild.
    '=========================================================================

    ' Fuselage cross-section
    Private Const FUSELAGE_OUTER_RADIUS  As Double = 100.0
    Private Const FUSELAGE_INNER_RADIUS  As Double = 90.0   ' bulkhead inner ring

    ' Bulkhead disc
    Private Const BULKHEAD_THICKNESS     As Double = 20.0

    ' Longerons
    Private Const LONGERON_RADIUS        As Double = 1.0    ' 2 mm diameter rod
    Private Const LONGERON_OFFSET        As Double = 65.0   ' corner Y & Z position
    Private Const LONGERON_START_X       As Double = 200.0  ' begins inside nose
    Private Const LONGERON_END_X         As Double = 1220.0 ' last bulkhead + thickness

    ' Tail boom
    Private Const TAIL_BOOM_RADIUS       As Double = 22.5   ' 45 mm diameter
    Private Const TAIL_BOOM_LENGTH       As Double = 800.0

    ' Ogive nose cone
    Private Const NOSE_LENGTH            As Double = 250.0
    Private Const NOSE_PROFILE_SEGMENTS  As Integer = 40

    ' Nose landing gear
    Private Const NGS_STRUT_RADIUS       As Double = 6.0
    Private Const NGS_WHEEL_OUTER_RADIUS As Double = 25.0
    Private Const NGS_WHEEL_INNER_RADIUS As Double = 17.0   ' = outer - tire thickness
    Private Const NGS_STRUT_LENGTH       As Double = 250.0
    Private Const NGS_TIRE_THICKNESS     As Double = 8.0    ' for pad extrusion

    ' Main landing gear
    Private Const MGS_STRUT_RADIUS       As Double = 8.0
    Private Const MGS_WHEEL_OUTER_RADIUS As Double = 30.0
    Private Const MGS_WHEEL_INNER_RADIUS As Double = 20.0
    Private Const MGS_STRUT_LENGTH       As Double = 320.0
    Private Const MGS_WHEEL_OFFSET_Y     As Double = 90.0   ' ±Y position of wheels
    Private Const MGS_TIRE_THICKNESS     As Double = 10.0

    ' Skin segment wall thicknesses (one per segment, front→rear)
    Private ReadOnly SkinThicknesses As Double() = {5.0, 4.0, 3.0, 2.0}

    ' Bulkhead X stations
    Private ReadOnly BulkheadX As Double() = {150.0, 450.0, 850.0, 1200.0}

    ' Skin segment station boundaries  (length = SkinThicknesses.Length + 1)
    Private ReadOnly SkinStations As Double() = {0.0, 150.0, 450.0, 850.0, 1220.0}

    ' Longeron corner labels and their Y / Z offsets
    Private ReadOnly LongeronNames As String()  = {"TopPort",         "TopStbd",          "BotPort",          "BotStbd"}
    Private ReadOnly LongeronY     As Double()  = { LONGERON_OFFSET, -LONGERON_OFFSET,    LONGERON_OFFSET,   -LONGERON_OFFSET}
    Private ReadOnly LongeronZ     As Double()  = { LONGERON_OFFSET,  LONGERON_OFFSET,   -LONGERON_OFFSET,   -LONGERON_OFFSET}

    '=========================================================================
    Private logCallback As Action(Of String)

    Public Sub New(logCb As Action(Of String))
        logCallback = logCb
    End Sub

    Private Sub Log(msg As String)
        logCallback(msg)
    End Sub

    '=========================================================================
    '  PUBLIC ENTRY POINT
    '=========================================================================
    Public Sub Run()
        Try
            Log("==============================================")
            Log("  Findus UAV — Parametric Assembly Builder")
            Log("==============================================")

            ConnectToCATIA()
            CreateProduct()

            Log("[1/7] Bulkheads — using reference part Para_bulkhead_V4...")
            InstantiateReferenceBulkheads()

            Log("[2/7] Longerons (4 corners)...")
            For i As Integer = 0 To 3
                BuildLongeronPart(LongeronNames(i), LongeronY(i), LongeronZ(i))
            Next

            Log("[3/7] Fuselage skin segments (" & SkinThicknesses.Length.ToString() & " segments)...")
            For i As Integer = 0 To SkinThicknesses.Length - 1
                BuildSkinSegmentPart(i + 1, SkinStations(i), SkinStations(i + 1), SkinThicknesses(i))
            Next

            Log("[4/7] Nose cone (ogive profile)...")
            BuildNoseConePart()

            Log("[5/7] Tail boom...")
            BuildTailBoomPart()

            Log("[6/7] Nose landing gear...")
            BuildNoseGearPart()

            Log("[7/7] Main landing gear...")
            BuildMainGearPart()

            Log("Updating product...")
            oProduct.Update()

            Log("")
            Log("==============================================")
            Log("  BUILD COMPLETE")
            Log("  Product : Findus_UAV_Fuselage.CATProduct")
            Log("  Parts   : " & oProducts.Count.ToString() & " components")
            Log("==============================================")

        Catch ex As Exception
            Log("[FATAL] " & ex.Message)
            Log(ex.StackTrace)
        End Try
    End Sub

    '=========================================================================
    '  CONNECT TO CATIA
    '=========================================================================
    Private Sub ConnectToCATIA()
        Log("  Connecting to CATIA V5...")
        CATIA = GetObject(, "CATIA.Application")
        CATIA.Visible = True
        oDocuments = CATIA.Documents
        Log("  OK – " & CATIA.Name)
    End Sub

    '=========================================================================
    '  CREATE CATPRODUCT
    '=========================================================================
    Private Sub CreateProduct()
        Log("  Creating CATProduct: Findus_UAV_Fuselage")
        oProductDoc = oDocuments.Add("Product")
        oProduct = oProductDoc.Product
        oProduct.PartNumber = "Findus_UAV_Fuselage"
        oProducts = oProduct.Products
    End Sub

    '=========================================================================
    '  REFERENCE BULKHEAD INSTANTIATION
    '=========================================================================

    ''' <summary>
    ''' Opens Para_bulkhead_V4.CATPart and adds it to the product once per
    ''' X station.  Each instance is a true reference to the same underlying
    ''' geometry — editing the reference part updates all instances.
    '''
    ''' Positioning assumption: the reference bulkhead geometry is at X=0 in
    ''' its own coordinate system.  Each instance is translated to BulkheadX(i)
    ''' via a 12-element CATIA position matrix [Xx,Xy,Xz, Yx,Yy,Yz, Zx,Zy,Zz, Tx,Ty,Tz].
    ''' </summary>
    Private Sub InstantiateReferenceBulkheads()
        Log("  Opening: " & REF_BULKHEAD_PATH)

        Dim refDoc As Object = oDocuments.Open(REF_BULKHEAD_PATH)
        Dim refProduct As Object = refDoc.Product
        Log("  Reference part: " & refProduct.PartNumber)

        For i As Integer = 0 To BulkheadX.Length - 1
            Dim xPos As Double = BulkheadX(i)
            Log("    Bulkhead_" & (i + 1).ToString() & "  X=" & xPos.ToString() & " mm")

            ' Add this reference part as a new instance in the assembly
            Dim oComp As Object = oProducts.AddComponent(refProduct)
            oComp.Name = "Bulkhead_" & (i + 1).ToString()

            ' Translate to X=xPos, no rotation
            ' CATIA V5 Position matrix (12 elements):
            '   [0-2] X-axis direction  [3-5] Y-axis direction  [6-8] Z-axis direction
            '   [9]   Tx  [10] Ty  [11] Tz
            Dim pos(11) As Double
            pos(0) = 1.0 : pos(1) = 0.0 : pos(2) = 0.0
            pos(3) = 0.0 : pos(4) = 1.0 : pos(5) = 0.0
            pos(6) = 0.0 : pos(7) = 0.0 : pos(8) = 1.0
            pos(9) = xPos : pos(10) = 0.0 : pos(11) = 0.0

            Try
                oComp.Position.SetComponents(pos)
            Catch ex As Exception
                Log("      [WARN] Position.SetComponents failed – trying Move.Apply: " & ex.Message)
                Try
                    oComp.Move.Apply(pos)
                Catch ex2 As Exception
                    Log("      [WARN] Could not set position: " & ex2.Message)
                End Try
            End Try

            ' ---- Optional: drive reference part parameter for X station ----
            ' If Para_bulkhead_V4.CATPart exposes a length parameter for X,
            ' set it here instead of (or in addition to) the assembly translation.
            ' Uncomment and adjust the parameter name as needed:
            '   Try
            '       Dim oPrm = refDoc.Part.Parameters.Item("BulkheadX_mm")
            '       oPrm.Value = xPos
            '       refDoc.Part.Update()
            '   Catch : End Try
        Next

        Log("    " & BulkheadX.Length.ToString() & " bulkhead instances placed.")
    End Sub

    '=========================================================================
    '  SHARED HELPERS
    '=========================================================================

    ''' <summary>
    ''' Add a new Part component to the CATProduct and return its Part object.
    ''' Each component lives in its own CATPart embedded in the product.
    ''' </summary>
    Private Function AddComponentPart(name As String) As Object
        Dim compRef = oProducts.AddNewComponent("Part", "")
        compRef.Name = name
        Dim oPD = oDocuments.Item(compRef.PartNumber & ".CATPart")
        Dim oPart = oPD.Part
        oPart.PartNumber = name
        Return oPart
    End Function

    ''' <summary>
    ''' Return (or create) a HybridBody called "Construction" inside the part.
    ''' Reference planes live here, separate from solid bodies.
    ''' </summary>
    Private Function EnsureConstructionBody(oPart As Object) As Object
        Dim hbs = oPart.HybridBodies
        Try
            Return hbs.Item("Construction")
        Catch
            Dim hb = hbs.Add()
            hb.Name = "Construction"
            Return hb
        End Try
    End Function

    ''' <summary>
    ''' Create an offset plane parallel to the YZ plane at xOffset along X.
    ''' The plane is computed and appended to the given HybridBody.
    ''' Returns the HybridShape plane (use CreateReferenceFromObject to get a ref).
    ''' </summary>
    Private Function MakeOffsetPlaneX(oPart As Object,
                                      oYZPlane As Object,
                                      xOffset As Double,
                                      constrBody As Object) As Object
        Dim oHSF = oPart.HybridShapeFactory
        Dim baseRef = oPart.CreateReferenceFromObject(oYZPlane)
        Dim plane = oHSF.AddNewPlaneOffset(baseRef, xOffset, False)
        oHSF.Compute(plane)
        constrBody.AppendHybridShape(plane)
        Return plane
    End Function

    ''' <summary>
    ''' Create an offset plane parallel to the XY plane at zOffset along Z.
    ''' </summary>
    Private Function MakeOffsetPlaneZ(oPart As Object,
                                      oXYPlane As Object,
                                      zOffset As Double,
                                      constrBody As Object) As Object
        Dim oHSF = oPart.HybridShapeFactory
        Dim baseRef = oPart.CreateReferenceFromObject(oXYPlane)
        Dim plane = oHSF.AddNewPlaneOffset(baseRef, zOffset, False)
        oHSF.Compute(plane)
        constrBody.AppendHybridShape(plane)
        Return plane
    End Function

    '=========================================================================
    '  [1] BULKHEAD PART
    '  Hollow annular disc at X = xPos.
    '  Sketch plane : YZ + offset to xPos
    '  Sketch       : outer circle R=100, inner circle R=90
    '  Feature      : Pad, thickness = 20 mm
    '=========================================================================
    Private Sub BuildBulkheadPart(idx As Integer, xPos As Double)
        Dim name As String = "Bulkhead_" & idx.ToString()
        Log("    " & name & "  X=" & xPos.ToString() & " mm")

        Dim oPart  = AddComponentPart(name)
        Dim oOE    = oPart.OriginElements
        Dim oYZ    = oOE.PlaneYZ
        Dim oCB    = EnsureConstructionBody(oPart)
        Dim oSF    = oPart.ShapeFactory
        Dim oBody  = oPart.Bodies.Item(1)

        Dim oPlane = MakeOffsetPlaneX(oPart, oYZ, xPos, oCB)
        Dim oRef   = oPart.CreateReferenceFromObject(oPlane)

        oPart.InWorkObject = oBody
        Dim oSk = oBody.Sketches.Add(oRef)
        Dim sk2D = oSk.OpenEdition()
        sk2D.CreateCircle(0, 0, FUSELAGE_OUTER_RADIUS)
        sk2D.CreateCircle(0, 0, FUSELAGE_INNER_RADIUS)
        oSk.CloseEdition()

        oSF.AddNewPad(oSk, BULKHEAD_THICKNESS)
        oPart.Update()
    End Sub

    '=========================================================================
    '  [2] LONGERON PART
    '  Solid round rod running along X from LONGERON_START_X to LONGERON_END_X.
    '  Sketch plane : YZ + offset to LONGERON_START_X
    '  Sketch       : single circle at corner (cornerY, cornerZ), R=1 mm
    '  Feature      : Pad, length = LONGERON_END_X - LONGERON_START_X
    '  Note: sketch H-axis = assembly Y, sketch V-axis = assembly Z
    '=========================================================================
    Private Sub BuildLongeronPart(name As String, cornerY As Double, cornerZ As Double)
        Dim fullName As String = "Longeron_" & name
        Log("    " & fullName & "  Y=" & cornerY.ToString() & " Z=" & cornerZ.ToString())

        Dim oPart  = AddComponentPart(fullName)
        Dim oOE    = oPart.OriginElements
        Dim oYZ    = oOE.PlaneYZ
        Dim oCB    = EnsureConstructionBody(oPart)
        Dim oSF    = oPart.ShapeFactory
        Dim oBody  = oPart.Bodies.Item(1)

        ' Start offset plane at LONGERON_START_X
        Dim oPlane = MakeOffsetPlaneX(oPart, oYZ, LONGERON_START_X, oCB)
        Dim oRef   = oPart.CreateReferenceFromObject(oPlane)

        oPart.InWorkObject = oBody
        Dim oSk = oBody.Sketches.Add(oRef)
        Dim sk2D = oSk.OpenEdition()
        ' In a YZ-parallel sketch: H = assembly Y, V = assembly Z
        sk2D.CreateCircle(cornerY, cornerZ, LONGERON_RADIUS)
        oSk.CloseEdition()

        Dim padLen As Double = LONGERON_END_X - LONGERON_START_X
        oSF.AddNewPad(oSk, padLen)
        oPart.Update()
    End Sub

    '=========================================================================
    '  [3] FUSELAGE SKIN SEGMENT PART
    '  Hollow cylinder section from X=x1 to X=x2 with given wall thickness.
    '  Sketch plane : YZ + offset to x1
    '  Sketch       : outer circle R=100, inner circle R=(100-wallT)
    '  Feature      : Pad, length = x2-x1
    '=========================================================================
    Private Sub BuildSkinSegmentPart(idx As Integer,
                                     x1 As Double, x2 As Double,
                                     wallT As Double)
        Dim name As String = "Skin_Seg_" & idx.ToString()
        Log("    " & name & "  X=" & x1.ToString() & "→" & x2.ToString() &
            " mm  wall=" & wallT.ToString() & " mm")

        Dim oPart  = AddComponentPart(name)
        Dim oOE    = oPart.OriginElements
        Dim oYZ    = oOE.PlaneYZ
        Dim oCB    = EnsureConstructionBody(oPart)
        Dim oSF    = oPart.ShapeFactory
        Dim oBody  = oPart.Bodies.Item(1)

        Dim oPlane = MakeOffsetPlaneX(oPart, oYZ, x1, oCB)
        Dim oRef   = oPart.CreateReferenceFromObject(oPlane)

        oPart.InWorkObject = oBody
        Dim oSk = oBody.Sketches.Add(oRef)
        Dim sk2D = oSk.OpenEdition()
        sk2D.CreateCircle(0, 0, FUSELAGE_OUTER_RADIUS)
        sk2D.CreateCircle(0, 0, FUSELAGE_OUTER_RADIUS - wallT)
        oSk.CloseEdition()

        oSF.AddNewPad(oSk, x2 - x1)
        oPart.Update()
    End Sub

    '=========================================================================
    '  [4] NOSE CONE PART
    '  Ogive (circular arc) profile, revolved 360° about the X axis.
    '  Sketch plane : XY plane
    '  Sketch       : NOSE_PROFILE_SEGMENTS straight-line segments approx the
    '                 arc + closing vertical + horizontal axis (Construction)
    '  Feature      : Shaft (revolution solid)
    '  Profile math :  x(t) = L*(1-cos(π/2*t)),  y(t) = R*sin(π/2*t)
    '                  tangent to Y-axis at tip (t=0), tangent to X-axis at base (t=1)
    '=========================================================================
    Private Sub BuildNoseConePart()
        Log("    NoseCone  L=" & NOSE_LENGTH.ToString() & " mm  base_R=" &
            FUSELAGE_OUTER_RADIUS.ToString() & " mm")

        Dim oPart  = AddComponentPart("NoseCone")
        Dim oOE    = oPart.OriginElements
        Dim oXY    = oOE.PlaneXY
        Dim oSF    = oPart.ShapeFactory
        Dim oBody  = oPart.Bodies.Item(1)

        Dim oXYRef = oPart.CreateReferenceFromObject(oXY)
        oPart.InWorkObject = oBody
        Dim oSk = oBody.Sketches.Add(oXYRef)
        Dim sk2D = oSk.OpenEdition()

        ' Ogive profile polyline (H = assembly X, V = assembly Y in XY sketch)
        For seg As Integer = 0 To NOSE_PROFILE_SEGMENTS - 1
            Dim t1 As Double = CDbl(seg)     / CDbl(NOSE_PROFILE_SEGMENTS)
            Dim t2 As Double = CDbl(seg + 1) / CDbl(NOSE_PROFILE_SEGMENTS)
            Dim hx1 As Double = NOSE_LENGTH * (1.0 - Math.Cos(Math.PI / 2.0 * t1))
            Dim hy1 As Double = FUSELAGE_OUTER_RADIUS * Math.Sin(Math.PI / 2.0 * t1)
            Dim hx2 As Double = NOSE_LENGTH * (1.0 - Math.Cos(Math.PI / 2.0 * t2))
            Dim hy2 As Double = FUSELAGE_OUTER_RADIUS * Math.Sin(Math.PI / 2.0 * t2)
            sk2D.CreateLine(hx1, hy1, hx2, hy2)
        Next

        ' Close the profile: vertical line at X=NOSE_LENGTH (base wall)
        sk2D.CreateLine(NOSE_LENGTH, 0.0, NOSE_LENGTH, FUSELAGE_OUTER_RADIUS)

        ' Revolution axis: horizontal line at Y=0, marked Construction
        Dim axisLine = sk2D.CreateLine(0.0, 0.0, NOSE_LENGTH, 0.0)
        axisLine.Construction = True

        oSk.CloseEdition()

        Try
            oSF.AddNewShaft(oSk)
        Catch ex As Exception
            Log("      [WARN] Shaft (revolve) failed – sketch may need axis check: " & ex.Message)
        End Try

        oPart.Update()
    End Sub

    '=========================================================================
    '  [5] TAIL BOOM PART
    '  Solid cylinder from X = tailStartX, length = TAIL_BOOM_LENGTH.
    '  Sketch plane : YZ + offset to tailStartX
    '  Sketch       : circle R = 22.5 mm
    '  Feature      : Pad
    '=========================================================================
    Private Sub BuildTailBoomPart()
        Dim tailStartX As Double = BulkheadX(BulkheadX.Length - 1) + BULKHEAD_THICKNESS
        Log("    TailBoom  X=" & tailStartX.ToString() & "  L=" & TAIL_BOOM_LENGTH.ToString() & " mm")

        Dim oPart  = AddComponentPart("TailBoom")
        Dim oOE    = oPart.OriginElements
        Dim oYZ    = oOE.PlaneYZ
        Dim oCB    = EnsureConstructionBody(oPart)
        Dim oSF    = oPart.ShapeFactory
        Dim oBody  = oPart.Bodies.Item(1)

        Dim oPlane = MakeOffsetPlaneX(oPart, oYZ, tailStartX, oCB)
        Dim oRef   = oPart.CreateReferenceFromObject(oPlane)

        oPart.InWorkObject = oBody
        Dim oSk = oBody.Sketches.Add(oRef)
        Dim sk2D = oSk.OpenEdition()
        sk2D.CreateCircle(0, 0, TAIL_BOOM_RADIUS)
        oSk.CloseEdition()

        oSF.AddNewPad(oSk, TAIL_BOOM_LENGTH)
        oPart.Update()
    End Sub

    '=========================================================================
    '  [6] NOSE LANDING GEAR PART
    '  Strut  : vertical cylinder at (X=gearX, Y=0), from Z=-100 down 250 mm
    '  Wheel  : hollow annular disc on a YZ-parallel plane at X=gearX,
    '           centered at (Y=0, Z=-350), tire thickness = 8 mm
    '
    '  Sketch-plane coordinate notes:
    '    Strut  sketch on XY-parallel plane (normal=+Z): H=X, V=Y in sketch
    '    Wheel  sketch on YZ-parallel plane (normal=+X): H=Y, V=Z in sketch
    '=========================================================================
    Private Sub BuildNoseGearPart()
        Dim gearX  As Double = BulkheadX(0)
        Dim strutZ As Double = -FUSELAGE_OUTER_RADIUS               ' bottom of fuselage
        Dim wheelZ As Double = strutZ - NGS_STRUT_LENGTH            ' wheel centre Z

        Log("    NoseGear  X=" & gearX.ToString() &
            "  strutStartZ=" & strutZ.ToString() &
            "  wheelZ=" & wheelZ.ToString())

        Dim oPart  = AddComponentPart("NoseGear")
        Dim oOE    = oPart.OriginElements
        Dim oXY    = oOE.PlaneXY
        Dim oYZ    = oOE.PlaneYZ
        Dim oCB    = EnsureConstructionBody(oPart)
        Dim oSF    = oPart.ShapeFactory

        '-- Strut body
        Dim oStrutBody = oPart.Bodies.Add()
        oStrutBody.Name = "NoseGear_Strut"

        Dim oStrutPlane = MakeOffsetPlaneZ(oPart, oXY, strutZ, oCB)
        Dim oStrutRef   = oPart.CreateReferenceFromObject(oStrutPlane)

        oPart.InWorkObject = oStrutBody
        Dim oStrutSk = oStrutBody.Sketches.Add(oStrutRef)
        Dim st2D = oStrutSk.OpenEdition()
        ' H = assembly X, V = assembly Y in XY-parallel sketch
        st2D.CreateCircle(gearX, 0.0, NGS_STRUT_RADIUS)
        oStrutSk.CloseEdition()

        Dim oStrutPad = oSF.AddNewPad(oStrutSk, NGS_STRUT_LENGTH)
        oStrutPad.IsDirectionOpposite = True   ' extrude downward (−Z)

        '-- Wheel body
        Dim oWheelBody = oPart.Bodies.Add()
        oWheelBody.Name = "NoseGear_Wheel"

        Dim oWheelPlane = MakeOffsetPlaneX(oPart, oYZ, gearX, oCB)
        Dim oWheelRef   = oPart.CreateReferenceFromObject(oWheelPlane)

        oPart.InWorkObject = oWheelBody
        Dim oWheelSk = oWheelBody.Sketches.Add(oWheelRef)
        Dim wh2D = oWheelSk.OpenEdition()
        ' H = assembly Y, V = assembly Z in YZ-parallel sketch
        wh2D.CreateCircle(0.0, wheelZ, NGS_WHEEL_OUTER_RADIUS)
        wh2D.CreateCircle(0.0, wheelZ, NGS_WHEEL_INNER_RADIUS)
        oWheelSk.CloseEdition()

        oSF.AddNewPad(oWheelSk, NGS_TIRE_THICKNESS)
        oPart.Update()
    End Sub

    '=========================================================================
    '  [7] MAIN LANDING GEAR PART
    '  Two struts  : vertical cylinders at (X=gearX, Y=±90), downward 320 mm
    '  Two wheels  : hollow annular discs on YZ-parallel plane at X=gearX,
    '                centred at (Y=±90, Z=wheelZ), tire thickness = 10 mm
    '=========================================================================
    Private Sub BuildMainGearPart()
        Dim gearX   As Double = BulkheadX(BulkheadX.Length - 1)
        Dim strutZ  As Double = -FUSELAGE_OUTER_RADIUS + 70.0     ' attach point on fuselage bottom
        Dim wheelZ  As Double = strutZ - MGS_STRUT_LENGTH         ' wheel centre Z

        Log("    MainGear  X=" & gearX.ToString() &
            "  strutStartZ=" & strutZ.ToString() &
            "  wheelZ=" & wheelZ.ToString())

        Dim oPart  = AddComponentPart("MainGear")
        Dim oOE    = oPart.OriginElements
        Dim oXY    = oOE.PlaneXY
        Dim oYZ    = oOE.PlaneYZ
        Dim oCB    = EnsureConstructionBody(oPart)
        Dim oSF    = oPart.ShapeFactory

        ' Shared strut sketch plane (XY-parallel at strutZ)
        Dim oStrutPlane = MakeOffsetPlaneZ(oPart, oXY, strutZ, oCB)
        Dim oStrutRef   = oPart.CreateReferenceFromObject(oStrutPlane)

        '-- Left strut (−Y side)
        Dim oLStrutBody = oPart.Bodies.Add()
        oLStrutBody.Name = "MainGear_StrutLeft"
        oPart.InWorkObject = oLStrutBody
        Dim oLSk = oLStrutBody.Sketches.Add(oStrutRef)
        Dim ls2D = oLSk.OpenEdition()
        ls2D.CreateCircle(gearX, -MGS_WHEEL_OFFSET_Y, MGS_STRUT_RADIUS)
        oLSk.CloseEdition()
        Dim oLStrutPad = oSF.AddNewPad(oLSk, MGS_STRUT_LENGTH)
        oLStrutPad.IsDirectionOpposite = True

        '-- Right strut (+Y side)
        Dim oRStrutBody = oPart.Bodies.Add()
        oRStrutBody.Name = "MainGear_StrutRight"
        oPart.InWorkObject = oRStrutBody
        Dim oRSk = oRStrutBody.Sketches.Add(oStrutRef)
        Dim rs2D = oRSk.OpenEdition()
        rs2D.CreateCircle(gearX, MGS_WHEEL_OFFSET_Y, MGS_STRUT_RADIUS)
        oRSk.CloseEdition()
        Dim oRStrutPad = oSF.AddNewPad(oRSk, MGS_STRUT_LENGTH)
        oRStrutPad.IsDirectionOpposite = True

        ' Shared wheel sketch plane (YZ-parallel at gearX)
        Dim oWheelPlane = MakeOffsetPlaneX(oPart, oYZ, gearX, oCB)
        Dim oWheelRef   = oPart.CreateReferenceFromObject(oWheelPlane)

        '-- Left wheel (−Y side)
        Dim oLWheelBody = oPart.Bodies.Add()
        oLWheelBody.Name = "MainGear_WheelLeft"
        oPart.InWorkObject = oLWheelBody
        Dim oLWsk = oLWheelBody.Sketches.Add(oWheelRef)
        Dim lw2D = oLWsk.OpenEdition()
        ' H = assembly Y, V = assembly Z
        lw2D.CreateCircle(-MGS_WHEEL_OFFSET_Y, wheelZ, MGS_WHEEL_OUTER_RADIUS)
        lw2D.CreateCircle(-MGS_WHEEL_OFFSET_Y, wheelZ, MGS_WHEEL_INNER_RADIUS)
        oLWsk.CloseEdition()
        oSF.AddNewPad(oLWsk, MGS_TIRE_THICKNESS)

        '-- Right wheel (+Y side)
        Dim oRWheelBody = oPart.Bodies.Add()
        oRWheelBody.Name = "MainGear_WheelRight"
        oPart.InWorkObject = oRWheelBody
        Dim oRWsk = oRWheelBody.Sketches.Add(oWheelRef)
        Dim rw2D = oRWsk.OpenEdition()
        rw2D.CreateCircle(MGS_WHEEL_OFFSET_Y, wheelZ, MGS_WHEEL_OUTER_RADIUS)
        rw2D.CreateCircle(MGS_WHEEL_OFFSET_Y, wheelZ, MGS_WHEEL_INNER_RADIUS)
        oRWsk.CloseEdition()
        oSF.AddNewPad(oRWsk, MGS_TIRE_THICKNESS)

        oPart.Update()
    End Sub

    '=========================================================================
    '  POWERCOPY INSTANTIATION UTILITY
    '
    '  Call this instead of the Build* methods above once you have created
    '  reference .CATPart files with CATIA PowerCopy definitions:
    '
    '  To create a PowerCopy definition in CATIA (manual step, one time):
    '    1. Open the reference .CATPart
    '    2. Multi-select the features that form one component instance
    '    3. Insert → Knowledge Templates → PowerCopy
    '    4. Name it (e.g. "Bulkhead_Template"), choose input planes
    '    5. Save the .CATPart
    '
    '  Then call this method to instantiate it parametrically:
    '    Dim planes As New Dictionary(Of String, Object)
    '    planes("PlacementPlane") = destPart.FindObjectByName("MyOffsetPlane")
    '    InstantiateFromPowerCopy("C:\Ref\Bulkhead.CATPart", "Bulkhead_Template", destPart, planes)
    '
    '  Parameters:
    '    sourcePartPath : absolute path to the .CATPart that contains the PowerCopy
    '    powerCopyName  : name given to the PowerCopy in CATIA
    '    destinationPart: Part object (from AddComponentPart) to receive the instance
    '    inputRefs      : maps each PowerCopy input name → replacement reference object
    '=========================================================================
    Public Sub InstantiateFromPowerCopy(
            sourcePartPath As String,
            powerCopyName  As String,
            destinationPart As Object,
            inputRefs As Dictionary(Of String, Object))

        Log("  Instantiating PowerCopy '" & powerCopyName & "'")
        Log("    from: " & sourcePartPath)

        ' Step 2: Get InstanceFactory from the destination part
        Dim factory = destinationPart.GetCustomerFactory("InstanceFactory")

        ' Step 3: Point factory at the source .CATPart and named PowerCopy
        factory.BeginInstanceFactory(powerCopyName, sourcePartPath)

        ' Step 4: Start the instantiation transaction
        factory.BeginInstantiate()

        ' Step 5: Supply replacement references for each named PowerCopy input
        For Each kvp As KeyValuePair(Of String, Object) In inputRefs
            factory.PutInputData(kvp.Key, kvp.Value)
        Next

        ' Step 6: Create the instance
        Dim oInstance = factory.Instantiate()

        ' Steps 7–8: Close the transaction and release the template
        factory.EndInstantiate()
        factory.EndInstanceFactory()

        ' Step 9: Propagate to CATIA Part tree
        destinationPart.Update()
        Log("    Instance created successfully.")
    End Sub

End Class
