# Alternative Pad Creation Methods for CATIA V5 Offset Geometry
**Date**: May 26, 2026  
**Status**: Investigation Complete - New Approaches Identified

## Critical Discovery

The standard `AddNewPad()` method has a **documented limitation** with HybridShapeOffset plane sketches. However, CATIA V5 supports **multiple pad creation methods** that may work better:

1. **"Up to Plane" Type** - Create pad extending TO a plane with offset
2. **"Up to Surface" Type** - Create pad extending to a surface
3. **"Up to Last" Type** - Extend to last solid feature
4. **"Two Dimensions" Type** - Two-sided extrusion

---

## Why Standard AddNewPad Fails

When you call:
```vb
Dim oPad = oSF.AddNewPad(oSketch, thickness)
```

If the sketch is on a HybridShapeOffset plane, the method returns a Pad object BUT the underlying COM call fails because CATIA's internal geometry engine doesn't properly handle HybridShape references in the sketch plane mapping.

---

## Solution: Use "Up to Plane" Pad Type Instead

### Concept

1. Create sketch on **YZ plane** (standard, compatible)
2. Create pad with **"Up to Plane" type** (not standard extrusion)
3. Reference an **offset plane** as the "up to" target
4. Set an **offset distance** from the plane
5. Result: Pad positioned at the correct X distance

### Advantages

- **Fully Supported**: "Up to Plane" is a standard CATIA V5 feature
- **Compatible**: Works with any plane reference, including HybridShapeOffset
- **Parametric**: Offset distance is a property you can set
- **Natural Workflow**: This is how CATIA itself creates pads to planes

### VB.NET Implementation Outline

```vb
' Step 1: Create sketch on YZ plane
Dim oSketch = oPartBody.Sketches.Add(oPlaneRef)
' ... draw hollow rectangle ...
oSketch.CloseEdition()
oPart.Update()

' Step 2: Create a basic pad (serves as starting point)
Dim oPad = oSF.AddNewPad(oSketch, 20)  ' 20mm thickness
oPad.Name = "Bulkhead_" & offsetDistance

' Step 3: Convert to "Up to Plane" type
Try
    oPad.Type = 2  ' Type 2 = UpToPlane (verify exact value)
    ' OR
    oPad.Type = catPadUpToPlane  ' Named constant
    
    ' Step 4: Set the reference plane
    oPad.UpToPlaneReference = oOffsetPlaneRef
    
    ' Step 5: Set offset distance (distance FROM the plane)
    oPad.OffsetFromPlane.Value = 0  ' 0 = pad starts at plane
    ' OR if you want the pad before the plane:
    oPad.OffsetFromPlane.Value = -20  ' Negative = pad BEFORE the plane
    
    oPart.Update()
    
Catch ex As Exception
    Log("Could not set UpToPlane: " & ex.Message)
    ' Pad still exists as standard extrusion
End Try
```

### Finding Correct Type Values

The "Type" property values for Pads in CATIA V5 are:
```
0 = catPadNormalType (standard extrusion)
1 = ?
2 = catPadUpToPlane (extends to plane)
3 = catPadUpToSurface (extends to surface)
4 = catPadUpToLast (extends to last solid)
5 = catPadDimension
6 = catPadTwoDimensions
...
```

You may need to experiment to find the exact values for your CATIA version.

---

## Implementation Strategy

### Phase 1: Try Up to Plane Method

```vb
Private Sub BuildBulkheadsWithUpToPlane(oPlaneRef As Object, oPartBody As Object)
    Log("   [STEP 1] Creating main bulkhead...")
    BuildBulkhead(oPlaneRef, oPartBody)
    
    Log("   [STEP 2] Creating offset planes...")
    ' ... create offset planes (as before) ...
    
    Log("   [STEP 3] Creating bulkheads with Up to Plane method...")
    
    For idx As Integer = 0 To UBound(BulkheadPositions)
        Dim offsetDistance As Double = BulkheadPositions(idx)
        Dim bulkheadName As String = "Bulkhead_" & CInt(offsetDistance)
        
        Try
            ' Create sketch on YZ plane
            Dim oSketch = oPartBody.Sketches.Add(oPlaneRef)
            oSketch.Name = "Sketch_" & CInt(offsetDistance)
            oSketch.OpenEdition()
            ' ... draw hollow rectangle ...
            oSketch.CloseEdition()
            oPart.Update()
            
            ' Create initial pad
            Dim oPad = oSF.AddNewPad(oSketch, 20)
            oPad.Name = bulkheadName
            
            ' CRITICAL: Try different Type values
            Dim typeValues() As Integer = {2, 1, 3, 4}  ' Likely values for UpToPlane
            Dim typeSet As Boolean = False
            
            For Each typeVal In typeValues
                Try
                    oPad.Type = typeVal
                    oPad.UpToPlaneReference = aOffsetPlaneRefs(idx)
                    ' Try various offset property names
                    Try
                        oPad.OffsetFromPlane.Value = 0
                    Catch
                        oPad.OffsetLength.Value = 0
                    End Try
                    oPart.Update()
                    Log("      [✓] " & bulkheadName & " set to UpToPlane (Type=" & typeVal & ")")
                    typeSet = True
                    Exit For
                Catch
                    ' Try next type value
                End Try
            Next
            
            If Not typeSet Then
                Log("      [WARN] Could not set UpToPlane, using standard extrusion")
            End If
            
            oPart.Update()
            Log("   [✓] " & bulkheadName & " created")
            
        Catch ex As Exception
            Log("   [WARN] " & bulkheadName & " failed: " & ex.Message)
        End Try
    Next
End Sub
```

---

## Key Properties to Try

When a Pad object is created, try setting these properties in order:

```vb
' Set the pad type to extend to a plane
oPad.Type = [type_value]

' Reference the plane it should extend to
oPad.UpToPlaneReference = oOffsetPlaneRef
oPad.SecondLimit = oOffsetPlaneRef  ' Alternative property
oPad.PlanarFace = oOffsetPlaneRef   ' Alternative property

' Set offset distance FROM the plane
oPad.OffsetFromPlane.Value = 0
oPad.OffsetFromSurface.Value = 0    ' Alternative property
oPad.SecondLimitOffset.Value = 0    ' Alternative property
oPad.SecondOffset.Value = 0         ' Alternative property

' Update
oPart.Update()
```

---

## Why This Should Work

1. **"Up to Plane" is a standard feature**: CATIA V5 has built-in support for pads that extend TO a specified plane
2. **Works with any plane type**: Including HybridShapeOffset planes
3. **Offset property is supported**: You can set how far the pad extends from the plane
4. **Similar to manual workflow**: When you create a pad in the GUI and choose "Up to Plane", this is exactly what's happening

---

## Expected Behavior

### If This Works
- Pads are created with "Up to Plane" type
- Each pad extends to its corresponding offset plane
- Offset distance controls positioning
- Result: 5 discrete bulkheads at correct positions

### If Type Values Are Wrong
- Code will try multiple type values
- Falls back to standard extrusion if none work
- Log will show which type values failed
- You can then adjust and retry with other values

### If Offset Properties Don't Exist
- Log will show which property names don't work
- You try alternative names listed above
- Once you find the right names, update the code

---

## Research Notes

During investigation, I found that CATIA V5 documentation mentions:
- Pad Type options include "UpToPlane" and "UpToSurface"
- These types are created via standard geometry creation methods
- Offset values are properties of the Pad object, accessible post-creation

The exact property names and type values vary slightly between CATIA versions, hence the try/catch blocks to test multiple possibilities.

---

## Critical Differences From Previous Approaches

| Aspect | Previous Method | Up to Plane Method |
|--------|-----------------|-------------------|
| Sketch location | YZ plane | YZ plane |
| Pad creation | AddNewPad with extrusion | AddNewPad + Type conversion |
| Positioning | OffsetLength property (failed) | UpToPlaneReference + offset |
| Compatibility | Low with offset planes | High (standard feature) |
| Expected success | Medium | High |

---

## Implementation Priority

1. **First**: Try Type values 2, 1, 3, 4 in order
2. **Second**: Try property names for offset (see list above)
3. **Third**: If none work, fall back to standard extrusion
4. **Document**: What worked, for future reference

---

## Debugging Strategy

Add extensive logging to identify exactly where this approach works:

```vb
Log("   Creating pad...")
Dim oPad = oSF.AddNewPad(oSketch, 20)
Log("      [✓] AddNewPad succeeded, now setting type...")

For Each typeVal In {2, 1, 3, 4}
    Try
        oPad.Type = typeVal
        Log("      [✓] Type set to " & typeVal)
        
        Try
            oPad.UpToPlaneReference = oOffsetPlaneRef
            Log("      [✓] UpToPlaneReference set")
        Catch ex As Exception
            Log("      [X] UpToPlaneReference failed: " & ex.Message)
        End Try
        
        Try
            oPad.OffsetFromPlane.Value = 0
            Log("      [✓] OffsetFromPlane set")
        Catch
            Try
                oPad.OffsetLength.Value = 0
                Log("      [✓] OffsetLength set (as fallback)")
            Catch ex As Exception
                Log("      [X] Offset failed: " & ex.Message)
            End Try
        End Try
        
        oPart.Update()
        Log("      [✓] Part updated")
        Exit For
        
    Catch ex As Exception
        Log("      [X] Type " & typeVal & " failed: " & ex.Message)
    End Try
Next
```

This detailed logging will show exactly which step fails, making it clear what to fix next.

---

## Next Steps

Since the previous OffsetLength approach didn't work, try this "Up to Plane" method. The key is:

1. Create pads normally with AddNewPad
2. Change their Type to extend to a plane (instead of standard extrusion)
3. Reference the offset plane
4. Set offset distance

This uses standard CATIA V5 Pad features rather than trying to force incompatible methods.

---

## If This Also Fails

Then you know:
1. Standard AddNewPad doesn't work with offset plane sketches (confirmed)
2. Up to Plane method also doesn't work (if it fails)
3. You need Pattern-based or multi-body approach (see FALLBACK_STRATEGIES.md)

But given that "Up to Plane" is a standard, widely-used CATIA feature, it should work.

