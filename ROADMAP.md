# ROADMAP

<!-- format: v1 | fields: status, phase, title -->

+ [x] P-01: Build the LLM-native DSL and its full test base
+ [ ] P-02: Have an LLM alone finish a real game (dogfood it)
+ [ ] P-03: Play real-time sound through Quyno and Signo
+ [~] P-XX: Work that came up outside the first plan

## Detail

### P-01

Phases 1 through 5 of the older, detailed plan (in
`germio_roadmap.md`) are done: the DSL itself, the JSON Schema, the
LLM system prompts, a two-way Mermaid link, and a hardening pass
(Vault, persistence, a schema-version Migrator). Every `DoD` line
for these phases reads `[x]` in the older, detailed plan.

### P-02

The next big goal, once picked back up: hand a short game spec to
an LLM alone, with no state chart, no JSON example, and no code
given, and see it build a real, playable game through `germio.json`,
fixed up round after round from the Validator's own error messages.
Every `DoD` line for this phase still reads `[ ]` in the older,
detailed plan; none of it has begun.

### P-03

**Master's own word, 2026-08-18.** Every `germio`-based game
(`stemic` first) already calls `SoundSystem.Play(SfxClip)` and
`SoundSystem.Play(MusicClip)` for its own sound. Today, inside
`SoundSystem.cs`, this plays a given `AudioClip` through a plain
Unity `AudioSource` — replace that inner true work alone with a
call through to `Quyno`/`Signo`, with **no change at all owed to
any game that calls `SoundSystem`** (`stemic`, `flugi`, `tropika`,
all at once, given the one shared true class).

**A germio-based game must never touch `Signo.Core` directly**
(Master's own word) — every true call passes through `Quyno.Bridge`.
`MusicClip` holds one true value today (`BeatLevel`); it must grow
to at least six, to match `stemic`'s own true song set (`Title`,
`Level1..3`, `Ending`, `BeatLevel`). `SfxClip` (seven true values:
`Item`/`Jump`/`Climb`/`Walk`/`Run`/`Grounded`/`Push`) does not map
one-to-one onto `Signo`'s own `SEType` (eight true values) — this
mapping is still open. **This whole phase is held on `signo`'s own
TASK-027 (a true SE spec and sound-quality pass) landing first** —
a real check found `Signo`'s own SE sound not yet game-ready, the
true root gate behind every later step here. See `TASKLIST.md` for
the open work under this phase.

### P-XX

Real, day-to-day work on `flugi`'s own level-clear bug turned up
gaps the older plan never named: `Command.request_notify` (a way
for a Rule to ask for a message on screen, with no need to touch
C#), and a browser-based `germio.json` editor tool, built with no
Unity needed at all. Both are done and in use. The older plan's own
phase numbers do not cover this kind of work; it is tracked here,
and in `TASKLIST.md`, instead.

**A second body of work of this kind landed 2026-08-22: what `modio`
needs.** `modio` is the HOW layer, standing between `animo` (WHY) and
this build (WHAT), and carrying a want through as a deed that takes
time. Four things were added here for it, each held to TDD, each with
its own tests:

| Added              | To        | What it does                             |
| ------------------ | --------- | ---------------------------------------- |
| `actor`            | `Rule`    | says whose rule this is                  |
| `update_need`      | `Command` | the one way anything reaches `animo`     |
| `request_deed`     | `Command` | starts work that takes time              |
| the `$target` mark | —         | stands for what a deed has not yet found |

Beside those: two events out of the `Store` (`NeedRequested`,
`DeedRequested`), 9 new checks (V028 to V034, V036), a deep copy
for `Rule` and `Command`, a way to sort rules by actor, and the sums
behind a line spoken over a character's head.

**Two test builds were opened here for it** — `Tests~/ModelTests` and
`Tests~/CoreTests` — so this build now checks itself, rather than
leaning on a game to do it. `animo` has held its own tests all along;
this brings the same to a build made to be handed out as a package.

See `TASKLIST.md` for the open work
under this phase.
