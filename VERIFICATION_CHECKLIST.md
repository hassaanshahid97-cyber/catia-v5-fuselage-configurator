# BuildSolidFuselage.vb - Verification Checklist

## Code Changes Completed ✓

### 1. Landing Gear Wheel Positioning
- [x] **Nose Gear (Stage 7):** BuildNoseLandingGear() 
  - Uses YZ plane offset by noseX=450
  - Wheel circle positioned at (0.0, bottomZ)
  - Expected result: Nose wheel at X=450, Y=0, Z=-350

- [x] **Main Gear (Stage 8):** BuildMainLandingGear()
  - Uses YZ plane offset by mainX=1200
  - Left wheel at (-wheelOffset, bottomZ) = (-90, bottomZ)
  - Right wheel at (+wheelOffset, bottomZ) = (+90, bottomZ)
  - Expected result: Two wheels symmetrically placed below bulkhead at X=1200

### 2. Aero Nose Ogive Shape
- [x] **Stage 4:** BuildAeroNoseEgg() — *Renamed to Ogive nose*
  - Implements stacking disk method (25 circular slices)
  - Creates YZ planes from X=-250 to X=0
  - **Parabolic ogive profile:** radius = 100 * t² where t = (-X/250)
  - Each slice padded along X-axis
  - Expected result: Smooth pointed ogive nose cone with sharp tip at X=0

### 3. Removed Code
- [x] BuildPropeller() subroutine - REMOVED
- [x] BuildPropellerShaft() subroutine - REMOVED
- [x] No references to propeller building in Run() flow

---

## Test Plan

**To verify the build:**

1. **Run the script in CATIA V5** using the macro runner
2. **Check Debug output** for any errors in Stages 4, 7, or 8
3. **Verify in CATIA CAD view:**

   **Nose Landing Gear Check:**
   - Navigate to bulkhead at X=450mm
   - Find single strut extending downward (negative Z)
   - At bottom of strut, verify wheel is at position:
     - X = 450mm
     - Y = 0mm  
     - Z = -350mm (or calculated bottomZ value)
   
   **Main Landing Gear Check:**
   - Navigate to bulkhead at X=1200mm
   - Verify two struts extending downward symmetrically
   - Left wheel should be at (1200, -90, bottomZ)
   - Right wheel should be at (1200, +90, bottomZ)
   
   **Aero Nose Check:**
   - At front of fuselage (X=-250 to X=0)
   - Verify smooth egg-shaped 3D cone
   - Tip should be sharp (near X=0)
   - Base should be full radius (near X=-250)

---

## Known Parameters

- **BulkheadWidth:** 200mm (radius = 100mm)
- **Nose strut length:** 250mm
- **Main strut length:** 250mm
- **Nose wheel radius:** 25mm + 8mm tire
- **Main wheel radius:** 30mm + 10mm tire
- **Main gear wheel offset:** 90mm (left/right spacing)
