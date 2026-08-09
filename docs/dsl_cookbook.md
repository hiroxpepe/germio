# Germio Cookbook — Pattern Library for LLM-Native Authoring

> **Version**: 2.2
> **Authors**: STUDIO MeowToon
> **Meant for**: an LLM agent writing `germio.json`, and a human reviewer reading these patterns

---

> ## A Warning — Do Not Follow These Patterns Word for Word Yet
>
> This cookbook was written before it was fully checked against the running code, and it holds a **known gap against how the game truly runs now**:
>
> **`next[].condition` is NOT read by the running game.** `Store.GetNextNode` exists, but nothing calls it; `Store.DispatchTrigger` only ever fires `rules[]`, and never looks at `next[]` at all. The `next[]` array is only read by the **Validator** (its V006/V011/V012 checks) and by **Grapher** (for a Mermaid picture). To truly move between nodes while the game runs, a Rule must run `command.request_transition: "<target_node_id>"`.
>
> Most patterns below (1.1 through 5.x) show a gate on a transition written as `next[].condition`. Read those `next[]` blocks as **a plain hint for the Validator and for the picture Grapher draws**, not as something that truly steers the game while it runs. The `Assets/StreamingAssets/germio.json` shipped with the game follows the correct pattern instead: every node's `next[]` is left empty, and every real transition lives inside a Rule, through `command.request_transition`.
>
> This cookbook will be rewritten once Phase 6 (the LLM dogfood test) makes clear which way of writing this is the one true way. Until then, **choose the working `request_transition` pattern over the `next[].condition` pattern shown here**.

---

## Background

This document is a **library of patterns** for writing `germio.json`.
For each common need in how a game moves forward, it shows:

1. the intent (what the builder wanted)
2. the smallest piece of JSON that gives it to them
3. why this pattern is the one to choose (against other ways to do it)
4. common mistakes an LLM tends to make, to steer clear of

Every JSON example here is checked by `CookbookExamplesTests.cs`, in the
test suite.

### A quick card of fields

| Need | Field | Example |
| --- | --- | --- |
| a bool flag | `state.flags` / `set_flag` | `{ "key": "door_open", "value": true }` |
| a number counter | `state.counters` / `update_counter` | `{ "key": "score", "delta": 100, "op": "Add" }` |
| an inventory item | `state.inventory` / `update_inventory` | `{ "key": "key_item", "delta": 1 }` |
| a scene change (while running) | `command.request_transition`, inside a Rule | `"command": { "request_transition": "lv_02" }` |
| a scene change (a hint only, not read while running) | `next[].id`, with an optional `condition` | `{ "id": "lv_02", "condition": "flags.goal == true" }` |
| data shared across scenes | `state.persistence` / `set_persistence` | `{ "key": "player_name", "value": "Aria" }` |
| a one-time UI notify (e.g. a "Level Clear!" message) | `command.request_notify` | `"command": { "request_notify": "level_clear" }` |

---

## Section 1: Stage Progression Patterns

### Pattern 1.1 — a plain, 3-stage chain

**Intent**: a player clears Stage 1, then Stage 2, then reaches the
ending.

**Pattern**:

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
        "next": [
          {
            "id": "lv_02",
            "condition": "flags.stage1_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_clear_s1",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "stage1_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_01"
      },
      {
        "id": "lv_02",
        "next": [
          {
            "id": "lv_ending",
            "condition": "flags.stage2_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_clear_s2",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "stage2_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_02"
      },
      {
        "id": "lv_ending",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_ending"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: one `set_flag` rule per stage, with `once: true`,
makes sure the clear flag fires exactly once. The `next[].condition`
gates the transition until that flag is set.

**Mistakes to avoid**:

+ wrong: `setFlag` — write `set_flag` (snake_case) instead
+ wrong: `"actions": [...]` — `Rule.command` is one single object, not a
  list

---

### Pattern 1.2 — branching, based on a flag

**Intent**: after a key event in Stage 1, the player takes either the
"hero route" or the "thief route".

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_hub",
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
        "id": "lv_hub",
        "next": [
          {
            "id": "lv_hero_route",
            "condition": "flags.chose_hero == true"
          },
          {
            "id": "lv_thief_route",
            "condition": "flags.chose_thief == true"
          }
        ],
        "rules": [
          {
            "id": "rule_choose_hero",
            "trigger": "signal_hero_choice",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "chose_hero",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_choose_thief",
            "trigger": "signal_thief_choice",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "chose_thief",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_hub"
      },
      {
        "id": "lv_hero_route",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_hero_route"
      },
      {
        "id": "lv_thief_route",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_thief_route"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: two flags that can never both be true steer which
branch turns on. Only one `next` condition can hold true at once.

**Mistakes to avoid**:

+ wrong: branching with no `once: true` — the rule may fire more than
  once if triggers happen to overlap

---

### Pattern 1.3 — branching, based on a counter's threshold

**Intent**: a player who scored 1000 or more goes to the hard route;
everyone else goes to the normal route.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_stage1",
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
        "id": "lv_stage1",
        "next": [
          {
            "id": "lv_hard",
            "condition": "counters.score >= 1000"
          },
          {
            "id": "lv_normal",
            "condition": "counters.score < 1000"
          }
        ],
        "rules": [
          {
            "id": "rule_add_score",
            "trigger": "zone_coin",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "score",
                "delta": 100,
                "op": "Add"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_stage1"
      },
      {
        "id": "lv_hard",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_hard"
      },
      {
        "id": "lv_normal",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_normal"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: comparing a counter against a threshold, right
inside `next[].condition`, gives a score-gated route with no need to turn
it into a flag first.

**Mistakes to avoid**:

+ wrong: `"counter:score >= 1000"` — write `"counters.score >= 1000"`
  instead (with a dot, and the plural form)

---

### Pattern 1.4 — an optional bonus stage

**Intent**: a player who found a hidden key can enter the bonus stage;
everyone else skips straight to Stage 2.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_stage1",
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
        "id": "lv_stage1",
        "next": [
          {
            "id": "lv_bonus",
            "condition": "flags.found_bonus_key == true"
          },
          {
            "id": "lv_stage2",
            "condition": "flags.stage1_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_find_key",
            "trigger": "zone_hidden_key",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "found_bonus_key",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_clear_s1",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "stage1_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_stage1"
      },
      {
        "id": "lv_bonus",
        "next": [
          {
            "id": "lv_stage2",
            "condition": "flags.bonus_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_bonus_clear",
            "trigger": "zone_bonus_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "bonus_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_bonus"
      },
      {
        "id": "lv_stage2",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_stage2"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: the optional branch and the main path both lead to
the same place (`lv_stage2`). The Validator allows this, since
`lv_stage2` exists in that same world.

---

### Pattern 1.5 — a 5-stage chain (an action game)

**Intent**: a classic 5-stage action game: 1-1 → 1-2 → 1-3 → 1-4 → 1-5
(the boss).

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_1_1",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_world1",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_1_1",
        "next": [
          {
            "id": "lv_1_2",
            "condition": "flags.lv1_1_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_clear_1_1",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "lv1_1_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_1_1"
      },
      {
        "id": "lv_1_2",
        "next": [
          {
            "id": "lv_1_3",
            "condition": "flags.lv1_2_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_clear_1_2",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "lv1_2_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_1_2"
      },
      {
        "id": "lv_1_3",
        "next": [
          {
            "id": "lv_1_4",
            "condition": "flags.lv1_3_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_clear_1_3",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "lv1_3_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_1_3"
      },
      {
        "id": "lv_1_4",
        "next": [
          {
            "id": "lv_1_5_boss",
            "condition": "flags.lv1_4_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_clear_1_4",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "lv1_4_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_1_4"
      },
      {
        "id": "lv_1_5_boss",
        "next": [
          {
            "id": "lv_ending",
            "condition": "flags.boss_defeated == true"
          }
        ],
        "rules": [
          {
            "id": "rule_boss_down",
            "trigger": "signal_boss_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "boss_defeated",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_1_5_boss"
      },
      {
        "id": "lv_ending",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_ending"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: each stage has exactly one rule (the clear rule)
and one `next` entry. This is the standard shape for an "action game
chain".

---

## Section 2: Win / Loss Condition Patterns

### Pattern 2.1 — a goal zone leads to a stage clear

**Intent**: a player reaches the goal zone, and the stage ends.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_main",
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
        "id": "lv_main",
        "next": [
          {
            "id": "lv_clear",
            "condition": "flags.goal_reached == true"
          }
        ],
        "rules": [
          {
            "id": "rule_goal",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "goal_reached",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_main"
      },
      {
        "id": "lv_clear",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_clear"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: this pattern, built on a trigger, keeps the "enter
the zone" event (the trigger) apart from the logic for the transition
(the condition on `next`).

---

### Pattern 2.2 — a time limit (a counter reaching zero)

**Intent**: a timer counts down; once time runs out, the player loses.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_timed",
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
        "id": "lv_timed",
        "next": [
          {
            "id": "lv_clear",
            "condition": "flags.goal_reached == true"
          },
          {
            "id": "lv_game_over",
            "condition": "counters.time_left <= 0"
          }
        ],
        "rules": [
          {
            "id": "rule_tick",
            "trigger": "signal_timer_tick",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "time_left",
                "delta": 1.0,
                "op": "Sub"
              }
            },
            "once": false
          },
          {
            "id": "rule_goal",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "goal_reached",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_timed"
      },
      {
        "id": "lv_clear",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_clear"
      },
      {
        "id": "lv_game_over",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_game_over"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: the timer's own tick uses `once: false` (it
repeats), with `"op": "Sub"`. Both the win condition and the lose
condition are written as `next` entries at the same level.

**Mistakes to avoid**:

+ wrong: `"op": "sub"` — a CounterOp's value is written in PascalCase:
  `"Add"`, `"Sub"`, `"Set"`

---

### Pattern 2.3 — a lives system

**Intent**: a player starts with 3 lives; losing all of them leads to a
game over.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_stage",
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
        "id": "lv_stage",
        "next": [
          {
            "id": "lv_game_over",
            "condition": "counters.lives <= 0"
          },
          {
            "id": "lv_clear",
            "condition": "flags.goal_reached == true"
          }
        ],
        "rules": [
          {
            "id": "rule_lose_life",
            "trigger": "signal_player_died",
            "condition": "counters.lives > 0",
            "command": {
              "update_counter": {
                "key": "lives",
                "delta": 1.0,
                "op": "Sub"
              }
            },
            "once": false
          },
          {
            "id": "rule_goal",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "goal_reached",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_stage"
      },
      {
        "id": "lv_game_over",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_game_over"
      },
      {
        "id": "lv_clear",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_clear"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: the "lose a life" rule is guarded by
`counters.lives > 0`, so lives never drop below zero. Both the lose path
and the win path are `next` entries, checked in order.

---

### Pattern 2.4 — a continue mechanic

**Intent**: a player has 3 lives and 3 continues. Each continue refills
the lives. It is only a game over once both reach zero.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {"lives": 3, "continues": 3},
    "inventory": {},
    "current_node": "lv_stage",
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
        "id": "lv_stage",
        "name": "Stage",
        "kind": "level",
        "scene": "Lv_stage",
        "children": [],
        "next": [
          { "id": "lv_clear", "condition": "flags.stage_clear == true" },
          { "id": "lv_game_over", "condition": "counters.lives <= 0 && counters.continues <= 0" }
        ],
        "rules": [
          {
            "id": "rule_lose_life",
            "trigger": "signal_player_died",
            "condition": "counters.lives > 0",
            "command": { "update_counter": { "key": "lives", "delta": 1.0, "op": "Sub" } },
            "once": false
          },
          {
            "id": "rule_consume_continue",
            "trigger": "signal_player_died",
            "condition": "counters.lives <= 0 && counters.continues > 0",
            "command": { "update_counter": { "key": "continues", "delta": 1.0, "op": "Sub" } },
            "once": false
          },
          {
            "id": "rule_restore_lives_after_continue",
            "trigger": "signal_player_died",
            "condition": "counters.lives <= 0 && counters.continues > 0",
            "command": { "update_counter": { "key": "lives", "delta": 3.0, "op": "Set" } },
            "once": false
          }
        ]
      },
      {
        "id": "lv_clear",
        "name": "Stage Clear",
        "kind": "ending",
        "scene": "Lv_clear",
        "children": [],
        "next": [],
        "rules": []
      },
      {
        "id": "lv_game_over",
        "name": "Game Over",
        "kind": "ending",
        "scene": "Lv_game_over",
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

**Why this pattern**: three rules fire on the player's death, in a set
order of priority. While `lives > 0`, only the lives go down by one.
Once `lives == 0` and `continues > 0`, a continue is spent, and lives
are set back to 3 (this stands in for the "continue" action). Only
once BOTH reach zero does the transition to `lv_game_over` turn on.
This avoids any loop in the transitions (`lv_stage → lv_game_over →
lv_stage`), which would trip Validator V012.

**Mistakes to avoid**:

+ wrong: a loop in the transitions: `lv_game_over → lv_stage` trips
  V012 (a loop found in the transition chain). Handle a continue
  *within* the stage itself, rather than looping back from a separate
  game-over screen.
+ wrong: leaving out the `condition` that keeps things apart: dropping
  `counters.lives > 0` from `rule_lose_life` lets lives go below zero on
  that same death event.

### Pattern 2.5 — a score attack (no lose condition, time-limited)

**Intent**: score as high as you can within the time limit; this
always leads to the ranking screen.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_score_attack",
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
        "id": "lv_score_attack",
        "next": [
          {
            "id": "lv_ranking",
            "condition": "counters.time_left <= 0"
          }
        ],
        "rules": [
          {
            "id": "rule_coin",
            "trigger": "zone_coin",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "score",
                "delta": 100.0,
                "op": "Add"
              }
            },
            "once": false
          },
          {
            "id": "rule_tick",
            "trigger": "signal_timer_tick",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "time_left",
                "delta": 1.0,
                "op": "Sub"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_score_attack"
      },
      {
        "id": "lv_ranking",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_ranking"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: there is no lose condition written in plain terms.
The game always moves on to ranking once `time_left <= 0`. The score
builds up through rules marked `once: false`.

---

## Section 3: Inventory and Key Patterns

### Pattern 3.1 — a locked door (needs a key item)

**Intent**: a door can only be opened once the player holds at least one
key.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_dungeon_entrance",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_dungeon",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_dungeon_entrance",
        "next": [
          {
            "id": "lv_dungeon_inner",
            "condition": "inventory.key_item >= 1"
          }
        ],
        "rules": [
          {
            "id": "rule_pick_key",
            "trigger": "zone_key_pickup",
            "condition": "",
            "command": {
              "update_inventory": {
                "key": "key_item",
                "delta": 1
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_dungeon_entrance"
      },
      {
        "id": "lv_dungeon_inner",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_dungeon_inner"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: checking the inventory, right inside
`next[].condition`, gates the door. Picking up the key uses
`update_inventory`, with `delta: 1`.

**Mistakes to avoid**:

+ wrong: `"id": "key_item"` — write `"key": "key_item"` instead (the
  field is called `key`, not `id`)

---

### Pattern 3.2 — a consumable item (a potion, ammo, and so on)

**Intent**: a player picks up potions; each pickup adds 1. Using a potion
takes away 1.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_overworld",
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
        "id": "lv_overworld",
        "next": [
          {
            "id": "lv_boss",
            "condition": "flags.entered_boss_room == true"
          }
        ],
        "rules": [
          {
            "id": "rule_pickup_potion",
            "trigger": "zone_potion",
            "condition": "",
            "command": {
              "update_inventory": {
                "key": "potion",
                "delta": 1
              }
            },
            "once": false
          },
          {
            "id": "rule_use_potion",
            "trigger": "signal_use_potion",
            "condition": "inventory.potion >= 1",
            "command": {
              "update_inventory": {
                "key": "potion",
                "delta": -1
              }
            },
            "once": false
          },
          {
            "id": "rule_enter_boss",
            "trigger": "zone_boss_entrance",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "entered_boss_room",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_overworld"
      },
      {
        "id": "lv_boss",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_boss"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: `delta: -1` is used for using it up, guarded by
`inventory.potion >= 1`, so it never drops below zero.

---

### Pattern 3.3 — a multi-key puzzle (3 keys needed)

**Intent**: a door needs 3 different keys before it can be opened.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_puzzle",
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
        "id": "lv_puzzle",
        "next": [
          {
            "id": "lv_vault",
            "condition": "inventory.key_red >= 1 && inventory.key_blue >= 1 && inventory.key_green >= 1"
          }
        ],
        "rules": [
          {
            "id": "rule_red",
            "trigger": "zone_key_red",
            "condition": "",
            "command": {
              "update_inventory": {
                "key": "key_red",
                "delta": 1
              }
            },
            "once": true
          },
          {
            "id": "rule_blue",
            "trigger": "zone_key_blue",
            "condition": "",
            "command": {
              "update_inventory": {
                "key": "key_blue",
                "delta": 1
              }
            },
            "once": true
          },
          {
            "id": "rule_green",
            "trigger": "zone_key_green",
            "condition": "",
            "command": {
              "update_inventory": {
                "key": "key_green",
                "delta": 1
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_puzzle"
      },
      {
        "id": "lv_vault",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_vault"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: `&&` joins more than one inventory check inside a
single condition string. Each key's own rule fires `once: true`, so it
cannot stack.

---

### Pattern 3.4 — an item upgrade (gathering N stones sets a flag)

**Intent**: gathering 3 magic stones unlocks the magic-sword flag.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_collect",
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
        "id": "lv_collect",
        "next": [
          {
            "id": "lv_empowered",
            "condition": "flags.has_magic_sword == true"
          }
        ],
        "rules": [
          {
            "id": "rule_collect_stone",
            "trigger": "zone_magic_stone",
            "condition": "",
            "command": {
              "update_inventory": {
                "key": "magic_stone",
                "delta": 1
              }
            },
            "once": false
          },
          {
            "id": "rule_upgrade",
            "trigger": "signal_upgrade_check",
            "condition": "inventory.magic_stone >= 3",
            "command": {
              "set_flag": {
                "key": "has_magic_sword",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_collect"
      },
      {
        "id": "lv_empowered",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_empowered"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: the upgrade check fires when
`signal_upgrade_check` is sent (say, the player presses an upgrade
button) AND the stone count is enough.

---

## Section 4: ADV / Branching Story Patterns

### Pattern 4.1 — a multi-route choice (3 routes from a hub)

**Intent**: at a crossroads, the player picks one of three routes,
through a dialogue signal.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_crossroads",
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
        "id": "lv_crossroads",
        "next": [
          {
            "id": "lv_forest",
            "condition": "flags.route_forest == true"
          },
          {
            "id": "lv_cave",
            "condition": "flags.route_cave == true"
          },
          {
            "id": "lv_town",
            "condition": "flags.route_town == true"
          }
        ],
        "rules": [
          {
            "id": "rule_forest",
            "trigger": "signal_choose_forest",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "route_forest",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_cave",
            "trigger": "signal_choose_cave",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "route_cave",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_town",
            "trigger": "signal_choose_town",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "route_town",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_crossroads"
      },
      {
        "id": "lv_forest",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_forest"
      },
      {
        "id": "lv_cave",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_cave"
      },
      {
        "id": "lv_town",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_town"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: three flags that can never both be true, each set
by its own dialogue signal. Only one `next` condition can hold true at
once (as long as the signals never overlap).

---

### Pattern 4.2 — a karma system (a counter shapes the ending)

**Intent**: kind choices raise karma; karma of 10 or more leads to the
good ending.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_journey",
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
        "id": "lv_journey",
        "next": [
          {
            "id": "lv_good_ending",
            "condition": "counters.karma >= 10"
          },
          {
            "id": "lv_bad_ending",
            "condition": "flags.journey_end == true && counters.karma < 10"
          }
        ],
        "rules": [
          {
            "id": "rule_kind_act",
            "trigger": "signal_kind_act",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "karma",
                "delta": 1.0,
                "op": "Add"
              }
            },
            "once": false
          },
          {
            "id": "rule_end_journey",
            "trigger": "zone_final_gate",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "journey_end",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_journey"
      },
      {
        "id": "lv_good_ending",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_good_ending"
      },
      {
        "id": "lv_bad_ending",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_bad_ending"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: comparing a counter, right inside
`next[].condition`, gives a natural threshold to route on. The bad
ending is guarded with `&& counters.karma < 10`, so it never wrongly
turns on.

---

### Pattern 4.3 — data that lasts across scenes (a cross-scene save)

**Intent**: hold onto the player's chosen name and difficulty, so they
last through a scene change.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_title",
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
        "id": "lv_title",
        "next": [
          {
            "id": "lv_prologue",
            "condition": "flags.name_set == true"
          }
        ],
        "rules": [
          {
            "id": "rule_set_name",
            "trigger": "signal_name_confirmed",
            "condition": "",
            "command": {
              "set_persistence": {
                "key": "player_name",
                "value": "Hero"
              }
            },
            "once": true
          },
          {
            "id": "rule_start",
            "trigger": "signal_start_game",
            "condition": "flags.name_set == false",
            "command": {
              "set_flag": {
                "key": "name_set",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_title"
      },
      {
        "id": "lv_prologue",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_prologue"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: `set_persistence` holds a string value that lives
on through a scene reload. Unlike `flags` or `counters`,
`persistence`'s values must always be strings.

**Mistakes to avoid**:

+ wrong: `"value": 100` — a persistence value must be a string:
  `"value": "100"`

---

### Pattern 4.4 — more than one ending (two flags decide it)

**Intent**: the true ending needs both `hero_won` AND
`loved_one_saved` to hold; otherwise, it is the normal ending.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_final_battle",
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
        "id": "lv_final_battle",
        "next": [
          {
            "id": "lv_true_end",
            "condition": "flags.hero_won == true && flags.loved_one_saved == true"
          },
          {
            "id": "lv_normal_end",
            "condition": "flags.hero_won == true && flags.loved_one_saved == false"
          },
          {
            "id": "lv_bad_end",
            "condition": "flags.hero_lost == true"
          }
        ],
        "rules": [
          {
            "id": "rule_hero_win",
            "trigger": "signal_hero_wins",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "hero_won",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_save_npc",
            "trigger": "signal_npc_saved",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "loved_one_saved",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_hero_lose",
            "trigger": "signal_hero_dies",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "hero_lost",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_final_battle"
      },
      {
        "id": "lv_true_end",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_true_end"
      },
      {
        "id": "lv_normal_end",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_normal_end"
      },
      {
        "id": "lv_bad_end",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_bad_end"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: `&&` inside a condition joins more than one flag
check, giving an N-by-M set of ending paths with no extra levels needed
in between.

---

### Pattern 4.5 — a chapter gate (unlocking the next chapter)

**Intent**: Chapter 2 stays locked until every flag from Chapter 1 is
set.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_ch1_final",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_story",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_ch1_final",
        "next": [
          {
            "id": "lv_ch2_start",
            "condition": "flags.ch1_complete == true"
          }
        ],
        "rules": [
          {
            "id": "rule_ch1_done",
            "trigger": "zone_chapter_end",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "ch1_complete",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_ch1_final"
      },
      {
        "id": "lv_ch2_start",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_ch2_start"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: one single flag gates the chapter's transition.
The `zone_chapter_end` trigger is sent out by the Zone MonoBehaviour when
the player steps into the end area.

---

## Section 5: Boss Fight Patterns

### Pattern 5.1 — a phase change based on an HP threshold

**Intent**: once the boss's HP drops below 50%, Phase 2 begins (it grows
angry).

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_boss_arena",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_boss",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_boss_arena",
        "next": [
          {
            "id": "lv_victory",
            "condition": "flags.boss_defeated == true"
          }
        ],
        "rules": [
          {
            "id": "rule_boss_hit",
            "trigger": "signal_boss_hit",
            "condition": "counters.boss_hp > 0",
            "command": {
              "update_counter": {
                "key": "boss_hp",
                "delta": 10.0,
                "op": "Sub"
              }
            },
            "once": false
          },
          {
            "id": "rule_phase2",
            "trigger": "signal_hp_check",
            "condition": "counters.boss_hp <= 50",
            "command": {
              "set_flag": {
                "key": "boss_phase2",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_boss_die",
            "trigger": "signal_boss_hit",
            "condition": "counters.boss_hp <= 0",
            "command": {
              "set_flag": {
                "key": "boss_defeated",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_boss_arena"
      },
      {
        "id": "lv_victory",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_victory"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: two rules listen for the same trigger
(`signal_boss_hit`): one takes away HP, the other checks whether it has
died. Phase 2 is its own, separate rule, checked through
`signal_hp_check`, to keep the two concerns apart.

---

### Pattern 5.2 — a sequence of bosses (beat A, then B)

**Intent**: a player must beat Boss A, then Boss B, before reaching the
final stage.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_boss_a",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_boss_sequence",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_boss_a",
        "next": [
          {
            "id": "lv_boss_b",
            "condition": "flags.boss_a_defeated == true"
          }
        ],
        "rules": [
          {
            "id": "rule_boss_a_die",
            "trigger": "signal_boss_a_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "boss_a_defeated",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_boss_a"
      },
      {
        "id": "lv_boss_b",
        "next": [
          {
            "id": "lv_final_stage",
            "condition": "flags.boss_b_defeated == true"
          }
        ],
        "rules": [
          {
            "id": "rule_boss_b_die",
            "trigger": "signal_boss_b_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "boss_b_defeated",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_boss_b"
      },
      {
        "id": "lv_final_stage",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_final_stage"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: each boss is its own level. The order they depend
on is shown by the order of the levels themselves: Boss A → Boss B →
the Final Stage.

---

### Pattern 5.3 — an optional boss (only open with the boss key)

**Intent**: a hidden boss room can only be reached once the player has
picked up the boss key.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_pre_boss",
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
        "id": "lv_pre_boss",
        "next": [
          {
            "id": "lv_hidden_boss",
            "condition": "inventory.boss_key >= 1"
          },
          {
            "id": "lv_final_castle",
            "condition": "flags.ready_for_castle == true"
          }
        ],
        "rules": [
          {
            "id": "rule_enter_castle",
            "trigger": "zone_castle_gate",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "ready_for_castle",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_pre_boss"
      },
      {
        "id": "lv_hidden_boss",
        "next": [
          {
            "id": "lv_final_castle",
            "condition": "flags.hidden_boss_clear == true"
          }
        ],
        "rules": [
          {
            "id": "rule_hidden_boss_clear",
            "trigger": "signal_hidden_boss_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "hidden_boss_clear",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_hidden_boss"
      },
      {
        "id": "lv_final_castle",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_final_castle"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: both the optional boss path and the path around it
meet again at `lv_final_castle`. A player with no key skips straight to
the castle.

---

### Pattern 5.4 — a boss rush (3 bosses, one shared continues counter)

**Intent**: a player fights 3 bosses, one after another; running out of
lives ends the game.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_rush_boss1",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_boss_rush",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_rush_boss1",
        "next": [
          {
            "id": "lv_rush_boss2",
            "condition": "flags.rush_b1_clear == true"
          },
          {
            "id": "lv_rush_gameover",
            "condition": "counters.lives <= 0"
          }
        ],
        "rules": [
          {
            "id": "rule_rush_b1_clear",
            "trigger": "signal_boss1_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "rush_b1_clear",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_rush_lose_life_1",
            "trigger": "signal_player_died",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "lives",
                "delta": 1.0,
                "op": "Sub"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_rush_boss1"
      },
      {
        "id": "lv_rush_boss2",
        "next": [
          {
            "id": "lv_rush_boss3",
            "condition": "flags.rush_b2_clear == true"
          },
          {
            "id": "lv_rush_gameover",
            "condition": "counters.lives <= 0"
          }
        ],
        "rules": [
          {
            "id": "rule_rush_b2_clear",
            "trigger": "signal_boss2_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "rush_b2_clear",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_rush_lose_life_2",
            "trigger": "signal_player_died",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "lives",
                "delta": 1.0,
                "op": "Sub"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_rush_boss2"
      },
      {
        "id": "lv_rush_boss3",
        "next": [
          {
            "id": "lv_rush_victory",
            "condition": "flags.rush_b3_clear == true"
          },
          {
            "id": "lv_rush_gameover",
            "condition": "counters.lives <= 0"
          }
        ],
        "rules": [
          {
            "id": "rule_rush_b3_clear",
            "trigger": "signal_boss3_defeated",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "rush_b3_clear",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_rush_lose_life_3",
            "trigger": "signal_player_died",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "lives",
                "delta": 1.0,
                "op": "Sub"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_rush_boss3"
      },
      {
        "id": "lv_rush_victory",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_rush_victory"
      },
      {
        "id": "lv_rush_gameover",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_rush_gameover"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: the `lives` counter is shared across every boss
level. Each boss level checks `counters.lives <= 0` on its own, as its
own way out.

---

### Pattern 5.5 — a boss weak-point system

**Intent**: the boss's weak point is bared once its phase changes;
damage is raised while it is bared.

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_final_boss",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_final",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_final_boss",
        "next": [
          {
            "id": "lv_true_ending",
            "condition": "flags.final_boss_down == true"
          }
        ],
        "rules": [
          {
            "id": "rule_break_armor",
            "trigger": "signal_armor_hit",
            "condition": "counters.boss_armor > 0",
            "command": {
              "update_counter": {
                "key": "boss_armor",
                "delta": 20.0,
                "op": "Sub"
              }
            },
            "once": false
          },
          {
            "id": "rule_expose_weakpoint",
            "trigger": "signal_armor_hit",
            "condition": "counters.boss_armor <= 0",
            "command": {
              "set_flag": {
                "key": "weakpoint_exposed",
                "value": true
              }
            },
            "once": true
          },
          {
            "id": "rule_weakpoint_hit",
            "trigger": "signal_weakpoint_hit",
            "condition": "flags.weakpoint_exposed == true",
            "command": {
              "update_counter": {
                "key": "boss_hp",
                "delta": 50.0,
                "op": "Sub"
              }
            },
            "once": false
          },
          {
            "id": "rule_boss_dies",
            "trigger": "signal_weakpoint_hit",
            "condition": "counters.boss_hp <= 0",
            "command": {
              "set_flag": {
                "key": "final_boss_down",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "lv_final_boss"
      },
      {
        "id": "lv_true_ending",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "lv_true_ending"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**Why this pattern**: two trigger listeners, each standing on its own
(`signal_armor_hit`), work together: one takes armor away, the other
sets the exposed flag once armor hits zero.

---

## Section 6: Common Failure Patterns (Keeping an LLM from Making Things Up)

These patterns show the **correct JSON**, next to a note on what an LLM
often builds wrongly instead. Put these into your system prompt to keep
an LLM from making things up.

### Pattern 6.1 — correct: command fields in snake_case

**Intent**: set a flag on entering the goal zone. (A common mistake:
`setFlag`, `updateCounter`)

**The correct pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_test",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_test",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_test",
        "next": [
          {
            "id": "lv_done",
            "condition": "flags.goal == true"
          }
        ],
        "rules": [
          {
            "id": "rule_goal",
            "trigger": "zone_goal",
            "condition": "",
            "command": {
              "set_flag": {
                "key": "goal",
                "value": true
              }
            },
            "once": true
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_test"
      },
      {
        "id": "lv_done",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_done"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**NG (mistakes to watch for)**:

+ wrong: `setFlag: {...}` — must be `set_flag`
+ wrong: `updateCounter: {...}` — must be `update_counter`
+ wrong: `setPersistence: {...}` — must be `set_persistence`

---

### Pattern 6.2 — correct: one single `command` object (not a list called `actions`)

**Intent**: raise the score on picking up a coin. (A common mistake: an
`actions: [...]` list)

**The correct pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_collect",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_test",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_collect",
        "next": [
          {
            "id": "lv_end",
            "condition": "counters.score >= 500"
          }
        ],
        "rules": [
          {
            "id": "rule_coin",
            "trigger": "zone_coin",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "score",
                "delta": 100.0,
                "op": "Add"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_collect"
      },
      {
        "id": "lv_end",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_end"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**NG (mistakes to watch for)**:

+ wrong: `"actions": [{ "update_counter": ... }]` — `Rule.command` is one
  single object, not a list
+ wrong: `"commands": [...]` — the field is `command` (singular)

---

### Pattern 6.3 — correct: a condition written with dot notation

**Intent**: move on once the score reaches 1000. (A common mistake: a
prefix written as `counter:score >= 1000`)

**The correct pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "lv_game",
    "current_team": "",
    "persistence": {}
  },
  "root": {
    "id": "w_test",
    "name": "Main World",
    "kind": "world",
    "scene": "",
    "children": [
      {
        "id": "lv_game",
        "next": [
          {
            "id": "lv_clear",
            "condition": "counters.score >= 1000"
          }
        ],
        "rules": [
          {
            "id": "rule_score",
            "trigger": "zone_coin",
            "condition": "",
            "command": {
              "update_counter": {
                "key": "score",
                "delta": 100.0,
                "op": "Add"
              }
            },
            "once": false
          }
        ],
        "name": "",
        "kind": "",
        "scene": "Lv_game"
      },
      {
        "id": "lv_clear",
        "next": [],
        "rules": [],
        "name": "",
        "kind": "",
        "scene": "Lv_clear"
      }
    ],
    "next": [],
    "rules": []
  }
}
```

**NG (mistakes to watch for)**:

+ wrong: `"counter:score >= 1000"` — write `"counters.score >= 1000"`
  instead (a dot, and the plural form)
+ wrong: `"flag:goal == true"` — write `"flags.goal == true"` instead (a
  dot, and the plural form)
+ wrong: `"inventory:key >= 1"` — write `"inventory.key_item >= 1"`
  instead (a dot; inventory does not change to a plural form)

---

## Section 7: History-Dependent Patterns (added in Phase 5.8)

Patterns that lean on `History`. These cover effects that flags and
counters alone cannot give.

These patterns use the `history.*` DSL functions brought in with Phase
5.8. The `history.*` family reads from `Snapshot.history.entries` (a log
of events, in the order they happened), which lets a condition be built
on order, time passed, or how many times something happened.

**Limits on the DSL, in Phase 5.8 v2**:

+ arithmetic operators (`-`, `+`, `*`, `/`) are not supported
+ `now()` has a place set aside for it in the spec, but is not yet built
  in ExprParser
+ a string in quotes is not supported (for example,
  `history.last(...).target_id == "shop"` will not parse)
+ a `history.*` call **does not work nested inside `&&`, `||`, or `!`**
  — the part of the evaluator that handles this falls back to a
  state-only path, and the history node throws, giving back `false`
  instead. Use these only at the top level (or right inside one
  comparison). To check more than one history condition at once, split
  them into separate rules, or work the result out ahead of time into a
  `flag`.
+ `history.has(...)` can stand alone as a bool, but writing it as
  `history.count(...) >= 1` is the form we suggest, since it is safer
  for the Validator
+ the forms we suggest: `history.count(kind=..., target_id=...) >= N`,
  and `history.session_count() >= N`

**How history is recorded (which entries does the running game write on
its own?)**:

+ **only `kind="rule_fire"` is recorded on its own.**
  `Store.DispatchTrigger` writes one entry every time a `once=true` rule
  fires (its `target_id` is that rule's own id).
+ **every other kind** (`node_enter`, `node_exit`, `node_fail`, or any
  word you choose) **must be written by a Rule whose command is
  `record_event`**. Every pattern below already holds such a rule — never
  assume the running game fills in `node_enter` on its own. Forget the
  `record_event` rule, and `history.count(kind=node_enter, ...)` stays at
  `0` forever.
+ `_session_count` and `_total_play_time` are read by
  `history.session_count()` / `history.total_play_time()`, from
  `state.counters`, but **neither is raised on its own either** — the
  game's own code must raise them, through `update_counter`.

### Pattern 7.1 — a rescue, based on how many times something failed (adjusting the difficulty on its own)

**Intent**: give a rescue item (`help_item`) to a player who has failed
Stage 01 five times or more. A "the world remembers" kind of effect, in
the style of Hades.

**The key part of the DSL**: `history.count(kind=node_fail,
target_id=stage_01) >= 5`

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "stage_01",
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
        "id": "stage_01",
        "name": "Stage 01",
        "kind": "level",
        "scene": "Stage_01",
        "children": [],
        "next": [
          { "id": "stage_clear", "condition": "flags.stage_01_cleared == true" }
        ],
        "rules": [
          {
            "id": "rule_fail",
            "trigger": "zone_fail",
            "condition": "",
            "command": {
              "record_event": { "kind": "node_fail", "target_id": "stage_01" }
            },
            "once": false
          },
          {
            "id": "rule_give_help_on_5th_failure",
            "trigger": "zone_fail",
            "condition": "history.count(kind=node_fail, target_id=stage_01) >= 5",
            "command": {
              "update_inventory": { "key": "help_item", "delta": 1 }
            },
            "once": true
          }
        ]
      },
      {
        "id": "stage_clear",
        "name": "Stage Clear",
        "kind": "ending",
        "scene": "StageClear",
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

**Why this pattern**:

+ use `record_event` to log each failure into the history (`once:
  false`)
+ a second rule sums up the history (`history.count >= 5`), and gives
  the rescue item (`once: true`)
+ flags and counters alone cannot hold "failed 5 or more times in the
  past", with a timestamp for each

**Mistakes to avoid**:

+ `counters.fail_count >= 5` is a working alternative, but history also
  keeps a timestamp, and bends more easily to other uses
+ leaving out `target_id` in `history.count` sums up every failure,
  even from other stages

---

### Pattern 7.2 — an NPC reacting to how many times it was visited (aware of past visits)

**Intent**: give a first-time visitor a long explanation; a returning
visitor gets a short greeting instead.

**The key part of the DSL**: `history.count(kind=node_enter,
target_id=shop) >= 2`

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "shop",
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
        "id": "shop",
        "name": "Shop",
        "kind": "shop",
        "scene": "Shop",
        "children": [],
        "next": [
          { "id": "dialog_greeting_short", "condition": "history.count(kind=node_enter, target_id=shop) >= 2" },
          { "id": "dialog_greeting_first", "condition": "" }
        ],
        "rules": [
          {
            "id": "rule_record_visit",
            "trigger": "zone_shop_enter",
            "condition": "",
            "command": {
              "record_event": { "kind": "node_enter", "target_id": "shop" }
            },
            "once": false
          }
        ]
      },
      {
        "id": "dialog_greeting_first",
        "name": "First Visit Dialog",
        "kind": "level",
        "scene": "DialogFirstVisit",
        "children": [],
        "next": [],
        "rules": []
      },
      {
        "id": "dialog_greeting_short",
        "name": "Short Greeting Dialog",
        "kind": "level",
        "scene": "DialogShortGreeting",
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

**Why this pattern**:

+ use `record_event` to log each visit into the history (`once: false`)
+ use a history count inside a transition's condition, to branch
  between a first visit and a returning one
+ a plain counter is a working alternative, but history also keeps when
  each visit happened

**Mistakes to avoid**:

+ keeping `counters.shop_visit_count >= 2` on the side copies what the
  history already holds
+ checking elapsed time (such as `now() - history.time_since(...)`)
  cannot be used yet, since arithmetic operators and `now()` are not
  supported in the DSL as it stands (this is planned for a later phase)

---

### Pattern 7.3 — finding a New Game+ (aware of the playthrough)

**Intent**: show the true ending to a player on their second playthrough
or later; a first-time player sees the normal ending instead.

**The key part of the DSL**: `history.session_count() >= 2`

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "ending_check",
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
        "id": "ending_check",
        "name": "Ending Branch",
        "kind": "level",
        "scene": "EndingCheck",
        "children": [],
        "next": [
          { "id": "secret_ending", "condition": "history.session_count() >= 2" },
          { "id": "normal_ending", "condition": "" }
        ],
        "rules": []
      },
      {
        "id": "secret_ending",
        "name": "Secret Ending",
        "kind": "ending",
        "scene": "SecretEnding",
        "children": [],
        "next": [],
        "rules": []
      },
      {
        "id": "normal_ending",
        "name": "Normal Ending",
        "kind": "ending",
        "scene": "NormalEnding",
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

**Why this pattern**:

+ `history.session_count()` reads `state.counters["_session_count"]`, by
  custom (see the DSL spec, §6)
+ **the running game does NOT raise `_session_count` on its own.** The
  game's own code must raise it by hand (say, through `update_counter`
  on a launch trigger, or in its own start-up code), for this pattern to
  work
+ conditions are checked from top to bottom; the first `next` that comes
  out true is the one chosen
+ the order matters here: put `secret_ending` first

**Mistakes to avoid**:

+ making up your own `counters.playthrough_count` copies what the
  history counter already holds
+ checking the conditions in the reverse order always picks
  `normal_ending` instead

---

### Pattern 7.4 — branching, based on the order of past visits (aware of the path taken)

**Intent**: a player who visited the optional dungeon before the boss
fight gets the true-route boss fight; everyone else gets the normal
route.

**The key part of the DSL**: `history.count(kind=node_enter,
target_id=optional_dungeon) >= 1`

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "boss_door",
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
        "id": "optional_dungeon",
        "name": "Optional Dungeon",
        "kind": "level",
        "scene": "OptionalDungeon",
        "children": [],
        "next": [
          { "id": "boss_door", "condition": "" }
        ],
        "rules": [
          {
            "id": "rule_record_optional_visit",
            "trigger": "zone_dungeon_enter",
            "condition": "",
            "command": {
              "record_event": { "kind": "node_enter", "target_id": "optional_dungeon" }
            },
            "once": true
          }
        ]
      },
      {
        "id": "boss_door",
        "name": "Boss Door",
        "kind": "level",
        "scene": "BossDoor",
        "children": [],
        "next": [
          {
            "id": "boss_true_route",
            "condition": "history.count(kind=node_enter, target_id=optional_dungeon) >= 1"
          },
          {
            "id": "boss_normal_route",
            "condition": ""
          }
        ],
        "rules": []
      },
      {
        "id": "boss_true_route",
        "name": "Boss True Route",
        "kind": "boss",
        "scene": "BossTrue",
        "children": [],
        "next": [],
        "rules": []
      },
      {
        "id": "boss_normal_route",
        "name": "Boss Normal Route",
        "kind": "boss",
        "scene": "BossNormal",
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

**Why this pattern**:

+ `history.count(...) >= 1` says "at least one matching event exists"
+ this comes to the same thing as `history.has(...)`, but it holds a
  plain comparison operator, and is safer for the Validator's own checks
+ use `record_event` to log the fact of the visit into history (`once:
  true` keeps it from being recorded twice)

**Mistakes to avoid**:

+ setting `flags.optional_dungeon_visited = true` is a working
  alternative, but history also keeps when the visit happened (which
  bends more easily to future patterns)
+ forgetting `once: true` on the `optional_dungeon` rule lets the
  history grow every time it is entered

---

### Pattern 7.5 — unlocking a hidden thing, tied to a condition (unlocked, conditional on a hint)

**Intent**: only a player who found the hint (the `hint_rule` fired) can
unlock the hidden room. Everyone else sees a plain wall.

**The key part of the DSL**: `history.count(kind=rule_fire,
target_id=hint_rule) >= 1`

**Pattern**:

```json
{
  "schema_version": 1,
  "initial_state": {
    "flags": {},
    "counters": {},
    "inventory": {},
    "current_node": "library",
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
        "id": "library",
        "name": "Library",
        "kind": "level",
        "scene": "Library",
        "children": [],
        "next": [
          { "id": "secret_room", "condition": "flags.secret_room_unlocked == true" }
        ],
        "rules": [
          {
            "id": "hint_rule",
            "trigger": "zone_read_book",
            "condition": "",
            "command": {
              "set_flag": { "key": "hint_seen", "value": true }
            },
            "once": true
          },
          {
            "id": "rule_unlock_secret",
            "trigger": "zone_examine_wall",
            "condition": "history.count(kind=rule_fire, target_id=hint_rule) >= 1",
            "command": {
              "set_flag": { "key": "secret_room_unlocked", "value": true }
            },
            "once": true
          }
        ]
      },
      {
        "id": "secret_room",
        "name": "Secret Room",
        "kind": "level",
        "scene": "SecretRoom",
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

**Why this pattern**:

+ a rule-fire event (`kind=rule_fire`) is recorded into the history by
  the running game, on its own
+ set `target_id` to that rule's own id
+ use `history.count(...) >= 1` to check "has the hint been seen at
  least once"
+ a player who has never seen the hint cannot unlock it through
  `examine_wall`

**Mistakes to avoid**:

+ using `flags.hint_seen` directly is a working choice, but the history
  path also keeps track of which rule fired, and when — useful while
  debugging
+ forgetting `once: true` on `hint_rule` lets the history grow every
  time it fires

---

### Section 7 Summary

| Pattern | DSL function | What it is used for |
| --- | --- | --- |
| 7.1 | `history.count(kind=..., target_id=...) >= N` | summing up a count (a rescue after N failures, and so on) |
| 7.2 | `history.count(kind=..., target_id=...) >= N` | checking how many visits (a first visit against a return) |
| 7.3 | `history.session_count() >= N` | checking the playthrough count (a New Game+, and so on) |
| 7.4 | `history.count(kind=..., target_id=...) >= 1` | branching, based on a visit on record |
| 7.5 | `history.count(kind=..., target_id=...) >= 1` | unlocking, based on a rule-fire on record |

**Key customs to follow**:

+ **only `kind=rule_fire` is recorded on its own**, by
  `Store.DispatchTrigger` (when a `once=true` rule fires)
+ every other kind (`node_enter`, `node_exit`, `node_fail`, or any word
  you choose) needs a plain `Command.record_event` Rule — see the note
  at the start of §7 for the full detail
+ history is kept in `Snapshot.history.entries`, up to 1000 entries
  (`max_entries`)
+ `Snapshot.history` must be passed to `Evaluator.Evaluate`, for the DSL
  to be checked

**Limits on the DSL right now (Phase 5.8 v2)**:

+ the `history.count(...) >= N` form is the safest (both the Validator
  and the Evaluator support it in full)
+ `history.has(...)` and `history.last(...).property` are built into
  the AST, but the Cookbook suggests the plain `history.count(...)`
  form, with a comparison operator, instead
+ arithmetic operators (`-`, `+`, `*`, `/`) and `now()` are planned for a
  later phase
