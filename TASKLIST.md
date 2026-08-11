# TASKLIST

Work still open for this repository. Any person may put in a new
item; the person who does the work marks it done (`+ [x]`) and puts
the change in as a commit.

<!-- format: v1 | fields: status, id, title -->

+ [ ] TASK-001: Build a Scene wiring checker and auto-fixer
+ [ ] TASK-002: Add a timed event step-line to the DSL (play_sequence)
+ [ ] TASK-003: Add Command.request_notify (built; a real playtest is still open)
+ [x] TASK-004: Build a germio.json viewer and editor with no Unity (done)
+ [ ] TASK-005: Move germio to a Unity Package, off the git submodule
+ [ ] TASK-006: Put the rest of the docs into Basic English
+ [ ] TASK-007: Sync germio_roadmap.md's own state to the real code

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

| Game | Moment checked | Does a single notify fit? |
| --- | --- | --- |
| Super Mario Bros. | touching the goal flag | yes — the moment is simple, and there is only one |
| Super Mario 64 | grabbing a star | for the most part — one notify covers "cleared", but the pose/sound/walk *line of steps* after it still needs C#, or `play_sequence` later |
| Tomb Raider | a many-switch door opening | yes — "door opened" is one notify; the movie's own content is always the game's own job, never the DSL's |
| The Legend of Zelda (a dungeon clear) | boss put down, item shows | for the most part — one notify fires the sound, but the sound-then-item-shows staging still needs C#, or `play_sequence` later |
| Dragon Quest (going up a level) | after a fight's own points are added | no — more than one party member can go up a level in the same fight; a single string cannot carry more than one at once |
| Mega Man (a boss put down) | screen flash, way out, stage pick | for the most part — one notify fires it, but the flash-then-way-out-then-change staging still needs C#, or `play_sequence` later |
| Pac-Man (clearing a board) | the last dot eaten | yes — one fixed, plain flash-then-move-on way |
| Final Fantasy (a fight won) | the outcome screen | no — points, things won, and going up levels have to be shown all at once; one string cannot carry all 3 |
| Sonic the Hedgehog (100-ring bonus) | the ring count going past 100 | for the most part — a sound and a life-count showing are two different things a single notify string is pushed hard to carry at once |
| this `flugi` fix (Level Clear) | the A-button-held clear | yes — this is right what tonight's fix needs, and no more |
| Bomberman (clearing a stage) | reaching the way out door | yes — the notify alone covers it; the door's own showing is a separate Rule |
| Family Circuit (a race's end) | crossing the line | for the most part — the notify can fire the outcome screen, but rank and time are read from `counters` already there, not carried by the notify string itself |
| Sky Kid (landing well) | landing on the carrier | yes — the bonus score itself runs through `update_counter` already there, next to the notify |
| Tokimeki Memorial (a love-words event) | more than one game person's own liking crosses a line at once | no — this needs picking one out of many, which a single string cannot say on its own |
| Daisenryaku (a turn's end report) | many fights and land-takes in one turn | no — this needs many different things that took place put together and grouped, which is `record_event` and History's own job, not one notify string's |

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
