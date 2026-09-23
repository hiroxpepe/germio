# NPC input — making Human run true for a player and an NPC, either one

> Made 2026-09-22, after a hard look at whether an NPC (an enemy or a
> friend, run by animo's own Engine) could ever move, jump, and climb
> the very same way `Pete` (the player) does, through the very same
> `Human` code. No line of this is built yet. This is the family's
> first attempt at this — no other repository here has ever done it.

---

## The true finding, held first

`Human : InputMapper` reads its own five buttons
(`Up`/`Down`/`Left`/`Right`/`Y`/`B`) straight off a real device,
through `Keyboard.current` / `Gamepad.current` — Unity's own single,
shared pointer to *the one* real device in play. `mapGamepad()`
sets every field again to this shared pointer, new, every frame, for
every instance. Given more than one `Human`-based body at once (a
player and an NPC, or two NPCs), every one of them would fight over
this one shared pointer — not simply "fail to read AI input" but instead steal the player's own real input, or steal a UI object
(below).

## Two roads, held and dropped

**Road A — a real Unity Input System virtual device per NPC, driven
by `InputSystem.QueueStateEvent`.** Dropped outright. `.current`
itself moves to point at whichever device was touched last — so a
virtual device, once written to, would itself become `Gamepad.current`,
locking the player's own real pad out. Never checked for zero
garbage either; `QueueStateEvent` is Unity's own closed code, held
nowhere in this repository to check against.

**Road B — replace the button fields' own type outright, in one
sweeping change.** Dropped as too broad and too unclear on its own —
it never said what the new type should hold, nor how much of
`InputMapper` it would touch.

**What both missed:** the question was never "which trick changes the
device." It was always about `InputMapper` itself — its own grain
was too fine (a real, named Unity type, `ButtonControl`), tied to one
real, shared device. The fix is `InputMapper` made to work the same true way
with either a real device or a computed one, at its own root.

## The one true grain, checked against real use

Every one of the five buttons is asked only three true questions,
checked live across the whole of `Human`, `Human_Acceleration`, and
`InputMapper` itself:

```text
interface IButtonState {
    bool isPressed { get; }
    bool wasPressedThisFrame { get; }
    bool wasReleasedThisFrame { get; }
}
```

No other member of `ButtonControl` is ever read. `Human` itself
never needs to change at all — it already only ever reads these
three, off a field; only the field's own type, and what fills it,
must change.

## What still stands in the way, found live, not yet settled

+ [ ] `mapGamepad()` holds no `virtual` mark at all today (nor any
      access mark past its own default), so nothing may yet stand in
      for it. It must be opened up before an NPC's own fill-in can
      exist at all.
+ [ ] `VirtualControllerObject` is found once, by name
      (`Find(name: "VController")`), and is a single, shared UI
      object — the player's own on-screen touch pad. An NPC calling
      `mapGamepad()` unchanged would fight the player over showing
      and hiding this same object, every frame. An NPC's own fill-in
      must never touch it at all.
+ [ ] The vibration calls set up in `Start()` (phone vibration on a
      button press) read the very same button fields. Given to an
      NPC unchanged, they hold no true meaning, and must be held out
      of whatever an NPC's own `Start()` truly runs.
+ [ ] `protected static bool Look` is shared across every instance of
      `Human` at once — checked live, nothing anywhere sets it true,
      so it is dead, not yet a true bug. But it is a real, waiting
      one: the day anything sets it, one body's own "looking" would
      read true for every body at once, NPCs included. Held here so
      it is not found the hard way later.
+ [ ] `Human` is a `partial class`, split across several files
      (`Human_DoUpdate.cs`, `Human_DoFixedUpdate.cs`,
      `Human_Acceleration.cs`, `Human_Extensions.cs`). Whether an NPC
      stands as `Human` itself (given a computed fill-in for its own
      buttons) or as a true child class has not yet been checked against
      how a `partial class` truly splits.
+ [ ] The computed fill-in's own three true answers
      (`isPressed`/`wasPressedThisFrame`/`wasReleasedThisFrame`) must
      be worked out from one plain, given "is this held down right
      now" signal, held by the fill-in itself, checked once a frame
      against its own last-held value — not asked of Unity at all.
+ [ ] Where the "is this held down" signal itself comes from — how
      `germio`'s own `Deed` (a direction to move, a wish to jump)
      turns into five true button-shaped answers — is not yet
      worked out at all.
+ [ ] Zero-garbage, for whatever an NPC's own fill-in turns out to
      be, is not yet checked. `UniRx`'s own `Where`/`Subscribe`
      chain, checked live against its own held source
      (`Plugins/UniRx/Scripts/Operators/Where.cs`), holds true
      already — every true piece is made once, at `Subscribe` time,
      never again on a tick. Whatever new code an NPC's own fill-in
      adds must be checked the same true way, by a real run, not by
      eye.
+ [ ] Many bodies at once (a large NPC count named outright) has not
      been checked against `Start()`'s own per-instance cost (each
      one still calls `Find(name: "VController")` once, at its own
      start) — a one-time cost, not a tick cost, but not yet held
      true by a real number either.
