# Germio LLM-First Design Principles

> **Version**: 2.2
> **Meant for**: those building on the Germio Framework, and those linking it to an LLM

---

## Background

Germio v2.2 is built around a single idea:

> **An LLM should be able to build a working, valid `germio.json` from
> nothing but a description in plain words.**

Older game frameworks are tuned for how easy they are for a human to use,
at the code level. Germio v2.2 is tuned for **how easy it is for an LLM to
build**, at the JSON/data level, while staying just as easy for a Unity
C# builder to use as before.

This document sets down the 10 design rules (G9-G18) that shape every
choice made in v2.2.

---

## G9 — Four Ideas, No More

**The whole model of how a game moves forward stands on exactly four
ideas:**

| Idea | C# class | Role |
| --- | --- | --- |
| `State` | `Germio.Model.State` | every value that changes while the game runs (flags / counters / inventory / persistence) |
| `Rule` | `Germio.Model.Rule` | a change of state, set off by an event |
| `Command` | `Germio.Model.Command` | the action a Rule carries out |
| `Next` | `Germio.Model.Next` | a level change, tied to a condition |

An LLM that has these four ideas in hand can build any working
`germio.json`. No further idea will be added to the core model.

---

## G10 — a Public JSON Schema (Draft 2020-12)

A machine-readable `schemas/germio.schema.json` is put out alongside the
code.

**Why this is worth doing:**

+ an LLM's prompt can hold the schema, to keep what it builds within
  bounds
+ an IDE with a JSON language server can check the JSON at once, and
  offer to fill it in
+ CI can check `germio.json` against the schema before the game even runs

The schema is **built fresh each time** from the C# types, through
`NJsonSchema` (`SchemaExporter.ExportSchemaJson()`), and can also be
committed as a plain file (`schemas/germio.schema.json`), for an IDE to
read.

This makes sure that:

+ the schema always shows the `Germio.Model` classes exactly as they
  stand now
+ every property has a correct `description`, in plain English
+ nothing odd is left over from how the schema is built (no stray
  `$defs`, no wrong casing on an enum, and so on)

---

## G11 — Declarative, Not a Set of Steps

Every piece of the game's logic is written as **a plain statement of
data**, not as code.

```json
{
  "rules": [
    {
      "id": "pickup_key",
      "trigger": "zone_key_room",
      "condition": "!flags.key_collected",
      "command": { "set_flag": { "key": "key_collected", "value": true } },
      "once": true
    }
  ]
}
```

The LLM builds data. The framework runs it. There is no "write a script"
step at all.

---

## G12 — an Error Format that Fixes Itself

`Validator.ValidationResult` holds more than just a message — it holds
several set fields:

| Field | What it is for |
| --- | --- |
| `severity` | either `Error` or `Warning` (under G17, this is the one true name; the old alternate name `level` was taken out in P5.5) |
| `rule_id` | a machine-readable ID (V000-V026, with some gaps), to sort errors by hand or by script |
| `message` | a short line, plain enough for a human to read |
| `cause_detail` | a clear line on exactly why the condition does not hold |
| `fix_suggestion` | plain-English steps for the fix |
| `suggested_json` | the smallest piece of JSON that would fix it |
| `location.json_path` | a JSONPath pointing right at the node with the problem (the main field an LLM reads) |
| `location.line` / `location.column` | where in the source this sits (0, when this is not known) |

**The LLM's own fix loop:**

```text
a user, to an LLM: "Build me a 5-stage action game"
the LLM, to the user: germio.json
the user: Validator.Validate(scenario) -> errors V001 and V006
the user, to the LLM: pastes in the ToLlmReadable() output
the LLM: fixes it on its own -> a working germio.json
```

Each round of fixing uses nothing but the `ToLlmReadable()` output — no
code, and no logs.

---

## G13 — a Small, Closed DSL

The condition DSL is kept narrow, on purpose.

### Accessor prefixes that can be read in a condition (3 in all)

| Prefix | Type | Default |
| --- | --- | --- |
| `flags.KEY` | bool | false |
| `counters.KEY` | a floating-point number | 0.0 |
| `inventory.KEY` | a whole number | 0 |

> `persistence` is part of the `State` data model, and can be WRITTEN
> through `Command.set_persistence`, but right now it cannot be READ in a
> condition (`AccessorNode` does not know how to handle the `persistence`
> prefix).

**Comparison operators (6 in all):** `==`, `!=`, `>`, `<`, `>=`, `<=`

**Logical operators (3 in all):** `&&`, `||`, `!`

**The history.\* family of functions (parsed by ExprParser, needs a
History object to be given):**

+ `history.count(kind=..., target_id=...)` — how many entries match
+ `history.has(kind=..., target_id=...)` — checks if even one exists
+ `history.last(kind=...).target_id` or `.timestamp` — a property of the
  last match
+ `history.time_since(kind=..., target_id=...)` — the timestamp of the
  last match
+ `history.session_count()` — the number of sessions
+ `history.total_play_time()` — the total time played

No string in quotes, no `now()`, no "if this then that, else this",
and no assignment.

**Why kept so narrow:** every part of a DSL an LLM CAN use is also a part
it CAN get wrong. A closed, small set of words gives almost no room for
an LLM to make something up, next to a full language such as Lua or
Python.

See `docs/dsl_spec.md` for the full grammar.

---

## G14 — the Grammar, Plus Worked Examples, Not the Grammar Alone

How well an LLM can write a DSL grows a great deal once it has worked
examples to learn from, not the grammar alone. The Cookbook
(`docs/dsl_cookbook.md`) holds 32 patterns, each with notes (Pattern
1.1 through 7.5), covering stage progress, win and loss conditions,
inventory, a branching adventure game, and boss fights. Each pattern
holds a piece of JSON, a note on why it is the better choice, and common
mistakes an LLM tends to make, to steer clear of.

It also holds "Common Failure Patterns" (Section 6), setting down the
mistaken patterns an LLM is likely to produce.

---

## G15 — Positioned as LLM-Native

Germio is put forward as an **LLM-Native** game framework, not a
"data-driven" one.

> "Data-driven" means a human designer can set it up with no code
> written.
> "LLM-Native" means an LLM can set it up **better than a human can**, and
> the framework is shaped, in particular, to keep an LLM's error rate as
> low as it can be.

Design choices made in service of being LLM-Native:

+ every property name is in `snake_case` (an LLM tends to mix up
  `camelCase` prefixes)
+ a JSON key matches its C# property name, one to one (nothing sits
  between them, turning one into the other)
+ a key under the Schema's `$defs` matches its class name exactly (with
  nothing standing in between)
+ every idea has one true name, used the same way everywhere (never a
  second word for the same thing)

---

## G16 — Four-Layer Naming Consistency

The same name is used across all four of the ways something is written
down:

| Layer | Example |
| --- | --- |
| the C# class name | `Rule` |
| the JSON key | `"rules"` (plural), `"rule"` (single) |
| the Schema's `$defs` key | `"Rule"` |
| the word an LLM sees in its prompt | `Rule` |

Old names (`DataEvent`, `firedEvents`, `setFlag`) were taken out in v2.2
(P3.5). Not one old name is left in any of the four layers.

---

## G17 — snake_case Throughout

Every name shown in JSON is written in `snake_case`:

+ `current_node` (not `currentScene`, nor `current_scene` — this was
  renamed again in Phase 5.8 v2)
+ `set_flag` (not `setFlag`)
+ `update_counter`, `update_inventory`, `request_transition`

This holds true for:

+ a C# model class's properties (matching the JSON key by name alone,
  with no `[JsonProperty]` needed)
+ the JSON written out by `Storage.SaveAsync`
+ the property keys in the Schema
+ the words a DSL accessor starts with (`flags`, `counters`,
  `inventory`) — these can be read in a condition

**Why snake_case:** an LLM trained mostly on Python and JSON writes
`snake_case` more reliably than `camelCase`, outside of a TypeScript
setting.

---

## G18 — Layered Namespaces

| Namespace | Layer | Holds |
| --- | --- | --- |
| `Germio.Model` | the data model | Scenario, Node, Next, Rule, Command, SetFlag, UpdateCounter, UpdateInventory, SetPersistence, RecordEvent, CounterOp, Snapshot, State, History, HistoryEntry |
| `Germio.Core` | the engine's own logic | Evaluator, Executor, Validator, Grapher, MermaidParser, Storage, Vault, Store, ExprLexer, ExprParser, ExprAst, ScenarioNavigator |
| `Germio.Schema` | tooling | SchemaExporter (built on NJsonSchema; runs both in the Unity Editor and in a stand-alone .NET build) |
| `Germio.Editor` | the Editor's own UI | Dashboard, SchemaExportMenu, McpServerMenu, SceneCodeSyncer, SceneCodeSyncMenu |
| `GameDev` | built for this one game | Human_Abilities, subclasses of InputMapper |

---

## G19 — Node Recursion (added in Phase 5.8 v2)

**The rule**: the scenario's own tree is written as one class, `Node`,
that can hold more of itself. The old, fixed tree (World above Level) is
taken out.

**Why**: a real game needs anywhere from 2 to 7 layers. An action game
needs 2; an RPG needs 5 to 7. A tree fixed at 2 layers cannot hold a
modern RPG.

**Limits**:

+ a hard limit of 10 layers deep (`Env.MAX_NODE_DEPTH`)
+ a warning past 5 layers deep (the default; can be set through
  `Env.warning_node_depth`)
+ a loop back to an earlier Node is not allowed (checked by Validator
  V026)

**How it is built**:

```csharp
public class Node {
    public string id;
    public string name;
    public string kind;  // "world", "level", "title", ...
    public string scene;
    public List<Node> children;
    public List<Next> next;
    public List<Rule> rules;
}
```

**How deep the tree runs, in a few well-known games**:

+ Mario 64: 2 layers (the Castle, then the Stages)
+ Zelda: Ocarina of Time: 4 layers (Hyrule, then a Region, then a
  Dungeon, then a Room)
+ Final Fantasy VII: 6 layers (the World, then a Continent, then a
  Region, then a Building, then a Floor, then a Room)

## G20 — Keeping Static and Dynamic Apart (added in Phase 5.8 v2)

**The rule**: keep data an LLM can edit (static) apart from data the
running game manages (dynamic), at the level of which FILE each lives in.

**Why**: an LLM grows confused when asked to build dynamic data (the
state right now, the history so far). Only the static, fixed parts are
ever shown to the LLM; the dynamic data is kept for the running game
alone to hold.

**How it is built**:

+ `germio.json` (the Scenario — static, and an LLM may edit it)
  + holds `schema_version`, `initial_state`, and `root`
+ `snapshot_{slot}.json` (the Snapshot — dynamic, held by the running
  game alone)
  + holds `schema_version`, `state`, and `history`

**A rule that follows from this**: never build a shortcut between the
running-game side (Store, Snapshot) and the static side (Scenario) — for
example, an alias such as `Store.state` pointing at
`Scenario.initial_state`. Always write `scenario.initial_state` or
`snapshot.state` out in full. Why: a shortcut like this causes
"the state right now" and "the state at the start" to be mixed up by
accident.

## G21 — History as a First-Class Idea (added in Phase 5.8 v2)

**The rule**: keep a log of what has happened in the game as a
first-class idea of its own.

**Why**: flags and counters alone cannot show order, time passed, or how
many times something happened — this keeps a game locked to a simple
fetch-quest shape. Effects where "the world remembers" (harder or easier
play based on past events, spotting a New Game+, an NPC that reacts to
time passed) all need a history to draw from.

**How it is built**: `History.entries: List<HistoryEntry>`, kept as a
log. Any sum or count over it is worked out fresh each time, through the
DSL.

```csharp
public class History {
    public List<HistoryEntry> entries;
    public int max_entries = 1000;
}

public class HistoryEntry {
    public string kind;        // "rule_fire" (recorded on its own, when a once=true rule fires) | "node_enter"/"node_exit"/"node_fail"/anything you name (recorded by hand, through a record_event command)
    public string target_id;
    public float timestamp;
}
```

Reading it from the DSL:

+ `history.count(kind=node_fail, target_id=stage_01) >= 3` — how many
  match
+ `history.has(kind=rule_fire, target_id=secret_rule)` — checks if even
  one exists
+ `history.last(kind=node_enter).timestamp > 100.0` — a property of the
  last match (only a number can be read this way)
+ `history.time_since(kind=node_enter, target_id=shop) >= 100` — the
  timestamp of the last match
+ `history.session_count() >= 2` — the number of sessions
+ `history.total_play_time() > 3600` — the total time played

> An argument is written with no quotes: `kind=node_fail`, not
> `kind="node_fail"`.
> Comparing this against a string in quotes (such as
> `.target_id == "shop"`) is not yet supported.

**Rules:**

+ `Germio.Model` does not depend on any other `Germio.*` namespace.
+ `Germio.Core` depends only on `Germio.Model`.
+ `Germio.Editor` and `Germio.Schema` depend on both Model and Core.
+ `GameDev` depends on `Germio.*`, but nothing under `Germio.*` depends
  back on `GameDev`.

This layered shape makes sure `Germio.Model` and `Germio.Core` can be
built and tested by `dotnet test` with no Unity needed at all, while the
Editor and Schema tools stay Unity-only.

---

## Summary Table

| Rule | In one line |
| --- | --- |
| G9 | four ideas: State / Rule / Command / Next |
| G10 | a public JSON Schema, Draft 2020-12 |
| G11 | declarative data, not a set of steps to run |
| G12 | an error format that fixes itself, V000-V026 (with some gaps) |
| G13 | a small, closed DSL (3 readable prefixes, plus history.*, 6 operators) |
| G14 | the grammar, plus 32 patterns with notes (Cookbook §1-§7) |
| G15 | positioned as LLM-Native (not "data-driven") |
| G16 | four-layer naming consistency (never a second word for one idea) |
| G17 | snake_case, held to, in every layer |
| G18 | layered namespaces: Model, then Core, then Schema/Editor, then GameDev |
| G19 | Node recursion (a tree of `Node.children` takes the place of a fixed World above Level) |
| G20 | static kept apart from dynamic (`germio.json` against `snapshot_{slot}.json`) |
| G21 | history as a first-class idea (`Snapshot.history.entries`) |

---

## Phase 5.5: Polish Before the Dogfood Test

Once Phase 5 (building it in C#) is done, one more phase comes before
Phase 6 (the LLM dogfood test): the **Polish Before the Dogfood Test**
phase. That this phase exists at all reflects a key idea in LLM-First
design:

> **Finishing the code is not the same as being ready for an LLM to write
> for.**

Phase 5.5 closes the gap between:

+ *the build being finished* (the framework compiles, and the tests
  pass), and
+ *being ready for an LLM to write for* (the framework is written up in
  docs, its schemas are put out as plain files, its prompts are laid
  out in order, and its marketing matches the LLM-Native positioning).

### What Phase 5.5 produces

1. `schemas/germio.schema.json` — a plain file, committed, and the fixed
   point that `schemastore.org`, an IDE's own auto-complete, and an
   LLM's prompt can all be built around.
2. `docs/dsl_cookbook.md` — the document read most often while an LLM
   writes Germio JSON; 25+ patterns covering stage progress, win and
   loss, inventory, an adventure game, and boss fights, each with its
   intent, the pattern itself, why it is chosen, and the mistakes to
   avoid.
3. `docs/llm_workflow_guide.md` — a first-time guide, for a human or an
   LLM using Germio for the first time.
4. `overview_JP.md` / `overview_EN.md` — the project's own overview, in
   both Japanese and English, using v2.2's LLM-Native wording.
5. `README.md` — refreshed to lead with "LLM-Native", rather than
   "data-driven".
6. `prompts/system/` laid out again, into `*_quick.md` and
   `*_designer.md` files.
7. `prompts/tasks/` cleaned up (duplicates settled).
8. New files: `docs/security_spec.md` and `docs/save_data_spec.md`.

### Why Phase 5.5 cannot be skipped

If Phase 6 (the LLM dogfood test) begins with none of this in place, the
dogfood session itself ends up building all of it on the fly — under
time pressure, with priorities shifting, and in a voice that does not
hold together. What comes out reads like documentation bolted on after
the fact, not documentation truly designed. Phase 5.5 stops this from
happening, by building these pieces in their proper place within the
whole design, before the pressure of the dogfood test shapes them
instead.

---

## See Also

+ `docs/dsl_cookbook.md` — 27 patterns, tried and checked with an LLM,
  covering nearly every common way a game's flow is built
+ `docs/llm_workflow_guide.md` — the full path, start to finish, for
  writing a scenario together with an LLM
+ `docs/naming_spec.md` — the naming rules behind G16/G17/G18, in full
+ `docs/dsl_spec.md` — the full grammar for the condition DSL
+ `docs/mcp_spec.md` — the tools an MCP server would offer (an optional
  feature, for after v1.0)
