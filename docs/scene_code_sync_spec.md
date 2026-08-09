# Scene Code Sync — specification

> **Written on**: 2026-05-04
> **Author**: Claude (tactics) / under the master's strategy
> **Status**: built and in use (`SceneCodeSyncer.cs`, `SceneCodeSyncMenu.cs`)

---

## 1. Summary

### 1.1 Aim

Add a tool that, with one button press in the Unity Editor, builds and
updates the C# Scene class files under `Assets/Scripts/Scenes/` from the
Node tree in `Assets/StreamingAssets/germio.json` (from here on, germio.json).

### 1.2 The problem to fix

Right now, germio.json and the C# Scene classes are kept in step **by hand,
in full**. Each time the master changes the Node tree in germio.json — adds
one, takes one out, moves it under a new parent, renames its `scene` value,
or renames its `id` — the matching C# classes must be rewritten by hand.
This brings the following cost:

+ When a Node is added, a C# Scene class must be hand-written (about 30
  lines of set boilerplate per file)
+ When a Node is removed, the matching C# Scene class does not go away by
  itself. It stays as dead code, easy to miss.
+ When a Node moves to a new parent, the C# side (the `class Foo : Bar`
  line) and the file's place on disk must both be moved by hand to match.
+ When a `scene` value changes, the C# class name, file name, `.cs.meta`
  file, and the Unity Scene link must all be kept in step by hand.
+ When an `id` changes, the string inside
  `[GermioSceneHandler(id: "...")]` must be kept in step by hand.

These problems come up **every time the master tries out a new design**,
and pull attention away from the true work: thinking through the game's
flow (the `rules` and `next` in germio.json).

### 1.3 The plan for a fix

Treat germio.json as the **one source of truth**. The C# Scene classes are
built from it. A new Unity Editor menu item, `Tools/Germio/Sync Scene
Code`, runs a Generator that reads germio.json, works out what has changed
against the C# files on disk, and makes the needed files, renames, moves,
and attribute updates.

The user's (the master's) **own work in the C# files — method bodies,
fields, `using` lines, XML doc comments — is kept safe at all times**. The
Generator only ever changes four exact kinds of line in a file. It never
reads or writes anything else.

### 1.4 What this document is for

This is the agreed plan before work on Phase_5_19_1 begins. It holds the
reasons behind each choice and the full test plan. Once a point is set
here, it does not change during the work. If a new question comes up
while working, a new section is added to this document for it.

---

## 2. Terms

| Term | Sense |
| --- | --- |
| **Node** | A unit in the tree under `root` in germio.json. It holds `id`, `name`, `kind`, `scene`, `children`, `next`, and `rules` |
| **Scene** | In this document, not the Unity Scene (the `.unity` file) but the **C# Scene class** (a `.cs` file under `game/Assets/Scripts/Scenes/`). Where this could be unclear, the text says "Unity Scene" or "Scene class" in full |
| **handler** | A method in a C# Scene class marked with `[GermioSceneHandler(id: "...")]`. The one whose `id` matches is called by reflection, from root down to leaf, when that Scene starts |
| **the `[GermioSceneHandler]` attribute** | Set already in `Plugins/Germio/Scripts/Scene.cs`. Takes one string, `id`. Used to link a Node to its handler |
| **leaf Node** | A Node with no `children` (or an empty list) and a non-empty `scene`. Stands for one Unity Scene, one to one |
| **branch Node** | A Node with a non-empty `children` list. Its `scene` is empty. It has no matching Unity Scene, but it still gets a C# Scene class (the point from which its child handlers are called) |
| **scene value** | The string in a Node's `scene` field. Matches a Unity Scene name and its name in Build Settings |
| **id value** | The string in a Node's `id` field. Must be unique across the whole scenario (checked by Validator V004) |
| **Generator** | The new class this phase adds, `SceneCodeSyncer`. Reads germio.json and builds or updates the C# files from it |
| **Generator-owned lines** | The lines in a C# file that the Generator reads and writes. This spec fixes them at four kinds |
| **touches** | The Generator reads that line or area, and may write to it |
| **does not touch** | The Generator never reads or writes that line or area |
| **orphan** | A class on the C# side whose handler `id` has no matching Node in germio.json any more |
| **sync** | The act of making germio.json and the C# Scene classes match, done by the Generator |
| **idempotent** | Running the same input through a tool any number of times gives the same result each time. A must for this Generator |

---

## 3. The problem, in four parts

The problem is split into four numbered points. The numbers stay the same
through this whole document, and match the test cases too.

### 3.1 #1 — Auto-build C# when a Node is added

**Now**: once the master adds a new Node to germio.json, the matching C#
Scene class (class line, parent line, and the handler with its attribute)
must be hand-written. The shape never changes, but there is a lot of it,
and hand work here is a common place for mistakes.

**After the fix**: on button press, the Generator finds each Node in
germio.json with no matching handler attribute on the C# side, and builds
the set skeleton file for it. What gets built follows the template in
§6.4.

### 3.2 #2 — Find orphan C# when a Node is removed

**Now**: if the master removes a Node from germio.json, the matching C#
Scene class stays. At start-up, the only sign is a log line saying "no
handler for node 'xxx'" — nothing marks the code itself as dead. Coming
back to it later, there is no way to tell if it is still needed.

**After the fix**: when the Generator finds an `id` on the C# side with no
matching Node in germio.json, it puts one line —
`// germio: orphan (removed from germio.json on YYYY-MM-DD)` — right
before the class line (or does nothing, if that line is there already).
Deleting the file is left to the user; the Generator never deletes it on
its own.

### 3.3 #3 — Keep the parent line and folder in step when a Node's parent changes

**Now**: if the master moves a Node under a new parent in germio.json, the
following on the C# side must be kept in step by hand:

+ the class's parent line (`class Foo : OldParent` → `class Foo :
  NewParent`)
+ its place on disk (`Scenes/World/Levels/Foo.cs` →
  `Scenes/World/BossLevels/Foo.cs`)
+ its `.cs.meta` file's place

**After the fix**: the Generator finds the parent change, rewrites the one
parent line, moves the file and its `.cs.meta` to the new folder, and, if
the old folder is now empty, removes it along with its own `.meta`. Method
bodies and fields are not touched.

### 3.4 #4 — Keep the class name and file name in step when a `scene` value changes

**Now**: if the master changes a `scene` value in germio.json (say, `Title`
→ `TitleScreen`), the following must be kept in step by hand:

+ the C# class name (`class Title` → `class TitleScreen`)
+ the C# file name (`Title.cs` → `TitleScreen.cs`)
+ the `.cs.meta` file name (`Title.cs.meta` → `TitleScreen.cs.meta`)
+ the handler method name (`OnTitle()` → `OnTitleScreen()`, fixed by Q5-(b))

**After the fix**: the Generator rewrites only the class line and the
method line, and renames the file plus its meta. Method bodies, fields,
`using` lines, and the rest are not touched.

### 3.5 Out of scope (later phase candidates)

These are not covered in this phase. They may be looked at in a later,
stand-alone phase.

+ Checking that a Unity Scene file (`.unity`) matches a Node's `scene`
  value (a candidate for Validator V028)
+ Checking that a Scene listed in Build Settings matches a `scene` value
  in germio.json
+ Checking that a `bus.Publish(signal_id: "...")` string on the C# side
  matches a `rule.trigger` in germio.json
+ Finding an `id` rename as such (right now it is seen as "new Node plus
  old orphan", set out plainly in §6.6)
+ Checking that `next[].id` / `request_transition` values in germio.json
  point somewhere real (already found by Validator V006)

---

## 4. The plan

### 4.1 germio.json is the one source of truth

The base rule of this whole plan. The C# Scene classes are built from
germio.json. If the two do not match, germio.json is **always** taken to
be right, and the sync brings the C# side in line with it.

Exception: if the user hand-edits `[GermioSceneHandler(id:"x")]` on the C#
side to some other `id`, the Generator logs a warning but does not fix it
on its own. This could mean the user meant to cut that C# file loose from
the JSON on purpose, so the Generator never takes that kind of action
(see §6.5).

### 4.2 Match by the `[GermioSceneHandler(id:"...")]` attribute already there

No new mark (such as a UUID) is added. The Generator reads the `id` value
already held in this attribute, and matches it as a plain string against
a Node's `id` in germio.json.

#### 4.2.1 Why no UUID

| Point | With a UUID | Without one (this plan) |
| --- | --- | --- |
| Auto-follow a Node `id` rename | possible | not possible (seen as new + orphan) |
| File/class name rename | held by the fixed UUID | held by the fixed `id` |
| Folder move | held by the fixed UUID | held by the fixed `id` |
| Schema change | needed | not needed |
| Spread to other documents | large (cookbook, save_data_format, llm_first_design — all of them) | none |
| Effect on LLM prompts | "do not touch the UUID" must go in every prompt | none |
| Validator work | a new check needed (UUID form, no duplicates) | not needed (V004 already checks `id` for duplicates) |
| Generator build cost | must give out, track, and sync UUID v4 values | not needed |

The one problem a UUID would fix is **auto-following a Node `id` rename**.
This is rare, and an IDE's find-and-replace handles it well enough. The
cost of a UUID is far more than what it would buy, so this plan does not
use one.

### 4.3 Generator-owned lines: fixed at four kinds

Out of a whole C# file, the Generator only ever writes the following four
kinds of line. Each of the four can be found safely with one regular
expression; none of them ever spans a block or more than one line.

| # | Name | Pattern (in words) | Example |
| --- | --- | --- | --- |
| L1 | namespace line | `^\s*namespace\s+\w+\s*\{` | `namespace GameDev {` |
| L2 | class line | `^\s*public\s+class\s+\w+\s*:\s*\w+\s*\{` | `public class Title : World {` |
| L3 | handler attribute line | `^\s*\[GermioSceneHandler\(id:\s*"[^"]*"\)\]` | `[GermioSceneHandler(id: "title")]` |
| L4 | handler method line (right after L3) | `^\s*protected\s+void\s+On\w+\(\)\s*\{` | `protected void OnTitle() {` |

The Generator **never reads or writes** anything but these four kinds of
line. To be exact:

+ a method body (from its `{` to the matching `}`) is never read
+ field lines are never read
+ `using` lines are never read
+ XML doc comments are never read
+ the Copyright line at the top of the file is never read

This means the Generator needs no full C# parser (such as Roslyn), which
keeps its build very plain. It also gives a strong, built-in promise that
the user's own work is safe: what the Generator never reads, it can never
break.

#### 4.3.1 What is touched and what is not

Looking at the real files Select.cs and Levels.cs, here is how each part
sorts:

| Kind | Example | Touched? |
| --- | --- | --- |
| (1) the class line's shape (class name, parent name) | `class Title : World` | **touched** |
| (2) the handler attribute `[GermioSceneHandler(id:"...")]` | `id="title"` | **touched** |
| (3) the handler method line | `protected void OnTitle()` | **touched** |
| (4) the handler method's body | `_sound_system = Find(...).Get<SoundSystem>(); ...` | **not touched** |
| (5) class fields | `[SerializeField] Image? _easy;` | **not touched** |
| (6) methods that are not handlers | `void changeSelectedColor()` | **not touched** |
| (7) `using` lines | `using UnityEngine;` | **not touched** |
| (8) the `namespace` line | `namespace GameDev` | **touched** |
| (9) the class's XML doc | `/// <summary>...</summary>` | **not touched** |
| (10) the Copyright line at the top | `// Copyright (c) STUDIO MeowToon` | **not touched** |

The Generator writes only (1), (2), (3), and (8). It fully leaves (4),
(5), (6), (7), (9), and (10) alone.

### 4.4 Matches the files already in place

The eight C# Scene classes now in place (`World.cs`, `Title.cs`,
`Select.cs`, `Ending.cs`, `Levels.cs`, `Level1.cs`, `Level2.cs`,
`Level3.cs`) already follow the four Generator-owned line kinds set out
in this plan. The first time the Generator runs, none of them will
change (checked by idempotency test Z2).

---

## 5. Fixed rules

### 5.1 Naming rules

#### 5.1.1 Class name

| Input | Rule | Example |
| --- | --- | --- |
| leaf Node (`scene` set) | turn the `scene` value into PascalCase, with underscores taken out | `scene="Level_1"` → `Level1` |
| branch Node (`scene` empty) | turn the `id` value into PascalCase | `id="levels"` → `Levels` |
| root | turn the `id` value into PascalCase | `id="world"` → `World` |

Turning a value into PascalCase: split it at each underscore, put the
first letter of each part in capitals, keep the rest as it is, then join
the parts with no underscore between them.

| Input | Result |
| --- | --- |
| `title` | `Title` |
| `Title` | `Title` |
| `level_1` | `Level1` |
| `Level_1` | `Level1` |
| `boss_levels` | `BossLevels` |
| `world` | `World` |

#### 5.1.2 File name

Matches the class name in full, with the ending `.cs`. Example: class
`Level1` → file `Level1.cs`.

#### 5.1.3 Folder

Follows the parent/child shape of the Nodes in germio.json. Each branch
Node gets its own sub-folder, named after its class (§5.1.1).

| Node | Folder path |
| --- | --- |
| `world` (root) | `Assets/Scripts/Scenes/` |
| `world/title` | `Assets/Scripts/Scenes/World/Title.cs` |
| `world/levels` | `Assets/Scripts/Scenes/World/Levels.cs` |
| `world/levels/level_1` | `Assets/Scripts/Scenes/World/Levels/Level1.cs` |

The root Node's own file goes right under `Assets/Scripts/Scenes/`
(`World.cs`).

#### 5.1.4 namespace

Every Scene class is under one flat `namespace GameDev`. No layers.

Reason: it matches the eight files already in place (all under `namespace
GameDev`). Since Validator V004 already makes sure every `id` is unique
across the whole scenario, there is no need for a namespace layer to
keep names from clashing.

#### 5.1.5 Parent line

A child Node's C# class has the parent Node's C# class as its parent.

| Node | Parent Node | Parent line |
| --- | --- | --- |
| `world` | (none, it is the root) | `: Scene` (the Germio base class) |
| `title` | `world` | `: World` |
| `level_1` | `levels` | `: Levels` |

The root Node's parent class is always `Scene` (`Germio.Scene`).

### 5.2 Handler rules

#### 5.2.1 Attribute

Each Scene class has exactly one method marked with
`[GermioSceneHandler(id: "...")]`, holding its own Node's `id`.

```csharp
[GermioSceneHandler(id: "title")]
protected void OnTitle() {
    // user implementation
}
```

#### 5.2.2 Method name rule

A handler method's name is `On<PascalCase(id)>()`. No arguments, `void`
return, `protected`.

| Node `id` | Method name |
| --- | --- |
| `world` | `OnWorld` |
| `title` | `OnTitle` |
| `level_1` | `OnLevel1` |
| `boss_levels` | `OnBossLevels` |

#### 5.2.3 How the method line is found (fixed by Q6)

The Generator **treats the line right after L3 (the attribute line) as
L4 (the method line)**. This keeps it apart from any other method with no
attribute (such as `void changeSelectedColor()`).

If a blank line or a comment sits between the attribute line and the
method line, the Generator treats the method as missing and adds a new
one. It never removes such blank lines or comments.

### 5.3 The exact patterns for Generator-owned lines

The four line kinds (L1 to L4) shown in §4.3 are given here as the exact
regular expressions used in the build. All are written for C# regex.

```text
L1 (namespace):
    ^\s*namespace\s+(?<ns>[\w\.]+)\s*\{?\s*$

L2 (class declaration):
    ^\s*(?<modifiers>public\s+)?class\s+(?<class>\w+)\s*:\s*(?<parent>\w+)\s*\{?\s*$

L3 (handler attribute):
    ^\s*\[GermioSceneHandler\(\s*id\s*:\s*"(?<id>[^"]*)"\s*\)\s*\]\s*$

L4 (handler signature, must follow L3):
    ^\s*(?<modifiers>protected\s+)?void\s+(?<name>On\w+)\s*\(\s*\)\s*\{?\s*$
```

Any line that is not one of these four is out of the Generator's reach.

---

## 6. How the Generator works

### 6.1 Input

| Input | Needed? | What it is |
| --- | --- | --- |
| `germio.json` | yes | `Assets/StreamingAssets/germio.json`. Stops if it breaks Validator V004 |
| the `Scenes/**/*.cs` files now in place | no | if there, used to work out what has changed; if not, every Node is treated as new |
| the `Scenes/**/*.cs.meta` files now in place | no | moved or renamed along with their `.cs` file |
| the `Scenes/**/*.meta` files for folders | no | moved along with their folder |

### 6.2 The steps

```text
[Step 1] Read germio.json and run it through the Validator
    + if V004 fails (duplicate id), stop and give back the ValidationResult
    + any other Validator error is shown as a warning log, and work goes on
    + only moves on to Step 2 once V004 has passed

[Step 2] Walk the Node tree and work out what each Node's C# side should look like
    + the file path it should have (§5.1.3)
    + the class name it should have (§5.1.1)
    + the parent line it should have (§5.1.5)
    + the attribute id it should have (the same as the Node's own id)
    + the method name it should have (§5.2.2)

[Step 3] Walk the C# files now in place, and build an index by handler attribute id
    + read L3 out of each file, building a map from id value to file path
    + more than one file with the same id is a Generator error (§6.5)
    + a file whose L3 cannot be read (broken) gets a warning log and is skipped

[Step 4] Sort each Node into one of T1-T10 and act on it (§6.3)
    + no matching C# found for a Node → T1 or T2 (build new)
    + matching C# found in the right place → T4, T7, T8, or T9
    + matching C# found in the wrong place → T5, T7', or T8
    + see §6.3.1 to §6.3.10 for how each Tn is handled

[Step 5] Find orphans
    + from the files found in Step 3, pull out any whose id has no matching Node
    + run T10 on each one found

[Step 6] Remove empty folders
    + find any folder left empty by a move in Step 4 or Step 5
    + remove each empty folder, along with its own .meta

[Step 7] Print a summary log
    + built: N
    + renamed: N
    + moved: N
    + updated: N
    + orphans found: N
    + errors: N
```

### 6.3 The ten behaviours, T1 to T10

#### 6.3.1 T1 — New Node, folder not yet made

**When**: a new Node is in germio.json, there is no matching C# file, and
its parent folder does not exist yet either.

**What happens**:

1. Build all needed folders, working down from the parent Node
2. For each folder, leave its `.meta` to Unity (it is built on its own
   next time the Editor starts)
3. Build the skeleton `.cs` file (template in §6.4)

#### 6.3.2 T2 — New Node, folder already made

**When**: a new Node is in germio.json, there is no matching C# file, but
its parent folder already exists.

**What happens**:

1. Skip building the folder
2. Build the skeleton `.cs` file (template in §6.4)

#### 6.3.3 T3 — Node still in germio.json, C# file missing

**When**: the Node is still in germio.json, but its matching C# file was
deleted by hand.

**What happens**:

1. Build a new skeleton file, the same as in T1 or T2
2. Print a log line: `[INFO] Recreated <file> for node '<id>' (was
   deleted manually)`
3. This is only a note to the user; the fix itself is the same as a plain
   new build

#### 6.3.4 T4 — Node unchanged, C# in the right place

**When**: neither germio.json nor the C# side has changed, and the file is
where it should be.

**What happens**: nothing. The file's mtime does not change. **The core of
idempotency.**

#### 6.3.5 T5 — Node unchanged, C# in the wrong place

**When**: germio.json has not changed, but the C# file sits somewhere
other than its right folder (moved by hand, or left over from a past
parent change that was never followed through).

**What happens**:

1. Move the C# file to its right place
2. Move its `.cs.meta` along with it (keeps the Unity GUID)
3. If the old folder is now empty, it is removed in Step 6 of §6.2

#### 6.3.6 T6 — Node still in germio.json, more than one matching C# file

**When**: more than one C# file holds the same `id` attribute (the user
copy-pasted by mistake).

**What happens**:

1. The Generator logs an error: `[ERROR] Duplicate
   [GermioSceneHandler(id:"<id>")] in: <file1>, <file2>, ...`
2. Work on that Node is skipped (no fix is made on its own)
3. The sync is not seen as done for that Node until the user fixes the
   duplicate by hand

#### 6.3.7 T7 — `scene` value changed (class name and file name follow)

**When**: the `scene` value changed in germio.json, and the C# file is in
its right place.

**What happens**:

1. Read the whole file
2. Rewrite L2 (the class line) with the new class name (one line only)
3. Rewrite L4 (the handler method line) with the new method name (one
   line only)
   + **fixed by Q5-(b)**: an `id` rename together with a `scene` rename
     also carries the method name along with it
4. Rename the file to the new class name
5. Rename its `.cs.meta` to match

Everything else (method body, fields, `using`, XML doc) is left alone.

#### 6.3.8 T7' — `scene` change plus C# in the wrong place (T5 and T7 together)

**What happens**: run T7 first, then move the file to its right place. An
empty folder left over is removed in Step 6 of §6.2.

#### 6.3.9 T8 — Node's parent changed (parent line and folder follow)

**When**: the Node's parent changed in germio.json, and the C# file is in
its old, right-for-before place.

**What happens**:

1. Read the whole file
2. Rewrite L2's parent name with the new parent class name (one line
   only)
3. Move the file and its `.cs.meta` to the new folder
4. If the old folder is now empty, it is removed in Step 6 of §6.2

#### 6.3.10 T9 — Only `name`, `kind`, `rules`, or `next` changed (identity unchanged)

**When**: in germio.json, the Node's `id`, `scene`, and parent stay the
same; only `name`, `kind`, `rules`, or `next` changed.

**What happens**: nothing. None of these four fields shape the C# side, so
the file's mtime does not change.

#### 6.3.11 T10 — Node removed (mark as orphan)

**When**: the Node was removed from germio.json, but its matching C#
handler attribute is still there.

**What happens**:

1. Put one line, `// germio: orphan (removed from germio.json on
   YYYY-MM-DD)`, right before the C# file's class line (L2)
2. If a line starting with `// germio: orphan` is already right there, do
   nothing (idempotent)
3. The file is not deleted (left to the user)

### 6.4 The template for a new skeleton `.cs` file

```csharp
// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

using Germio;

namespace GameDev {
    /// <summary>
    /// Scene controller for the <NodeId> node (id="<NodeId>").
    /// Generated by Germio SceneCodeSyncer (Phase 5.19).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class <ClassName> : <ParentClassName> {
#nullable enable

        [GermioSceneHandler(id: "<NodeId>")]
        protected void <HandlerName>() {
            GermioLog.Write(message: "[<ClassName>] <HandlerName> invoked");
            // Empty placeholder. Add <ClassName>-specific logic here.
        }
    }
}
```

Fill-in values:

+ `<NodeId>`: the Node's `id` value
+ `<ClassName>`: the class name worked out in §5.1.1
+ `<ParentClassName>`: the parent Node's class name (`Scene` for the root)
+ `<HandlerName>`: the method name worked out in §5.2.2

### 6.5 Errors and what happens

| Error | Reported through | What happens |
| --- | --- | --- |
| Validator V004 (duplicate id) | `UnityEditor.Debug.LogError` | the whole sync stops |
| Same `id` attribute in more than one C# file | `UnityEditor.Debug.LogError` | only that Node is skipped |
| An `id` on the C# side with no match in the JSON | `UnityEditor.Debug.LogWarning` | T10 runs (marked as orphan) |
| A C# file could not be read | `UnityEditor.Debug.LogError` | only that file is skipped |
| A folder could not be built, moved, or removed | `UnityEditor.Debug.LogError` | only that Node is skipped |

### 6.6 How an `id` rename is handled (set out plainly)

In this plan, a Node `id` rename is **treated as a new Node plus an old
orphan**. Here is why:

+ the Generator only knows how to compare germio.json's current state
  against the C# side's current state
+ it holds no way to watch germio.json change over time (that would need
  the Generator to keep a saved history — which means a UUID)
+ since this plan does not add a UUID, there is no way to "follow" an
  `id` rename

If the user wants to rename an `id`, one of these two paths is best:

1. Rename the `id` in germio.json
2. Rename the matching C# file by hand (the attribute `id`, the class
   name, and the file name all changed to match the new `id`)
3. Run the Generator (the renamed file now matches the new `id`, and is
   handled as T4 or T9)

Or:

1. Rename the `id` in germio.json
2. Run the Generator (the old `id`'s C# file becomes an orphan; a new C#
   file is built for the new `id`)
3. The user copies over whatever logic is needed, from the orphan file
   into the new C# file
4. The user deletes the orphan file

### 6.7 How meta files (`.cs.meta`, folder `.meta`) are handled

#### 6.7.1 `.cs.meta`

Unity builds a `.cs.meta` file for each `.cs` file, and keeps a **GUID**
inside it. The GUID is how Unity ties an Inspector reference (say, a
GameObject set in a SerializeField) back to the right file.

+ On file rename: `.cs.meta` is renamed to match. **Its contents (the
  GUID) never change.**
+ On file move: `.cs.meta` moves along with it.
+ On building a new file: no `.cs.meta` is built. Unity builds it on its
  own, next time the Editor starts.
+ On file delete: this phase never deletes a file, so this case does not
  come up.

#### 6.7.2 Folder `.meta`

Unity also gives each folder a `<foldername>.meta` file.

+ On building a new folder: no `.meta` is built. Unity builds it on its
  own.
+ On removing a folder (once it is empty): the folder itself and its
  `.meta` are removed together.

### 6.8 Keeping line endings and BOM as they are

When rewriting a file, the Generator **keeps** that file's line endings
(CRLF or LF) and BOM (with or without a UTF-8 BOM) as they were. Its
template uses LF with no BOM, but an update to an existing file follows
that file's own form.

The default for a new file is LF with no BOM (Unity's own default).

### 6.9 Keeping modifiers and attributes as they are

When L2 (the class line) or L4 (the method line) is rewritten, any
modifiers already there (`public`, `partial`, `abstract`, and so on) and
any attributes are kept. The Generator **only captures the modifier part
with its regular expression, and rewrites just the class name, parent
name, or method name**.

Example:

```text
in:  public partial class Title : World {
out: public partial class TitleScreen : World {  ← "partial" kept
```

---

## 7. Settled questions

| # | Question | Answer | Reason / instruction |
| --- | --- | --- | --- |
| Q1 | Should branch Nodes get a C# class too | (a) yes, file named after the `id` | the master's instruction. Matches the files already in place (Levels.cs, World.cs) |
| Q2 | Rule for turning a `scene` value into a file name | (b) PascalCase, underscores taken out | the master's instruction. Matches the files already in place (Level1.cs) |
| Q3 | Should a `scene` rename carry the class name along | (a) yes | the master's instruction |
| Q4 | Should the namespace be layered | (i) flat `GameDev` | the master's instruction: "work within the limits given" |
| Q5 | Should an `id` rename carry the method name along | (b) yes | the master's instruction: "pick whichever helps the user more" |
| Q6 | Is the method line the one right after the attribute line | yes | the master's instruction |

---

## 8. Decision table

### 8.1 The axes

#### Axis A: state of the Node on the JSON side

| Value | State |
| --- | --- |
| A1 | new (the `id` has no match on the C# side) |
| A2 | already there, unchanged |
| A3 | already there, `scene` changed (class/file name follows) |
| A4 | already there, parent changed (parent line/folder follows) |
| A5 | already there, only `name`/`kind`/`rules`/`next` changed (identity unaffected) |
| A6 | removed |

#### Axis B: state of the file on the C# side (matched by `id` attribute)

| Value | State |
| --- | --- |
| B1 | no `.cs` with that `id`, and no folder either |
| B2 | no `.cs` with that `id`, but the folder exists |
| B3 | a `.cs` with that `id` is where it should be |
| B4 | a `.cs` with that `id` is where it should not be |
| B5 | more than one `.cs` with that `id` |

#### Axis C: whether the user has made edits

| Value | Meaning |
| --- | --- |
| C1 | the handler method body has the user's own logic in it |
| C2 | there are non-handler methods, fields, or `using` lines |
| C3 | no edits (same as right after it was built) |

#### Axis D: folder actions

| Value | Action |
| --- | --- |
| D1 | building a new, multi-layer folder |
| D2 | removing an empty folder left by a move |
| D3 | keeping a folder that still holds other files |

#### Axis E: line endings and BOM

| Value | State |
| --- | --- |
| E1 | the file already there is CRLF, no BOM |
| E2 | the file already there is LF, no BOM |
| E3 | the file already there has a UTF-8 BOM |

#### Axis F: keeping modifiers and attributes

| Value | Meaning |
| --- | --- |
| F1 | modifiers on the class line (`partial`, `abstract`, and so on) |
| F2 | keeping `using` lines |

#### Axis G: Unity meta files

| Value | Meaning |
| --- | --- |
| G1 | `.cs.meta` renamed to follow |
| G2 | `.cs.meta` moved to follow |
| G3 | `.cs.meta`'s contents (the GUID) never change |
| G4 | a new folder's `.meta` is left to Unity |
| G5 | an empty folder's `.meta` is removed with it |

#### Axis X: catching errors

| Value | Meaning |
| --- | --- |
| X1 | duplicate `id` attribute on the C# side |
| X2 | an `id` on the C# side with no match in the JSON |
| X3 | stop on a Validator V004 failure on the JSON side |
| X4 | an `id` rename is treated as new-plus-orphan |

#### Axis Z: idempotency

| Value | Meaning |
| --- | --- |
| Z1 | running twice in a row gives no changes |
| Z2 | the first run on an already-in-place project gives no changes |

### 8.2 The A × B table (30 cells)

| A↓ \ B→ | B1 neither there | B2 folder only | B3 right place | B4 wrong place | B5 more than one |
| --- | --- | --- | --- | --- | --- |
| **A1 new** | T1 | T2 | n/a | n/a | n/a |
| **A2 unchanged** | T3 | T3 | T4 | T5 | T6 |
| **A3 scene changed** | T3 | T3 | T7 | T7' | T6 |
| **A4 parent changed** | T3 | T3 | T8 | T8 | T6 |
| **A5 name etc. changed** | T3 | T3 | T9 | T5 | T6 |
| **A6 removed** | — | — | T10 | T10 | T6 |

**Ten** distinct behaviours in all: T1 through T10.

### 8.3 Checking each axis on its own

Axes C, D, E, F, G, X, and Z are each checked together with one of the
behaviours from the A × B table above. See §9 for the full test list.

---

## 9. Test list

**32** tests in all. Each test checks exactly one behaviour. Test names
stay short, in the shape `<Feature>_<Behavior>`.

### 9.1 The ten behaviours, T-series (10 tests)

| # | Test name | What it checks | Related Tn |
| --- | --- | --- | --- |
| 1 | `Sync_AssignsNewFile_OnNewNode` | a skeleton `.cs` is built for a new Node, folder too | T1 |
| 2 | `Sync_AssignsNewFile_OnNewNodeInExistingDir` | new Node, folder already there, only the `.cs` is built | T2 |
| 3 | `Sync_RecreatesFile_WhenManuallyDeleted` | an existing Node's C# was deleted by hand → built again | T3 |
| 4 | `Sync_DoesNothing_OnUnchangedNode` | an existing Node unchanged → file mtime unchanged | T4 |
| 5 | `Sync_MovesFile_WhenInUnexpectedDir` | Node unchanged, C# in the wrong place → moved to the right place | T5 |
| 6 | `Sync_ReportsError_OnDuplicateIdInCSharp` | the same `id` attribute in more than one file → error reported | T6 |
| 7 | `Sync_RenamesFileAndClass_OnSceneChange` | `scene` change keeps file name, class name, method name, and attribute `id` in step | T7 |
| 8 | `Sync_RenamesAndMoves_OnSceneAndParentChange` | `scene` change plus wrong place → renamed and moved | T7' |
| 9 | `Sync_MovesAndUpdatesInheritance_OnParentChange` | parent change updates the parent line, moves the folder, removes an empty one | T8 |
| 10 | `Sync_DoesNothing_OnNonIdentityChange` | only `name`/`kind`/`rules`/`next` changed → C# side unchanged | T9 |

### 9.2 Node removed and orphan (1 test)

| # | Test name | What it checks | Related Tn |
| --- | --- | --- | --- |
| 11 | `Sync_MarksOrphan_OnNodeRemoved` | Node removed from germio.json → C# gets a marker line, file stays | T10 |

### 9.3 Keeping the user's edits safe, C-series (3 tests)

| # | Test name | What it checks | Related axes |
| --- | --- | --- | --- |
| 12 | `Sync_PreservesHandlerBody_OnSceneRename` | `scene` rename keeps the body untouched, only the method line is rewritten | T7 × C1 |
| 13 | `Sync_PreservesUserMethodsAndFields_OnAnyChange` | non-handler methods and fields untouched | T7/T8 × C2 |
| 14 | `Sync_PreservesHandlerBody_OnParentChange` | parent change keeps the body untouched, only the parent line is rewritten | T8 × C1 |

### 9.4 Folder work, D-series (3 tests)

| # | Test name | What it checks | Related axis |
| --- | --- | --- | --- |
| 15 | `Sync_CreatesNestedDirectory_WhenMissing` | a multi-layer folder is built on its own (World/Levels/) | D1 |
| 16 | `Sync_RemovesEmptyDirectory_AfterMove` | an empty folder left by a move is removed (its meta too) | D2 |
| 17 | `Sync_KeepsNonEmptyDirectory_AfterMove` | a folder still holding other files is not removed | D3 |

### 9.5 Line endings and BOM, E-series (3 tests)

| # | Test name | What it checks | Related axis |
| --- | --- | --- | --- |
| 18 | `Sync_PreservesLineEndings_CRLF` | a CRLF file is not turned into LF on its own | E1 |
| 19 | `Sync_PreservesLineEndings_LF` | an LF file is not turned into CRLF on its own | E2 |
| 20 | `Sync_PreservesUtf8Bom_WhenPresent` | a BOM is kept if it was there | E3 |

### 9.6 Keeping modifiers safe, F-series (2 tests)

| # | Test name | What it checks | Related axis |
| --- | --- | --- | --- |
| 21 | `Sync_PreservesPartialModifier_OnRename` | modifiers such as `partial` are kept | F1 |
| 22 | `Sync_PreservesUsingStatements` | `using` lines are not removed on their own | F2 |

### 9.7 Unity meta files, G-series (5 tests)

| # | Test name | What it checks | Related axis |
| --- | --- | --- | --- |
| 23 | `Sync_RenamesMetaFile_OnSceneRename` | T7's rename carries the `.cs.meta` along too | G1 |
| 24 | `Sync_MovesMetaFile_OnParentChange` | T8's move carries the `.cs.meta` along too | G2 |
| 25 | `Sync_PreservesMetaContent_OnRenameAndMove` | the meta's contents (the GUID) do not change | G3 |
| 26 | `Sync_LeavesDirectoryMetaToUnity_OnNewDir` | a new folder's meta is not built; it is left to Unity | G4 |
| 27 | `Sync_DeletesDirectoryMeta_OnEmptyDirRemoval` | an empty folder's meta is removed along with it | G5 |

### 9.8 Catching errors, X-series (4 tests)

| # | Test name | What it checks | Related axis |
| --- | --- | --- | --- |
| 28 | `Sync_StopsAndReports_OnV004Violation` | a Validator V004 failure in germio.json → sync stops | X3 |
| 29 | `Sync_ReportsWarning_OnHandlerWithUnknownId` | an `id` on the C# side with no match in the JSON → warning plus T10 | X2 |
| 30 | `Sync_ReportsError_OnDuplicateAttribute` | the same `id` attribute in more than one C# file → error (backs up T6) | X1 |
| 31 | `Sync_TreatsIdRenameAsAddPlusOrphan` | an `id` rename is treated as a new build plus an old orphan | X4 |

### 9.9 Idempotency, Z-series (1 test)

| # | Test name | What it checks | Related axis |
| --- | --- | --- | --- |
| 32 | `Sync_RunTwiceWithoutChanges_NoFileModified` | running twice in a row, and the first run on an already-in-place project, both give zero file changes | Z1, Z2 |

---

## 10. RED/GREEN plan

### 10.1 TDD steps

1. **RED step**: write all 32 tests first. Run `dotnet test` with
   `SceneCodeSyncer` not yet built, and check that every one FAILs (either
   a build error or a failed check).
2. **GREEN step**: build the pieces in the order below, checking that the
   matching tests PASS at each stage.

### 10.2 Build order

| Stage | What is built | Tests it should PASS |
| --- | --- | --- |
| 1 | the data model (holding the read-in state of Nodes and Scene classes) | (none yet) |
| 2 | pulling the four line kinds (L1-L4) out of a C# file | (unit tests may be added here, out of the main 32) |
| 3 | T4 (no change, do nothing — the base of idempotency) | #4, #32 |
| 4 | T1, T2 (build new) | #1, #2, #15 |
| 5 | T7 (scene rename) | #7, #12, #18, #19, #20, #21, #23 |
| 6 | T8 (parent change) | #9, #14, #16, #17, #22, #24, #25 |
| 7 | T7' (scene change and parent change together) | #8 |
| 8 | T5 (C# in the wrong place) | #5 |
| 9 | T9 (change with no effect on identity) | #10 |
| 10 | T3 (bring back a file deleted by hand) | #3 |
| 11 | T10 (mark as orphan) | #11, #29 |
| 12 | T6 (find duplicates) | #6, #30 |
| 13 | error handling (V004) | #28, #31 |
| 14 | folder meta handling | #26, #27 |
| 15 | check that every test now PASSes | #1-#32 |

### 10.3 Why this order

+ **Idempotency (T4) comes first**, so any later work that brings an
  unwanted side effect shows up right away
+ T1/T2 (build new) are the plainest; they only need correct output from
  the template
+ T7/T8 are the hardest (editing an existing file, moving its meta along
  with it), and are at the heart of keeping the user's edits safe. Once
  these pass, the C-series, F-series, and G-series tend to pass on their
  own
+ T6 (find duplicates) does not depend on anything else, so it is built
  near the end

---

## 11. Out of scope (later phase candidates)

| Item | When it may be looked at | Related phase idea |
| --- | --- | --- |
| Validator V028 (catching a C# ↔ JSON `id` mismatch) | Phase 5.20 | stronger static checking |
| Matching a Unity Scene file (`.unity`) against a `scene` value | Phase 5.21 | Build Settings sync too |
| Matching a `signal_id` (C# `Publish` ↔ JSON `rule.trigger`) | Phase 5.22 | maybe paired with auto-built constants |
| Adding a UUID (if truly needed, once watched for a while) | Phase 5.23 or later, on hold | only if auto-following an `id` rename turns out to be truly needed |
| Linking Scene Code Sync with the Mermaid export | Phase 6 or later | inside the LLM dogfooding work |

---

## 12. What this phase produces

### 12.1 New files

| Path | Kind |
| --- | --- |
| `game/Assets/Plugins/Germio/Scripts/Editor/SceneCodeSyncer.cs` | the Generator itself |
| `game/Assets/Plugins/Germio/Scripts/Editor/SceneCodeSyncMenu.cs` | the Unity Editor menu item |
| `game/tests/IntegrationTests/Scripts/Editor/SceneCodeSyncerTests.cs` | the 32 tests |
| `docs/scene_code_sync_spec.md` | this document |

### 12.2 Changed files

| Path | What changes |
| --- | --- |
| `game/tests/IntegrationTests/IntegrationTests.csproj` | add the Editor test files to its include list (if needed) |
| `evidence_Phase_5_19_1.md` | written once the work is done |

### 12.3 The eight Scene classes already in place

Once built, the Generator's first run must show **no changes** — checked
by test #32 (idempotency). No hand fixes are made to these files.

---

## 13. Known limits and points the user must mind

### 13.1 Anything outside the four Generator-owned lines is fully safe

Method bodies, fields, `using` lines, XML doc, and comments written by
the user are never read or written by the Generator, so they can never
be broken.

### 13.2 A hand rename of the C# class or file name is still matched by the attribute `id`

If a file holding `[GermioSceneHandler(id:"title")]` is named `Foo.cs`,
the Generator still treats it as the file for the Node with `id="title"`.
The next time Sync runs, it is renamed to the right file name (`Title.cs`)
under the rule in §5.1.2.

### 13.3 A duplicate attribute `id` from a copied file is the user's own to fix

If the user copy-pastes a `.cs` file and ends up with the same
`[GermioSceneHandler(id:"x")]` in more than one file, the Generator stops
with T6 (error report) and waits for the user to fix the duplicate by
hand. It does not try to fix this on its own.

### 13.4 A hand-edited attribute `id` with no match in the JSON only gets a warning

If the user writes `[GermioSceneHandler(id:"unknown")]` with an `id` that
is not in the JSON, the Generator runs T10 (marks it as an orphan). This
might be the user cutting that file loose on purpose, so no fix is made
on its own.

### 13.5 An `id` rename is not auto-followed

As set out in §6.6, an `id` rename is treated as new-plus-orphan. If it
must be fully followed, the steps are: rename by hand, then run Sync.

### 13.6 Deleting a Node whose handler body holds the user's own work

Only T10 (mark as orphan) runs. Nothing is lost. Work is only lost once
the user deletes the C# file by hand.

### 13.7 Renaming an `id` whose handler body holds the user's own work

The old `id`'s C# file becomes an orphan; the new `id` gets a new
skeleton file. The user must move the logic from the old orphan file into
the new C# file by hand.

---

## Appendix A: how the Generator reads the eight files already in place

Once built, the Generator reads these as shown below. No difference
should turn up here (T4) — this is the pass condition for idempotency
test #32.

| File | L1 namespace | L2 class line | L3 attribute | L4 method line |
| --- | --- | --- | --- | --- |
| `Scenes/World.cs` | `namespace GameDev {` | `public class World : Scene {` | `[GermioSceneHandler(id: "world")]` | `protected void OnWorld() {` |
| `Scenes/World/Title.cs` | `namespace GameDev {` | `public class Title : World {` | `[GermioSceneHandler(id: "title")]` | `protected void OnTitle() {` |
| `Scenes/World/Select.cs` | `namespace GameDev {` | `public class Select : World {` | `[GermioSceneHandler(id: "select")]` | `protected void OnSelect() {` |
| `Scenes/World/Ending.cs` | `namespace GameDev {` | `public class Ending : World {` | `[GermioSceneHandler(id: "ending")]` | `protected void OnEnding() {` |
| `Scenes/World/Levels.cs` | `namespace GameDev {` | `public class Levels : World {` | `[GermioSceneHandler(id: "levels")]` | `protected void OnLevels() {` |
| `Scenes/World/Levels/Level1.cs` | `namespace GameDev {` | `public class Level1 : Levels {` | `[GermioSceneHandler(id: "level_1")]` | `protected void OnLevel1() {` |
| `Scenes/World/Levels/Level2.cs` | `namespace GameDev {` | `public class Level2 : Levels {` | `[GermioSceneHandler(id: "level_2")]` | `protected void OnLevel2() {` |
| `Scenes/World/Levels/Level3.cs` | `namespace GameDev {` | `public class Level3 : Levels {` | `[GermioSceneHandler(id: "level_3")]` | `protected void OnLevel3() {` |

Every one of these matches the naming and handler rules set out in §5,
so the first Sync run will make no change.

---

## Appendix B: the full map from Node to class hierarchy

```text
germio.json                         C# Scene Class                         File Path
─────────────                       ────────────────                       ─────────────────────────────────────
root (id="world")                   class World : Scene                    Scenes/World.cs
├─ id="title"                       class Title : World                    Scenes/World/Title.cs
├─ id="select"                      class Select : World                   Scenes/World/Select.cs
├─ id="levels"                      class Levels : World                   Scenes/World/Levels.cs
│  ├─ id="level_1"                  class Level1 : Levels                  Scenes/World/Levels/Level1.cs
│  ├─ id="level_2"                  class Level2 : Levels                  Scenes/World/Levels/Level2.cs
│  └─ id="level_3"                  class Level3 : Levels                  Scenes/World/Levels/Level3.cs
└─ id="ending"                      class Ending : World                   Scenes/World/Ending.cs
```

---

(end of specification)
