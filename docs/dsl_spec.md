# Germio Condition DSL Specification

> **Version**: 3.0 (after Phase 5.8 v2)
> **Last updated**: 2026-05-02

---

## 1. Overview

The Germio Condition DSL is a small expression language, used in two
places:

+ `Next.condition` (a guard on a transition)
+ `Rule.condition` (a guard on whether a rule fires)

It is parsed by `ExprLexer` plus `ExprParser` (no regular expressions are
used), checked by `Validator` (V009), and run by `Evaluator`.

**Added in Phase 5.8**:

+ the `history.*` family of functions (reading from the history log)

> **Where a condition is actually checked, at runtime.**
> A condition can sit in two places in `germio.json`: `Rule.condition` and
> `Next.condition`. Only **`Rule.condition`** is ever read by the running
> game — `Store.DispatchTrigger` calls `Evaluator.Evaluate(rule.condition,
> state)` right before it runs `rule.command`. `Next.condition` is, right
> now, only read by the `Validator` (its V006/V009/V011/V012 structural
> checks) and by `Grapher` (for the labels on a Mermaid edge). A running
> game moves between nodes only through
> `command.request_transition: "<target_id>"`, held inside a Rule — never
> through `next[]`. See `dsl_cookbook.md` for the same note.

---

## 2. EBNF Grammar

```ebnf
expr =
    or_expr

or_expr =
    and_expr ("||" and_expr)*

and_expr =
    not_expr ("&&" not_expr)*

not_expr =
    "!" not_expr
  | comparison_expr

comparison_expr =
    primary_expr (comparison_op primary_expr)?

comparison_op =
    "==" | "!=" | "<" | "<=" | ">" | ">="

primary_expr =
    "(" expr ")"
  | accessor_expr
  | history_expr
  | now_expr
  | literal

accessor_expr =
    identifier "." identifier

history_expr =
    "history" "." history_func "(" history_args? ")" ("." identifier)?

history_func =
    "count" | "last" | "has" | "time_since"
  | "session_count" | "total_play_time"

history_args =
    history_arg ("," history_arg)*

history_arg =
    ("kind" | "target_id") "=" identifier  (* a string in quotes is not accepted by parseNamedParam *)

now_expr =
    "now" "(" ")"  (* not yet built *)

literal =
    number | "true" | "false"  (* a string in quotes is not supported: ExprLexer has no token for one *)

identifier =
    [a-zA-Z_] [a-zA-Z0-9_-]*  (* the ExprLexer's own rule for an identifier — a hyphen is allowed; the first letter must be a letter or an underscore *)

number =
    [0-9]+ ("." [0-9]+)?  (* the ExprLexer's own rule for a number — a whole number or one with a decimal point *)
```

---

## 3. What Each Operator Does

### 3.1 Logical operators

+ `&&` (AND): stops checking early once the answer is already known
+ `||` (OR): stops checking early once the answer is already known
+ `!` (NOT): takes one value

### 3.2 Comparison operators

+ `==` / `!=`: checks if two things are equal
  + for a floating-point number: checked with a small margin of error
    (`eps = 1e-6`) — see §9
  + for a whole number, a bool, or a string: checked for an exact match
+ `<` / `<=` / `>` / `>=`: only work on numbers (whole or floating-point)

### 3.3 Accessors

+ `flags.X` → gives a bool (false, if it was never set)
+ `counters.X` → gives a floating-point number (0.0, if it was never set)
+ `inventory.X` → gives a whole number (0, if it was never set)
+ `persistence.X` → **cannot currently be read in a condition** (an
  accessor for it always gives false; `persistence` can be WRITTEN through
  `Command.set_persistence`, but `AccessorNode` does not know how to READ
  this prefix)

---

## 4. Type Rules (checked by V009)

Each operator only accepts certain types:

| Operator | Left side | Right side |
| --- | --- | --- |
| `&&`, `\|\|` | bool | bool |
| `!` | (none) | bool |
| `==`, `!=` | same type as the other side | same type as the other side |
| `<`, `<=`, `>`, `>=` | a number | a number |

V009 (part of the Validator) reports an error for any condition string
that breaks one of these type rules, once it has been parsed.

---

## 5. What Happens on an Error

`Evaluator.Evaluate()` gives back `false` whenever something throws an
error while a condition is being checked (this is the safe path to fall
back on).

Reading an accessor can go wrong in two distinct ways:

+ **A prefix that is not supported** (such as `persistence.X`, which
  `AccessorNode` does not know how to read): this always gives `false`
+ **A key that was never set** (such as `flags.unknown_key`, where the key
  is not in `state.flags`): this is treated as that type's own default
  value — `false` for `flags`, `0.0` for `counters`, `0` for `inventory`

Other ways it can go wrong:

+ a parse error → normally, the Validator's V009 catches this ahead of
  time; if it slips through, the game gives back `false` at runtime
+ a `history.*` node is checked with no `History` object given → gives
  back `false` (an `InvalidOperationException` is thrown, then caught)
+ a `history.*` node sits nested inside `&&`, `||`, or `!` → gives back
  `false` (see the limit noted in §6)

---

## 6. History Functions (added in Phase 5.8)

The `history.*` family of functions reads from `Snapshot.history.entries`.
To turn this on, pass a `History` object as the third argument to
`Evaluator.Evaluate(condition, state, history)`.

> **Entries recorded on their own, against entries recorded by hand.** The
> running game only records **one** kind of entry on its own:
> `kind="rule_fire"` (added by `Store.DispatchTrigger` each time a rule
> with `once=true` fires; its `target_id` is the rule's own id). Every
> other kind — `node_enter`, `node_exit`, `node_fail`, or any name you
> make up — must be recorded **by hand**, through a `record_event` command
> in a Rule. If a scenario calls `history.count(kind=node_enter, ...)` with
> no matching `record_event` rule that writes a `kind=node_enter` entry,
> the count stays at `0` forever.
>
> **A limit — only works at the top level.** A `history.*` call works when
> it stands alone, or sits right inside a comparison (such as
> `history.count(...) >= 3`). It **does not** work correctly when nested
> inside `&&`, `||`, or `!`: the part of the evaluator that handles this
> case falls back to a path that only reads plain state, which throws an
> `InvalidOperationException` for any history node — this is caught, and
> `false` is given back instead. To check more than one history condition
> at once, write them as separate rules with their own, distinct
> triggers, or work the result out ahead of time into a plain `flag`,
> through an earlier rule.

### 6.1 history.count

**Shape**: `history.count(kind=..., target_id=...) → a whole number`

**What it does**: gives back how many entries in `History.entries` match
the given `kind` (and, if given, `target_id`).

**Examples**:

```text
history.count(kind=node_fail, target_id=stage_01) >= 5
history.count(kind=rule_fire) > 100
```

### 6.2 history.has

**Shape**: `history.has(kind=..., target_id=...) → a bool`

**What it does**: gives back true if even one matching entry exists.

**Examples**:

```text
history.has(kind=rule_fire, target_id=secret_rule)
history.has(kind=node_enter, target_id=optional_dungeon)
```

### 6.3 history.last

**Shape**:

+ `history.last(kind=...).target_id → a string`
+ `history.last(kind=...).timestamp → a floating-point number`

**What it does**: gives back the `target_id` or the `timestamp` of the
most recent matching entry.

**Examples**:

```text
history.last(kind=rule_fire).timestamp > 100.0
```

> Comparing this against a string in quotes (such as
> `.target_id == "shop"`) is **not supported**: `ExprLexer` has no token
> for a quoted string.

### 6.4 history.time_since

**Shape**: `history.time_since(kind=..., target_id=...) → a
floating-point number`

**What it does**: gives back the timestamp of the most recent matching
entry (in seconds, counted from when the session began).

**Examples**:

```text
history.time_since(kind=node_enter, target_id=shop) >= 100
```

> A pattern such as `now() - history.time_since(...)` needs `now()`, which
> is **not yet built**. See §6.7.

### 6.5 history.session_count

**Shape**: `history.session_count() → a whole number`

**What it does**: gives back the number of sessions (times the game has
been started). By custom, this reads `state.counters["_session_count"]`.
**The running game does not raise this counter on its own** — the game's
own code must raise it by hand (the Evaluator gives back `0` if this
counter was never set).

**Examples**:

```text
history.session_count() >= 2
```

### 6.6 history.total_play_time

**Shape**: `history.total_play_time() → a floating-point number`

**What it does**: gives back the total time played, added up in seconds.
By custom, this reads `state.counters["_total_play_time"]`. **The running
game does not track this counter on its own** — the game's own code must
update it by hand (the Evaluator gives back `0.0` if this counter was
never set).

**Examples**:

```text
history.total_play_time() > 3600
```

### 6.7 The now() function

> **Not yet built.** `now()` is set out in this spec, but no matching AST
> node or Evaluator support for it exists in the code as it stands now.

**Shape**: `now() → a floating-point number`

**What it does**: would give the time passed, in seconds, since the
Snapshot's own session began.

Would be used together with `history.time_since` for checks based on
elapsed time (this is planned, not built).

**Examples** *(these cannot be run yet, since `now()` is not built)*:

```text
now() - history.time_since(kind=node_enter, target_id=shop) > 1800
```

### 6.8 Common patterns using history.*

| Pattern | Condition string | Cookbook reference |
| --- | --- | --- |
| a rescue, based on how many times something failed | `history.count(kind=node_fail, target_id=stage_01) >= 5` | Pattern 7.1 |
| an NPC reacts based on time passed | `history.time_since(kind=node_enter, target_id=shop) >= 100` *(see §6.7 — `now()` is not yet built)* | Pattern 7.2 |
| finding a "New Game+" | `history.session_count() >= 2` | Pattern 7.3 |
| branching based on the path taken so far | `history.has(kind=node_enter, target_id=optional_dungeon)` | Pattern 7.4 |
| a hidden unlock, tied to a condition | `history.has(kind=rule_fire, target_id=hint_rule)` | Pattern 7.5 |

---

## 7. AST Node Types

The AST nodes are set out in `ExprAst.cs`:

### 7.1 Nodes already there (from Phase 4)

+ `AndNode` (`&&`)
+ `OrNode` (`||`)
+ `NotNode` (`!`)
+ `ComparisonNode` (`==`, `!=`, `<`, `<=`, `>`, `>=`) — its left side must
  be an `AccessorNode`
+ `GenericComparisonNode` — used when the left side of a comparison is a
  `history.*` node (such as `history.count(...) >= 3`)
+ `AccessorNode` (`flags.x`, `counters.y`, `inventory.z`) — the
  `persistence` prefix is not handled when this is checked
+ `LiteralNode` (a number, or a bool) — a string in quotes is not
  supported

### 7.2 History nodes (added in Phase 5.8)

+ `HistoryCountNode`
+ `HistoryHasNode`
+ `HistoryLastNode` (supports reading a property off it)
+ `HistoryTimeSinceNode`
+ `HistorySessionCountNode`
+ `HistoryTotalPlayTimeNode`

### 7.3 A "now" node (planned, not yet built)

> **Not present in the code.** Even though `now_expr` has a place set
> aside for it in the EBNF in §2, and §6.7 writes up `now()`, no
> `NowNode` class exists in `ExprAst.cs`. Trying to use `now()` in a
> condition gives an `ExprParseException` ("Unknown history function")
> — the parser simply does not know this word yet.

+ `NowNode` *(the name is set aside for it; the class is not built)*

---

## 8. Files

| File | Namespace | Role |
| --- | --- | --- |
| `Scripts/Core/ExprLexer.cs` | `Germio.Core` | breaks text into tokens |
| `Scripts/Core/ExprParser.cs` | `Germio.Core` | builds a parse tree, following the EBNF, top-down |
| `Scripts/Core/ExprAst.cs` | `Germio.Core` | the tree of AST node types |
| `Scripts/Core/Evaluator.cs` | `Germio.Core` | checks the AST (given the state, and the history) |
| `Scripts/Core/Validator.cs` | `Germio.Core` | checks the structure and the meaning (V000-V027) |

---

## 9. The G4 Rule: a Margin of Error for Numbers

Comparing two floating-point numbers uses a margin of error:

```text
equal(a, b) := abs(a - b) <= eps x max(abs(a), abs(b), 1.0)
where eps = 1e-6
```

**Special cases**:

+ `NaN == NaN` → `false` (this follows the IEEE 754 standard's own rule)
+ `+Infinity == +Infinity` → `true`
+ `0.0 == 0.0` → `true` (the smallest allowed size is clipped to 1.0)

---

## See Also

+ `docs/dsl_cookbook.md` — 32 patterns, tried and checked with an LLM,
  covering nearly every common way a game's flow is built (Section 7 uses
  `history.*`)
+ `docs/save_data_spec.md` — the JSON shape of Snapshot and History
+ `docs/llm_design_spec.md` — G21 (History as a First-Class Idea)
