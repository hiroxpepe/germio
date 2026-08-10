# CLAUDE

> How the agent works in this repository. These are rules for the agent
> (a language model) that helps build this project. They are kept short and
> plain, by the writing standard.

## Set up once, before any other work

+ Run `git config core.hooksPath .githooks` once for each clone. This
  makes `git commit` run the shared checks in `.githooks/pre-commit`
  on every markdown file staged for the commit, before the commit is
  let through.

## Documents

+ Every document follows the writing standard in `docs/standard/`. The
  words are kept simple, so a reader whose first language is not
  English can take in the sense. See `docs/standard/writing_standard.md`.
+ Every hard word used in a document must be in the word list first.
  If a word is not there, add it to `docs/standard/tech_terms.md`
  before you use it.

## Three files, three jobs

+ `CLAUDE.md` (this file) — the rules and the word given: how the
  agent works here, checked every time, not tied to any one act of
  work.
+ `TASKLIST.md` — the full list of open work, with a plan for when.
  A short checkbox line up top for each item, a full write-up below
  it.
+ `HANDOFF.md` — the hand-off to the next chat: where things stand
  right now, and the next move. Kept short; the full list lives in
  `TASKLIST.md`.

## Markdown check

+ Before you commit any markdown file, run the check and get no
  errors at all. Do not commit a markdown file that still has errors.
+ The rules are set in `.markdownlint.json` at the root of the
  repository. Use that file, not your own idea of the rules.
+ The list mark is the plus sign. Use `+` for every list line, not
  `-` or `*`.

Run the check like this:

```bash
npx --yes markdownlint-cli -c .markdownlint.json <file>
```

## Commits

+ The commit message is one line, with no body under it.
+ The form is `type: Verb subject`. The verb is one of Add, Update,
  or Delete. The type is one of `feat`, `fix`, `refactor`, `docs`,
  `chore`, or `test`.
+ Keep the first line between 57 and 60 letters long.
+ Do not put square marks or forward lines in the message; keep it
  plain.

## History

+ Keep the history one straight line. Do not make a commit that
  joins two lines back into one.
+ If a push is turned down because the copy on the server is ahead,
  put your work back on top of it first, then push. Do not join the
  two lines back into one.
