#!/usr/bin/env python3
"""
Checks the Unity package files (package.json, .asmdef) for shape and
consistency, since no real Unity stands here to check them itself.

Germio TASK-005.
"""
import json, sys, pathlib, re

ROOT = pathlib.Path(__file__).resolve().parents[2]
errors = []

def check(cond, msg):
    if not cond:
        errors.append(msg)

# 1. package.json is well-formed and holds the required fields
pkg_path = ROOT / "package.json"
check(pkg_path.exists(), "package.json must exist at the repo root")
pkg = json.loads(pkg_path.read_text(encoding="utf-8"))
for field in ["name", "version", "displayName", "unity"]:
    check(field in pkg, f"package.json must hold '{field}'")
check(pkg.get("name") == "com.meowtoon.germio",
      "package.json name must be com.meowtoon.germio, to match what animo's own package.json already asks for")

# 2. Every .asmdef under Scripts/ is well-formed JSON
asmdefs = list((ROOT / "Scripts").rglob("*.asmdef"))
check(len(asmdefs) >= 2, "at least two asmdef files are owed: Germio, Germio.Editor")

by_name = {}
for p in asmdefs:
    data = json.loads(p.read_text(encoding="utf-8"))
    check("name" in data, f"{p}: asmdef must hold a name")
    by_name[data["name"]] = (p, data)

check("Germio" in by_name, "one asmdef must be named Germio")
check("Germio.Editor" in by_name, "one asmdef must be named Germio.Editor")

# 3. Germio.Editor references Germio, and is Editor-only
if "Germio.Editor" in by_name:
    p, data = by_name["Germio.Editor"]
    check("Germio" in data.get("references", []),
          "Germio.Editor must reference Germio")
    check(data.get("includePlatforms") == ["Editor"],
          "Germio.Editor must be Editor-only")

# 4. Germio itself is not Editor-only, and references nothing that would
#    make it depend on any other build
if "Germio" in by_name:
    p, data = by_name["Germio"]
    check(data.get("includePlatforms", []) == [],
          "Germio's own asmdef must not be limited to Editor: it is used at play time too")
    check(data.get("references", []) == [],
          "Germio depends on nothing else; a reference here would be a mistake")

# 5. Every .cs file under Scripts/Editor/ sits inside the Editor asmdef's
#    own folder, and every other .cs file sits under the main one — no file
#    is left outside both.
germio_path = by_name["Germio"][0].parent if "Germio" in by_name else None
editor_path = by_name["Germio.Editor"][0].parent if "Germio.Editor" in by_name else None

for cs in (ROOT / "Scripts").rglob("*.cs"):
    if editor_path and editor_path in cs.parents:
        continue
    if germio_path and germio_path in cs.parents:
        continue
    check(False, f"{cs} sits under neither asmdef")

# 6. Every namespace under Scripts/ starts with Germio (asmdef's
#    rootNamespace), except UnityEngine's own extension pattern
for cs in (ROOT / "Scripts").rglob("*.cs"):
    text = cs.read_text(encoding="utf-8")
    for m in re.finditer(r"^namespace\s+([\w.]+)", text, re.MULTILINE):
        ns = m.group(1)
        check(ns.startswith("Germio") or ns.startswith("UnityEngine"),
              f"{cs}: namespace '{ns}' does not sit under Germio")

# 7. animo's own package.json names the same version this one gives
animo_pkg = ROOT.parent / "animo" / "package.json"
if animo_pkg.exists():
    animo_data = json.loads(animo_pkg.read_text(encoding="utf-8"))
    wanted = animo_data.get("dependencies", {}).get("com.meowtoon.germio")
    check(wanted == pkg["version"],
          f"animo asks for germio {wanted}, but package.json here gives {pkg['version']}")

if errors:
    print(f"FAILED: {len(errors)} check(s)")
    for e in errors:
        print(f"  - {e}")
    sys.exit(1)
else:
    print("OK: every package check passed")
