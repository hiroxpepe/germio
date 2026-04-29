# Germio Framework 概要・設計思想・特徴・サンプル集

## 1. Germio とは

Germio は **LLM-Native なゲーム進行フレームワーク** です。Claude / GPT / Gemini などの LLM が自然言語の指示から正しい `germio_config.json` を生成できるよう、データモデルと DSL を一から設計しています。

> 人間が書きやすい JSON ではなく、**LLM が書きやすい JSON** で Unity ゲームの進行を記述する。

### 4 つの概念

| 概念 | C# クラス | 役割 |
|------|----------|------|
| `State` | `Germio.Model.State` | すべての実行時値（フラグ・カウンター・インベントリ） |
| `Rule` | `Germio.Model.Rule` | トリガー駆動の状態変更（条件付き） |
| `Command` | `Germio.Model.Command` | Rule が実行するアクション |
| `Next` | `Germio.Model.Next` | 次レベルへの条件付き遷移 |

### LLM-Native の特徴

+ `snake_case` で統一 — C# プロパティ名と JSON キーが 1:1 で一致
+ 公開 JSON Schema (`schemas/germio_config.schema.json`) — プロンプトに埋め込み可能
+ 静的バリデーション V001–V012 — `ToLlmReadable()` で LLM に直接貼れる自己修正可能形式
+ 最小閉じた DSL — 3 プレフィックス・6 演算子のみ（ハルシネーション発生面積ゼロ）
+ Mermaid 双方向可視化 — シナリオをフローチャートとしてエクスポート

### 設計原則

詳細は [`docs/llm_first_design.md`](../../../../../../docs/llm_first_design.md) を参照（G9〜G18 原則）。

## 2. Germio 標準 JSON 構造

Germio のゲーム構成は、

+ `state`（進行・フラグ・インベントリ・スコア等）
+ `worlds`（ワールド単位の構成・遷移）
+ `levels`（各ワールド内のシーン/ステージ/部屋/マップ）

で一貫して管理します。

各 level は `next`（遷移・条件）、`events`（トリガー・アクション）を持ち、条件式やフラグ、インベントリ、スコア、ターン等も state で管理します。

## 3. サンプル JSON（全ジャンル一貫構成・有名レトロゲーム風）

### 3.1 RPG（ドラゴンクエスト風）

```json
{
  "state": {
    "flags": { "hasSword": false, "metVillager": false, "bossDefeated": false },
    "counters": { "gold": 100 },
    "inventory": { "potions": 2 }
  },
  "worlds": [
    {
      "id": "overworld",
      "name": "World Map",
      "scene": "Scene_Overworld",
      "levels": [
        {
          "id": "town1",
          "name": "Beginner Town",
          "scene": "Scene_Town1",
          "next": [ { "id": "field1", "condition": "flags.metVillager" } ],
          "events": [
            { "id": "meetVillager", "trigger": "onEnter", "action": { "setFlag": { "key": "metVillager", "value": true } } }
          ]
        },
        {
          "id": "field1",
          "name": "Green Field",
          "scene": "Scene_Field1",
          "next": [ { "id": "dungeon1", "condition": "flags.hasSword" } ],
          "events": [
            { "id": "findSword", "trigger": "onSearch", "action": { "setFlag": { "key": "hasSword", "value": true } } }
          ]
        },
        {
          "id": "dungeon1",
          "name": "Ancient Cave",
          "scene": "Scene_Dungeon1",
          "next": [ { "id": "field1", "condition": "flags.bossDefeated" } ],
          "events": [
            { "id": "bossBattle", "trigger": "onBossRoomEnter", "action": { "setFlag": { "key": "bossDefeated", "value": true } } }
          ]
        }
      ]
    }
  ]
}
```

### 3.2 ADV（ポートピア連続殺人事件風）

```json
{
  "state": {
    "flags": { "foundKey": false },
    "counters": {},
    "inventory": {}
  },
  "worlds": [
    {
      "id": "main",
      "name": "Mansion",
      "scene": "Scene_Mansion",
      "levels": [
        {
          "id": "room1",
          "name": "First Room",
          "scene": "Scene_Room1",
          "next": [ { "id": "hallway", "condition": "flags.foundKey" } ],
          "events": [
            { "id": "findKey", "trigger": "onSearch", "action": { "setFlag": { "key": "foundKey", "value": true } } }
          ]
        },
        {
          "id": "hallway",
          "name": "Hallway",
          "scene": "Scene_Hallway",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

### 3.3 SLG（ファイアーエムブレム風）

```json
{
  "state": {
    "flags": { "bossDefeated": false },
    "counters": { "turn": 1 },
    "inventory": {},
    "currentTeam": "player"
  },
  "worlds": [
    {
      "id": "battlefield",
      "name": "Battlefield",
      "scene": "Scene_Battlefield",
      "levels": [
        {
          "id": "map1",
          "name": "First Map",
          "scene": "Scene_Map1",
          "next": [ { "id": "map2", "condition": "flags.bossDefeated" } ],
          "events": [
            { "id": "bossBattle", "trigger": "onBossDefeat", "action": { "setFlag": { "key": "bossDefeated", "value": true } } }
          ]
        },
        {
          "id": "map2",
          "name": "Second Map",
          "scene": "Scene_Map2",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

### 3.4 アクション（スーパーマリオブラザーズ風）

```json
{
  "state": {
    "flags": { "cleared1_1": false },
    "counters": { "score": 0, "lives": 3 },
    "inventory": {}
  },
  "worlds": [
    {
      "id": "mainWorld",
      "name": "World 1",
      "scene": "Scene_World1",
      "levels": [
        {
          "id": "1-1",
          "name": "World 1-1",
          "scene": "Scene_1_1",
          "next": [ { "id": "1-2", "condition": "flags.cleared1_1" } ],
          "events": [
            { "id": "goal", "trigger": "onGoal", "action": { "setFlag": { "key": "cleared1_1", "value": true } } }
          ]
        },
        {
          "id": "1-2",
          "name": "World 1-2",
          "scene": "Scene_1_2",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

### 3.5 シューティング（グラディウス風）

```json
{
  "state": {
    "flags": { "clearedStage1": false },
    "counters": { "score": 0, "lives": 3 },
    "inventory": {}
  },
  "worlds": [
    {
      "id": "space",
      "name": "Space",
      "scene": "Scene_Space",
      "levels": [
        {
          "id": "stage1",
          "name": "Stage 1",
          "scene": "Scene_Stage1",
          "next": [ { "id": "stage2", "condition": "flags.clearedStage1" } ],
          "events": [
            { "id": "bossDefeat", "trigger": "onBossDefeat", "action": { "setFlag": { "key": "clearedStage1", "value": true } } }
          ]
        },
        {
          "id": "stage2",
          "name": "Stage 2",
          "scene": "Scene_Stage2",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

### 3.6 ボンバーマン風

```json
{
  "state": {
    "flags": { "clearedStage1": false },
    "counters": { "score": 0, "lives": 3, "bombs": 1 },
    "inventory": {}
  },
  "worlds": [
    {
      "id": "bombermanWorld",
      "name": "Bomberman Stages",
      "scene": "Scene_Bomberman",
      "levels": [
        {
          "id": "stage1",
          "name": "Stage 1",
          "scene": "Scene_Bomberman_1",
          "next": [ { "id": "stage2", "condition": "flags.clearedStage1" } ],
          "events": [
            { "id": "clear", "trigger": "onExit", "action": { "setFlag": { "key": "clearedStage1", "value": true } } }
          ]
        },
        {
          "id": "stage2",
          "name": "Stage 2",
          "scene": "Scene_Bomberman_2",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

### 3.7 マイティ・ボンジャック風

```json
{
  "state": {
    "flags": { "clearedRoom1": false },
    "counters": { "score": 0, "lives": 3, "powerUps": 0 },
    "inventory": {}
  },
  "worlds": [
    {
      "id": "mbjWorld",
      "name": "Mighty Bomb Jack Rooms",
      "scene": "Scene_MBJ",
      "levels": [
        {
          "id": "room1",
          "name": "Room 1",
          "scene": "Scene_MBJ_1",
          "next": [ { "id": "room2", "condition": "flags.clearedRoom1" } ],
          "events": [
            { "id": "clear", "trigger": "onGoal", "action": { "setFlag": { "key": "clearedRoom1", "value": true } } }
          ]
        },
        {
          "id": "room2",
          "name": "Room 2",
          "scene": "Scene_MBJ_2",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

### 3.8 スカイキッド風

```json
{
  "state": {
    "flags": { "mission1Complete": false },
    "counters": { "score": 0, "lives": 3, "bombs": 1 },
    "inventory": {}
  },
  "worlds": [
    {
      "id": "skykidWorld",
      "name": "Sky Kid Missions",
      "scene": "Scene_SkyKid",
      "levels": [
        {
          "id": "mission1",
          "name": "Mission 1",
          "scene": "Scene_SkyKid_1",
          "next": [ { "id": "mission2", "condition": "flags.mission1Complete" } ],
          "events": [
            { "id": "missionComplete", "trigger": "onBombTarget", "action": { "setFlag": { "key": "mission1Complete", "value": true } } }
          ]
        },
        {
          "id": "mission2",
          "name": "Mission 2",
          "scene": "Scene_SkyKid_2",
          "next": [],
          "events": []
        }
      ]
    }
  ]
}
```

## 4. Germio 設定ファイル運用手順（開発～製品版）

### 開発時

1. `Assets/StreamingAssets/germio_config.json` に平文 JSON を配置
2. テキストエディターや Germio ダッシュボード（Unity メニュー: Germio / Dashboard）で自由に編集・テスト
3. Unity 実行時はそのまま JSON を読み込んで動作確認

### 製品版（リリース時）

1. ビルド前に JSON を AES 等で暗号化し、`germio_config.dat` などのバイナリに変換
   + 変換は Unity エディター拡張（Dashboard）や専用ツールで自動化可能
2. `Assets/StreamingAssets/germio_config.dat` としてビルドに含める
3. ゲーム起動時はアプリ内で復号→DataRoot 化して利用
   + 復号ロジックは C# で自動化（`Storage.LoadAsync` が `.json` → `.dat` の順で自動判別）
4. 平文 JSON はビルド成果物に含めない（または消去）

#### 備考

+ 暗号化には C# 標準の AES（System.Security.Cryptography）を利用
+ 鍵・IV はプロジェクト内で安全に管理
+ チート・改ざん対策として有効
+ 開発時は JSON、製品版は暗号化バイナリで運用することで、
  ユーザーの利便性とセキュリティを両立

## 5. まとめ

+ Germio は「3D モデル・レベル制作は Blender 等で、ゲーム進行・構成は JSON で」
+ ノンプログラミング・データ駆動・拡張性・汎用性を両立
+ 既存のゲームエンジンにはない新しい価値を提供
+ どんなジャンルでも柔軟にシミュレート可能
+ 設定ファイルは開発時に JSON、製品版は暗号化バイナリで安全に運用

（このドキュメントは AI アシスタント GitHub Copilot によって自動生成されました）
