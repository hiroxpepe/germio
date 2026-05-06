# Germio

> **The LLM-Native Game Progression Framework for Unity.**
> Describe your game in natural language. Let an LLM author the logic. Ship it.

[![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?logo=unity)](https://unity.com/)
[![Version](https://img.shields.io/badge/version-v0.5.19--alpha-orange)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

---

## What is Germio?

Germio is a Unity framework where **game progression logic lives entirely in a single JSON file** — authored by an LLM, validated automatically, and executed by a lightweight runtime. No visual scripting. No node graphs. No hand-written state machines.

```mermaid
flowchart LR
    HUMAN[Natural language\nrequirement] --> LLM[LLM\nClaude · GPT · Gemini]
    LLM --> JSON[germio.json\nvalidated]
    JSON --> RUNTIME[Germio Runtime\non Unity]
    RUNTIME --> GAME[Playable game]

    style HUMAN fill:#4caf50,color:#fff
    style LLM  fill:#1976d2,color:#fff
    style JSON fill:#FF8F00,color:#fff
    style GAME fill:#c62828,color:#fff
```

---

## Four concepts. That is the whole model.

```mermaid
mindmap
  root((Germio))
    State
      flags
      counters
      inventory
      persistence
      current_node
    Rule
      trigger
      condition
      command
      once
    Command
      set_flag
      update_counter
      update_inventory
      request_transition
      set_persistence
      record_event
    Next
      target_id
      condition
```

Any Unity game progression you can name, expressed as **State · Rule · Command · Next**. No more concepts will ever be added to the core model.

---

## Why LLM-Native?

Most data-driven frameworks were designed for human designers. Germio was designed so that an LLM can write the data **without help**.

```mermaid
quadrantChart
    title LLM affinity vs genre specificity
    x-axis "Genre specific" --> "Genre agnostic"
    y-axis "GUI binary" --> "Text declarative"
    quadrant-1 "LLM and agnostic"
    quadrant-2 "LLM and specific"
    quadrant-3 "GUI and specific"
    quadrant-4 "GUI and agnostic"
    "PlayMaker": [0.75, 0.15]
    "Unity Visual Scripting": [0.80, 0.15]
    "Yarn Spinner": [0.15, 0.85]
    "Ink": [0.18, 0.82]
    "Twine": [0.20, 0.70]
    "RPG Maker": [0.05, 0.20]
    "Germio": [0.78, 0.97]
```

Six measured properties make Germio LLM-Native:

| Property | Implementation |
|---|---|
| `snake_case` throughout all layers | G17 naming theorem |
| Public JSON Schema (Draft 2020-12) | `schemas/germio.schema.json` |
| Self-correcting validator errors | `Validator` → `ToLlmReadable()` G12 format |
| Minimal closed DSL for conditions | `ExprLexer` + `ExprParser` + `Evaluator` |
| Bidirectional Mermaid conversion | `Grapher.Export()` + `MermaidParser.Parse()` |
| Multi-LLM neutral design | Claude / GPT-4 / Gemini prompt packs included |

---

## A 30-second example

You write:

> Five stage action game. Each stage clears when the player reaches the goal. Three lives total.

LLM produces a validated `germio.json`. The Germio runtime plays it.

```mermaid
sequenceDiagram
    participant Dev as Developer
    participant LLM as Claude · GPT · Gemini
    participant Val as Validator
    participant Unity as Unity Runtime

    Dev->>LLM: system prompt + JSON Schema + requirement
    LLM-->>Dev: germio.json
    Dev->>Val: Validator.Validate(scenario)
    Val-->>Dev: ValidationResult list (G12 format)
    alt errors found
        Dev->>LLM: ToLlmReadable() error feedback
        LLM-->>Dev: corrected germio.json
    else clean
        Note over Dev,Val: ready to play
    end
    Dev->>Unity: place in StreamingAssets · press Play
    Unity-->>Dev: running game
```

---

## Runtime flow

```mermaid
flowchart TD
    subgraph LOAD[Load]
        SA[StreamingAssets/germio.json] --> STORAGE[Storage.LoadAsync]
        STORAGE --> STORE[Store]
    end

    subgraph TICK[Every trigger]
        ZONE[Zone / Bus.Publish] --> DISPATCH[Store.DispatchTrigger]
        DISPATCH --> EVAL[Evaluator\nrule.condition]
        EVAL -->|pass| EXEC[Executor\nrule.command]
        EXEC --> RT[request_transition]
        RT --> SCENE[SceneLoader\nload Unity Scene]
    end

    subgraph SAVE[Save]
        STORE --> SNAP[Snapshot
snapshot_N.json]
    end

    STORE --> TICK
    EXEC --> STORE
```

---

## Data model

```mermaid
classDiagram
    class Scenario {
        +int schema_version
        +State initial_state
        +Node root
    }
    class Node {
        +string id
        +string name
        +string kind
        +string scene
        +List~Node~ children
        +List~Next~ next
        +List~Rule~ rules
    }
    class State {
        +Map flags
        +Map counters
        +Map inventory
        +Map persistence
        +string current_node
        +string current_team
    }
    class Rule {
        +string id
        +string trigger
        +string condition
        +Command command
        +bool once
    }
    class Command {
        +SetFlag set_flag
        +UpdateCounter update_counter
        +UpdateInventory update_inventory
        +string request_transition
        +SetPersistence set_persistence
        +RecordEvent record_event
    }
    class Next {
        +string id
        +string condition
    }
    class Snapshot {
        +int schema_version
        +State state
        +History history
    }

    Scenario --> State
    Scenario --> Node
    Node --> Node : children
    Node --> Next
    Node --> Rule
    Rule --> Command
    Snapshot --> State
    Snapshot --> History
```

---

## Namespace architecture

```mermaid
flowchart TB
    MODEL[Germio.Model\nScenario · Node · Rule · Command\nState · Snapshot · History]
    CORE[Germio.Core\nStorage · Vault · Store\nValidator · Evaluator · Executor\nGrapher · MermaidParser\nExprLexer · ExprParser · ExprAst\nScenarioNavigator]
    SCHEMA[Germio.Schema\nSchemaExporter]
    EDITOR[Germio.Editor\nDashboard · McpServerMenu\nSceneCodeSyncer · SceneCodeSyncMenu\nSchemaExportMenu]
    SYSTEMS[Germio.Systems\nGameSystem · SceneLoader · Bus\nZone · SoundSystem · CameraSystem]
    GAMEDEV[GameDev\ngame-specific scripts]

    MODEL --> CORE
    CORE --> SCHEMA
    CORE --> EDITOR
    CORE --> SYSTEMS
    SYSTEMS --> GAMEDEV

    style MODEL  fill:#1976d2,color:#fff
    style CORE   fill:#388e3c,color:#fff
    style SCHEMA fill:#f57c00,color:#fff
    style EDITOR fill:#7b1fa2,color:#fff
    style SYSTEMS fill:#0097a7,color:#fff
    style GAMEDEV fill:#c62828,color:#fff
```

---

## Files

| File | Role | LLM-editable |
|---|---|---|
| `StreamingAssets/germio.json` | Scenario definition (static, plaintext) | ✅ Yes |
| `StreamingAssets/germio.dat` | Scenario, AES-CBC encrypted (release) | ❌ No |
| `StreamingAssets/snapshot_{slot}.json` | Runtime snapshot per save slot (plaintext) | ❌ No |
| `StreamingAssets/snapshot_{slot}.dat` | Runtime snapshot per save slot (encrypted) | ❌ No |
| `StreamingAssets/germio_key.bin` | AES-256 key (48 bytes) | ❌ No |
| `schemas/germio.schema.json` | JSON Schema Draft 2020-12 | ✅ Reference |

---

## Editor menus

| Menu | Role |
|---|---|
| `Germio > Dashboard` | Load `germio.json`, run Validator, view scenario tree |
| `Tools > Germio > Export Schema to Clipboard` | Copy `germio.schema.json` for LLM prompts |
| `Tools > Germio > Sync Scene Code` | Sync C# Scene classes with `germio.json` |
| `Tools > Germio > MCP Server > Start MCP Server` | *(stub — Phase 7)* |
| `Tools > Germio > MCP Server > Stop MCP Server` | *(stub — Phase 7)* |

---

## Getting started

```sh
# Use as a submodule
git submodule add https://github.com/hiroxpepe/germio.git \
    game/Assets/Plugins/Germio

# Or copy the folder directly into your Unity project
# Requires: Unity 6 LTS + Newtonsoft.Json (com.unity.nuget.newtonsoft-json)
```

1. Place your scenario at `Assets/StreamingAssets/germio.json`
2. Open `Germio > Dashboard` in the Unity Editor to validate
3. Press Play

---

## Documentation

| Document | Purpose |
|---|---|
| [LLM Workflow Guide](../../docs/llm_workflow_guide.md) | End-to-end LLM authoring guide |
| [Pattern Library Cookbook](../../docs/germio_cookbook.md) | 32 ready-to-use patterns |
| [DSL Specification](../../docs/germio_dsl_spec.md) | EBNF grammar for conditions |
| [LLM-First Design](../../docs/llm_first_design.md) | Design principles G9–G21 |
| [Naming Convention](../../docs/naming_convention.md) | G16–G18 naming theorem |
| [Security Model](../../docs/germio_security_model.md) | AES key management |
| [Save Data Format](../../docs/germio_save_data_format.md) | Snapshot format and schema |
| [MCP Design](../../docs/mcp_design.md) | Future MCP server design (Phase 7) |

**Reference game**: [Stemic](https://github.com/hiroxpepe/stemic) — a full Unity 3D action game built on Germio.

---

## License

MIT — see [LICENSE](LICENSE).
