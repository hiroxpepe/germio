# Germio Framework: Overview, Design Philosophy, Features, and Sample Collection

## 1. What is Germio?

Germio is a new game development framework that aims to enable the creation of full-fledged 3D games without programming, simply by creating 3D models and levels (scenes) in DCC tools like Blender, and describing game progression, structure, branching, and events in JSON (or Mermaid).

### Features

+ Freely create 3D models and levels in Blender or other DCC tools
+ Intuitive design of game progression, world structure, and events using JSON (or Mermaid)
+ Manage game logic and branching with data, no need for Unity or C# knowledge
+ Built-in validation, visualization, and error detection in the framework
+ Creators can focus on delivering the game experience they truly want to make

## 2. Germio Standard JSON Structure

Germio manages game structure with:

+ `state` (progress, flags, inventory, score, etc.)
+ `worlds` (world-level structure and transitions)
+ `levels` (scenes/stages/rooms/maps within each world)

Each level has `next` (transitions/conditions) and `events` (triggers/actions), and all conditions, flags, inventory, score, turns, etc. are managed in `state`.

## 3. Sample JSON (Unified Structure for All Genres, Classic Game Style)

### 3.1 RPG (Dragon Quest Style)

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

### 3.2 ADV (Portopia Serial Murder Case Style)

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

### 3.3 SLG (Fire Emblem Style)

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

### 3.4 Action (Super Mario Bros. Style)

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

### 3.5 Shooting (Gradius Style)

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

### 3.6 Bomberman Style

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

### 3.7 Mighty Bomb Jack Style

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

### 3.8 Sky Kid Style

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

## 4. Germio Configuration File Workflow (Development to Release)

### During Development

1. Place plain JSON in `Assets/StreamingAssets/germio_config.json`
2. Edit and test freely with a text editor or Germio editor
3. During Unity runtime, load the JSON as is for testing

### For Release

1. Before build, encrypt the JSON with AES, etc., and convert to a binary like `germio_config.dat`
   + Conversion can be automated with a dedicated tool or Unity editor extension
2. Include as `Assets/StreamingAssets/germio_config.dat` in the build
3. On game launch, decrypt in-app and convert to JObject for use
   + Decryption logic can be automated in C#
4. Do not include plain JSON in the build output (or delete it)

#### Notes

+ Use C# standard AES (System.Security.Cryptography) for encryption
+ Manage keys/IV securely within the project
+ Effective as a countermeasure against cheating and tampering
+ By using JSON during development and encrypted binaries for release, both user convenience and security are achieved

## 5. Summary

+ Germio: "Create 3D models/levels in Blender, design game progression/structure in JSON"
+ Achieves no-programming, data-driven, extensible, and versatile development
+ Provides new value not found in existing game engines
+ Flexible simulation for any genre
+ Safely manage config files as JSON during development and as encrypted binaries for release

(This document was automatically generated by the AI assistant GitHub Copilot)
