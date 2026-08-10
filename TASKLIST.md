# TASKLIST

Outstanding work items for this repository. Anyone may add an entry;
whoever picks it up checks it off (`- [x]`) and commits the diff.

+ [ ] **Scene wiring checker / auto-fixer**: a Unity scene that backs a
      `germio.json` node needs two kinds of wiring that nothing currently
      verifies, so a mistake silently breaks the game at play time with no
      compile-time or edit-time warning:

    1. **Required singleton objects.** The `Scene` base class resolves
       `GameSystem` (and games often add `SoundSystem`, etc.) by name via
       `Find(name: GAME_SYSTEM)`. If that GameObject is missing from the
       scene, `GameSystem` stays null and the first `signal_btn_*` publish
       throws `NullReferenceException`. Every playable scene must contain
       these singleton objects with their components attached.

    2. **Scene-derived component.** Each node with a non-empty `scene`
       field expects a GameObject in that scene carrying the game's own
       `Scene`-derived leaf class (e.g. `Title`, `Level1`) whose
       `[GermioSceneHandler(id: "...")]` methods cover that node's id
       (directly or via an ancestor class).

    3. **Build scene list registration + name match.** Every `scene`
       value in `germio.json` must (a) correspond to an actual `.unity`
       file whose name matches exactly, and (b) be registered in the
       active Build Profile / EditorBuildSettings scene list. A missing
       registration throws "Scene 'X' couldn't be loaded ... not been
       added to the build profile" only at transition time. A name
       mismatch (e.g. json says `Level1` but the file is `Level_1.unity`)
       fails the same way. The checker must flag both.

    Build an Editor tool that reads the scenario tree, opens (or inspects)
    each referenced `.unity` scene, checks all three kinds of wiring,
    reports gaps in a Dashboard-style window, and optionally auto-fixes
    them (create missing GameObjects + components, register scenes in the
    build list, flag name mismatches). This would have caught every flugi
    title-to-level failure at edit time instead of at play time: no
    `Scene`-derived component existed anywhere; the `GameSystem` object
    reference was missing from the scene; the target scenes were absent
    from the build list; and the json scene names (`Level1`) did not match
    the files (`Level_1.unity`).

    Related building blocks already in the codebase to reuse or extend:
    `SceneCodeSyncer` (germio.json → C# class tree) covers the `.cs` side
    but never touches `.unity` files; briko's `Exporter`/`Importer` show
    the `.unity` YAML read/write pattern for GameObjects and components.

+ [ ] **A timed, ordered event sequence in the DSL (`play_sequence`)**:
      right now, anything that must happen *after* a Rule fires — a
      pause message, a level-clear message, a level-start name display —
      is hard-coded straight into C# (`Germio.Systems.NoticeSystem`
      listens for `GameSystem.OnPauseOn` / `OnCameBackHome` and sets fixed
      English text). This works for a single flat message, but breaks
      down the moment a game wants a **game-specific, multi-step,
      timed** post-clear presentation — exactly the kind of thing G11
      (Declarative, Not Procedural) says should live in the JSON, not in
      C#.

    **Reference cases studied (retro games, chosen for contrast):**

      + *Super Mario 64* (a star grab): the instant the star is touched IS
      the goal; everything after (pose, camera pan, fanfare, an
      automatic walk back to the lobby with **no button press needed**)
      is a fixed, linear, no-branching sequence of small events.
      + *Tomb Raider* (a room puzzle): clearing a room can need several
      switches thrown in some order, and the reward is often a full,
      pre-rendered cutscene far too complex for any DSL to describe the
      *content* of — only its *place in the order* is worth naming.

    Both cases point to the same conclusion: **the DSL should never hold
    the content of a presentation step, only its name, its place in the
    order, and roughly when it fires.** What a given name actually does
    (play an animation, show text, run a full cutscene) is always left to
    the game's own C# (`GameDev`), the same way `trigger` and
    `HistoryEntry.kind` are already free-form strings the game gives
    meaning to.

    **Sketch of the shape**:

        "command": {
          "set_flag": { "key": "star_collected", "value": true },
          "play_sequence": [
            { "event": "star_get_pose",       "delay": 0.0 },
            { "event": "show_message",        "delay": 0.5, "params": { "text": "Star Get!" } },
            { "event": "play_movie",          "delay": 1.0, "params": { "clip": "ending_cutscene_01" } },
            { "event": "request_transition",  "delay": 0.0, "params": { "target": "lobby" } }
          ]
        }

    **Merits**:

      + closes the one real gap found tonight: there is currently no way
      at all to write "show this text, in this order, with this timing"
      in `germio.json` — only in C#.
      + stays open-ended on purpose: a brand-new kind of step (a camera
      move, an input lock, a future feature nobody has thought of yet)
      needs zero changes to germio's own schema, since `event` is a
      free-form name and `params` is a free-form bag, exactly mirroring
      how `trigger` and `HistoryEntry.kind` already work.
      + covers both reference cases at the same grain: Mario's simple pose
        + fanfare, and Tomb Raider's full cutscene, are both just one
      `event` name apiece — the DSL never needs to know which is simple
      and which is complex.

    **Demerits (found in a 15-point critique before any code was
    written)**:

    1. `Executor.Execute` today is a synchronous, one-shot static
       method. A `delay`-bearing sequence needs something that spans
       many frames (a coroutine or similar) — this is not a small field
       addition, it is a change to Executor's whole execution model.
    2. `play_sequence`'s own `event: "request_transition"` step would
       sit alongside the already-existing top-level
       `Command.request_transition`, creating two ways to ask for the
       same thing with no rule for which one to use.
    3. the same duplication risk applies to `set_flag` and any other
       existing Command field, if a step ever wants to change state.
    4. a fully free-form `params` bag re-opens exactly the kind of
       "anything-goes" surface G13 (Minimal Closed DSL) was written to
       close off — this is a real tension, not a small one.
    5. no rule is set for what happens when the game does not recognise
       an `event` name (ignore it? warn? hard error?).
    6. `delay`'s reference point is undefined (from sequence start, or
       from the previous step?) — this alone would confuse both an LLM
       and a human author.
    7. no thought given to a rule re-firing mid-sequence (when
       `once: false`) and possibly starting a second, overlapping
       sequence.
    8. no thought given to a player pressing a button mid-sequence —
       should the sequence keep going, stop, or be ignored entirely?
    9. a sequence's mid-flight progress cannot be captured in a Snapshot
       at all under the current model, so a save made mid-sequence has
       no way to resume it correctly on load (a genuine G20 concern).
    10. no thought given to whether a `play_sequence` step should also
        write a `HistoryEntry`, and if so, how that differs from
        `record_event`.
    11. no new Validator rule has been designed for this at all (a
        missing `event`, a malformed `params`, and so on).
    12. `CookbookExamplesTests.cs` checks known-good JSON shapes; with a
        free-form `params`, there is no fixed shape left to check against.
    13. nothing has been designed yet for *who*, on the game side,
        actually listens for a `Store`-level sequence-step event and
        dispatches each named `event` to real code.
    14. the new words (`play_sequence`, `event`, `delay`, `params`) have
        not been checked against G16 (four-layer naming) for whether
        they sit at the same part of speech and the same level of
        abstraction as `set_flag`, `update_counter`, and the rest.
    15. building this for real needs four things together — an async
        Executor, a new Store-level event, new Validator rules, and a
        game-side dispatcher — which is too large a change to start
        the same night it was first raised; it needs its own, dedicated
        design pass.

    **Where this stands**: not started. No animo or briko precedent was
    found for a timed, ordered event chain (animo's own JSON is purely
    numeric — rates, thresholds, coefficients; briko's is purely spatial
    — grid units, block placement), so this would be new ground for the
    whole G+B+A family, not something to copy from a sibling library.

+ [ ] **`Command.request_notify` — a narrow, immediate fix chosen ahead
      of `play_sequence`, with known gaps left open on purpose**: this
      entry records the full path taken to reach `request_notify`, so a
      later reader does not have to re-walk it, and so the gaps it
      still carries are not mistaken for oversights.

    **The immediate need**: in flugi, `NoticeSystem` shows "Level
    Clear!" the instant `GameSystem.OnCameBackHome` fires — the moment
    the player's body touches the Home object, not the moment the
    level truly clears (pressing the A button while `is_beat` and
    `player_at_home` both hold). The text shows too early.

    **Design paths tried and rejected, in order:**

    1. *`show_message: string` as a seventh `Command` field.* Rejected:
       every existing `Command` field (`set_flag`, `update_counter`,
       `update_inventory`, `request_transition`, `set_persistence`,
       `record_event`) mutates State or History — a change of state.
       Showing text on screen changes nothing that is saved; it does
       not belong next to fields whose whole point is a state change.
    2. *`message: string` as a new field on `Rule` itself, next to
       `command`.* Rejected as the same mistake moved one level up:
       nothing stops the next need (a sound, a camera move) from
       demanding its own new top-level field too, and `Rule` grows a
       field per concern forever, with no rule for where a new concern
       should go.
    3. *`notify: string`, a free-form string on `Rule`, mirroring
       `Rule.trigger` and `HistoryEntry.kind`* (both already free-form
       strings germio itself never reads the meaning of — the game
       gives them meaning). This is the path that held up.

    **Five merits weighed for path 3**: stays open to any future
    meaning with zero schema change; matches germio's own existing
    precedent (`trigger`, `HistoryEntry.kind`); reads as the natural
    pair to `trigger` (`trigger` is the signal coming in, `notify` the
    signal going out); a path is open to keeping the actual English
    text out of the JSON entirely (an id such as `"level_clear"`, with
    the real string held in a C# table) if the game is ever localised;
    keeps `Command` itself untouched, so its "every field is a state
    change" shape is not muddied.

    **Five demerits weighed, and what became of each**:

    1. the string's meaning is invisible from the JSON alone — **not
       fixed**, only accepted, on the grounds that `trigger` already
       carries the same weakness and germio has lived with it from the
       start.
    2. the Validator cannot check that a given string is ever actually
       handled on the game side — **partly fixed** (an empty string can
       be flagged), the deeper question (does the game truly handle
       this id) stays open, for the same reason as #1.
    3. the order `command` and the notify fire in, relative to each
       other, was undefined — **fixed**: a plain rule (`command` first,
       then the notify) closes this with no technical hurdle at all.
    4. whether `once` covers the notify too was undefined — **fixed**
       the same way: a plain rule (`once` covers the whole Rule, notify
       included) closes it.
    5. one Rule firing cannot carry more than one notify at once, since
       the field holds a single string — **not fixed**; this is a real
       structural limit. Turning it into a list would, in effect,
       re-open `play_sequence` (see the entry above) through a side
       door, which is out of scope for tonight's narrow fix.

    **Ten retro games checked against a single-string notify, to see
    how far "one notify per Rule" actually reaches:**

    | Game | Moment checked | Fits a single notify? |
    | --- | --- | --- |
    | Super Mario Bros. | touching the goal flag | yes — the moment is simple and singular |
    | Super Mario 64 | grabbing a star | mostly — one notify covers "cleared", but the pose/fanfare/walk *sequence* after it still needs C#, or `play_sequence` later |
    | Tomb Raider | a multi-switch door opening | yes — "door opened" is one notify; the cutscene's own content is always the game's job, never the DSL's |
    | The Legend of Zelda (a dungeon clear) | boss defeated, item appears | mostly — one notify fires the fanfare, but the fanfare-then-item-appears staging still needs C#, or `play_sequence` later |
    | Dragon Quest (a level-up) | after a battle's XP is added | no — more than one party member can level up in the same battle; a single string cannot carry more than one at once |
    | Mega Man (a boss defeated) | screen flash, exit, stage select | mostly — one notify fires it, but the flash-then-exit-then-transition staging still needs C#, or `play_sequence` later |
    | Pac-Man (clearing a board) | the last dot eaten | yes — one fixed, simple flash-then-advance pattern |
    | Final Fantasy (a battle won) | the results screen | no — XP, items, and level-ups must all be shown together; one string cannot carry all three |
    | Sonic the Hedgehog (100-ring bonus) | the ring count crossing 100 | mostly — a sound and a life-count display are two distinct things a single notify string strains to carry at once |
    | this flugi fix (Level Clear) | the A-button-confirmed clear | yes — this is exactly what tonight's fix needs, and no more |
    | Bomberman (clearing a stage) | reaching the exit door | yes — the notify alone covers it; the door's own appearance is a separate Rule |
    | Family Circuit (a race finish) | crossing the line | mostly — the notify can fire the results screen, but rank and time are read from existing `counters`, not carried by the notify string itself |
    | Sky Kid (a successful landing) | landing on the carrier | yes — the bonus score itself runs through the existing `update_counter`, alongside the notify |
    | Tokimeki Memorial (a confession event) | more than one character's affection Threshold crosses at once | no — this needs picking one candidate among several, which a single string cannot express on its own |
    | Daisenryaku (a turn's end report) | many battles and captures in one turn | no — this needs many distinct events gathered and grouped, which is `record_event` plus History's own job, not a single notify string's |

    A first pass claimed Tokimeki Memorial and Daisenryaku could be
    solved today with Rule ordering (first-match-wins) and `History`
    aggregation respectively. On a harder look, both claims did not
    hold: Rule firing order across Rules sharing one `trigger` was
    asserted, not read from `Store`'s actual dispatch code, so
    "first-in-JSON wins" is unverified; and pointing at `History` for
    Daisenryaku only moves the whole problem into C#, it does not show
    germio's own DSL expressing anything about it. Both stand as open,
    genuine gaps against germio's own "genre-blind" claim in
    `framework_roadmap.md` §1.3 — recorded here rather than papered
    over.

    **Why `event`/`Action<string>`, not a plain data write**: `Executor`
    was read line by line. Five of the six existing Command fields
    (`set_flag`, `update_counter`, `update_inventory`,
    `set_persistence`, `record_event`) only ever write straight into
    `store.Scenario.initial_state.*` or `History` — no event fires; the
    game side must poll that data itself, whenever it needs to. Only
    `request_transition` calls `store.RequestTransition(...)`, which
    fires the one event `Store` has: `OnTransitionRequested`. The
    reason lines up cleanly: a flag is a state that holds true across
    many frames, so polling suits it; a transition matters for one
    instant only, so a push-style event is the only way to not miss it.
    A notify is exactly the same kind of one-instant thing a flag is
    not, so it should be wired the same way `request_transition` is —
    a new field, `request_notify: string?`, calling a new
    `store.RequestNotify(notify_id: ...)`, firing a new
    `OnNotifyRequested` event — not the plain-write pattern the other
    five fields use.

    **The decision reached**: adopt `Command.request_notify` (a
    single, free-form string, wired through a `Store` event exactly
    like `request_transition`) as tonight's fix, on the understanding
    that this is a **deliberately narrow, stopgap shape** — it covers
    this flugi fix and the "yes"/"mostly" rows in the table above, and
    knowingly leaves the "no" rows, and demerit #5 (more than one
    notify per Rule), for `play_sequence` (or some other, larger
    redesign) to close later.

    **Built** (through TDD, verified against the real `Data.cs`,
    `Store.cs`, `Executor.cs`, and `Validator.cs` in a throwaway test
    harness, since this sandbox has no network access to restore
    `stemic`'s own `IntegrationTests.csproj`): `Command.request_notify`;
    `Store.NotifyRequested` and `Store.RequestNotify(notify_id)`,
    wired the same way `TransitionRequested` already is;
    `request_notify` does not set `mutated` on its own, but a `Rule`
    combining it with `set_flag` (or any other state-changing field)
    still does; V010 no longer misreports a `request_notify`-only
    command as having no effect; a new V027 catches an empty or
    whitespace-only `request_notify` value. `NoticeSystem` now listens
    for `Store.NotifyRequested` and shows the level-clear message only
    for the `"level_clear"` id, in place of the old, too-early
    `HomeReturned` (was `OnCameBackHome`) hook.

    Also done in the same pass, since it touched the same files: six
    events (`Despawn.Despawned`, `Home.Returned`, `GameSystem.Paused`,
    `.Resumed`, `.LevelStarted`, `.HomeReturned`,
    `Store.TransitionRequested`) were renamed off their old,
    ungrammatical shapes (`OnDespawn`, `OnCameBack`, `OnPauseOn`,
    `OnPauseOff`, `OnStartLevel`, `OnCameBackHome`,
    `OnTransitionRequested`) to a plain past participle with no `On`
    prefix, matching `midiplayer`'s own `Started`/`Ended` precedent over
    `meowziq`'s `On`-prefixed one; `ConventionRules.cs` gained a new
    `check_participle` check (11 new tests) holding every event name to
    this shape from here on; `coding_standard.md`'s Events row and
    `stemic`'s `Levels.cs` were both updated to match.

    **Still open, on purpose, for a later pass**: the risk of
    `request_notify` and `record_event` being used to say the same
    thing twice, in two different places, with no rule against it; a
    real Unity playtest of the flugi fix this was all built for (the
    master's own next step, on the real Windows 11 build) — everything
    above was checked by unit test alone, never inside Unity itself.

+ [ ] **A germio.json viewer and editor, built without Unity**: a
      single web page that opens a `germio.json` file, shows its full
      tree in a clear way, and lets a person add, change, and remove a
      Node or a Rule with forms — no need to type raw JSON by hand, and
      no need for the Unity Editor to be open at all.

    **Why this is wanted**: right now, the only way to change
    `germio.json` is to hand-write JSON. This is a real risk for a
    typo (a missing comma breaks the whole file) and a real barrier
    for anyone who does not want to read raw JSON. A person should be
    able to set up a full Scenario with just this page and a text
    editor for `germio.json` — the Unity Editor is not a must.

    **Reference used**: `Animo`'s own `Monitor/dashboard.html`. Its
    live, frame-by-frame link (a WebSocket to a running C# engine) is
    NOT wanted here — germio does not run on its own outside Unity the
    way Animo's console can. What IS wanted, and was carried over
    one-for-one, is its look: a retro, dense, engineering-tool style
    (`--window` `--panel` `--field` `--line` color names, 1px hairline
    borders, a grey button gradient, small 11-12px text, a panel
    header with a bold label). A later pass added rounder button
    corners (closer to the real Unity Editor's own look), drag-and-
    drop node reordering, autocomplete lists on key fields (`trigger`,
    `condition`, `value`), and a Properties grid styled like an old
    Visual Studio (VB6-era) RAD tool — a label-and-value table with
    alternating row colors.

    **A first mock was shown and checked over 15 points.** A flat list
    of 15 mixed big and small jobs with no order was not a good split,
    since some jobs depend on others and some are far bigger than
    others. The work is now split into phases, each phase built on the
    one before it, and each phase split into small, concrete tasks:

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

    **Where this stands**: built and working, at `Editor/` in this
    repo, with a Vitest test suite (144 tests) covering the pure logic
    in `src/lib/`, plus real end-to-end integration tests that load
    the actual `main.js` in a jsdom page and drive it through the
    open/select/edit/save/undo flow. Every phase (0 through 6) and
    every task under them is done; V009 (a true DSL parse check)
    stays out of scope on purpose, replaced by a light,
    best-effort sanity check in `condition_syntax.js`. A late pass
    also found and fixed a real XSS hole (every user-supplied string
    from the loaded file is now escaped before it ever touches
    innerHTML) and a real risk of committing the whole `node_modules`
    folder (a missing `.gitignore`, now added). Not yet done in
    person: an actual look at it running in a real browser — every
    check above ran through jsdom, never a true browser window.

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

+ [ ] **Move germio from a git submodule to a Unity Package (a git URL
      in the Package Manager)**: right now, every game repo (stemic,
      flugi, tropika) pulls germio in as a git submodule. A plain user
      most likely expects `Window > Package Manager > + > Add package
      from git URL`, not a submodule, since Unity's own Package
      Manager already supports a git URL out of the box.

    **Why this does not clash with how germio itself gets built**: the
    master builds germio in one role, and uses it in another. As the
    builder, the master keeps a plain, separate git clone of germio (the
    same kind of clone this whole long session ran tonight) to edit,
    commit, and push from. As a user (working inside stemic, flugi, or
    tropika), the master would then pull germio the same way any other
    person would — through the Package Manager's git URL, pointed at
    the latest tag or commit. Trying out a fresh, unpushed germio
    change inside a game repo just means re-pointing that git URL once
    the change has been pushed from the separate builder clone. The two
    roles never fight each other.

    **What is missing today**: `package.json` (the file Unity's
    Package Manager reads to know a folder is a package at all) and
    any `.asmdef` file (an assembly definition — not a hard need, but
    it speeds up compiling and makes germio's own dependencies clear).
    Neither exists in germio right now.

    **The work this would take**:

    1. add a `package.json` at germio's root (`name`, `version`,
       `displayName`, and the rest of the standard UPM fields).
    2. add an `.asmdef` covering `Scripts/`.
    3. drop the `.gitmodules` entry in stemic, flugi, and tropika, and
       bring germio back in through `Add package from git URL` in each.

    **Where this stands**: not started, agreed on in chat only.
