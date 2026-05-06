# Germio Framework: Overview, Design Philosophy, Features, and Samples

## 1. What is Germio?

Germio is an **LLM-Native game progression framework**. Its data model and DSL are designed from the ground up so that LLMs such as Claude, GPT, and Gemini can generate a correct `germio.json` from a natural-language description alone.

> Not JSON that is easy for humans to write — **JSON that is easy for LLMs to write** to describe Unity game progression.

### Four Concepts

| Concept | C# class | Role |
|---------|----------|------|
| `State` | `Germio.Model.State` | All runtime values (flags / counters / inventory / persistence) |
| `Rule` | `Germio.Model.Rule` | Event-driven state mutation (conditional) |
| `Command` | `Germio.Model.Command` | The action a Rule performs |
| `Next` | `Germio.Model.Next` | Conditional transition to another Node |

### LLM-Native Characteristics

+ Unified `snake_case` — C# property names and JSON keys match 1:1
+ Public JSON Schema (`schemas/germio.schema.json`) — embeddable in LLM prompts
+ Static validation V000–V026 — `ToLlmReadable()` produces self-correctable output for direct LLM injection
+ Closed DSL — 3 accessor prefixes (`flags` / `counters` / `inventory`) + `history.*` function family (for condition evaluation)
+ Bidirectional Mermaid visualisation — export scenarios as flowcharts

### Design Principles

See [`docs/llm_first_design.md`](../../../../../../docs/llm_first_design.md) for principles G9–G21.

---

## 2. JSON Structure

Game structure is managed as a **single recursive tree** (`Scenario.root`).

```
Scenario
├── schema_version  : 1
├── initial_state   : State (initial values for flags, counters, inventory, persistence)
└── root            : Node (root node)
    ├── children[]  : Node[] (recursive; nodes with children act as logical groups)
    ├── next[]      : Next[] (conditional transitions)
    └── rules[]     : Rule[] (trigger-driven rules)
```

Leaf nodes (empty `children`) correspond 1:1 with Unity Scenes (non-empty `scene` field).

### File Layout

| File | Role | LLM-editable |
|---|---|---|
| `StreamingAssets/germio.json` | Scenario definition (static, plaintext) | ✅ Yes |
| `StreamingAssets/germio.dat` | Scenario, AES-CBC encrypted (release builds) | ❌ No |
| `StreamingAssets/snapshot_{slot}.json` | Runtime snapshot per save slot (plaintext) | ❌ No |
| `StreamingAssets/snapshot_{slot}.dat` | Runtime snapshot per save slot (AES-CBC encrypted) | ❌ No |
| `StreamingAssets/germio_key.bin` | AES-256 key (48 bytes; fallback when `GERMIO_AES_KEY` env var is unset) | ❌ No |
| `schemas/germio.schema.json` | Public JSON Schema (Draft 2020-12) for LLMs and IDEs | ✅ Reference |

---

## 3. Sample JSON

> **Note on `next[].condition` in the samples below.**
> The sample scenarios show `next[]` arrays with `condition` strings to communicate the intended progression flow. **At runtime the engine does not evaluate `next[].condition`** — `Store.DispatchTrigger` fires `rules[]` only, and node transitions occur when a Rule's `command.request_transition` is executed. Treat `next[]` here as documentation/visualisation structure consumed by the Validator and the Mermaid `Grapher`. To make any of these samples actually transition between scenes, add Rules whose `command.request_transition` targets the next node (the shipped `Assets/StreamingAssets/germio.json` shows this canonical form). See `docs/germio_dsl_spec.md` §1 and `docs/germio_cookbook.md` (top callout).

### 3.1 RPG (Dragon Quest style)

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": { "has_sword": false, "met_villager": false, "boss_defeated": false },
    "counters": { "gold": 100 },
    "inventory": { "potions": 2 },
    "persistence": {},
    "current_node": "town1"
  },
  "root": {
    "id": "world",
    "name": "World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "town1",
        "name": "Beginner Town",
        "kind": "level",
        "scene": "Scene_Town1",
        "rules": [
          {
            "id": "rule_meet_villager",
            "trigger": "vol_villager",
            "condition": "!flags.met_villager",
            "command": { "set_flag": { "key": "met_villager", "value": true } },
            "once": true
          }
        ],
        "next": [ { "id": "field1", "condition": "flags.met_villager" } ]
      },
      {
        "id": "field1",
        "name": "Green Field",
        "kind": "level",
        "scene": "Scene_Field1",
        "rules": [
          {
            "id": "rule_find_sword",
            "trigger": "vol_treasure",
            "condition": "!flags.has_sword",
            "command": { "set_flag": { "key": "has_sword", "value": true } },
            "once": true
          }
        ],
        "next": [ { "id": "dungeon1", "condition": "flags.has_sword" } ]
      },
      {
        "id": "dungeon1",
        "name": "Ancient Cave",
        "kind": "level",
        "scene": "Scene_Dungeon1",
        "rules": [
          {
            "id": "rule_boss_battle",
            "trigger": "vol_boss",
            "condition": "!flags.boss_defeated",
            "command": { "set_flag": { "key": "boss_defeated", "value": true } },
            "once": true
          }
        ],
        "next": [ { "id": "town1", "condition": "flags.boss_defeated" } ]
      }
    ]
  }
}
```

### 3.2 ADV (Mystery adventure style)

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": { "found_key": false },
    "counters": {},
    "inventory": {},
    "persistence": {},
    "current_node": "room1"
  },
  "root": {
    "id": "mansion",
    "name": "Mansion",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "room1",
        "name": "First Room",
        "kind": "level",
        "scene": "Scene_Room1",
        "rules": [
          {
            "id": "rule_find_key",
            "trigger": "vol_search",
            "condition": "!flags.found_key",
            "command": { "set_flag": { "key": "found_key", "value": true } },
            "once": true
          }
        ],
        "next": [ { "id": "hallway", "condition": "flags.found_key" } ]
      },
      {
        "id": "hallway",
        "name": "Hallway",
        "kind": "level",
        "scene": "Scene_Hallway",
        "rules": [],
        "next": []
      }
    ]
  }
}
```

### 3.3 Action (Side-scrolling platformer style)

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": { "cleared_1_1": false },
    "counters": { "score": 0, "lives": 3 },
    "inventory": {},
    "persistence": {},
    "current_node": "level_1_1"
  },
  "root": {
    "id": "world_1",
    "name": "World 1",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "level_1_1",
        "name": "World 1-1",
        "kind": "level",
        "scene": "Scene_1_1",
        "rules": [
          {
            "id": "rule_goal",
            "trigger": "vol_goal",
            "condition": "!flags.cleared_1_1",
            "command": { "set_flag": { "key": "cleared_1_1", "value": true } },
            "once": true
          }
        ],
        "next": [ { "id": "level_1_2", "condition": "flags.cleared_1_1" } ]
      },
      {
        "id": "level_1_2",
        "name": "World 1-2",
        "kind": "level",
        "scene": "Scene_1_2",
        "rules": [],
        "next": []
      }
    ]
  }
}
```

---

## 4. Condition DSL Quick Reference

Condition expressions are used in `Next.condition` and `Rule.condition`.

### 4.1 Accessor Prefixes (available in condition evaluation)

| Prefix | Type | Default | Example |
|--------|------|---------|---------|
| `flags.KEY` | bool | false | `flags.boss_defeated` |
| `counters.KEY` | float | 0.0 | `counters.score >= 100` |
| `inventory.KEY` | int | 0 | `inventory.potions > 0` |

> `persistence` exists in the `State` model and is writable via `Command.set_persistence`, but is not currently supported in condition evaluation (`AccessorNode`).

### 4.2 history.* Function Family

`history.*` functions work when a `History` object is explicitly passed to `Evaluator.Evaluate(condition, state, history)`. Use them in top-level comparisons (e.g. `history.count(...) >= 3`).

```
history.count(kind=node_fail, target_id=stage_01) >= 5
history.has(kind=rule_fire, target_id=secret_rule)
history.last(kind=node_enter).target_id
history.time_since(kind=node_enter, target_id=shop)
history.session_count() >= 2
history.total_play_time() > 3600
```

### 4.3 Operators

| Category | Operators |
|----------|-----------|
| Comparison | `==`  `!=`  `>`  `<`  `>=`  `<=` |
| Logical | `&&`  `\|\|`  `!` |

See `docs/germio_dsl_spec.md` for the full EBNF grammar.

---

## 5. File Operation Guide (Development to Production)

### Editor menus

| Menu path | Source | Role |
|---|---|---|
| `Germio > Dashboard` | `Editor/Dashboard.cs` | Load `germio.json`, run validation, view scenario tree |
| `Tools > Germio > Export Schema to Clipboard` | `Editor/SchemaExportMenu.cs` | Copy current `germio.schema.json` to clipboard for LLM prompt |
| `Tools > Germio > Sync Scene Code` | `Editor/SceneCodeSyncMenu.cs` | Synchronise C# Scene classes under `Assets/Scripts/Scenes/` with `germio.json` (Phase 5.19) |
| `Tools > Germio > MCP Server > Start MCP Server` | `Editor/McpServerMenu.cs` | *(stub — full launch implementation targeted at Phase 7)* |
| `Tools > Germio > MCP Server > Stop MCP Server` | `Editor/McpServerMenu.cs` | *(stub — full stop implementation targeted at Phase 7)* |

### Development

1. Place plain JSON at `Assets/StreamingAssets/germio.json`
2. Open Unity Editor menu `Germio > Dashboard` to load and run validation
3. Run in Unity to verify behaviour directly from JSON

### Production (Release)

1. Export `germio.dat` (AES-CBC encrypted) from Dashboard
2. Include as `Assets/StreamingAssets/germio.dat` in the build
3. `Storage.LoadAsync()` auto-detects and decrypts `.json` then `.dat` in order
4. Do not include plain JSON in the build artefact

---

## 6. Summary

+ Germio is an LLM-Native framework that describes game progression in **JSON optimised for LLM generation**
+ Scenarios are managed as a `root/children` tree in a single file (`germio.json`)
+ V000–V026 static validation enables LLM-generated JSON to be self-corrected
+ Plain JSON in development; AES-encrypted binary in production
