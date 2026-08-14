# Word Forms

> How the word lists hold a base word with its forms. The convention check reads
> a name one word part at a time, so every form a part may take must be a known
> word. This tells how those forms are found, so the lists can be built the same
> way every time. It is written in the project writing standard, so a reader
> whose first language is not English can follow it.

---

## The one rule that bounds all of it

A form is kept only when it can be written as an identifier. An identifier is one
run of letters and digits, with no apostrophe and no space. So these forms are
made:

+ a noun plural — `tabs`, `boxes`, `children`
+ a verb's third person, past, past participle, and present participle —
  `runs`, `ran`, `run`, `running`
+ a short adjective's comparative and superlative — `older`, `oldest`
+ an adverb built from an adjective with `ly` — `quickly`, `simply`
+ a pronoun's object and possessive — `me`, `my`, `him`, `his`
+ a word built across parts of speech — `careful`, `carefully`, `action`

And these forms are never made, because a name can not hold them:

+ the possessive `'s` — an apostrophe is not an identifier letter
+ a contraction like `don't` or `it's` — same reason
+ a `more` or `most` comparative — that is two words with a space

---

## How a base word is read

A word may be more than one part of speech at once. `light` is a noun, a verb,
and an adjective. So each part of speech it holds is taken, and all of their
forms are put on one line together. A form is added once; a shape shared by two
parts of speech is not repeated.

The part of speech comes from a word sense list. A word the list does not know —
a made-up project word like `notifier`, or a function word like `and` — gets no
generated form. It stays as the base alone, and any real form it needs is added
by hand.

---

## The forms by part of speech

**Noun.** Add the plural.

+ add `s` by default — `tab` to `tabs`
+ add `es` after `s`, `sh`, `ch`, `x`, `z` — `box` to `boxes`
+ turn `y` after a consonant into `ies` — `entry` to `entries`
+ an irregular plural comes from the table below — `child` to `children`

**Verb.** Add the third person, the past, the past participle, and the present
participle. The past and past participle are the same for a regular verb, so it
gives three written forms.

+ third person like the plural — `run` to `runs`, `pass` to `passes`
+ past and past participle add `ed` — `call` to `called`
+ drop a final `e` before `ed` — `close` to `closed`
+ turn `y` after a consonant into `ied` — `carry` to `carried`
+ present participle adds `ing` — `call` to `calling`
+ drop a final `e` before `ing` — `close` to `closing`
+ turn a final `ie` into `ying` — `tie` to `tying`
+ a short verb ending consonant-vowel-consonant doubles the last letter —
  `stop` to `stopped` and `stopping`
+ an irregular verb comes from the table below — `send` to `sent`

**Adjective.** Add the comparative and the superlative, but only for a short
adjective of one or two syllables. A longer adjective takes `more` and `most`,
which are not made, so it stays as the base alone.

+ add `er` and `est` — `old` to `older` and `oldest`
+ drop a final `e` — `large` to `larger` and `largest`
+ turn `y` after a consonant into `ier` and `iest` — `happy` to `happier`
+ a short one doubles the last letter — `big` to `bigger` and `biggest`
+ an irregular one comes from the table below — `good` to `better` and `best`

**Adverb.** An adverb made from an adjective with `ly` is its own word, so it is
made from the adjective, not left out — `quick` gives `quickly`, `slow` gives
`slowly`. The spelling shifts: `y` after a consonant turns to `ily` (`happy` to
`happily`), a final `le` turns to `ly` (`simple` to `simply`), `ic` takes
`ally` (`basic` to `basically`), a final `ll` adds only `y` (`full` to `fully`).
A comparative adverb takes `more` and `most`, which are not made. An adverb that
is not built from an adjective — `soon`, `now` — stays as the base alone.

**Pronoun.** A pronoun changes by case, and the object and possessive forms are
identifiers, so they are made — `i` gives `me` and `my`, `he` gives `him` and
`his`, `we` gives `us` and `our`, `they` gives `them` and `their`, `you` gives
`your`. The possessive with `'s` is never made.

**Auxiliary and be.** These change by tense in fixed, irregular ways, and the
forms are identifiers, so they are made from the table — `be` gives
`am is are was were been being`, `have` gives `has had having`, `do` gives
`does did done doing`, `can` gives `could`, `will` gives `would`, `shall` gives
`should`, `may` gives `might`. `must` has no other form.

**Function word and project word.** A true function word with no case or tense
change — `the`, `of`, `and`, `if` — makes no form and stays alone. A project word
the model does not know as English also stays alone.

---

## Derived forms across parts of speech

Beyond the forms of one word, English builds one part of speech from another, and
each built word is its own identifier a name may use. These are not made by a
blind rule — which suffix fits is decided by the model per word, the way a reader
knows `care` gives `careful` but not `careous`. The chain often runs noun to
adjective to adverb.

+ noun to adjective — `care` to `careful`, `nature` to `natural`, `type` to
  `typical`, `option` to `optional`, `danger` to `dangerous`, `use` to `useful`
  and `useless`
+ adjective to adverb — `careful` to `carefully`, `natural` to `naturally`,
  `typical` to `typically`, `optional` to `optionally`, `recursive` to
  `recursively`
+ verb to noun — `act` to `action`, `move` to `movement`, `read` to `reader`,
  `run` to `runner`, `serialize` to `serializer`
+ verb to adjective — `read` to `readable`, `run` to `running`, `depend` to
  `dependent`
+ adjective to noun — `dark` to `darkness`, `weak` to `weakness`, `able` to
  `ability`

A derived word is written on the line of the base it grows from when it reads as
a form of that base — `careful carefully` join `care`. When a derived word is a
distinct code word in its own right — `serializer`, `optional` — it may instead
stand as its own base line. The model chooses whichever keeps the list clear, and
never invents a suffix a word does not truly take.

---

## The irregular table

Regular rules can not reach these, so they are listed by hand and override the
rule.

+ verbs — `go` to `went` and `gone`, `make` to `made`, `take` to `took` and
  `taken`, `see` to `saw` and `seen`, `give` to `gave` and `given`, `come` to
  `came`, `get` to `got` and `gotten`, `keep` to `kept`, `say` to `said`,
  `send` to `sent`, `build` to `built`, `find` to `found`, `hold` to `held`,
  `catch` to `caught`
+ nouns — `child` to `children`, `man` to `men`, `foot` to `feet`
+ adjectives — `good` to `better` and `best`, `bad` to `worse` and `worst`

---

## The edge cases to watch

+ a word with two parts of speech — take all of them, join on one line, no
  repeat
+ a long adjective — no `er` or `est`, so `beautiful` stays alone, not
  `beautifuler`
+ a function word the sense list still marks as a noun or verb — `up`, `be`,
  `a`; hold these to the base alone by the function word list
+ an uncountable noun — `information` has no natural plural; the machine may
  still form one, and that is left for a later pass
+ a project word the sense list does not know — `notifier`, `ringtone`; base
  alone, real forms added by hand

---

## How the list is built

+ start from the base words, one per line
+ read each base word's parts of speech
+ make the regular forms for each part of speech
+ override with the irregular table where it applies
+ drop every form that is not an identifier
+ check that no base word is lost, then write each base with its forms on one
  line

---

## Asking the LLM to do it

This file is the prompt. There is no generator program. The forms are worked out
by a language model reading these rules, once at build time, and written into the
list. The check never calls a model; it only reads the finished list. So the
model runs offline, its output is frozen into the file, and the same rules give
the same result the next time, in this project or in another.

Two-step build:

+ first, a rough machine pass adds regular endings to every base, fast and
  dumb, to lay down a draft
+ then the model reads this file and fixes the draft: it drops a form the rules
  forbid, adds an irregular form, and settles each word by its real part of
  speech

The model is the judge. A word's part of speech, its syllable count for the
adjective rule, whether a noun is countable — these are read by the model from
the word itself, not from a table it must be handed. A made-up project word the
model does not know as English stays as the base alone.

**What the model is given.** A list of base words, one per line, each as
`+ base` — or a rough draft where some lines already carry machine forms.

**What the model returns.** The same lines, each as `+ base form form ...`, with
the base first and its identifier forms after it, space-joined, in the same order
as the input, none lost.

**Worked examples:**

+ `+ light` → `+ light lights lighted lighting lighter lightest` — noun, verb,
  and adjective at once, so plural, verb forms, and short-adjective forms all
  join on one line
+ `+ beautiful` → `+ beautiful` — a long adjective, so no `er` or `est`, and it
  is not a noun or verb, so it stays alone
+ `+ child` → `+ child children` — an irregular plural from the table
+ `+ go` → `+ go goes went gone going` — an irregular verb from the table
+ `+ close` → `+ close closes closed closing closer closest` — verb and
  adjective; the final `e` is dropped before `ed`, `ing`, `er`, `est`
+ `+ information` → `+ information` — uncountable, so no plural, even though the
  machine draft may have added one
+ `+ notifier` → `+ notifier notifiers` — not known as English, but it reads as
  a plain agent noun, so only the safe plural is added; if unsure, base alone

---

## Open work

The rule here is ahead of the lists. The lists were first built by a rough
machine pass and then mended by the model, but only for the forms the machine
knew — plurals, verb tenses, and short comparatives. The newer parts of this rule
are written but not yet carried into `basic_words.md` and `plain_words.md`. So
the following is still to do, in this rough order:

+ **Adverbs in `ly`** — the lists hold almost no `ly` adverb. Words a name may
  well use — `quickly`, `slowly`, `simply`, `basically`, `recursively`,
  `carefully`, `naturally`, `optionally` — are missing. Walk the adjectives, add
  the `ly` form where it is real, with the spelling shifts in the Adverb rule.
+ **Derived forms across parts of speech** — the noun-to-adjective-to-adverb
  chains and the verb-to-noun forms are not in the lists yet. `care` has no
  `careful`; `nature` has no `natural`; `act` has `action` only by chance. Decide
  per word whether the derived word joins the base line or stands on its own, and
  add the ones a name would really use.
+ **Pronoun cases** — `i me my`, `he him his`, `you your` are in `basic_words`
  now, but `we us our`, `they them their`, `she her` should be checked and
  completed.
+ **A second irregular sweep** — the machine still regularises some irregulars on
  first pass. Each rebuild must be re-checked against the irregular table for
  verbs like `ring`, `spring`, `wind`, `bind`, `wake`, and for any newly added
  base. Do not trust the machine draft here.
+ **Uncountable nouns and absolute adjectives** — the machine still adds a plural
  to `information` or a comparative to `main` or `unique`. These are trimmed by
  hand for now; a marked list of such words would let the rule skip them.

Until this is done, treat a missing `ly` adverb or derived form as a gap to fill,
not as a sign the word is wrong. When you fill a gap, fix the rule first if the
machine got it wrong, then the lists, so the two stay in step.

---

## How to keep this

+ One base word, one line. The base leads; its forms follow, space-joined.
+ A form must be an identifier. No apostrophe, no space.
+ When the machine makes a wrong form, fix it here in the rule first, then
  rebuild, so the fix holds next time too.
+ To add or mend a word later, ask the LLM in chat with this file as the rule.
  No program is needed; the file and the model are the tool.
+ The rule leads the lists. When the two disagree, the rule is right and the
  lists are behind; see Open work for what is not yet carried over.
