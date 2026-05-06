# Germio Framework 概要・設計思想・特徴・サンプル集

## 1. Germio とは

Germio は **LLM-Native なゲーム進行フレームワーク** です。Claude / GPT / Gemini などの LLM が自然言語の指示から正しい `germio.json` を生成できるよう、データモデルと DSL を一から設計しています。

> 人間が書きやすい JSON ではなく、**LLM が書きやすい JSON** で Unity ゲームの進行を記述する。

### 4 つの概念

| 概念 | C# クラス | 役割 |
|------|----------|------|
| `State` | `Germio.Model.State` | すべての実行時値（フラグ・カウンター・インベントリ・永続値） |
| `Rule` | `Germio.Model.Rule` | トリガー駆動の状態変更（条件付き） |
| `Command` | `Germio.Model.Command` | Rule が実行するアクション |
| `Next` | `Germio.Model.Next` | 別 Node への条件付き遷移 |

### LLM-Native の特徴

+ `snake_case` で統一 — C# プロパティ名と JSON キーが 1:1 で一致
+ 公開 JSON Schema (`schemas/germio.schema.json`) — プロンプトに埋め込み可能
+ 静的バリデーション V000–V026 — `ToLlmReadable()` で LLM に直接貼れる自己修正可能形式
+ 閉じた DSL — 3 プレフィックス（`flags` / `counters` / `inventory`）+ `history.*` 関数族（条件評価）
+ Mermaid 双方向可視化 — シナリオをフローチャートとしてエクスポート

### 設計原則

詳細は [`docs/llm_first_design.md`](../../../../../../docs/llm_first_design.md) を参照（G9〜G21 原則）。

---

## 2. Germio の JSON 構造

ゲーム構成は **単一の再帰ツリー**（`Scenario.root`）で管理します。

```
Scenario
├── schema_version  : 1
├── initial_state   : State（フラグ・カウンター・インベントリ・永続値の初期値）
└── root            : Node（ルートノード）
    ├── children[]  : Node[]（再帰。子を持つノードは論理グループ）
    ├── next[]      : Next[]（条件付き遷移）
    └── rules[]     : Rule[]（トリガー駆動ルール）
```

`children` が空のリーフノードが Unity Scene と 1:1 で対応します（`scene` フィールドに Unity シーン名を記載）。

### ファイル構成

| ファイル | 役割 | LLM 編集 |
|---|---|---|
| `StreamingAssets/germio.json` | シナリオ定義（静的・平文）| ✅ 可 |
| `StreamingAssets/germio.dat` | シナリオ AES-CBC 暗号版（リリースビルド用）| ❌ 不可 |
| `StreamingAssets/snapshot_{slot}.json` | セーブスロット別の実行時スナップショット（平文）| ❌ 不可 |
| `StreamingAssets/snapshot_{slot}.dat` | セーブスロット別の実行時スナップショット（AES-CBC 暗号）| ❌ 不可 |
| `StreamingAssets/germio_key.bin` | AES-256 鍵（48 バイト・`GERMIO_AES_KEY` 環境変数未設定時のフォールバック）| ❌ 不可 |
| `schemas/germio.schema.json` | 公開 JSON Schema（Draft 2020-12）— LLM/IDE 用 | ✅ 参照 |

---

## 3. サンプル JSON

> **下記サンプル中の `next[].condition` についての注意。**
> 以下のサンプルは進行フローを示すため `next[]` に `condition` を書いていますが、**ランタイムでは `next[].condition` は評価されません** — `Store.DispatchTrigger` は `rules[]` のみを発火し、ノード遷移は Rule の `command.request_transition` 実行時に起こります。`next[]` は Validator と Mermaid 出力（`Grapher`）が読む構造ヒントとして扱ってください。実際にシーン遷移させるには、`command.request_transition` を持つ Rule を追加します（`Assets/StreamingAssets/germio.json` がその正しい形）。詳細は `docs/germio_dsl_spec.md` §1 と `docs/germio_cookbook.md` 冒頭の注意を参照。

### 3.1 RPG（ドラゴンクエスト風）

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

### 3.2 ADV（ポートピア連続殺人事件風）

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

### 3.3 アクション（スーパーマリオブラザーズ風）

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

### 3.4 実際の構成（Sprout Quest）

`game/Assets/StreamingAssets/germio.json` の構造を示します。中間ノード（`levels`）が子を束ね、タイトル・セレクト・エンディングが兄弟として並ぶ構成です。

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": { "player_at_home": false, "is_beat": false },
    "counters": {},
    "inventory": {},
    "persistence": {},
    "current_node": "title"
  },
  "root": {
    "id": "world",
    "name": "World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "title",
        "name": "Title",
        "kind": "title",
        "scene": "Title",
        "rules": [
          {
            "id": "rule_title_to_level_1",
            "trigger": "signal_btn_start_pressed",
            "command": { "request_transition": "level_1" },
            "once": false
          },
          {
            "id": "rule_title_reset",
            "trigger": "_on_enter_node",
            "command": { "reset_flags": true, "reset_counters": true, "reset_inventory": true },
            "once": false
          }
        ]
      },
      {
        "id": "levels",
        "name": "Levels",
        "kind": "world",
        "scene": "",
        "children": [
          {
            "id": "level_1",
            "name": "Level 1",
            "kind": "level",
            "scene": "Level_1",
            "rules": [
              {
                "id": "rule_level_1_to_level_2",
                "trigger": "signal_btn_start_pressed",
                "condition": "flags.player_at_home && flags.is_beat",
                "command": { "request_transition": "level_2" },
                "once": false
              }
            ]
          }
        ]
      },
      {
        "id": "ending",
        "name": "Ending",
        "kind": "ending",
        "scene": "Ending",
        "rules": [
          {
            "id": "rule_ending_to_title",
            "trigger": "signal_btn_start_pressed",
            "command": { "request_transition": "title" },
            "once": false
          }
        ]
      }
    ]
  }
}
```

---

## 4. Condition DSL クイックリファレンス

条件式は `Next.condition` および `Rule.condition` で使用します。

### 4.1 アクセサープレフィックス（条件評価で使用可能）

| プレフィックス | 型 | デフォルト | 例 |
|---|---|---|---|
| `flags.KEY` | bool | false | `flags.boss_defeated` |
| `counters.KEY` | float | 0.0 | `counters.score >= 100` |
| `inventory.KEY` | int | 0 | `inventory.potions > 0` |

> `persistence` は `State` モデルに存在し、`Command.set_persistence` で書き込み可能ですが、条件評価（`AccessorNode`）では現在未対応です。

### 4.2 history.* 関数族

`history.*` 関数は `Evaluator.Evaluate(condition, state, history)` で `History` オブジェクトを明示的に渡した場合に動作します。トップレベルの比較（例: `history.count(...) >= 3`）で使用してください。

```
history.count(kind=node_fail, target_id=stage_01) >= 5
history.has(kind=rule_fire, target_id=secret_rule)
history.last(kind=node_enter).target_id
history.time_since(kind=node_enter, target_id=shop)
history.session_count() >= 2
history.total_play_time() > 3600
```

### 4.3 演算子

| 種別 | 演算子 |
|---|---|
| 比較 | `==`  `!=`  `>`  `<`  `>=`  `<=` |
| 論理 | `&&`  `\|\|`  `!` |

詳細な EBNF 仕様は `docs/germio_dsl_spec.md` を参照してください。

---

## 5. ファイル運用手順（開発〜製品版）

### エディターメニュー

| メニュー | ソース | 役割 |
|---|---|---|
| `Germio > Dashboard` | `Editor/Dashboard.cs` | `germio.json` 読み込み・バリデーション・シナリオツリー表示 |
| `Tools > Germio > Export Schema to Clipboard` | `Editor/SchemaExportMenu.cs` | 現在の `germio.schema.json` をクリップボードに転送（LLM プロンプト用）|
| `Tools > Germio > Sync Scene Code` | `Editor/SceneCodeSyncMenu.cs` | `Assets/Scripts/Scenes/` 配下の C# Scene クラスを `germio.json` と同期（Phase 5.19）|
| `Tools > Germio > MCP Server > Start MCP Server` | `Editor/McpServerMenu.cs` | *(スタブ — 起動本体は Phase 7 実装予定)* |
| `Tools > Germio > MCP Server > Stop MCP Server` | `Editor/McpServerMenu.cs` | *(スタブ — 停止本体は Phase 7 実装予定)* |

### 開発時

1. `Assets/StreamingAssets/germio.json` に平文 JSON を配置
2. Unity エディターメニュー `Germio > Dashboard` で読み込み・バリデーション実行
3. Unity 実行時はそのまま JSON を読み込んで動作確認

### 製品版（リリース時）

1. Dashboard から `germio.dat`（AES-CBC 暗号化）をエクスポート
2. `Assets/StreamingAssets/germio.dat` としてビルドに含める
3. `Storage.LoadAsync()` が `.json` → `.dat` の順で自動判別・復号する
4. 平文 JSON はビルド成果物に含めない

---

## 6. まとめ

+ Germio は **LLM が生成しやすい JSON** でゲーム進行を記述する LLM-Native フレームワーク
+ シナリオは `root/children` ツリー構造の単一ファイル（`germio.json`）で管理
+ V000–V026 の静的バリデーションにより、LLM が生成した JSON のエラーを自己修正できる
+ 開発時は JSON 平文、製品版は AES 暗号化バイナリで安全に運用
