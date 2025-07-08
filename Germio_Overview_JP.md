# Germio Framework 概要・設計思想・特徴・サンプル集

## 1. Germio とは

Germio は「Blender 等の DCC ツールで 3D モデルやレベル（シーン）を作成し、ゲームの進行・構成・分岐・イベントを JSON（または Mermaid）で記述するだけで、本格的な 3D ゲームがノンプログラミングで作れる」ことを目指した新しいゲーム開発フレームワークです。

### 特徴

+ 3D モデル・レベル制作は Blender 等で自由に
+ ゲーム進行・ワールド構成・イベントは JSON（または Mermaid）で直感的に設計
+ Unity や C# の知識がなくても、ゲームのロジックや分岐をデータで管理できる
+ FW 側でバリデーション・可視化・エラー検出もサポート
+ クリエイターが本当に作りたいゲーム体験に集中できる

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
    "inventory": { "gold": 100, "potions": 2 },
    "turn": 1
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
            { "id": "meetVillager", "trigger": "onEnter", "action": { "setFlag": "metVillager" } }
          ]
        },
        {
          "id": "field1",
          "name": "Green Field",
          "scene": "Scene_Field1",
          "next": [ { "id": "dungeon1", "condition": "flags.hasSword" } ],
          "events": [
            { "id": "findSword", "trigger": "onSearch", "action": { "setFlag": "hasSword" } }
          ]
        },
        {
          "id": "dungeon1",
          "name": "Ancient Cave",
          "scene": "Scene_Dungeon1",
          "next": [ { "id": "field1", "condition": "flags.bossDefeated" } ],
          "events": [
            { "id": "bossBattle", "trigger": "onBossRoomEnter", "action": { "setFlag": "bossDefeated" } }
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
    "inventory": { "items": [] },
    "currentScene": "room1"
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
            { "id": "findKey", "trigger": "onSearch", "action": { "setFlag": "foundKey" } }
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
    "turn": 1,
    "currentTeam": "player",
    "flags": { "bossDefeated": false }
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
            { "id": "bossBattle", "trigger": "onBossDefeat", "action": { "setFlag": "bossDefeated" } }
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
    "score": 0,
    "lives": 3,
    "flags": { "cleared1_1": false }
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
            { "id": "goal", "trigger": "onGoal", "action": { "setFlag": "cleared1_1" } }
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
    "score": 0,
    "lives": 3,
    "flags": { "clearedStage1": false }
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
            { "id": "bossDefeat", "trigger": "onBossDefeat", "action": { "setFlag": "clearedStage1" } }
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
    "score": 0,
    "lives": 3,
    "bombs": 1,
    "flags": { "clearedStage1": false }
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
            { "id": "clear", "trigger": "onExit", "action": { "setFlag": "clearedStage1" } }
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
    "score": 0,
    "lives": 3,
    "powerUps": 0,
    "flags": { "clearedRoom1": false }
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
            { "id": "clear", "trigger": "onGoal", "action": { "setFlag": "clearedRoom1" } }
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
    "score": 0,
    "lives": 3,
    "bombs": 1,
    "flags": { "mission1Complete": false }
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
            { "id": "missionComplete", "trigger": "onBombTarget", "action": { "setFlag": "mission1Complete" } }
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
2. テキストエディターや Germio 用エディターで自由に編集・テスト
3. Unity 実行時はそのまま JSON を読み込んで動作確認

### 製品版（リリース時）

1. ビルド前に JSON を AES 等で暗号化し、`germio_config.dat` などのバイナリに変換
   + 変換は専用ツールや Unity エディター拡張で自動化可能
2. `Assets/StreamingAssets/germio_config.dat` としてビルドに含める
3. ゲーム起動時はアプリ内で復号→JObject 化して利用
   + 復号ロジックは C# で自動化
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
