# GitLab setup — exact commands to run once

This file gives the exact commands to initialise the local Git repository and push it to GitLab LiU for the first time. Run them in order from inside the `TMKT57_FuselageLandingGear/` folder.

---

## 1. Create the project on GitLab LiU

1. Go to **https://gitlab.liu.se/** and log in with your LiU-ID.
2. Click **New project → Create blank project**.
3. Project name: `tmkt57-fuselage-landing-gear`
4. Visibility: **Private** (add teachers later as members).
5. Leave "Initialize repository with a README" **unchecked** (we already have one locally).
6. Press **Create project** and copy the HTTPS remote URL that GitLab shows, for example:
   `https://gitlab.liu.se/<your-liu-id>/tmkt57-fuselage-landing-gear.git`

---

## 2. Initialise the local repo and make the first commit

Open PowerShell or Git Bash in the `TMKT57_FuselageLandingGear/` folder and run:

```bash
# one-time global setup (skip if already done)
git config --global user.name  "Momin Ali Khan"
git config --global user.email "<your-liu-email>@student.liu.se"

# initialise and first commit
git init
git add .
git commit -m "Initial commit: project scaffolding, spec and gitignore"

# link to GitLab and push
git branch -M main
git remote add origin https://gitlab.liu.se/<your-liu-id>/tmkt57-fuselage-landing-gear.git
git push -u origin main
```

---

## 3. Add teachers as members

On GitLab → **Manage → Members → Invite members**. Add:

- Mehdi Tarkian (Examiner) — role **Reporter** is enough for read access.
- Any additional TAs the course asks you to add.

---

## 4. Day-to-day workflow

```bash
# start of each work session
git pull

# during the session, stage and commit often
git add .
git commit -m "W18: first fuselage Frame PowerCopy instantiation working"

# end of session — push to GitLab
git push
```

Try to commit at least **twice per week** so the activity log is dense and clearly shows individual progress. Every commit message should start with the week number (for example `W18:`) and describe what changed, not what was "worked on".

---

## 5. Good commit message examples

```
W17: add fuselage skeleton points (nose, tail, 5 stations) in References.CATPart
W17: add .gitignore rule for CATIA backup files (*.CATPart~)
W18: first Frame_PowerCopy with 1 input plane and circular cross-section
W18: Form1 reads length + numberOfFrames, instantiates Frame_PowerCopy in a loop
W19: extract InstantiatePowerCopy() into CatiaHelpers.vb — removes code duplication
W20: Strut_PowerCopy works + Wheel_PowerCopy instantiation upon Strut
W21: SQLite save / load of configurations, standard configs seeded
W22: poster finalised, report first draft complete
```

---

## 6. What *not* to commit

The `.gitignore` already handles most of this, but as a reminder:

- Do **not** commit `*.CATPart~` or `*.CATProduct~` backup files.
- Do **not** commit `bin/` or `obj/` output of Visual Studio.
- Do **not** commit anything under `CATIA/Instances/` — those are scratch parts that the configurator regenerates.
- Do **not** commit `*.user`, `*.suo`, or the `.vs/` folder.

If you accidentally staged one of these, un-stage it with:

```bash
git rm --cached <file>
```

and commit again.
