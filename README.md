<div align="center">

# CATIA V5 Fuselage and Landing Gear Configurator

**Parametric UAV fuselage and landing gear generation, driven from a VB.NET GUI through the CATIA V5 COM API**

[![Platform](https://img.shields.io/badge/CATIA-V5%20R21%2B-005386?style=flat-square&logo=dassaultsystemes&logoColor=white)](http://catiadoc.free.fr/online/interfaces/CAAMasterIdx.htm)
[![Language](https://img.shields.io/badge/VB.NET-.NET%20Framework-512BD4?style=flat-square&logo=dotnet&logoColor=white)](#how-to-build-and-run)
[![Course](https://img.shields.io/badge/TMKT57-Product%20Modelling-0b5394?style=flat-square)](#course-context)
[![Licence](https://img.shields.io/badge/licence-MIT-green?style=flat-square)](LICENSE)

</div>

---

Generating a new fuselage configuration by hand in CAD is repetitive work that scales badly. This
configurator removes the non-creative part of that loop: the user specifies fuselage length,
cross-section, nose and tail style, landing gear type and wheel arrangement in a Windows Forms
interface, and the complete parametric model is regenerated inside CATIA V5.

The design goal is **efficient modelling, not total automation**. The repetitive geometry is
generated; the engineer keeps the design decisions.

---

## What it does

1. Takes user inputs from a VB.NET form: fuselage length, diameter, number of frames, landing gear
   type and payload data.
2. Drives the CATIA V5 model through the COM API, modifying parameters and instantiating PowerCopy
   templates along the fuselage skeleton.
3. Computes centre of gravity and landing gear X-stations dynamically from the payload inputs.
4. Saves and loads complete configurations from a local SQLite database, so a library of standard
   layouts can be built up and recalled.

### Generated geometry

| Feature | Implementation |
|---|---|
| Bulkheads | Five hollow rectangular frames, sketched on the YZ plane and positioned by `Pad.OffsetLength` |
| Longerons | Four corner members following the same sketch-and-offset strategy |
| Aerodynamic nose | Revolved ogive profile, 250 mm ahead of bulkhead 1 |
| Tail boom | Rectangular pad extending 200 mm behind bulkhead 5 |
| Skin | Four loft-driven panels of variable thickness, 5 to 2 mm, between adjacent bulkheads |
| Nose landing gear | Single 6 mm strut dropping 250 mm from bulkhead 2 at X = 450 mm |
| Main landing gear | Twin 8 mm struts at plus and minus 90 mm lateral offset from bulkhead 4 at X = 1200 mm, dropping 320 mm |

A full account of how the geometry pipeline was developed, including the COM interop problems that
shaped it, is in **[IMPLEMENTATION_LOG.md](IMPLEMENTATION_LOG.md)**.

---

## Repository layout

```
.
├── BuildSolidFuselage.vb          main automation module
├── FuselagePowerCopyBuilder.vb    PowerCopy-based parametric builder
├── Form1.vb / Form1.Designer.vb   Windows Forms user interface
├── Catia Autiomation.vbproj       Visual Studio project
├── IMPLEMENTATION_LOG.md          development log with screenshots
├── archive/                       superseded iteration snapshots of the builder
├── docs/                          project description, weekly log, developer notes
│   └── dev-notes/                 debugging notes, fallback strategies, checklists
├── reference geometry/            team reference CATIA parts
├── pictures/                      development screenshots
└── video/                         demonstration video, poster and presentation
```

---

## How to build and run

### Prerequisites

- CATIA V5 R21 or later
- Visual Studio 2019 or 2022 with the .NET desktop development workload
- `System.Data.SQLite` NuGet package
- Git

### Steps

1. Open `Catia Autiomation.slnx` in Visual Studio and restore NuGet packages if prompted.
2. Confirm the CATIA Interop references are present with `Embed Interop Types = False`:
   `INFITF`, `MECMOD`, `PARTITF`, `ProductStructureTypeLib`, `KnowledgewareTypeLib`, `CATIA_APP_ITF`.
3. Start CATIA V5, then create a new empty part or open `CATIA\Assembly\Fuselage.CATProduct`.
   Ensure automatic update is on: Tools, Options, General, Automatic.
4. Press F5 in Visual Studio. Click **Build Solid Fuselage** in the form. Geometry streams into the
   running CATIA session in real time. Save from CATIA when the build completes.

---

## Modelling rules followed

- No Hybrid Design anywhere in the CATIA model.
- PowerCopy parts are stored separately in `CATIA/PowerCopies/`, never inside the active product.
- Save-safe workflow: the instance count is set to zero before any `.CATProduct` is saved.
- External reference cleanup is done through the `Clear History` command.

---

## Course context

| Field | Value |
|---|---|
| Course | TMKT57 Product Modelling, 6 hp |
| Programme | M.Sc. Aeronautical Engineering |
| University | Linköping University, Department of Management and Engineering (IEI) |
| Semester | VT 2026 |
| Examiners | Dr Mehdi Tarkian, Dr Mehrdad Tehrani |
| Project type | Team project |

### Team

| Member | Contribution |
|---|---|
| Momin Ali Khan | VB.NET automation lead, landing gear implementation, wheel orientation fixes |
| Hassaan Shahid | Parametric builder, PowerCopy approach, two independent Visual Studio project versions |
| Sachin | Manual reference CATIA geometry: bulkheads, longerons, skin, product assembly |

Design decisions, debugging sessions and code reviews were done collaboratively.

---

## Credits and disclosure

- Visual Studio template and code snippets provided through the course material (`TMKT57_IE_template`).
- CATIA API documentation: [catiadoc.free.fr](http://catiadoc.free.fr/online/interfaces/CAAMasterIdx.htm).
- Generative AI was used as a coding assistant during development, in line with Linköping
  University's guideline Dnr LiU-2023-02660 on the use of generative AI in education. The scope of
  that assistance is disclosed in the final project report.

---

## Licence

Source code released under the [MIT licence](LICENSE). CATIA geometry, course material and the
project report remain the property of their respective owners.

Related work: [engineering portfolio](https://github.com/hassaanshahid97-cyber/engineering-portfolio) ·
[project page](https://github.com/hassaanshahid97-cyber/engineering-portfolio/blob/main/projects/parametric-uav-configurator.md)
