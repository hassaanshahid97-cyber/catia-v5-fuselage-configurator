# Fuselage and Landing Gear Configurator

A parametric CATIA V5 + Visual Studio (VB.NET) configurator developed as part of the course **TMKT57 Product Modelling** at Linköping University, Spring 2026.

The present project aims to automate the repetitive design work which is usually involved in generating different configurations of an aircraft fuselage and its landing gear. The user is able to specify key parameters such as fuselage length, cross-section, nose and tail style, landing gear type and wheel arrangement through a graphical user interface, and the model is automatically regenerated in CATIA V5.

> **Grade aimed: 5**

---

## Course context

| Field | Value |
|---|---|
| Course | TMKT57 Product Modelling (6 hp) |
| Programme | M.Sc. Aeronautical Engineering |
| University | Linköping University |
| Semester | VT 2026 (Spring 2026) |
| Student | Momin Ali Khan |
| Project type | tream project |

---

## What this project does

On a practical level, the configurator does three things:

1. It takes user inputs from a Visual Studio form (fuselage length, diameter, number of frames, landing gear type, etc.).
2. It drives the CATIA V5 model through the VB.NET API, by modifying parameters and instantiating PowerCopy templates along the fuselage skeleton.
3. It saves and loads complete configurations from a local SQLite database, which allows the user to build up a small library of standard aircraft layouts.

The main focus is on **efficient modelling**, not 100% automation. The goal, as stated in the course introduction lecture, is to remove the non-creative and repetitive parts of design so that the engineer can give more heed to the creative and complex parts.

---

## Folder layout

```
TMKT57_FuselageLandingGear/
├── README.md                       ← you are here
├── .gitignore                      ← VS + CATIA ignore rules
│
├── docs/                           ← written deliverables & screenshots
│   ├── ProjectDescription_FuselageLandingGear.docx
│   ├── WeeklyLog.md                ← weekly activity log (Grade 5)
│   └── images/                     ← CATIA and GUI screenshots
│
├── CATIA/                          ← CATIA source files
│   ├── References/
│   ├── PowerCopies/
│   ├── Assembly/
│   └── Instances/                  ← scratch instances (git-ignored)
│
├── reference geometry/             ← team reference CATIA parts
│   └── OneDrive_1_5-27-2026/
│       ├── Sachin/                 ← Sachin's manual reference parts
│       └── Hassaan/                ← Hassaan's VS project snapshots
│
├── pictures/                       ← 145 development screenshots
│
├── BuildSolidFuselage.vb           ← main automation module (current)
├── FuselagePowerCopyBuilder.vb     ← PowerCopy-based parametric builder
├── Form1.vb / Form1.Designer.vb    ← Windows Forms UI
├── BuildSolidFuselage_*.vb         ← iteration snapshots (see log below)
├── FIXED/                          ← finalized version with fix notes
│
├── Poster/                         ← A1 poster for the final presentation
└── Report/                         ← final project report
```

---

## How to build and run

### 1. Prerequisites

- CATIA V5 (R21 or later)
- Visual Studio 2019 / 2022 with the **.NET desktop development** workload
- `System.Data.SQLite` NuGet package (Grade 5 feature)
- Git (for version control and GitLab push / pull)

### 2. Open the solution

Open `Catia Autiomation.slnx` in Visual Studio. Restore NuGet packages if prompted.  
Check that the following CATIA Interop references are present with `Embed Interop Types = False`:
`INFITF`, `MECMOD`, `PARTITF`, `ProductStructureTypeLib`, `KnowledgewareTypeLib`, `CATIA_APP_ITF`

### 3. Open CATIA in the correct way

Start CATIA V5. Create a new empty part or open `CATIA\Assembly\Fuselage.CATProduct`.  
Make sure *Automatic update* is ON (Tools → Options → General → Automatic).

### 4. Run the configurator

Press **F5** in Visual Studio. The Windows Forms window opens. Click **"Build Solid Fuselage"** — geometry streams into CATIA in real time. Save from CATIA (`Ctrl+S`) when done.

---

## Weekly progress (also tracked in `docs/WeeklyLog.md`)

- [x] **W16** — Form project idea, contact teachers, first draft sent
- [x] **W17** — Final project specification submitted, CATIA reference skeleton started
- [x] **W18** — First fuselage PowerCopy + first working VS GUI (instantiating frames)
- [x] **W19** — Advanced tutorials complete. **Grade 3 target reached.**
- [x] **W20** — Landing gear strut + wheel automation, correct COM interop. **Grade 4 target reached.**
- [x] **W21** — Wheel orientation fixed, full pipeline verified, documentation complete
- [ ] **W22** — Poster, final report, examination / presentation

---

## Important course rules I am following

- **No Hybrid Design** anywhere in the CATIA model.
- **PowerCopy parts are stored separately** in `CATIA/PowerCopies/`, never inside the active product.
- **Save-safe workflow**: the number of instances is set to zero before any `.CATProduct` is saved.
- **External References cleanup** is done through the `Clear History` command described in the LE2 VB lecture.
- **Weekly GitLab activity** is maintained so that the examination criteria for Grade 5 are clearly met.

---

## Credits

- **Course**: Dr. Mehdi Tarkian, Dr. Mehrdad Tehrani, Linköping University — Department of Management and Engineering (IEI).
- **VS template and code snippets**: provided through the course material (`TMKT57_IE_template`, Code Snippets folder).
- **CATIA API documentation**: [catiadoc.free.fr](http://catiadoc.free.fr/online/interfaces/CAAMasterIdx.htm).
- **AI assistance**: Claude was used as a coding assistant during development, in line with LiU's guideline Dnr LiU-2023-02660 on the use of generative AI in education. The scope of this assistance is disclosed in the final project report.

---

---

# Implementation Log — Team Workflow (May 2026)

The sections below document how **Momin, Hassaan, and Sachin** worked together to implement and iterate on the UAV fuselage automation over three intensive days (25–27 May 2026). Screenshots from `pictures/` are embedded at each stage to show the actual state of CATIA and Visual Studio at the time.

---

## Team

| Member | Contribution |
|--------|-------------|
| **Momin Ali Khan** | VB.NET automation lead, landing gear implementation, wheel orientation fixes |
| **Hassaan** | Parametric builder, PowerCopy approach, two independent VS project versions |
| **Sachin** | Manual reference CATIA geometry (bulkheads, longerons, skin, product assembly) |

We worked as a team across every stage — design decisions, debugging sessions, and code reviews were all done collaboratively.

---

## Iteration 1 — Initial Project Setup (25 May 2026, morning)

We started by agreeing on the project scope: a VB.NET Windows Forms application driving CATIA V5 through its COM API. Sachin set up the initial CATIA part files manually so we had a visual reference to aim for. Hassaan scaffolded the Visual Studio solution, and Momin began writing `BuildSolidFuselage.vb`.

At this stage the team could connect to a running CATIA session, create a new part, and extrude a single pad — a working end-to-end pipeline from day one.

![Project start – first CATIA connection](pictures/Screenshot%202026-05-25%20124154.png)

![First pad extruded successfully](pictures/Screenshot%202026-05-25%20140909.png)

---

## Iteration 2 — Bulkheads & Longerons (25 May 2026, afternoon)

We tackled the five hollow bulkheads. Each bulkhead is a rectangular frame at a specific X station. The team first tried sketching on `HybridShapeOffset` planes (offset from the YZ plane), but `AddNewPad()` refused to accept those planes as sketch supports — throwing a type-mismatch COM error.

After debugging together we switched strategy: sketch always on the standard YZ plane, then use the `Pad.OffsetLength` property to slide the resulting solid to its correct X position. This unlocked all five bulkheads at once. The four corner longerons followed the same approach.

![Debugging bulkhead offset planes error](pictures/Screenshot%202026-05-25%20162234.png)

![Five bulkheads generated at correct X stations](pictures/Screenshot%202026-05-25%20190246.png)

---

## Iteration 3 — Fuselage Skin & Aero Nose (25–26 May 2026, evening / night)

Once the structural frame was solid the team added the outer geometry:

- **Aerodynamic nose**: a revolved ogive profile 250 mm long ahead of BH-1
- **Tail boom**: a rectangular pad extending 200 mm behind BH-5
- **Segmented skin**: four variable-thickness panels (5 → 4 → 3 → 2 mm) between adjacent bulkheads, created with loft features driven by bulkhead sketches

The loft work exposed another COM issue — passing `Nothing` to the optional third argument of `AddSectionToLoft()` raised "parameter not optional" at runtime. The fix: omit the argument entirely (VB.NET COM interop behaves differently from VBScript in this regard).

![Aero nose revolved profile](pictures/Screenshot%202026-05-25%20234354.png)

![Full fuselage shell with skin segments](pictures/Screenshot%202026-05-26%20000901.png)

---

## Iteration 4 — Landing Gear Struts (26 May 2026, early hours)

With the fuselage body done the team moved to the undercarriage. Two new methods were added to `BuildSolidFuselage.vb`:

- **`BuildNoseLandingGear()`** — a single vertical strut (6 mm dia) dropping 250 mm from BH-2 (X = 450 mm)
- **`BuildMainLandingGear()`** — twin struts (8 mm dia) at ±90 mm lateral offset from BH-4 (X = 1 200 mm), dropping 320 mm

Both struts are circles sketched on an XY plane at the bottom of the strut, then padded upward by the strut length. This worked first time.

![Nose gear strut generated](pictures/Screenshot%202026-05-26%20025128.png)

![Both main gear struts in position](pictures/Screenshot%202026-05-26%20034943.png)

---

## Iteration 5 — Wheel Orientation Fixes (26 May 2026, evening)

Getting the wheels right took several attempts. The first version drew circles on XY planes and padded them in Z — producing flat horizontal discs, not proper tyres.

We needed the wheels **extruded in the XZ plane** (wheel face visible when viewed from the side). After investigating the CATIA COM API together the team found:
- `OriginElements.PlaneXZ` does **not** exist as a property
- `HybridShapeFactory.AddNewPlaneThroughPoints()` is also absent

**Solution**: offset the YZ plane to the X coordinate of each gear station. A sketch on this plane uses (Y, Z) as its natural axes, so a circle centred at `(±wheelOffset, bottomZ)` sits exactly at the strut base and the pad extrudes in X — giving each wheel correct depth and orientation when viewed from the side.

![Flat-disc wheels — incorrect orientation](pictures/Screenshot%202026-05-26%20212031.png)

![Wheel orientation corrected on YZ-offset plane](pictures/Screenshot%202026-05-26%20214326.png)

![Both main gear wheels correctly oriented](pictures/Screenshot%202026-05-26%20215214.png)

---

## Iteration 6 — Final Assembly & Cleanup (27 May 2026)

In the final session the team pulled everything together:

- Removed all dead code (leftover `AddNewPlaneThroughPoints` calls and orphan point geometry)
- Increased tyre thickness to `tireThickness × 2.0` (nose: ×3) so wheels look solid from all angles
- Verified the full build pipeline runs start-to-finish without errors in CATIA
- Cleaned up body/feature naming, added final documentation, prepared the repository

The completed model shows a coherent UAV fuselage with all structural members, smooth skin, and a complete tricycle undercarriage.

![Final model – full fuselage and landing gear](pictures/Screenshot%202026-05-27%20001222.png)

![Build log – all stages pass](pictures/Screenshot%202026-05-27%20025946.png)

![Clean CATIA tree with all bodies named](pictures/Screenshot%202026-05-27%20031536.png)

---

## Key Technical Decisions

| Challenge | Approach | Why |
|-----------|----------|-----|
| Sketch on offset planes | Sketch on YZ plane + `Pad.OffsetLength` for X | `AddNewPad()` rejects `HybridShapeOffset` planes via COM |
| `AddSectionToLoft` 3rd arg | Omit argument entirely | VB.NET: passing `Nothing` → "parameter not optional" (0x8002000F) |
| XZ-plane wheels | Offset YZ plane to gear X, circle at `(±Y, Z)` | No `PlaneXZ` in `OriginElements`; no `AddNewPlaneThroughPoints` in `HybridShapeFactory` |
| Wheel thickness | `tireThickness × 2` (nose: ×3) pad | Ensures wheels look solid from all viewing angles |

---

## Documentation Index

| File | Contents |
|------|----------|
| `START_HERE.txt` | 5-minute orientation guide |
| `QUICK_REFERENCE.txt` | Parameter lookup and common modifications |
| `BUILD_SUMMARY.txt` | Build process and stage overview |
| `IMPLEMENTATION_SUMMARY_v2.md` | Bulkhead OffsetLength fix technical detail |
| `FIX_IMPLEMENTATION_OffsetLength.md` | Deep-dive into the YZ plane approach |
| `FALLBACK_STRATEGIES.md` | Alternative methods if primary approach fails |
| `ALTERNATIVE_PAD_METHODS.md` | Other pad creation strategies explored |
| `BULKHEAD_FIX_SUMMARY.md` | Summary of bulkhead geometry fixes |
| `TEST_CHECKLIST.txt` | Manual test checklist |
| `VERIFICATION_CHECKLIST.md` | Quick verification steps |
| `PROJECT_COMPLETION_REPORT.txt` | Executive project completion summary |
| `FIXED/FIX_SUMMARY.txt` | COM interop fix details (loft + optional params) |

---

*Last updated: 27 May 2026*
