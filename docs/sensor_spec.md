# `Sensor` — spec

> **Written on**: 2026-08-19
> **Author**: Claude / under the master's strategy
> **Status**: a plan only; no code built yet

---

## 1. Summary

### 1.1 Aim

Give every character `Germio` moves — no matter which game, no matter
whether it holds an `Animo` mind or not — one shared way to read the
ground and other characters right in front of it, so it can stay on a
floating field, step over a low block, and (where it holds a mind) find
a target worth acting toward.

### 1.2 The problem to fix

A check of `super-nekokun`'s own `Enemy.cs` found its own true way of
staying on a floating field: a Collider named `EnemyWall`, placed by
hand at the true edge of each field, that the character turns away from
on touch. A `grep` across every `stemic` scene file found not one
`EnemyWall` mark anywhere — that trick calls for hand work in each new
level, which does not fit a plan built on quick, repeat level building.

A second check, of `battle-tank`'s own `Enemy.cs`, found a true way to
find the player: a Trigger Collider plus an angle check
(`Vector3.Angle`). This finds a target well, at a cheap cost, but with one
real gap: it never checks whether a wall sits between the character and
its target. A player standing behind a wall is still "found".

Both games solve close, related problems (what sits near me, and in
what direction) with two separate, one-off answers. Neither answer
moves to a new game with no change.

### 1.3 The plan for a fix

Build one `Sensor` class, inside `Germio` itself
(`Scripts/Systems/Sensor.cs`, beside `CameraSystem.cs` and
`SoundSystem.cs`), that answers one plain question — "what sits out
there, in this direction, this close, matching this mark?" — and lets
each caller (a drop-off check, a step-up check, or a target check) ask
it in its own way, through plain settings alone.

---

## 2. Where this class sits

### 2.1 Why inside `Germio`, not a new repository

`stemic`, `flugi`, and `tropika` all hold the same, one `Germio` git
submodule commit today (checked by `git submodule status` on
2026-08-19). `Germio` is already the shared home for work more than one
game needs — `Human.cs` (walk, jump, climb) sits there now, and
`CameraSystem.cs`/`SoundSystem.cs` sit in `Scripts/Systems/` as shared,
single-aim systems, tied to no one character. `Sensor` fits this same
shape; it does not call for one more repository.

### 2.2 Why one class, not two

A drop-off check and a target check both come down to the same act, at
the root: throw a straight line out from the character, in a given
direction, and read what it hits. What sets them apart is not the act
itself, but the settings: the mark to check against (a Ground/Block
layer, or a Player/character layer), the reach, and the angle. One class
with plain, open settings covers both, and any caller that comes later.

---

## 3. Shape (a plan, not final code)

### 3.1 Settings, read in

| Field | Type | Meaning |
| --- | --- | --- |
| `direction` | `Vector3` | Where the line is thrown from, in local space (say, `transform.forward` for a drop-off check, or a sweep that turns, for a wide watch). |
| `angle` | `float` | How wide a spread to check, in degrees (0 for one straight line). |
| `reach` | `float` | How far out the line goes. |
| `layer_mask` | `LayerMask` | Which marks count as a hit (Ground/Block, or Player). |

### 3.2 What it gives back

| Field | Type | Meaning |
| --- | --- | --- |
| `did_hit` | `bool` | Whether anything matching `layer_mask` sat within `reach`, at `angle`. |
| `hit_point` | `Vector3` | Where the line met what it hit. |
| `hit_distance` | `float` | How far off the hit sat. |
| `hit_object` | `GameObject` | What was hit — a Ground/Block, or another character. |

### 3.3 Two true stages, to keep the true Android/mobile cost low

**Stage one** (cheap, wide, run every true tick): a Trigger Collider (for
a target check) or a short, single straight-line check (for a drop-off
check), to see if anything is even near at all.

**Stage two** (a true, single straight-line check, run only when stage
one finds something): checks the real line of sight (this closes
`battle-tank`'s own gap, §1.2 above) and the true ground height (feeds a
jump call, §4 below).

`BoxCollider` sits on every `stemic` Ground/Block prefab today (checked
true, no `MeshCollider` at all) — a single straight-line check against a
`BoxCollider` stays cheap. The true win comes from calling stage two
only when truly needed, not every tick, for every character.

---

## 4. How a jump gets called

When stage two's own read finds a low block or a step right in front
(not a true drop-off — the block's own height sits under a set
point, still open, §6 below), the caller (not `Sensor` itself) calls
`DoFixedUpdate.Apply(type: FixedUpdate.Jump)` straight.
`Human.cs`'s own true jump force (`rb.AddRelativeForce`, tied to
`Acceleration.JumpPower`) works with no change at all; only the call-in
point (a new caller, not the player's own button press) is new.

`Sensor` itself never calls `Jump`; it only answers "what did the line
hit, and how high does it sit". The call to jump, or to turn away from a
drop-off, is the caller's own true choice.

---

## 5. How this joins Animo, through the Adapter alone

`Sensor` never talks to `Animo` straight. When a character's own Adapter
(given a full spec on the `animo` side, `docs/adapter_spec.md`) asks
`Sensor` for a
target check and gets a true hit back, the Adapter itself holds onto
that target and calls `Engine.Affect(need, delta)` with a plain number.
`Sensor` gives facts about the world; the Adapter decides what those
facts mean to a mind.

A character with no mind at all (say, a moving block) may still use
`Sensor`'s own drop-off check, with its target-check settings never
turned on.

---

## 6. What this costs on a phone

**No real device check has been run yet.** What is below is a plain
count of what this design asks of the CPU, given what is known
today — not a true, measured number. A real check, on a real phone,
is still owed before this task closes.

### 6.1 How many characters this must run for

`stemic`'s own current plan (`TASKLIST.md` TASK-013..024) calls for
two characters, each holding an `Animo` mind. `Sensor` also gives its
drop-off check to any character `Germio` moves, mind or not — a
moving block, say (see §5 above). So the true count to plan against
is not "how many minds", but "how many moving things sit in a scene
at once". Today, with only the player (`Human.cs`) and two true
characters, that count sits at three. This spec does not know what
that count grows to as `stemic`'s own levels grow, or once `flugi`/
`tropika` take up the same `Sensor` class.

### 6.2 What runs, and how often

+ **Stage one** (the cheap, wide check) runs once a tick, for every
  moving thing that holds a `Sensor` — a drop-off check for
  movement, a target check at whatever rate §3.3 above ends up
  setting.
+ **Stage two** (the true, single straight-line check) runs only
  when stage one finds something — so its true cost scales with how
  often characters truly sit near an edge or near each other, not
  with how many characters a scene holds.

`BoxCollider` (checked true on every `stemic` Ground/Block prefab,
no `MeshCollider` at all, §3.3 above) keeps a single straight-line
check cheap in itself; the true, open question is how many times a
second this happens, times how many characters, added up.

### 6.3 What is still owed

+ A real, device-run check (not a plan-only guess), once TASK-014
  (this class itself) is built, with the true character count
  `stemic`'s own level work ends up holding.
+ A plain, upper true limit on how many `Sensor`-holding characters
  one scene may hold at once, set once that real check comes back —
  today, no such limit is written down anywhere.

---

## 7. Open points

+ The true block-height point that tells a step-up apart from a
  true drop-off is still open — a number to set once real play on
  `stemic`'s own levels can be checked.
+ How often stage one itself should run (every tick, or a set true rate,
  a small number of checks a true second) is still open, and may end up
  different for a drop-off check (tied close to movement) against a
  target check (tied to notice, not to movement).
+ Whether `Sensor` should give back more than one hit at once (say, a
  full list, for a wide watch) or only the closest one is still open;
  today's plan holds to the closest hit alone, the plainest shape.
