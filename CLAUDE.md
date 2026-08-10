# CLAUDE

> How the agent works in this repository. These are rules for the agent
> (a language model) that helps build this project. They are kept short and
> plain, by the writing standard.

## Set up once, before any other work

+ Run `git config core.hooksPath .githooks` once per clone. This makes
  `git commit` run the shared checks in `.githooks/pre-commit` on every
  markdown file staged for the commit, before the commit is let through.

## Documents

+ Every document follows the writing standard in `docs/standard/`. The words
  are kept simple, so a reader whose first language is not English can take in
  the sense. See `docs/standard/writing_standard.md`.
+ Every technical term used in a document must be in the term list first. If a
  term is not there, add it to `docs/standard/tech_terms.md` before you use it.

## Three files, three jobs

+ `CLAUDE.md` (this file) — the rules and the promise: how the agent works
  here, checked every time, not tied to any one piece of work.
+ `TASKLIST.md` — the full list of open work, with a schedule. A short
  checkbox line up top for each item, a full write-up below it.
+ `HANDOFF.md` — the hand-off to the next chat: where things stand right
  now, and the next move. Kept short; the full list lives in `TASKLIST.md`.

## Markdown lint

+ Before you commit any markdown file, run the lint check and get zero errors.
  Do not commit a markdown file that still has lint errors.
+ The rules are set in `.markdownlint.json` at the repository root. Use that
  file, not your own idea of the rules.
+ The list marker is the plus sign. Use `+` for every list item, not `-` or
  `*`.

Run the check like this:

```bash
npx --yes markdownlint-cli -c .markdownlint.json <file>
```

## Commits

+ The commit message is one line, with no body.
+ The form is `type: Verb subject`. The verb is one of Add, Update, or Delete.
  The type is one of feat, fix, refactor, docs, chore, or test.
+ Keep the first line between 57 and 60 characters.
+ Do not put brackets or slashes in the message; keep it plain.

## History

+ Keep the history a single straight line. Do not make merge commits.
+ If a push is refused because the remote is ahead, rebase your work on top of
  the remote, then push. Do not merge.
