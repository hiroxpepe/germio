# HANDOFF

> New chat starts here. Read this file first, before anything else.

## Read this first, in every new chat

**Read `CLAUDE.md`, in this repository, before any other work.** It
holds the one command that must be run once, for each clone
(`git config core.hooksPath .githooks`), and every rule the agent
holds to, here. **Every other repository in this family holds its own
`CLAUDE.md` too — read that one, in that repository, before any work
begins there.**

## Where things stand

State on 2026-11-07, checked against the code.

| # | Part | State |
| --- | --- | --- |
| 1 | The tools | `Editor/` (a plain browser page, no Unity) edits `germio.json`; `Command.request_notify` is built and shipped; every event ends in a plain past word form (a rule of `ConventionRules.cs`) |
| 2 | The newer parts | `WorldNames` and `Sight` (built 2026-09-22, tested; `TASK-067` stays in part: the scene-load edge in Unity) and `V037` (built, tested; the two holes left are `TASK-083` and `TASK-084`) |
| 3 | The Package move | `stemic` and `flugi` take `germio` in through the Package Manager (checked on GitHub 2026-11-07); `tropika` names the package and still holds its submodule line (`TASK-005`, `TASK-064`) |
| 4 | The tests | 540 in `Tests~/CoreTests` and 106 in `Tests~/ConventionTests`, all pass. The 11 files of `Tests~/ModelTests` (108 tests) run in no project |
| 5 | Open work | 31 tasks in `TASKLIST.md` (28 open, 3 in part). An NPC that runs through the same `Human` code as a player: `TASK-070` to `075`, none built; `TASK-070` is on hold (see Next move) |

## Next move

See `TASKLIST.md` for the full list. The biggest open items right now are the Scene wiring checker (TASK-001) and the last step of moving germio into a Unity
Package (TASK-005: `tropika`). Waiting for the word of the master: putting `Tests~/ModelTests` into `CoreTests.csproj` (its 108 tests pass in a trial run). On hold until the word of the master: `TASK-070`. Its three names, which `Human` reads (`isPressed` and the rest), and `Tick` do not keep to the naming rule of `Tests~/ConventionTests`. The work is written but not committed, and is kept by `git stash` in the box only (see the note in `TASKLIST.md`).
