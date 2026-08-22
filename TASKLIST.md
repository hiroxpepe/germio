# TASKLIST

Work still open for this repository. Any person may put in a new
item; the person who does the work marks it done (`+ [x]`) and puts
the change in as a commit.

<!-- format: v1 | fields: status, id, title, phase -->

+ [ ] TASK-001 [P-XX]: Build a Scene wiring checker and auto-fixer
+ [ ] TASK-002 [P-XX]: Add a timed event step-line to the DSL (play_sequence)
+ [ ] TASK-003 [P-XX]: Add Command.request_notify (built; a real playtest is still open)
+ [x] TASK-004 [P-XX]: Build a germio.json viewer and editor with no Unity (done)
+ [ ] TASK-005 [P-XX]: Move germio to a Unity Package, off the git submodule
+ [ ] TASK-006 [P-XX]: Put the rest of the docs into Basic English
+ [ ] TASK-007 [P-XX]: Sync germio_roadmap.md's own state to the real code
+ [ ] TASK-008 [P-03]: Wait on signo's own true SE spec and sound brush-up
+ [ ] TASK-009 [P-03]: Wait on quyno's own true Germio-bridge entry point
+ [ ] TASK-010 [P-03]: Grow MusicClip from one true value to six, for stemic's own songs
+ [ ] TASK-011 [P-03]: Map SfxClip's own seven values against Signo's own eight SEType values
+ [ ] TASK-012 [P-03]: Replace SoundSystem's own inner true work with a Quyno.Bridge call
+ [ ] TASK-013 [P-03]: Play it for real, in stemic, and check every SfxClip/MusicClip still sounds
+ [xx] TASK-014 [P-XX]: Build one shared sensor — moved out, to modio
+ [xx] TASK-015 [P-XX]: Give every level piece a mark — dropped, nothing is needed
+ [ ] TASK-016 [P-XX]: Read actor off a Rule, out of the JSON
+ [ ] TASK-017 [P-XX]: Take an actor on DispatchTrigger, and on Bus Publish
+ [ ] TASK-018 [P-XX]: Fire a rule with no actor, whoever calls
+ [ ] TASK-019 [P-XX]: Fire a rule with an actor only for that one
+ [ ] TASK-020 [P-XX]: Read update_need as a list, out of the JSON
+ [ ] TASK-021 [P-XX]: Fire a need out of the Store, one call to a list entry
+ [ ] TASK-022 [P-XX]: V028 — an empty need key is an error
+ [ ] TASK-023 [P-XX]: V029 — a delta of zero is a warning
+ [ ] TASK-024 [P-XX]: Read request_deed, all five parts, out of the JSON
+ [ ] TASK-025 [P-XX]: Read the Command held inside a request_deed
+ [ ] TASK-026 [P-XX]: Let a request_deed stand with no target at all
+ [ ] TASK-027 [P-XX]: Fire a deed out of the Store, once
+ [ ] TASK-028 [P-XX]: V030 — a motion outside the seven is an error
+ [ ] TASK-029 [P-XX]: V031 — an until with no key, or two, is an error
+ [ ] TASK-030 [P-XX]: V032 — a request_deed inside a request_deed is an error
+ [ ] TASK-031 [P-XX]: V033 — a kind outside the six type marks is an error
+ [ ] TASK-032 [P-XX]: Put a found id in place of the $target mark
+ [ ] TASK-033 [P-XX]: Leave text with no $target mark just as it stands
+ [ ] TASK-034 [P-XX]: Swap every $target in a line, not the first alone
+ [ ] TASK-046 [P-XX]: Leave $targets and $TARGET alone, mark or not
+ [ ] TASK-047 [P-XX]: Swap once, and never look at what was put in
+ [ ] TASK-048 [P-XX]: Reach every text field inside a held Command
+ [ ] TASK-049 [P-XX]: Write out an id with a letter in front of it
+ [ ] TASK-050 [P-XX]: Run a put-in-place condition through the Evaluator
+ [ ] TASK-035 [P-XX]: Read a whole deed, end to end, off a real JSON file
+ [ ] TASK-036 [P-XX]: Hold every Executor test already there, still green
+ [ ] TASK-037 [P-XX]: Read act off a request_deed, and let it be left out
+ [ ] TASK-038 [P-XX]: V034 — an act outside the three is an error
+ [xx] TASK-039 [P-XX]: V035 — an until on a tie moving — dropped, no such until
+ [ ] TASK-040 [P-XX]: Add a NeedRequested event to the Store, and fire it
+ [ ] TASK-041 [P-XX]: Add a DeedRequested event to the Store, and fire it
+ [ ] TASK-042 [P-XX]: Find every caller of Bus Publish, and keep each one working
+ [ ] TASK-043 [P-XX]: Show a line over a character's head, for what it has in mind
+ [ ] TASK-044 [P-XX]: List a node's own rules by actor, so each may be read apart
+ [ ] TASK-045 [P-XX]: Take like beside target_id, in every history call
+ [ ] TASK-051 [P-XX]: Hold V007 back where a rule names an actor
+ [ ] TASK-052 [P-XX]: Hold V008 back where a set_flag sits inside a deed
+ [ ] TASK-053 [P-XX]: Let V010 know the two new commands
+ [ ] TASK-054 [P-XX]: Put the three new words into the exported schema
+ [ ] TASK-055 [P-XX]: Give Rule and Command a deep copy of their own
+ [ ] TASK-056 [P-XX]: Check a deed condition after the mark is put in place
+ [ ] TASK-057 [P-XX]: Leave actor empty where a Zone fires the rule
+ [ ] TASK-058 [P-XX]: V036 — an actor no persona holds is an error

## Detail

### TASK-001

  a `germio.json` Node needs 3 kinds of set-up that no tool checks
  today, so an error breaks the game only while it is running,
  with no warning at build time or edit time.

1. **The singleton objects.** The `Scene` base class gets `GameSystem`
   by name, with `Find(name: GAME_SYSTEM)` (and a game will many
   times want `SoundSystem` too). If that GameObject is not in the
   scene, `GameSystem` keeps an empty value, and the first
   `signal_btn_*` publish gives a `NullReferenceException`. Every
   scene a person can play has to have these singleton objects, each
   with its own parts joined to it.

2. **A part built from `Scene`.** Each Node with a scene name set
   needs a GameObject in that scene, with the game's own leaf
   class built from `Scene` (say `Title` or `Level1`). That
   class's `[GermioSceneHandler(id: "...")]` method has to have
   the same id as the Node.

3. **The build list and the name.** Every scene name in a
   `Scenario` has to (1) match, letter for letter, a real `.unity`
   file's own name, and (2) be put in the scene list of the
   Build Profile in use. If a scene is not in that list, the game
   gives an error, but only when a person tries to change to that
   scene while playing — not before. A name that does not match
   (say, the `Scenario` gives `Level1` but the file is named
   `Level_1.unity`) gives the same kind of error.

Build an Editor tool that reads the `Scenario`, opens each named
`.unity` scene, checks all 3 kinds of set-up, gives a clear report
of what is wrong, and, if wanted, makes the fix itself (puts in
the GameObjects and parts not there, puts the scene in the build
list, points out a name that does not match). This would have
caught every `flugi` error where the game went from the title to
a level and broke — at edit time, and not, as it did, at play
time.

Tools in the code now that may give help with this: `SceneCodeSyncer`
(`germio.json` to a C# class tree) does the `.cs` side but does not
touch `.unity` files; `briko`'s `Exporter` and `Importer` show a
way to read and write `.unity` files for GameObjects and parts.

### TASK-002

  right now, what has to happen after a Rule fires — a paused-game
  message, a level-clear message, a level-start name showing — is
  put right into the C# code (`Germio.Systems.NoticeSystem` waits
  for `GameSystem.OnPauseOn` / `OnCameBackHome` and puts up fixed
  English words). This is fine for one flat message, but breaks
  down the moment a game wants its own many-step, timed showing
  after a clear — the very kind of thing G11 (words in the file,
  not code) says should live in the JSON, not in C#.

**Old games studied, picked to show two different needs**:

+ *Super Mario 64* (grabbing a star): the moment the star is
  touched IS the win; every step after it (a pose, a camera move,
  a sound, a walk back to the lobby with no button push needed)
  is a fixed, straight-line run of small steps.
+ *Tomb Raider* (a room puzzle): clearing a room can need many
  switches thrown in some order, and the pay-off is often a full,
  made-ahead movie far too big for any DSL to hold the *content*
  of — only its *place in the line* is worth naming.

Both games point to one rule: **the DSL has to never hold what a
step does, only its name, its place in the line, and about when
it fires.** What a name does (play a sound, show words, run a
movie) is always left to the game's own C# (`GameDev`), the same
way `trigger` and `HistoryEntry.kind` are already free words the
game gives meaning to.

**A sketch of the shape**:

    "command": {
      "set_flag": { "key": "star_collected", "value": true },
      "play_sequence": [
        { "event": "star_get_pose",       "delay": 0.0 },
        { "event": "show_message",        "delay": 0.5, "params": { "text": "Star Get!" } },
        { "event": "play_movie",          "delay": 1.0, "params": { "clip": "ending_cutscene_01" } },
        { "event": "request_transition",  "delay": 0.0, "params": { "target": "lobby" } }
      ]
    }

**3 good points**:

+ closes the one real gap found this night: there is no way at
  all, right now, to say "show this text, in this order, with
  this time gap" in `germio.json` — only in C#.
+ stays open on purpose: a very new kind of step (a camera move,
  a locked input, a thing no one has thought of yet) needs no
  change to germio's own file shape, since `event` is a free name
  and `params` is a free bag, in the same way `trigger` and
  `HistoryEntry.kind` work.
+ covers both games at the same size: Mario's small pose and
  sound, and Tomb Raider's full movie, are both just one `event`
  name for each one — the DSL never has to know which is small and
  which is big.

**15 points against it, found before any code was written**:

1. `Executor.Execute` today runs a `Command` all at one time,
   with no way to hold a step waiting across many frames — this
   is not a small field to add, it is a change to how `Executor`
   runs at all.
2. a `play_sequence` step of `event: "request_transition"` would
   sit next to the top-level `Command.request_transition` that
   is there already, giving two ways to ask for the same thing
   with no rule for which one to pick.
3. the same risk holds for `set_flag`, or any other `Command`
   field, if a step ever wants to change state too.
4. a fully free `params` bag opens back up the very "put in
   almost any word" risk G13 (a small, closed word set) was
   written to close — this is a real pull, not a small one.
5. no rule is set for what happens when the game does not know an
   `event` name (does it do nothing? give a warning? stop hard?).
6. `delay`'s own starting point is not set (from the start of the
   line, or from the step before it?) — this alone would give an
   LLM, and a person, a hard time.
7. no thought was given to a Rule firing again in the middle of
   its own line (when `once` is false), maybe starting a second
   line on top of the first.
8. no thought was given to a player's button push in the middle
   of a line — should the line keep going, stop, or be ignored?
9. a line's own place, part-way through, cannot be put into a
   Snapshot at all under this shape, so a save made part-way has
   no way to pick back up right on load (a real G20 point).
10. no thought was given to whether a `play_sequence` step should
    also write a `HistoryEntry`, and if so, how that would differ
    from `record_event`.
11. no new Validator rule has been made for this at all (a missing
    `event`, a broken `params`, and more).
12. `CookbookExamplesTests.cs` checks known-good file shapes;
    with a free `params`, there is no fixed shape left to check
    against.
13. no thought was given yet to *who*, on the game side, hears a
    `Store`-level line-step and sends each named `event` on to
    real code.
14. the new words (`play_sequence`, `event`, `delay`, `params`)
    have not been checked against G16 (a rule for how words
    should read the same at every level) for whether they hold
    the same word type and the same size of meaning as
    `set_flag`, `update_counter`, and the rest.
15. building this for real needs 4 things at once — an `Executor`
    that can wait across frames, a new `Store` event, new
    Validator rules, and a game-side part to send each step on —
    too much to start the same night it came up; it needs its own
    design pass, on its own.

**Where this stands**: not started. No `animo` or `briko` case was
found for a timed, ordered line of steps (`animo`'s own file holds
only numbers — rates, cut-off points, weights; `briko`'s holds only
spots in space — grid units, block spots), so this would be new
ground for the whole family, not a thing to copy from a close one.

### TASK-003

  `play_sequence`, with known gaps left open on purpose**: this
  item keeps the full path taken to reach `request_notify`, so a
  later reader does not have to walk it again, and so the gaps it
  still holds are not read as things no one saw.

**The fast need**: in `flugi`, `NoticeSystem` showed "Level
Clear!" the moment `GameSystem.OnCameBackHome` fired — the moment
the player's own body touched the Home object, and not the moment
the level truly cleared (a press of the A button while `is_beat`
and `player_at_home` both hold true). The words came up too soon.

**Design paths tried, and why each one, but the last, was turned
down**:

1. *`show_message: string` as a new, seventh `Command` field.* Turned down:
   every `Command` field there is now (`set_flag`,
   `update_counter`, `update_inventory`, `request_transition`,
   `set_persistence`, `record_event`) makes a change to State or
   to History — a change to state. Showing words on a screen
   changes nothing that gets saved; it does not sit well next to
   fields whose whole point is a change to state.
2. *`message: string` as a new field on `Rule` itself, next to
   `command`.* Turned down as the same wrong move, one level up:
   nothing would stop the next need (a sound, a camera move) from
   asking for its own new top-level field too, and `Rule` grows a
   field for every new need, for all time, with no rule for where
   a new need should go.
3. *`notify: string`, a free word on `Rule`, in the same way as
   `Rule.trigger` and `HistoryEntry.kind`* (both already free
   words `germio` itself never reads the meaning of — the game
   gives them their meaning). This is the path that held up.

**5 good points weighed for path 3**: stays open to any meaning
to come, with no change to the file's shape; matches a thing
`germio` does now (`trigger`, `HistoryEntry.kind`); reads as the
right pair to `trigger` (`trigger` is the signal coming in,
`notify` the signal going out); a path stays open to keep the
real English words out of the JSON file at all (an id such as
`"level_clear"`, with the real words held in a C# table) if the
game is ever put into another language; keeps `Command` itself
untouched, so its "every field is a change to state" shape stays
clean.

**5 bad points weighed, and what was done about each**:

1. the string's meaning cannot be seen from the JSON alone — **not
   put right**, only taken on, on the grounds that `trigger`
   already carries the same weak point and `germio` has lived
   with it from the start.
2. the Validator cannot check that a given string is ever truly
   acted on, on the game side — **put right in part** (an empty
   string can be flagged), the deeper question (does the game
   truly act on this id) stays open, for the same reason as #1.
3. the order `command` and the notify fire in, held against each
   other, was not set — **put right**: a plain rule (`command`
   first, then the notify) closes this with no hard part at all.
4. whether `once` covers the notify too was not set — **put
   right** the same way: a plain rule (`once` covers the whole
   Rule, notify held in it too) closes it.
5. one Rule firing cannot carry more than one notify at once,
   since the field holds a single string — **not put right**;
   this is a real, hard limit. Turning it into a list would, in
   fact, open `play_sequence` back up (see the item above) by a
   side door, which is not in this fix's own reach tonight.

**10 old games checked against a single-string notify, to see how
far "one notify to a Rule" truly goes**:

| Game                                   | Moment checked                                                | Does a single notify fit?                                                                                                                                     |
| -------------------------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Super Mario Bros.                      | touching the goal flag                                        | yes — the moment is simple, and there is only one                                                                                                             |
| Super Mario 64                         | grabbing a star                                               | for the most part — one notify covers "cleared", but the pose/sound/walk *line of steps* after it still needs C#, or `play_sequence` later                    |
| Tomb Raider                            | a many-switch door opening                                    | yes — "door opened" is one notify; the movie's own content is always the game's own job, never the DSL's                                                      |
| The Legend of Zelda (a dungeon clear)  | boss put down, item shows                                     | for the most part — one notify fires the sound, but the sound-then-item-shows staging still needs C#, or `play_sequence` later                                |
| Dragon Quest (going up a level)        | after a fight's own points are added                          | no — more than one party member can go up a level in the same fight; a single string cannot carry more than one at once                                       |
| Mega Man (a boss put down)             | screen flash, way out, stage pick                             | for the most part — one notify fires it, but the flash-then-way-out-then-change staging still needs C#, or `play_sequence` later                              |
| Pac-Man (clearing a board)             | the last dot eaten                                            | yes — one fixed, plain flash-then-move-on way                                                                                                                 |
| Final Fantasy (a fight won)            | the outcome screen                                            | no — points, things won, and going up levels have to be shown all at once; one string cannot carry all 3                                                      |
| Sonic the Hedgehog (100-ring bonus)    | the ring count going past 100                                 | for the most part — a sound and a life-count showing are two different things a single notify string is pushed hard to carry at once                          |
| this `flugi` fix (Level Clear)         | the A-button-held clear                                       | yes — this is right what tonight's fix needs, and no more                                                                                                     |
| Bomberman (clearing a stage)           | reaching the way out door                                     | yes — the notify alone covers it; the door's own showing is a separate Rule                                                                                   |
| Family Circuit (a race's end)          | crossing the line                                             | for the most part — the notify can fire the outcome screen, but rank and time are read from `counters` already there, not carried by the notify string itself |
| Sky Kid (landing well)                 | landing on the carrier                                        | yes — the bonus score itself runs through `update_counter` already there, next to the notify                                                                  |
| Tokimeki Memorial (a love-words event) | more than one game person's own liking crosses a line at once | no — this needs picking one out of many, which a single string cannot say on its own                                                                          |
| Daisenryaku (a turn's end report)      | many fights and land-takes in one turn                        | no — this needs many different things that took place put together and grouped, which is `record_event` and History's own job, not one notify string's        |

A first look said Tokimeki Memorial and Daisenryaku could be
put right today with Rule order (first one to match wins) and
`History` grouping. On a harder look, both did not hold up: Rule
firing order, across Rules that share one `trigger`, was said to
be true but never read from `Store`'s own real code, so
"first-in-JSON wins" is not checked; and pointing at `History`
for Daisenryaku only moves the whole problem into C#, it does not
show `germio`'s own DSL saying anything about it. Both stand as
open, real gaps against `germio`'s own claim that it works for
any kind of game, in `germio_roadmap.md` §1.3 — put down here,
not hidden away.

**Why an `event`, and not a plain write of data**: `Executor` was
read line by line for this. 5 of the 6 `Command` fields there
were before this fix (`set_flag`, `update_counter`,
`update_inventory`, `set_persistence`, `record_event`) only ever
write straight into `store.Scenario.initial_state.*` or into
History — no event fires; the game side has to poll that data
itself, at any time it wants to. Only `request_transition` calls
`store.RequestTransition(...)`, which fires the one event `Store`
had: `OnTransitionRequested`. The reason for this holds up well:
a flag is a state that holds true across many frames, so polling
suits it; a change of scene matters for one moment only, so a
push-style event is the only sure way to not miss it. A notify is
the very same kind of one-moment thing a flag is not, so it
should be wired the same way `request_transition` is — a new
field, `request_notify: string?`, calling a new
`store.RequestNotify(notify_id: ...)`, firing a new
`OnNotifyRequested` event — not the plain-write way the other 5
fields use.

**The choice made**: take on `Command.request_notify` (a single,
free string, wired through a `Store` event in the same way as
`request_transition`) as tonight's fix, on the understanding that
this is **a small, stop-gap shape, on purpose** — it covers this
`flugi` fix and the "yes"/"for the most part" rows in the table
above, and it knowingly leaves the "no" rows, and bad point #5
(more than one notify to a Rule), for `play_sequence` (or some
other, bigger redesign) to close later.

**What was built** (through TDD, checked against the real
`Data.cs`, `Store.cs`, `Executor.cs`, and `Validator.cs` in a
throw-away test set-up, since this box has no way onto the net
to bring back `stemic`'s own `IntegrationTests.csproj`):
`Command.request_notify`; `Store.NotifyRequested` and
`Store.RequestNotify(notify_id)`, wired the same way
`TransitionRequested` already is; `request_notify` does not set
`mutated` on its own, but a `Rule` that puts it next to `set_flag`
(or any other state-changing field) still does; V010 no longer
says a `request_notify`-only command has no effect; a new V027
catches an empty or blank `request_notify` value. `NoticeSystem`
now waits for `Store.NotifyRequested` and shows the level-clear
words only for the `"level_clear"` id, in place of the old,
too-fast `HomeReturned` (was `OnCameBackHome`) hook.

Also done in the same pass, since it touched the same files: 6
events (`Despawn.Despawned`, `Home.Returned`, `GameSystem.Paused`,
`.Resumed`, `.LevelStarted`, `.HomeReturned`,
`Store.TransitionRequested`) had their old, wrong-shaped names
(`OnDespawn`, `OnCameBack`, `OnPauseOn`, `OnPauseOff`,
`OnStartLevel`, `OnCameBackHome`, `OnTransitionRequested`) put
right to a plain past form of the word, with no `On` in front,
matching `midiplayer`'s own `Started`/`Ended` way over
`meowziq`'s `On`-first way; `ConventionRules.cs` got a new
`check_participle` check (11 new tests) that holds every event
name to this shape from here on; `coding_standard.md`'s Events
row and `stemic`'s `Levels.cs` were both put up to date to match.

**Still open, on purpose, for a later pass**: the risk of
`request_notify` and `record_event` being used to say the same
thing twice, in two different spots, with no rule against it; a
real Unity playtest of the `flugi` fix this was all built for
(the master's own next step, on the real Windows 11 build) —
everything above was checked by test code alone, never inside
Unity itself.

### TASK-004

  web page that opens a `germio.json` file, shows its full tree in
  a clear way, and lets a person add, change, and take out a Node
  or a Rule with forms — with no need to write raw JSON by hand,
  and no need for the Unity Editor to be open at all.

**Why this is wanted**: right now, the only way to change
`germio.json` is to write JSON by hand. This is a real risk for a
small writing mistake (one missing comma breaks the whole file)
and a real wall for any person who does not want to read raw
JSON. A person should be able to set up a full Scenario with just
this page and a plain text tool for `germio.json` — the Unity
Editor is not needed for it.

**What was looked at first**: `Animo`'s own `Monitor/dashboard.html`.
Its live, frame-by-frame link (a `WebSocket` to a C# engine that
is running) is NOT wanted here — `germio` does not run on its own
outside Unity the way `Animo`'s own console can. What IS wanted,
and was carried over as it was, is its look: an old, dense,
engineering-tool style (`--window` `--panel` `--field` `--line`
color names, thin, 1-pixel line borders, a grey button that goes
from light to dark, small 11-12px words, a panel head with words
in bold). A later pass put in rounder button corners (closer to
the real Unity Editor's own look), a way to move a Node by
dragging it, lists that fill in a key field (`trigger`,
`condition`, `value`) as a person types, and a table of fields
styled like an old `Visual Studio` (`VB6`-time) build tool — a
name-and-value table with rows in two colors, one after the
other.

**A first mock was shown, and checked over 15 points.** A flat
list of 15 jobs, some big, some small, with no order to them, was
not a good way to split the work, since some jobs need others
done first, and some are far bigger than others. The work is now
split into phases, each phase built on top of the one before it,
and each phase split into small, clear tasks:

+ [ ] **Phase 0 — the base: real nested data.** Every later phase
        leans on this one.
  + [ ] 0-1: rebuild the tree's own data around germio's real,
        nested `children: []` array, not a flat `parent` pointer.
  + [ ] 0-2: redraw the tree by walking `children` on its own.
  + [ ] 0-3: redo drag-and-drop to add the dragged Node straight
        into the drop target's own `children`.
  + [ ] 0-4: before a drop is allowed, check the drop target is
        not a descendant of the dragged Node itself (the same
        loop check as the real V026 rule); block the drop if it
        is.
+ [ ] **Phase 1 — read and write a real file.**
  + [ ] 1-1: open a file with `showOpenFilePicker`, for browsers
        that support it.
  + [ ] 1-2: a fallback open path with a plain
        `<input type="file">`, for browsers that do not.
  + [ ] 1-3: parse the read text into the Phase 0 data shape.
  + [ ] 1-4: save back to the same file with
        `showSaveFilePicker`, for browsers that support it.
  + [ ] 1-5: a fallback save path with a plain download link.
  + [ ] 1-6: turn the data shape back into germio.json's own
        exact field order and types.
+ [ ] **Phase 2 — widen what can be edited.**
  + [ ] 2-1: check a new or renamed `Node.id` is unique across
        the whole file.
  + [ ] 2-2: check a new or renamed `Rule.id` is unique inside
        its own Node.
  + [ ] 2-3: a Node property block (`scene`, `name`) pinned
        above the Rule list.
  + [ ] 2-4: a "State" tab: `flags`, `counters`, `inventory`, and
        `persistence`, each as a list plus an add-row form.
  + [ ] 2-5: a "Next" block in the Node's property grid, each
        row holding an `id` and a `condition`.
  + [ ] 2-6: a small `once: true/false` badge on each rule card.
+ [ ] **Phase 3 — a real Command editor (more than one kind at
        once).**
  + [ ] 3-1: turn the single command-kind picker into a
        checklist; each checked kind shows its own small form.
  + [ ] 3-2: a `set_flag` form (a key, plus a bool).
  + [ ] 3-3: an `update_counter` form (a key, a delta, an op).
  + [ ] 3-4: an `update_inventory` form (a key, a delta).
  + [ ] 3-5: a `request_transition` / `request_notify` form (one
        plain string each).
  + [ ] 3-6: a `set_persistence` form (a key, plus a string
        value).
  + [ ] 3-7: a `record_event` form (a kind, a target_id).
  + [ ] 3-8: a `reset_*` form (three bools: flags, counters,
        inventory).
  + [ ] 3-9: write out only the checked kinds into the Command
        object on save.
+ [ ] **Phase 4 — a safety net: undo.**
  + [ ] 4-1: keep a stack of full-file JSON snapshots, one per
        change made.
  + [ ] 4-2: a button (and Ctrl+Z) that steps the stack back one
        and redraws.
+ [ ] **Phase 5 — link to the Validator (the biggest job here).**
  + [ ] 5-1: pick how: hand-port the checks to JavaScript, or
        compile the real `Validator.cs` to WASM and call it.
  + [ ] 5-2: build all of V001-V027 under the picked way.
  + [ ] 5-3: run every rule again after each change and keep the
        results.
  + [ ] 5-4: a small warning icon on a Node's own tree row when
        it fails a rule, click for the detail.
  + [ ] 5-5: autocomplete a known key right after typing
        `flags.` / `counters.` / `inventory.` in a condition.
  + [ ] 5-6: a small parser that flags a broken condition before
        save, the same way V009 would.
+ [ ] **Phase 6 — work without a mouse.**
  + [ ] 6-1: up/down buttons on each tree row.
  + [ ] 6-2: indent/outdent buttons on each tree row.
  + [ ] 6-3: `role="tree"`, `role="treeitem"`, `aria-expanded`,
        and the rest of the ARIA a real tree needs.

**Where this stands**: built, working, and checked in a real
browser by the master on a real machine — done. `Editor/` in this
repo has a Vitest test suite (159 tests) covering the pure logic
in `src/lib/`, real end-to-end integration tests that load the
actual `main.js` in a jsdom page and drive it through the
open/select/edit/save/undo flow, and one test built from a real,
unmodified `germio.json` pulled from stemic itself (`real_data.test.js`)
— every earlier fixture in this suite had spelled out
`"children": []` on every leaf node by hand, which is not how a
real file looks (germio's own C# JSON writer skips an empty array
field entirely), and this gap only surfaced once the master opened
the tool in a real browser and hit a real, live error. Every phase
(0 through 6) and every task under them is done; V009 (a true DSL
parse check) stays out of scope on purpose, replaced by a light,
best-effort sanity check in `condition_syntax.js`. Two real bugs
were found and fixed along the way: an XSS hole (every
user-supplied string from the loaded file is now escaped before it
ever touches innerHTML) and a missing `.gitignore` that would have
committed the whole `node_modules` folder. The look was reworked
twice more after the first pass shipped — first away from a
retro, Windows-95-style bevel look (too dated), then rebuilt with
a real 8px spacing scale, colored badges per command kind, and a
proper "Selected node" header, matched detail-for-detail against
an agreed design mock rather than left as a rough first pass.

**How to run it**: `Editor/` ships as part of germio itself, so it
reaches a game repo (stemic, flugi, tropika) the same way the rest
of germio's own Scripts do — through the git submodule at
`game/Assets/Plugins/Germio/`. From that path:

    cd game/Assets/Plugins/Germio/Editor
    npm install
    npm run dev

Then open the printed `http://localhost` address in a browser, and
use the "Open germio.json" button to browse to the real file for
that game — for stemic, that is
`game/Assets/StreamingAssets/germio.json`, three directories up
from `Editor/` itself. ES modules, the File System Access API, and
the Clipboard API all need a real server behind `http://`, not a
plain double-clicked `file://` page — `npm run dev` is what gives
that.

### TASK-005

  URL in the Package Manager)**: right now, every game repo
  (`stemic`, `flugi`, `tropika`) pulls `germio` in as a git
  submodule. A plain user most likely looks for `Window > Package
  Manager > + > Add package from git URL`, and not a submodule,
  since Unity's own Package Manager already works with a git URL
  right out of the box.

**Why this does not go against how `germio` itself gets built**:
the master builds `germio` in one part, and uses it in another. As
the builder, the master keeps a plain, separate git clone of
`germio` (the same kind of clone this whole long chat ran with
tonight) to change, commit, and push from. As a user (working
inside `stemic`, `flugi`, or `tropika`), the master would then
pull `germio` the same way any other person would — through the
Package Manager's git URL, pointed at the latest tag or commit.
Trying out a new, not-yet-pushed `germio` change inside a game
repo would only mean pointing that git URL again, once the change
has been pushed from the separate builder clone. The two parts
never work against each other.

**What is not there today**: `package.json` (the file Unity's
Package Manager reads to know a folder is a package at all) and
any `.asmdef` file (a file that names a group of code as one
build part — not a hard need, but it makes building faster and
makes clear what `germio` itself needs). Neither is in `germio`
right now.

**The work this would take**:

1. add a `package.json` at `germio`'s own root (`name`,
   `version`, `displayName`, and the rest of the standard fields
   a Unity Package needs).
2. add an `.asmdef` that covers `Scripts/`.
3. take out the `.gitmodules` line in `stemic`, `flugi`, and
   `tropika`, and bring `germio` back in through `Add package
   from git URL` in each.

**Where this stands**: not started, agreed on in chat only.

### TASK-006

`CLAUDE.md`, `TASKLIST.md`, `HANDOFF.md`, `writing_standard.md`,
`coding_standard.md`, and `tech_terms.md` are all in Basic English now,
in this repository and in `stemic`, `flugi`, `tropika`, `briko`, `animo`,
and `opinio`. The rest of the docs are not: `README.md`, `overview_EN.md`,
and every file under `docs/` (`dsl_spec.md`, `mcp_spec.md`,
`security_spec.md`, `naming_spec.md`, `llm_design_spec.md`,
`llm_workflow_guide.md`, `germio_roadmap.md`, `save_data_spec.md`,
`dsl_cookbook.md`, `scene_code_sync_spec.md`) still fail the check, from
tens of words to as many as 234 in one file.

Also still open: words put into `draft_words.md` tonight, in a hurry, to
get `coding_standard.md` and `tech_terms.md` to pass. Some of these are
real technical words (`Constructor`, `Destructor`, `Indexer`, `Operator`,
`alias`, `directive`, `algorithm`, `keyword`, and more) that should move
to `tech_terms.md`, each with its own short sense, and not sit in
`draft_words.md` with no sense given at all. This move needs the
master's own GO first, word by word.

### TASK-007

`germio_roadmap.md` (2326 lines) holds both a record of real, done
work and a plan for work still ahead, but its own last update was
2026-05-04 — well before tonight's `request_notify` fix and the
`germio.json` editor tool, and before that, a long stretch of other
real work. Read through the document again, phase by phase, and mark
each part done, still open, or no longer wanted, the way
`animo_roadmap.md` and `docs/live_monitor_spec.md` were just brought
up to date. Given the size of this document, split the work itself
into a few smaller passes rather than one long one.

### TASK-008

`signo`'s own TASK-027 (a true spec and a real, by-ear sound-quality
pass for its own SE engine) is a true, needed step before this
whole phase — a real check already found `SEType.Jump` not yet
game-ready. A plain, given wait/check gate, marked done once that
true work lands.

### TASK-009

`quyno`'s own P-06 (joining `Quyno` to a real `germio` game) is a
true, needed step before this whole phase — `Quyno.Bridge` must
give this repository a true, given call-in point (its own exact
shape still open, held on `signo`'s own TASK-027 first). A plain,
given wait/check gate, marked done once that true work lands.

### TASK-010

`SoundSystem.cs`'s own `MusicClip` enum holds one true value
(`BeatLevel`) today. Grow it to at least six, to match `stemic`'s
own true song set (`Title`, `Level1..3`, `Ending`, `BeatLevel`),
given TASK-008/009 land first.

### TASK-011

`SoundSystem.cs`'s own `SfxClip` enum (seven true values:
`Item`/`Jump`/`Climb`/`Walk`/`Run`/`Grounded`/`Push`) does not map
one-to-one onto `Signo`'s own `SEType` (eight true values:
`Laser`/`Explosion`/`Pickup`/`Powerup`/`Jump`/`Hit`/`Blip`/`Alarm`).
Work out a true mapping between the two sets (`Push`, say, has no
plain `Signo` match today; `Explosion`/`Powerup`/`Hit`/`Blip`/
`Alarm` have no plain `SfxClip` match), given TASK-008/009 land
first.

### TASK-012

Replace `SoundSystem.cs`'s own inner true work (today, a given
`AudioClip` played through a plain Unity `AudioSource`) with a call
through the true entry point TASK-009 built, given TASK-010/011
settle the true shape first. **The public `Play(SfxClip)`/
`Play(MusicClip)` shape must not change at all** — every game that
already calls `SoundSystem` (`stemic`, `flugi`, `tropika`) must
keep working with no change owed on its own true side.

### TASK-013

Play the true, live-built sound for real, inside `stemic` (the
first game checked), and check every `SfxClip`/`MusicClip` value
still plays true — the true, final check that closes this whole
phase, before `flugi`/`tropika` are checked the same true way.

### TASK-014

**Dropped 2026-08-21, and moved to `modio`.** This once held a plan
for a shared sensor class here, with its own full design in
`docs/sensor_spec.md` (now taken away).

Held up against what `modio` truly asks, that plan broke in three
places, all from one cause: **seeking had been cut off from
remembering.**

+ It picked things by `layer_mask`. `stemic` holds only Unity's own
  five stock layers — no Block, Ground or Player layer at all — while
  `germio` picks by name (`Like()`) through all three builds.
+ It gave back one thing only. "Find a Block not yet met" needs every
  near thing, then a choice against memory; one thing back leaves no
  second try.
+ No fading rate for that memory could be settled with seeking here
  and memory there.

The drop-off check went too. A character that walks to the same drop,
turns away, and walks back again has taken in nothing: **knowing an
edge is dangerous is remembering it.**

`germio` holds no part of it. `modio` calls Unity's own `Physics`
straight. See `modio`'s own `docs/modio_spec.md` §3.3.

### TASK-015

**Dropped 2026-08-21.** This once asked for a mark on every level
piece, so `modio` could say "I have been to that one before".

Nothing is needed. **Unity's own `GetInstanceID()` already gives every
`GameObject` a mark, and it holds while that one thing stands.**

Three other ways were weighed first, and each was dropped:

| Tried                      | Why it was dropped                                                                                                                                         |
| -------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| The object name            | Not one to a piece. Counted: `stemic`'s own `Level_1` holds 24 pieces, and three names are used twice over.                                                |
| A mark added to each piece | It would take a `MonoBehaviour`, and `Common` runs three `FixedUpdate` chains. On 24 pieces that is 72 chains, every step of the motion work, for nothing. |
| Where it stands            | Two ways of doing one job — one for things that move, one for things that do not. **One way, no exceptions.**                                              |

`GetInstanceID()` is made new when a scene is read again, and that is
right: a scene read again is a world built new, and the old pieces are
gone with it. `modio` holds the memory of a **place** apart from the
memory of a **thing**, so what is worth keeping is kept (see `modio`'s
own `docs/modio_spec.md` §4.5).

### The order these run in

TASK-016 to TASK-034 stand on their own, and may be taken in any
order. **TASK-035 and TASK-036 come last**: TASK-035 reads a whole
deed, so it cannot go green until every part before it does, and
TASK-036 holds what already stands.

Within each group, the first task reads a thing off the JSON, and the
rest lean on it:

| Group          | First    | Then                 |
| -------------- | -------- | -------------------- |
| `actor`        | TASK-016 | TASK-017 to TASK-019 |
| `update_need`  | TASK-020 | TASK-021 to TASK-023 |
| `request_deed` | TASK-024 | TASK-025 to TASK-031 |
| `$target`      | TASK-032 | TASK-033, TASK-034   |

### TASK-016

`Rule` today holds `id`, `trigger`, `condition`, `command`, `once`.
Add `actor`, a plain name, and read it off the JSON.

**Test first:** a rule with `"actor": "npc_01"` gives back `npc_01`;
a rule with none gives back empty.

### TASK-017

`Store.DispatchTrigger(string trigger_id)` and
`Bus.Publish(string signal_id)` take no actor today, so no caller can
say whose rule to fire.

Add an actor to both, able to be left out. Left out, it stands for the
world.

**Test first:** `DispatchTrigger(id, actor: "npc_01")` builds and runs.

### TASK-018

A rule with no `actor` belongs to the world, as every rule does today.
**It must fire whoever calls** — with an actor named or not.

**Test first:** a rule with no actor, called with `actor: "npc_01"`,
still fires.

### TASK-019

A rule with an `actor` belongs to that one character.

**Test first:** a rule with `actor: "npc_01"`, called with
`actor: "npc_02"`, does **not** fire. Called with `npc_01`, it does.

### TASK-020

Add `update_need` to `Command`, **as a list**:

    "update_need": [ { "key": "loneliness", "delta": -30 },
                     { "key": "separation", "delta": -40 } ]

A list even where it holds one. `modio`'s own `docs/modio_spec.md`
§5.4 sets out why: one arrival must be able to quiet two wants, or a
Need climbs with no way down.

**Test first:** a two-entry list reads back as two.

### TASK-021

`germio` knows nothing of `animo`. So the Executor does not call it;
it fires an event out of the `Store`, the same way
`request_notify` fires `NotifyRequested` today.

**Test first:** a two-entry `update_need` fires **twice**, each
carrying its own key and delta.

### TASK-022

V028: a `update_need` entry with an empty `key` names no Need at all.
**Error.**

### TASK-023

V029: a `delta` of zero moves nothing. Nothing breaks, but nothing
happens either, so it is likely a slip. **Warning**, matching V008 and
V027, which warn rather than stop.

### TASK-024

Add `request_deed` to `Command`, beside `request_transition` and
`request_notify` — all three ask for what does not finish on the spot.

Five parts: `target`, `condition`, `motion`, `until`, `command`. Full
shape in `modio`'s own `docs/modio_spec.md` §7.3.

**Test first:** all five read back off the JSON.

### TASK-025

`request_deed.command` is `germio`'s own `Command` type, held inside,
so every command there is works within a deed with nothing new added.

**Test first:** a `request_deed` holding `update_need` **and**
`record_event` reads both back.

### TASK-026

A deed that seeks nothing — standing still to rest, or calling out —
holds no `target` at all.

**Test first:** a `request_deed` with no `target` reads back with
`target` empty, and throws nothing.

### TASK-027

The Executor fires a deed out of the `Store`, the same way TASK-021
fires a need.

**Test first:** one `request_deed` fires the event once, carrying all
five parts.

### TASK-028

V030: `motion` must be one of the seven doing-states — `idle`, `walk`,
`run`, `backward`, `jump`, `abort_jump`, `stop`. Anything else names
no motion the body has. **Error.**

### TASK-029

V031: `until` holds exactly one key, out of `near`, `meets`,
`elapsed`, `while` (see §7.6 there). None, and the deed never ends;
two, and which one wins is anyone's guess. **Error.**

### TASK-030

V032: a `request_deed` inside a `request_deed` would set one stretch
of time running inside another, with no way to say which lock holds.
**Error.**

### TASK-031

V033: `target.kind` must be one of the six type marks in `Env.cs` —
`Ground`, `Block`, `Wall`, `Human`, `Item`, `Home`. Anything else
names nothing in the world. **Error.**

### TASK-032

A deed cannot name up front what it has not yet found, so `$target`
stands for it. Before the Evaluator or the Executor runs, the text is
put aside for the id the deed found.

**Test first:** `"target_id=$target"` with `g_1042` gives
`"target_id=g_1042"`.

### TASK-033

**Test first:** text holding no `$target` comes back just as it was.
So does empty text, and so does text that is not there at all.

### TASK-034

**Test first:** `"like=$target, target_id=$target"` takes the id in
both places, not the first alone.

### TASK-046

`$target` is a whole word. `$targets` is another word, and `$targe` is
not the mark at all. Big and small letters count, as they do
everywhere else in `germio`.

**Test first:** `$targets`, `$targe` and `$TARGET` all come back
untouched.

### TASK-047

A value holding the mark inside it — were one ever to — must not be
looked at a second time, or the change could run away with itself.

**Test first:** putting a value that itself holds
`$target` in place of the mark gives that value back once, and stops.

### TASK-048

`request_deed.command` holds a whole `Command`, and a `Command` holds
other things again (`set_flag`, `record_event`, and the rest). **The
mark must be reached wherever text sits inside it.**

**Test first:** a `request_deed` whose held `Command` carries
`record_event.target_id = "$target"` comes back with the id in place.

**And nowhere else:** `Rule.trigger`, `Rule.id` and `actor` name the
rule itself, never what a deed found, and are left alone.

### TASK-049

Measured 2026-08-21: a value inside
`history.count(kind=..., target_id=...)` must be an Identifier, and
`ExprLexer` reads an Identifier as `[a-zA-Z_][a-zA-Z0-9_-]*`. **It may
not start with a number.**

`GetInstanceID()` gives back a plain number, so `1042` on its own
throws at parse time. **Write it out with a letter in front:
`g_1042`.**

**Test first:** an id of 1042 is written out as `g_1042`, and
`"target_id=g_1042"` is read through with nothing thrown.

### TASK-050

**Test first:** `history.time_since(kind=met, target_id=$target) > 60`,
once put in place, runs through the Evaluator and gives back a true or
false. `$` belongs to no token kind today, so nothing else is touched.

### TASK-035

**Test first:** the whole deed in `modio`'s own
`docs/modio_spec.md` §7.10 reads end to end off a real JSON file —
`actor`, `request_deed`, the `Command` held inside, and `$target` in
three places.

### TASK-036

**Test first:** every test already standing stays green — not the 13
in `ExecutorTests.cs` alone, but `CommandTests.cs` (17),
`ValidatorTests.cs` (24), `NodeTests.cs`, `ScenarioTests.cs`,
`HistoryTests.cs` and the rest. **Over 50 together.** Nothing added
here may break what stands.

### TASK-037

Some of them end in a doing that no motion covers: handing a thing
over, taking one up, putting one down, holding one up to be seen, or
caring for another. These take an `act`, written beside `motion`
(`modio`'s own `docs/modio_spec.md` §7.5).

7 of the 10 need none, so it may be left out.

**Test first:** a `request_deed` with `"act": "hand_over"` reads it
back; one with no `act` reads back empty, and throws nothing.

### TASK-038

V034: `act` must be one of `hand_over`, `take_up`, `put_down`, `show`,
`tend`. Anything else names nothing that can be done. **Error.**

Counted off against every deed the two given personas hold: `ShowFind`
takes `show`, `Tend` takes `tend`, `Give` takes `hand_over`. The other
7 take no act at all.

### TASK-039

**Dropped 2026-08-21.** This asked for a check on
`{ "reparented": ... }`, an `until` that waited on the parent-child
tie moving.

**No such `until` stands any more.** Handing a thing over is an `act`,
and an act ends on its own clock. A deed that hands something over is
watched to the point of arrival — `{ "near": 1.5 }` — and the handing
is what follows. See `modio`'s own `docs/modio_spec.md` §7.6.

### TASK-040

`Store` holds two events today: `TransitionRequested` and
`NotifyRequested`. Add `NeedRequested`, in the same shape, and a
method that fires it.

`request_notify` shows the whole road: the Executor calls
`store.RequestNotify(id)`, `Store` fires `NotifyRequested`, and
`NoticeSystem` hears it. **`update_need` takes the same road**, and
`modio` is what hears it.

**Test first:** a `update_need` holding two entries fires the event
twice, each carrying its own key and delta.

### TASK-041

The same again, for `request_deed`: add `DeedRequested`, and a method
that fires it.

**Test first:** one `request_deed` fires the event once, carrying all
five of its parts.

### TASK-042

TASK-017 adds an actor to `Bus.Publish`, able to be left out. Every
caller standing today must go on working with nothing changed.

`Despawn.cs` is one such caller
(`_game_system.Bus?.Publish("sig_despawn")`). **Find them all before
changing the shape**, and leave each as it stands.

**Test first:** a call with no actor named still fires every rule with
no actor of its own.

---

## A note on V numbers

V numbers are never used twice. **V035 was taken and then let go**
(TASK-039), and its number stays empty — a reader of an older file
should never find it meaning something new.

### TASK-043

`Store.NotifyRequested` shows a line for the whole screen. There is no
way to show a line over one character's head.

`super-nekokun`'s own `Enemy.cs` had one (`say()`), and used it to
show what a character had in mind: "I change direction", "I wait...",
"I hit the wall...". **A mind that cannot be seen cannot be checked by
eye**, and with `modio` driving characters, a great deal will need
checking that way.

### TASK-044

Rules for the world (no `actor`) and rules for each character (an
`actor` named) sit side by side under one `Node`. With many
characters, which rule belongs to which character grows hard to see.

The Validator already walks every rule under every node. **Have it
give back a list by `actor`**, so a reader may take one character's
rules on their own.

### TASK-045

`history.count(kind=..., target_id=...)` asks after one thing, named.
**Add `like`, which asks after every row whose thing was of a sort
with the one named.**

    history.count(kind=met,  target_id=$target)     this one
    history.count(kind=edge, like=$target)          ones like it

This is how `modio` looks ahead: not by working a Need forward, but by
asking how it went with things of this sort before (`modio`'s own
`docs/modio_spec.md` §4.7). **The same table, the same call, one word
changed.**

`like` takes the same place `target_id` does, and the two are never
written together.

### TASK-051

V007 warns where a rule holds an empty `condition`, since such a rule
fires every time its trigger comes.

**Every deed rule will hold one.** `animo` has already settled what to
do (§7.10 in `modio`'s own spec); the rule's own `condition` is for
the world, and a deed leaves it empty. Of the 10 the two given
personas hold, **8 would warn every time**, and with 64 characters
running the log would fill with warnings that mean nothing, hiding the
ones that do.

**Hold V007 back where `actor` is not empty.**

### TASK-052

V008 warns where `once` is false and the command holds `set_flag`,
since such a rule could set the same flag over and over.

**Every deed rule holds `once: false`**, and `Give` holds a `set_flag`
inside its own deed. But a deed sets that flag only where it truly
lands — once, at the end of a stretch of work — so there is no loop
at all.

**Hold V008 back where the `set_flag` sits inside a `request_deed`.**

### TASK-053

V010 warns of a command it does not know. Its list already grew once,
for `reset_flags`, `reset_counters` and `reset_inventory`.

**Add `request_deed` and `update_need`**, or every deed rule will read
as unknown.

### TASK-054

`Scripts/Schema/SchemaExporter.cs` writes out the JSON schema a writer
— or an LLM — reads to know what a `germio.json` may hold.

**Add `actor` on `Rule`, and `request_deed` and `update_need` on
`Command`**, with every part of each. Left out, nothing outside would
know a deed may be written at all.

### TASK-055

`request_deed` holds a `Command` inside it, and that Command holds
others again. Nothing in `germio` copies a `Rule` or a `Command`
deeply today, so two rules built off one another would share what
sits inside.

`animo`'s own `Data.cs` gives every model type its own `DeepCopy()`
for this reason.

**Give `Rule`, `Command` and `RequestDeed` one each.**

### TASK-056

Two things are called `condition`, and they are not the same:

| Where                    | Asks                          | Holds `$target`? |
| ------------------------ | ----------------------------- | ---------------- |
| `Rule.condition`         | should this deed begin at all | no               |
| `request_deed.condition` | which found thing to take     | **yes**          |

V009 checks a condition's types by reading it. **A deed condition
cannot be read until `$target` is put in place**, and at check time no
deed is running.

**So V009 runs on `Rule.condition` as it does today, and on a deed
condition only after a stand-in id is put in.** A well-formed
stand-in (`g_0`) is enough to check the shape.

### TASK-057

`Rule.trigger` matches either a `Zone.zone_id` or a `Bus` signal. A
Zone belongs to the world — a place a body walks into — not to any
character.

**Where a Zone fires a rule, `actor` is left empty**, so only the
world's own rules answer. A character's rules are reached through
`Bus.Publish` with an actor named, and no other way.

### TASK-058

V036: an `actor` names a persona. A slip in the name — an `O` for a
`0` — would leave a rule that fires for nobody, and nothing would say
so.

**Check every `actor` against the `agent_id` list the personas hold.**
**Error** where no persona answers to it.
