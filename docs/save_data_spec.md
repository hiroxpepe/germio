# Germio Save Data Format

> **Version**: 3.0 (after Phase 5.8 v2)
> **Schema Version**: 1
> **Format**: JSON (through Newtonsoft.Json), or bytes encrypted with
> AES-CBC
> **Last updated**: 2026-05-02

Phase 5.8 v2 brought in separate files for the Scenario and the Snapshot.
The single-file form (`germio.json`, used in v2.x and earlier) is no
longer used.

---

## 1. File Layout

### 1.1 The files

| File | Mode | Holds | Can an LLM edit it |
| --- | --- | --- | --- |
| `StreamingAssets/germio.json` | while building / plain text | the Scenario (the fixed definition) | YES |
| `StreamingAssets/germio.dat` | a finished build / encrypted | the Scenario (AES-CBC) | NO |
| `StreamingAssets/snapshot_{slot}.json` | while building / plain text | the Snapshot for that `slot` (a whole number) | NO |
| `StreamingAssets/snapshot_{slot}.dat` | a finished build / encrypted | the Snapshot for that `slot` (AES-CBC) | NO |
| `StreamingAssets/germio_key.bin` | (either) | the AES key (a fallback) | NO |

### 1.2 Splitting the work: Scenario against Snapshot

**`germio.json` (the Scenario)**:

+ **the fixed definition**, built and edited by an LLM
+ the Node tree (the scenario's own shape), plus the starting State
+ **read only**, while the game runs
+ its Schema was made public in Phase 4
  (`https://germio.dev/schemas/germio.schema.json`)

**`snapshot_{slot}.json` (the Snapshot)**:

+ **the changing data**, written by the running game itself
+ the State right now, plus the History of what has happened
+ each save slot stands on its own (`slot` is a whole number; the code as
  it stands uses slot 0, but any number of 0 or more works)
+ its schema is for internal use only; it is not made public

### 1.3 The order it tries to load a file

`Storage.LoadAsync()` (for the Scenario):

1. try `germio.json`
2. if that is not found, try `germio.dat`
3. if neither is found, give back `null` (the state of a first launch)

`Storage.LoadSnapshotAsync(slot)`:

1. try `snapshot_{slot}.json`
2. if that is not found, try `snapshot_{slot}.dat`
3. if neither is found, give back `null` (a fresh, new slot)

### 1.4 Setting your own path, with base_path

`Storage.LoadAsync` and `Storage.SaveAsync` (the Scenario ones only) take
an optional `base_path` argument; if you leave it out, it falls back to
`Directory.GetCurrentDirectory()`. The Snapshot versions
(`LoadSnapshotAsync` / `SaveSnapshotAsync` / `SaveSnapshot` /
`SnapshotExistsAsync` / `DeleteSnapshotAsync`) do **not** take a
`base_path` at all — they always work out their own path from
`Application.streamingAssetsPath`:

```csharp
await Storage.LoadAsync(base_path: Application.persistentDataPath);
await Storage.SaveSnapshotAsync(snapshot: snap, slot: 1);  // the Snapshot APIs do not take base_path
```

### 1.5 Choosing between encrypted and plain text

| Case | Scenario | Snapshot |
| --- | --- | --- |
| while building it, or an LLM editing it | plain text (`germio.json`) | plain text (`snapshot_{slot}.json`) |
| a finished release | encrypted (`germio.dat`) | encrypted (`snapshot_{slot}.dat`) |
| editing it on a server | edit it as plain text, then ship it encrypted | not used (the running game builds this on its own) |

---

## 2. Keeping the Schema in Step, Across Versions

### 2.1 The schema_version field

`Scenario.schema_version` and `Snapshot.schema_version` are both a whole
number (`1`, by default):

| Value | Germio version | What it means |
| --- | --- | --- |
| `1` | v3.0 and later (after Phase 5.8 v2) | the form used now (a recursive Node, with the Snapshot split apart) |

Note: Phase 5.8 v2 brought a schema change that breaks older files. There
is no way to bring an earlier version (close to `0`) up to date on its
own. The `Migrator` class was taken out.

### 2.2 Bringing an old-form file up to date

If you hold a `germio.json` in the v2.x form or earlier (with
`worlds[]`/`levels[]`), it must be turned into the new form by hand, or
with help from an LLM. See §5 for the rules to convert by.

### 2.3 Staying open to future changes

If `schema_version: 2` or a later number is ever brought in, building a
`Migrator` again will be looked at. Once the schema is made public in
Phase 4, a change that breaks old files should be avoided from then on.

---

## 3. Examples of the JSON's Shape

### 3.1 germio.json (the smallest working setup)

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "title",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "game",
    "name": "Game Root",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "title",
        "name": "Title Screen",
        "kind": "title",
        "scene": "Title",
        "children": [],
        "next": [
          { "id": "stage_01", "condition": "" }
        ],
        "rules": []
      },
      {
        "id": "stage_01",
        "name": "Stage 01",
        "kind": "level",
        "scene": "Stage_01",
        "children": [],
        "next": [],
        "rules": []
      }
    ],
    "next": [],
    "rules": []
  }
}
```

### 3.2 snapshot_1.json (an example, mid-game)

```json
{
  "schema_version": 1,
  "state": {
    "flags": {
      "tutorial_completed": true,
      "stage_01_cleared": true
    },
    "counters": {
      "score": 1500,
      "_session_count": 1,
      "_total_play_time": 245.7
    },
    "inventory": {
      "key_a": 1
    },
    "current_node": "stage_02",
    "current_team": "",
    "persistence": {
      "save_slot": "slot_1"
    }
  },
  "history": {
    "entries": [
      { "kind": "node_enter", "target_id": "title", "timestamp": 0.0 },
      { "kind": "node_enter", "target_id": "stage_01", "timestamp": 12.8 },
      { "kind": "rule_fire", "target_id": "rule_clear_s1", "timestamp": 145.3 },
      { "kind": "node_enter", "target_id": "stage_02", "timestamp": 148.1 }
    ],
    "max_entries": 1000
  }
}
```

---

## 4. Every Field, in Full

### 4.1 Scenario (the root)

| Field | Type | Needed | Default | What it is |
| --- | --- | --- | --- | --- |
| schema_version | a whole number | yes | 1 | the schema's own version |
| initial_state | State | yes | (empty) | the State the game starts with |
| root | Node | yes | (empty) | the top of the scenario's own tree |

### 4.2 Node (a tree that can hold more of itself)

| Field | Type | Needed | Default | What it is |
| --- | --- | --- | --- | --- |
| id | a string | yes | "" | unique across the whole Scenario |
| name | a string | no | "" | the name shown to a player |
| kind | a string | yes | "" | a type label ("world", "level", "title", "ending", "boss", "shop", or any word you choose) |
| scene | a string | needed for a leaf | "" | the Unity Scene's own name |
| children | `List<Node>` | no | [] | the child nodes (empty means this is a leaf) |
| next | `List<Next>` | no | [] | where a transition can lead |
| rules | `List<Rule>` | no | [] | the rules that live inside this node |

**Limits on the node tree**:

+ a hard limit of 10 layers deep (`Env.MAX_NODE_DEPTH`)
+ a warning past 5 layers deep (`Env.warning_node_depth`, which can be
  set)
+ a loop back to an earlier node is not allowed (checked by Validator
  V026)

### 4.3 Next (a transition)

| Field | Type | Default | What it is |
| --- | --- | --- | --- |
| id | a string | "" | the target Node's own ID |
| condition | a string | "" | a DSL string (empty means it always holds true) |

### 4.4 Rule

| Field | Type | Default | What it is |
| --- | --- | --- | --- |
| id | a string | "" | the rule's own ID (unique within its Node) |
| trigger | a string | "" | the ID it fires on (a Zone's `zone_id`, or a signal from the Bus) |
| condition | a string | "" | a DSL string |
| command | Command | (empty) | the action taken once the rule fires |
| once | a bool | true | if true, this fires at most once per session |

### 4.5 Command

Set exactly **one** state-changing field below to something other than
null; `request_notify` may be set on its own, or alongside one of them,
since it changes no saved state:

| Field | Type | What it is |
| --- | --- | --- |
| set_flag | SetFlag? | sets a flag to true or false |
| update_counter | UpdateCounter? | adds to, takes from, or sets a counter |
| update_inventory | UpdateInventory? | raises or lowers an inventory count |
| request_transition | string? | asks to move to the given Node ID |
| request_notify | string? | asks for a one-time notify; a free-form id the game gives meaning to. Changes no saved state |
| set_persistence | SetPersistence? | sets one key and value in persistence |
| record_event | RecordEvent? | records any event you choose, into the History |

### 4.6 State (what the changing state holds)

| Field | Type | Default | What it is |
| --- | --- | --- | --- |
| flags | Dict<string, bool> | {} | bool flags |
| counters | Dict<string, float> | {} | number counters |
| inventory | Dict<string, int> | {} | how many of each item |
| current_node | a string | "" | the current Node's ID (renamed from `current_scene` in Phase 5.8) |
| current_team | a string | "" | which side is deciding right now |
| persistence | Dict<string, string> | {} | any data meant to last, key by key |

**Counter keys set aside for a special use** (Phase 5.8 v2):

+ `_session_count`: how many times the game has been started, read by
  `history.session_count()`
+ `_total_play_time`: the total time played, added up in seconds, read
  by `history.total_play_time()`

### 4.7 Snapshot (the root of the changing data)

| Field | Type | Default | What it is |
| --- | --- | --- | --- |
| schema_version | a whole number | 1 | the schema's own version |
| state | State | (empty) | the changing state, right now |
| history | History | (empty) | the log of what has happened |

### 4.8 History

| Field | Type | Default | What it is |
| --- | --- | --- | --- |
| entries | `List<HistoryEntry>` | [] | the event log, in the order things happened |
| max_entries | a whole number | 1000 | the most entries kept (the oldest is dropped past this) |

### 4.9 HistoryEntry

| Field | Type | Default | What it is |
| --- | --- | --- | --- |
| kind | a string | "" | the kind of event ("node_enter", "node_exit", "rule_fire", or any word you choose) |
| target_id | a string | "" | what it happened to (a Node's ID, a Rule's ID, and so on) |
| timestamp | a float | 0.0 | seconds passed since the session began |

**Which `kind` values are recorded on their own, and which by hand** (see
`docs/dsl_spec.md` §6 for the same note):

+ `rule_fire`: **recorded on its own.** `Store.DispatchTrigger` writes one
  entry every time a `once=true` rule fires (`target_id` is that rule's
  own id).
+ `node_enter` / `node_exit` / `node_fail` / any word you choose: **only
  by hand.** The running game does NOT record these on its own. Use a
  Rule whose `command` is `record_event` to write them yourself. If a
  scenario leans on `history.count(kind=node_enter, ...)` with no
  matching `record_event` Rule, the count stays at `0` forever.

---

## 5. Rules for Converting from the Old Form (v2.x to v3.0)

Converting from a `germio_config.json` in the v2.x form or earlier (with
`worlds[]`/`levels[]`).

### 5.1 What maps to what

| v2.x (old) | v3.0 (now) |
| --- | --- |
| `germio_config.json` (the file name) | `germio.json` |
| `germio_config.dat` (the file name) | `germio.dat` |
| `germio_config.schema.json` | `germio.schema.json` |
| `worlds: [{id, levels: [...]}, ...]` | `root: {kind: "world", children: [{kind: "world", children: [...]}, ...]}` |
| `worlds[i].levels[j]` | `root.children[i].children[j]` (add `kind: "level"` to each level) |
| `state: {current_scene: "x", ...}` | `initial_state: {current_node: "x", ...}` (the changing data is now split off, into `snapshot_{slot}.json`) |
| `state.fired_rules: [...]` | (taken out) — its place is now taken by `Snapshot.history.entries[].kind="rule_fire"` |
| (none) | a new `Snapshot { schema_version, state, history }` is added |

### 5.2 An example, converted

**v2.x (old)**:

```json
{
  "schema_version": 1,
  "state": {
    "flags": {},
    "counters": {},
    "current_scene": "lv_01",
    "fired_rules": []
  },
  "worlds": [{
    "id": "w_main",
    "levels": [
      { "id": "lv_01", "next": [...], "rules": [...] }
    ]
  }]
}
```

**v3.0 (now)** — the Scenario part (`germio.json`):

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_01",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_main",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_01",
        "name": "Level 01",
        "kind": "level",
        "scene": "Lv_01",
        "children": [],
        "next": [...],
        "rules": [...]
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**v3.0 (now)** — the Snapshot part (`snapshot_1.json`, built by the
running game itself):

```json
{
  "schema_version": 1,
  "state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_01",
    "current_team": "",
    "persistence": {}
  },
  "history": {
    "entries": [],
    "max_entries": 1000
  }
}
```

### 5.3 No automatic conversion is given

The `Migrator` class was **taken out** in Phase 5.8 v2. Why: the schema
was not yet public, so there was no promise to outside users to keep
their old files working.

For a file in the old form, convert it by hand, or ask an LLM (Claude or
GPT-4) to do the conversion for you.

---

## 6. Encryption

### 6.1 The method used

AES-CBC (a 256-bit key, a 128-bit IV). Built in `Vault.cs`.

### 6.2 Which files this covers

| File | Encrypted |
| --- | --- |
| `germio.dat` | YES (AES-CBC) |
| `snapshot_{slot}.dat` | YES (AES-CBC, sharing the same key as `germio.dat`) |
| `germio.json` (while building) | NO (plain text) |
| `snapshot_{slot}.json` (while building) | NO (plain text) |

### 6.3 The order the key is looked for

1. the `GERMIO_AES_KEY` setting
2. as a fallback: `StreamingAssets/germio_key.bin`

### 6.4 How the IV is handled

A fresh IV (16 random bytes) is made for each save, and placed before the
encrypted bytes. `Vault.Decrypt()` reads the first 16 bytes back out as
the IV.

Unlike the key itself, reusing the same IV would not truly break
security here, but a new one is still made each time, to be careful.

### 6.5 Why AES-CBC, and not AES-GCM

+ AES-CBC together with HMAC would check for tampering too, but CBC
  alone is used here, to keep the build simple
+ Phase 5.8 does not call for catching tampering in the save data (a
  player cheating on their own machine is allowed to stand)
+ adding HMAC will be looked at again once keeping a speedrun record
  free of tampering becomes something that is truly needed

### 6.6 Why the Snapshot needs encryption too

+ stops a speedrun record from being edited by hand (faking a
  `HistoryEntry.timestamp`)
+ stops save-data cheating (editing `State.flags` and `State.counters` by
  hand)
+ stops the history from being edited, to skip past an achievement's
  real unlock condition

---

## 7. PlayerPrefs

Phase 5.8 v2 does not use PlayerPrefs at all. Every read and write goes
through Storage instead.

To check this:

```sh
grep -rn "PlayerPrefs.GetString\|PlayerPrefs.SetString" game/Assets/Plugins/Germio/
```

This should turn up 0 hits (this is what G6 calls for).

That said, a system-level piece such as `SceneLoader` may still use
`PlayerPrefs` in a small, limited way, to hold a scene's name for a
short time (`CURRENT_SCENE_KEY`). This falls to whoever is putting
Germio into their own game, not to the Germio framework's own core.

---

## 8. Good Habits to Keep

### 8.1 For a game builder (using Germio in a real project)

+ use `germio.json` (plain text) while you build
+ add a CI step to turn it into `germio.dat` (encrypted), before you ship
+ do not edit `snapshot_{slot}.json` by hand; the running game builds it
  on its own
+ keep the key (`GERMIO_AES_KEY`) as a CI/CD secret

### 8.2 For an LLM writing `germio.json`

+ check your output against the JSON Schema
  (`schemas/germio.schema.json`) before you send it back
+ do **not** build the Snapshot part (state, history) at all — the
  Scenario alone
+ keep the node tree to 5 layers deep or fewer (past this, Validator
  V025 gives a warning)
+ for a node you make up yourself, lean on the common `kind` names
  (world/level/title/ending/boss/shop/...) where they fit

### 8.3 Working with a Snapshot in a test

```csharp
// set up a fresh Snapshot in a unit test
var snapshot = new Snapshot();
store.SetSnapshot(snapshot: snapshot);

// work on State through scenario.initial_state
store.scenario.initial_state.flags["test_flag"] = true;

// record a history entry
store.RecordHistoryEvent(kind: "test_event", target_id: "test_target");
```

`store.state` (a shortcut that stood before the Phase 5.8 v2 fix) has
been **taken out**. Always write `store.scenario.initial_state` or
`store.snapshot.state` out in full.

---

## See Also

+ `docs/dsl_spec.md` — the full DSL spec (this holds the `history.*`
  functions too)
+ `docs/dsl_cookbook.md` — 32 working patterns (Section 7 covers using
  History)
+ `docs/security_spec.md` — encryption and how keys are handled, in full
+ `docs/llm_design_spec.md` — G19/G20/G21 (Node recursion, keeping
  static and dynamic apart, History as a first-class idea)
