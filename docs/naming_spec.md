# Germio Naming Convention

> **Version**: 2.2
> **Base rules**: G16 (Four-Layer Naming Consistency), G17 (snake_case Throughout), G18 (Layered Namespace Architecture)

---

## 1. The Four-Layer Naming Theorem (G16)

Every idea in Germio must carry the **same name** across all four of the
ways it is written down:

| Layer # | Layer | Example: the "Rule" idea |
| --- | --- | --- |
| 1 | a C# class | `class Rule` in `Germio.Model` |
| 2 | a JSON key | `"rules": [...]` (a list, plural), each holding `"id"`, `"trigger"`, and the rest |
| 3 | the Schema's `$defs` | `"$defs": { "Rule": { ... } }` |
| 4 | the word an LLM sees | "Rule" is the one word used, kept the same, across every doc and Schema note |

**Breaking this rule leads to confusion.** If one idea carries a different
name in each layer, an LLM (or a human) must keep a table in its head to
turn one name into another. This raises the error rate.

### Old name to new name (v2.1 to v2.2)

| v2.1 (old, taken out) | v2.2 (the one true name) | Where it shows up |
| --- | --- | --- |
| `DataRoot` | `Scenario` | C# class + JSON root + Schema |
| `DataState` | `State` | C# class + JSON + Schema |
| `DataLevel` | `Level` | C# class + JSON + Schema |
| `DataAction` | `Command` | C# class + JSON + Schema |
| `DataEvent` | `Rule` | C# class + JSON + Schema |
| `setFlag` | `set_flag` | JSON key + Schema property + DSL |
| `firedEvents` to `fired_rules` | *(both taken out)* | both names removed; keeping track of once-fired rules moved into `Snapshot.history` |
| `currentScene` to `current_scene` to `current_node` | `current_node` | JSON key + Schema property (renamed twice: camelCase to snake_case, then `_scene` to `_node` in Phase 5.8 v2) |
| `currentTeam` | `current_team` | JSON key + Schema property |
| `SEClip` | `SfxClip` | C# class + SoundSystem |
| `BGMClip` | `MusicClip` | C# class + SoundSystem |
| `TriggerHub` / `TriggerHub.cs` | `Bus` / `Bus.cs` | C# class + file |
| `VolumeTrigger` / `VolumeTrigger.cs` | `Zone` / `Zone.cs` | C# class + file |
| `triggerId` / `trigger_id` | `zone_id` | a method's argument + JSON |
| `events` (a list, in Level) | `rules` | JSON key + Schema property |
| `actions` (a list, in Rule) | `commands` | JSON key + Schema property |

### New in v2.2's Phase 5 (more snake_case properties added)

| New property | C# class | JSON key | Notes |
| --- | --- | --- | --- |
| `schema_version` | `Scenario` | `"schema_version"` | a whole number, always `1` in v1 |
| `persistence` | `State` | `"persistence"` | `Dictionary<string, string>` — a key-value store that lives across sessions |
| `set_persistence` | `Command` | `"set_persistence"` | `{ "key": "...", "value": ... }` |

---

## 2. The snake_case Rule (G17)

Every name shown in JSON is written in `snake_case`. No exceptions.

### What counts as "shown in JSON"

+ a C# model class's properties (they turn straight into JSON, through
  Newtonsoft.Json)
+ JSON keys in `germio.json`
+ property names in the Schema, `germio.schema.json`
+ the words a DSL accessor starts with: `flags`, `counters`, `inventory`

### What is NOT written in snake_case

+ C# class names: `PascalCase` (`Scenario`, `State`, `Level`, `Rule`,
  `Command`)
+ C# enum values: `PascalCase` (`CounterOp.Add`, `CounterOp.Sub`,
  `CounterOp.Set`)
+ Unity `MonoBehaviour` method names: follow Unity's own rule for these
+ private fields in game scripts: `_snake_case` (with a leading
  underscore)
+ Inspector (`[SerializeField]`) fields: `_ALL_CAPS`

---

## 3. Layered Namespaces (G18)

```text
Germio (the root)             <- Env, Utils, Extensions, Enums, InputMapper, Scene, GermioLog, UnityUtils
    ├── Germio.Model          <- does not depend on any other Germio.* namespace
    ├── Germio.Core           <- depends only on Model
    │       ├── Germio.Schema         <- depends on Model + Core (uses NJsonSchema)
    │       └── Germio.Editor         <- depends on Model + Core (the Unity Editor UI)
    ├── Germio.Systems        <- depends on Model + Core (GameSystem, CameraSystem, NoticeSystem, SoundSystem, SceneLoader, Bus, Zone)
    ├── Germio.Triggers       <- depends on Model + Core (Despawn, Home)
    ├── Germio.Players        <- depends on Model + Core (Human, a partial base class, plus States/)
    └── Germio.Levels         <- depends on Model + Core (Block, Common)
GameDev                       <- depends on Germio.* (code built for this one game: Human_Abilities, a partial class, plus the Scenes/* tree)
```

### Namespace to file location

| Namespace | Folder | Main classes (P5 additions marked with a star, P5.8 v2 additions marked with a diamond) |
| --- | --- | --- |
| `Germio` | `game/Assets/Plugins/Germio/Scripts/` | Env, Utils, Extensions, Enums, InputMapper, Scene, ◇GermioLog, ◇UnityUtils |
| `Germio.Model` | `game/Assets/Plugins/Germio/Scripts/Model/` | Scenario, ◇Node (took the place of World+Level), Next, Rule, Command, SetFlag, UpdateCounter, UpdateInventory, ★SetPersistence, ◇RecordEvent, ◇Snapshot, State, ◇History, ◇HistoryEntry, CounterOp |
| `Germio.Core` | `game/Assets/Plugins/Germio/Scripts/Core/` | Evaluator, Executor, Validator, Grapher, Storage, Vault, Store, ExprLexer, ExprParser, ExprAst, ★MermaidParser, ◇ScenarioNavigator |
| `Germio.Schema` | `game/Assets/Plugins/Germio/Scripts/Schema/` | SchemaExporter |
| `Germio.Systems` | `game/Assets/Plugins/Germio/Scripts/Systems/` | GameSystem, CameraSystem, NoticeSystem, SoundSystem, SceneLoader, Bus (was TriggerHub), Zone (was VolumeTrigger) |
| `Germio.Triggers` | `game/Assets/Plugins/Germio/Scripts/Triggers/` | Despawn, Home |
| `Germio.Players` | `game/Assets/Plugins/Germio/Scripts/Players/` | Human (a partial base class), Human_Extensions, States/ (Human_Acceleration, Human_DoUpdate, Human_DoFixedUpdate) |
| `Germio.Levels` | `game/Assets/Plugins/Germio/Scripts/Levels/` | Block, Common |
| `Germio.Editor` | `game/Assets/Plugins/Germio/Scripts/Editor/` | Dashboard, SchemaExportMenu, ★McpServerMenu, ◇SceneCodeSyncer, ◇SceneCodeSyncMenu |
| `GameDev` | `game/Assets/Scripts/` | Human_Abilities (a partial class: Abilities/Human_Abilities, Abilities/Human_Climbable), Scenes/* (World, Title, Select, Ending, Levels, Level1, Level2, Level3 — this tree was added in Phase 5.10) |

### Rules

+ `Germio.Model` may not `using` any other `Germio.*` namespace.
+ `Germio.Core` may not `using` `Germio.Schema`, `Germio.Editor`,
  `Germio.Systems`, `Germio.Triggers`, `Germio.Players`, or
  `Germio.Levels`.
+ `Germio.Schema` and `Germio.Editor` may only `using` `Germio.Model` and
  `Germio.Core`.
+ `Germio.Systems`, `Germio.Triggers`, `Germio.Players`, and
  `Germio.Levels` may `using` `Germio.Model` and `Germio.Core`.
+ `GameDev` scripts may `using` `Germio.Model`, `Germio.Core`, and any
  `Germio.*` namespace.
+ nothing under any `Germio.*` namespace may reach back and use a
  `GameDev` script.

---

## 4. C# Field and Property Names (not shown in JSON)

These rules cover C# code that is never turned into JSON, and follow the
project's own custom:

| Kind | Rule | Example |
| --- | --- | --- |
| a private instance field | `_snake_case` | `_do_update`, `_jump_power` |
| a local variable | `snake_case` | `base_path`, `trigger_id` |
| a method's argument | `snake_case` | `level_id`, `world_id` |
| a public property (in `GameDev`) | `camelCase` | `home`, `beat`, `mode` |
| a Model property (shown in JSON) | `snake_case` | `current_node`, `set_flag` |
| a field shown in the Unity Inspector | `_ALL_CAPS` | `_JUMP_POWER`, `_FORWARD_SPEED_LIMIT` |
| a constant | `ALL_CAPS`, held in the static `Env` class | `GAME_SYSTEM`, `LEVEL_TYPE` |

---

## 5. Named Arguments Rule

Every call to a **method built for this project** (the Germio framework,
plus GameDev) must name its arguments:

```csharp
// correct
await Storage.LoadAsync(base_path: path);
Validator.Validate(root: data);
new SceneLoader(store: _store, load_scene: fn);

// wrong
await Storage.LoadAsync(path);
Validator.Validate(data);
```

**This rule does not cover** calls into the .NET base class library, the
Unity API, or Newtonsoft.Json.

---

## 6. JSON Property Names Must Never Change

The property names on `DataRoot` (now called `Scenario`) — `state`,
`worlds`, and the rest — and the property names on every class nested
inside it, are all **part of a public promise to whoever holds a save
file**.

+ Renaming a JSON key **breaks** any save file already made.
+ If a rename truly must happen, build a migration path first, so an old
  save file can still be read in.

---

## Where things stand now (as of v2.2 / Phase 5.14)

The G18 layered-namespace plan is built in full:

| Folder | Namespace | Number of files |
| --- | --- | --- |
| `Scripts/` | `Germio` | 8 |
| `Scripts/Core/` | `Germio.Core` | 12 |
| `Scripts/Model/` | `Germio.Model` | 1 (`Data.cs` holds every Model class) |
| `Scripts/Schema/` | `Germio.Schema` | 1 |
| `Scripts/Systems/` | `Germio.Systems` | 7 |
| `Scripts/Triggers/` | `Germio.Triggers` | 2 |
| `Scripts/Players/` | `Germio.Players` | 5 |
| `Scripts/Levels/` | `Germio.Levels` | 2 |
| `Scripts/Editor/` | `Germio.Editor` | 3 |

The test namespaces follow the same pattern (`Germio.Tests.Core` /
`Tests.Model` / `Tests.Schema` / `Tests.Systems`).

The folder once named `Value/` (holding `Data.cs`) was renamed to
`Model/` in P5.5, to match the `Germio.Model` namespace. This closed the
last place where G18 was not being followed.

In Phase 5.8 v2, the `World` and `Level` model classes were folded into
one, recursive `Node` class, and `Migrator.cs` was taken out. Phase 5.10
brought in the `Scenes/` tree under `GameDev` (stand-alone classes, each
inheriting down the line `Scene → World → Title/Select/Ending/Levels →
Level1/2/3`, taking the place of the earlier partial-class way of doing
it).

---

## Where these rules came from

The naming theorem and the layered namespaces came about in three steps:

1. **G16 (Four-Layer Naming Consistency)**: first put into words once the
   team saw that `[JsonProperty(...)]` attributes were quietly breaking
   the rule that "C# = JSON = Schema = the LLM's own word".

2. **G17 (snake_case Throughout)**: taken up to make the four-layer match
   exact, letter for letter, rather than only close in shape.

3. **G18 (Layered Namespace)**: forced by the `DataLevel` to `Level`
   rename, which ran into an existing MonoBehaviour class also named
   `Level`. Rather than make up a second word for the same idea (such as
   `Stage`), namespaces were used instead, so both layers could keep the
   one name, "Level".

These three rules depend on each other, and must be kept together.
Breaking any one of them quietly makes it harder for an LLM to write
Germio JSON well.
