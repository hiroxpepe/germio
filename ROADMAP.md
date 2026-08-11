# ROADMAP

<!-- format: v1 | fields: status, phase, title -->

+ [x] PHASE-01: Build the LLM-native DSL and its full test base
+ [ ] PHASE-02: Have an LLM alone finish a real game (dogfood it)
+ [~] PHASE-03: Work that came up outside the first plan

## Detail

### PHASE-01

Phases 1 through 5 of the older, detailed plan (in
`germio_roadmap.md`) are done: the DSL itself, the JSON Schema, the
LLM system prompts, a two-way Mermaid link, and a hardening pass
(Vault, persistence, a schema-version Migrator). Every `DoD` line
for these phases reads `[x]` in the older, detailed plan.

### PHASE-02

The next big goal, once picked back up: hand a short game spec to
an LLM alone, with no state chart, no JSON example, and no code
given, and see it build a real, playable game through `germio.json`,
fixed up round after round from the Validator's own error messages.
Every `DoD` line for this phase still reads `[ ]` in the older,
detailed plan; none of it has begun.

### PHASE-03

Real, day-to-day work on `flugi`'s own level-clear bug turned up
gaps the older plan never named: `Command.request_notify` (a way
for a Rule to ask for a message on screen, with no need to touch
C#), and a browser-based `germio.json` editor tool, built with no
Unity needed at all. Both are done and in use. The older plan's own
phase numbers do not cover this kind of work; it is tracked here,
and in `TASKLIST.md`, instead. See `TASKLIST.md` for the open work
under this phase.
