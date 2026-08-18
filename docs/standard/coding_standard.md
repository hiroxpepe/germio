# Coding Standard

> How code names are spelled in this project and in the other projects too, in
> C# and in JavaScript. This is a shared rule. It is written in the project
> writing standard, so a reader whose first language is not English can follow
> it. The convention tests read the code and check these rules by machine.

---

## The goal

One person reads all of this code — C# in one repository, JavaScript in the
next. The goal is to read them the same way, with the eye not thrown off by the
language. So the names follow a small set of rules that hold across languages,
and bend to a language only where the language truly forces it.

---

## The two ideas under every rule

**1. Follow the standard of what the name faces.**
A name that faces the outside follows the standard of the thing it meets. A
public method faces other code, so it takes that language's public form. A JSON
property faces a JSON file, so it takes the JSON form (snake_case). A name that
faces only inward — a local, a private helper — is for the reader alone, so it
takes the form that reads easiest.

**2. Read for a person, not for an authority.**
Where a language body and a reader disagree, the reader wins. Microsoft writes
`Json` and `Http`; print writes `JSON` and `HTTP`; a person reads the all-caps
form faster, so that is the one used. No name is chosen because a big name
chose it — not Microsoft, not Unity. The test is always: does it read clearly.

---

## Print form

The base rule for the letters of a name: spell a word the way a printed page
spells it — a technical magazine, a data sheet, the label on a piece of gear.

+ **Short forms are written in full.** `Message`, not `Msg`. `Button`, not
  `Btn`. `Config`, not `Cfg`. `Index`, not `Idx`. `Parameter`, not `Param`.
+ **Letter words are all caps.** Print writes "ID card" and "user ID", so code
  writes `ID`, not `Id`. This holds for `API`, `URL`, `JSON`, `HTTP`, `CPU`,
  `CSV`, `DOM`, `HTML`, `CSS`, and the like — two letters or more. This is the
  point where the rule parts from the Microsoft rule, on purpose: a reader
  sees the letter word stand out.
+ **Unit marks keep their print form.** Print writes "440 Hz", not "440 HZ", so
  code writes `Hz`, not `HZ`. Same for `kHz`, `dB`, `ms`. A unit mark is not a
  letter word; it has one fixed print form the whole world shares.

Print form holds in every language, for every kind of name.

---

## Case shape by language

The case shape (PascalCase, camelCase, snake_case, UPPER_SNAKE) follows idea 1:
names facing out take the standard of what they face; names held inside read easiest.

### C\#

| Name kind         | Case shape    | Faces       |
| ----------------- | ------------- | ----------- |
| type / class      | `PascalCase`  | code (.NET) |
| public method     | `PascalCase`  | code (.NET) |
| public property   | `PascalCase`  | code (.NET) |
| JSON property     | `snake_case`  | a JSON file |
| private method    | `camelCase`   | inward      |
| private field     | `_snake_case` | inward      |
| local             | `snake_case`  | inward      |
| constant          | `UPPER_SNAKE` | both        |
| namespace segment | `PascalCase`  | code (.NET) |
| file name         | `PascalCase`  | .NET tools  |

### JavaScript

| Name kind        | Case shape    | Faces          |
| ---------------- | ------------- | -------------- |
| class            | `PascalCase`  | code (JS)      |
| public function  | `snake_case`  | code (JS)      |
| JSON key         | `snake_case`  | a JSON message |
| private function | `snake_case`  | inward         |
| local            | `snake_case`  | inward         |
| constant         | `UPPER_SNAKE` | both           |
| file name        | `snake_case`  | JS tools       |

JavaScript leans on snake_case everywhere. This is not a break with the
language: JavaScript used snake_case in its early days inside a web page, before it
copied Java's camelCase to ride Java's name; and a reader knows snake_case
faster than camelCase. So snake_case is both the older form and the easier one.

---

## What the two tables share

The shapes line up more than they differ. These are the same in both languages:

+ **class / type** — `PascalCase`
+ **local** — `snake_case`
+ **constant** — `UPPER_SNAKE`
+ **JSON key** — `snake_case`
+ **print form** — the same short-form, letter-word, and unit-mark rules

What differs is narrow: the public method (C# `PascalCase`, JS `snake_case`) and
the file name — each following the standard of its own true world of tools, which is idea
1 at work, not a break in the rule.

---

## Outside names are not ours to change

A name that comes from a library — the .NET base library, Unity, an npm package
— is fixed by that library. We do not give it a new look. `transform.position` from
Unity stays `transform.position`, even though it breaks the .NET public rule;
`JsonConvert` from the JSON library stays `JsonConvert`. The rules here apply
only to names we declare. This keeps our code steady next to any outside code,
and it is why the convention tests read only our own declarations.

---

## How the tests use this

The convention tests hold two small word lists that put print form to work:

+ **the full-word list** turns a short form into its full word (`Msg` →
  `Message`).
+ **the all-caps list** turns a letter word into all caps (`Api` → `API`).

Each project adds to these lists the short forms and letter words that show up
in its own code, judged by the print rule. The lists are the only part that
changes from project to project; the rules themselves do not change.

---

## What is checked (decision table)

Every kind of name is checked, in the same way, on three points: its case
shape, short forms (the full-word list), and letter words (the all-caps list).
The table is the full set; no kind of name is left out. The case-shape column
below is for C#; JavaScript follows its own table above, checked on the same
three points.

| Name kind             | Case shape (C\#)                | Short form | Letter word |
| --------------------- | ------------------------------- | ---------- | ----------- |
| const / static field  | `UPPER_SNAKE`                   | yes        | yes         |
| private field         | `_snake_case`                   | yes        | yes         |
| exposed field         | `PascalCase` (JSON: snake_case) | yes        | yes         |
| local                 | `snake_case`                    | yes        | yes         |
| foreach variable      | `snake_case`                    | yes        | yes         |
| parameter             | `snake_case`                    | yes        | yes         |
| method                | exposed `Pascal`, else `camel`  | yes        | yes         |
| property              | exposed `Pascal`, else `camel`  | yes        | yes         |
| JSON property / field | `snake_case` or `PascalCase`    | yes        | yes         |
| enum member           | `PascalCase`                    | yes        | yes         |
| type                  | `PascalCase`                    | yes        | yes         |
| namespace segment     | `PascalCase`                    | yes        | yes         |
| file name             | (print form of its type)        | yes        | yes         |

A "JSON property or field" is an exposed member on a type marked
`[Serializable]`; its name is an external JSON key, so `snake_case` is allowed
there and only there.

The full-word list and the all-caps list at work — the short forms and letter
words this project keeps:

+ **full word:** `Message` not `Msg`, `Button` not `Btn`, `Config` not `Cfg`,
  `Index` not `Idx`, `Parameter` not `Param`, `Initialize` not `Init`,
  `Calculate` not `Calc`.
+ **all caps:** `ID`, `IO`, `UI`, `DB`, `API`, `URL`, `JSON`, `CSV`, `HTTP`,
  `HTML`, `CSS`, `DOM`, `CPU`, `GPU`, `GC`, `CLI`; and for audio work `LFO`,
  `FX`, `PCM`, `FM`, `VA`.

---

## Edge cases

These cases are settled on purpose, and each has a test that holds the line:

| Case                                      | What happens       | Why                                     |
| ----------------------------------------- | ------------------ | --------------------------------------- |
| `override` member                         | not checked        | name is fixed by the base               |
| explicit interface member                 | not checked        | name is fixed by the interface          |
| member inside an interface                | not checked        | the interface sets the name             |
| `extern` method                           | not checked        | name comes from outside                 |
| exposed member on `[Serializable]`        | snake_case allowed | it is a JSON key                        |
| event (field form and property form)      | checked as exposed | an event is a member                    |
| a unit mark such as `Hz`                  | left as is         | not a letter word; keeps its print form |
| a call to an outside type (`JsonConvert`) | not checked        | the name is not ours to change          |
| the plural `Ids`                          | left as is         | reads as a word, not the mark `ID`      |

---

## Member order

Every member in a type is checked on four points, in this priority: kind,
then (for fields only) const/static/instance, then access level, then
static before instance. A file that is already green on this order is what
lets the section-header rule below group members by runs that follow, one after another — same
kind, access, and static-ness always sit together, never split apart by
something else in between.

| Rank | Kind             |
| ---- | ---------------- |
| 0    | Field            |
| 1    | Constructor      |
| 2    | Destructor       |
| 3    | Delegate         |
| 4    | Event            |
| 5    | Enum             |
| 6    | Interface        |
| 7    | Property         |
| 8    | Indexer          |
| 9    | Method, Operator |
| 10   | Struct           |
| 11   | Class, Record    |

A field's sub-rank, ahead of the kind table above for that one row: `const`
(0), `static` (1), then instance (2). Access level ranks `public` first,
then the `internal`/`protected` combinations, then `private` last; within
a tie, `static` sits before instance.

**Operator is a standing exception.** It shares rank 9 with Method rather
than holding a rank of its own, so the order check does not require
operators and methods to stay in separate runs — they may mix, one kind among the other. No
repository has an operator today, so the section-header rule below leaves
Operator with no label until one appears and this gets settled for real, rather
than guessing at a shape now.

---

## Section-header comments

A block of members of the same kind, access level, and static-ness opens
with a divider line, a label line, then a blank line before the members
themselves:

```csharp
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private Methods [verb]

        void run() { ... }
```

**The divider line.** Its right edge always lands on column 103, so
sections line up with each other no matter how deep the indent is: the
slash count is `103 - indent`. A divider at 8 spaces of indent is 95
slashes; at 12 spaces, 91 slashes.

**The label line.** One `//` comment, right after the divider, in this
shape:

```text
// [access] [static] Kind [grammar hint]
```

`access` and `static` are left out when the section is private and
instance — the default needs no name, the same way a `private` keyword
is left off the member itself. `Kind` and its grammar hint follow the
table below.

| Kind          | Grammar hint                            | Bare form means                        |
| ------------- | --------------------------------------- | -------------------------------------- |
| Fields        | (none)                                  | private, instance                      |
| Constructor   | (none)                                  | instance                               |
| Destructor    | (none)                                  | — (no access modifier is possible)     |
| Delegate      | (none)                                  | private, instance                      |
| Properties    | `[noun, adjective]`                     | private, instance                      |
| Methods       | `[verb]`                                | *(access always shown, see edge case)* |
| inner Classes | (none)                                  | — (no access/static split observed)    |
| Events        | `[verb]`                                | *(access always shown, like Methods)*  |

**Tense inside an event name.** *Which* participle to use depends on
what the event stands for:

+ **Past participle** — for a single, completed happening (something
  that has already finished by the time the event fires): `Started`,
  `Ended`, `TransitionRequested`, `NotifyRequested`, `Paused`, `Resumed`,
  `LevelStarted`, `HomeEntered`.
+ **Present participle** — for a state that holds true over a span of
  time, not a single instant: `Playbacking`.
+ **`On` is never part of the event's own name.** `On` belongs only on
  the *protected method that raises* the event (`OnClosed()` raises
  `Closed`), never on the public event itself.

| Const         | `[nouns]`             | a literal `const` field                |
| Enums         | `[noun]`              | private, instance                      |
| Interfaces    | (none)                | private, instance                      |
| Indexers      | `[noun, adjective]`   | private, instance                      |

Worked examples, from an all-`private`-by-default class to one with
every modifier in play:

| Members below the label     | Label                                                    |
| --------------------------- | -------------------------------------------------------- |
| private instance fields     | `// Fields`                                              |
| private static fields       | `// static Fields`                                       |
| public instance fields      | `// public Fields`                                       |
| instance constructor        | `// Constructor`                                         |
| static constructor          | `// static Constructor`                                  |
| private instance properties | `// Properties [noun, adjective]`                        |
| public static properties    | `// public static Properties [noun, adjective]`          |
| private instance methods    | `// private Methods [verb]`                              |
| public instance methods     | `// public Methods [verb]`                               |
| public static methods       | `// public static Methods [verb]`                        |
| nested type declarations    | `// inner Classes`                                       |
| public events               | `// public Events [verb]`                                |
| `const` fields              | `// Const [nouns]`                                       |

## Section-header edge cases

| Case                                                      | What happens                                | Why                                                                                                                                                                                                                                                                              |
| --------------------------------------------------------- | ------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Fields / Properties, private + instance                   | access and `static` left off the label      | matches the left-out `private` keyword rule                                                                                                                                                                                                                                      |
| Methods / Events, private + instance                      | `private` is still spelled out on the label | a class most times mixes public and private methods; the label must say which without making the reader open every one                                                                                                                                                           |
| a label is an exact `[access] [static] Kind [hint]` match | made to match that one, true spelling       | close-but-off wording (`Private methods [verb, verb phrase]`, `Static fields`, `Public static methods`) is a spelling drift on a real Kind label, not free-form text                                                                                                             |
| a label does not exactly match that shape                 | left as free-form, with no change made      | the match is strict, not a loose keyword search — `Persona own-field merge` contains the word "field" but is not `Fields`, so it is never forced into that shape; this is what keeps `Menu items`, `GUI`, `Unity EditorWindow lifecycle`, and step-by-step algorithm labels safe |
| divider not landing on column 103                         | flagged                                     | breaks the true line-up across the file                                                                                                                                                                                                                                          |

---

## Using-directive order

Every `using` directive falls into one of three groups, checked in this
order: `System`, then any other outside library, then this project's own
namespace. Within a group, usings are further grouped by their own root
namespace (`UnityEngine`, `UniRx`, `Germio`, and so on); all usings that
share one root stay in one run held whole, with no other root between — once a different root has
begun, an earlier root coming back later is flagged, since that root's
lines were left spread apart instead of kept together.

A `using static` directive is grouped by the same root namespace as a
plain `using` from that root — `using static UnityEngine.GameObject;`
sits with `using UnityEngine;` in the third-party group, and
`using static Germio.Env;` sits with `using Germio;` in this project's
own group. It is not a fourth group of its own. Within one root, the
plain form always sorts before any `using static` form drawn from that
same root: the type itself is the root's main entry, its static members
are a further detail, listed only once the plain form is in.

A `using X = Y;` alias is never checked for grouping: the alias name is
ours to choose, and there is no single outside root to group it by. An
alias stays fixed to whatever directive immediately followed it in
the original file, riding along with that directive if the rest of the
block is put in a new order.

A file that is out of order on any of these points can be fixed in one
paste: the check computes the whole correct order in a single pass and
reports the complete expected block, not just the one line that is out
of place.
