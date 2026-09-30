# CLAUDE

> How the agent works in this repository. These are rules for the agent
> (a language model) that helps build this project. They are kept short and
> plain, by the writing standard.

## Set up once, before any other work

+ Run `git config core.hooksPath .githooks` once for each clone. This
  makes `git commit` run the shared checks in `.githooks/pre-commit`
  and `.githooks/commit-msg` on every commit, before the commit is
  let through. **Without this one command, neither hook runs at all
  — a broken commit message, or a markdown file still holding an
  error, would pass straight through, with nothing to stop it.**
  Check it is truly set, with `git config core.hooksPath`, which must
  answer `.githooks`. Running a hook by hand, on a file, is **not**
  proof the hook itself is live; only a true `git commit` proves that.
+ Set the git account to the one in the history of this repository
  (check it with `git log -3 --format="%an <%ae>"`), and do not make
  up another. Set both `user.name` and `user.email`, once for each
  clone.

## Documents

+ Every document follows the writing standard in `docs/standard/`. The
  words are kept simple, so a reader whose first language is not
  English can take in the sense. See `docs/standard/writing_standard.md`.
+ Every hard word used in a document must be in the word list first.
  If a word is not there, add it to `docs/standard/tech_terms.md`
  before you use it.

## Four files, four jobs

+ `CLAUDE.md` (this file) — the rules and the word given: how the
  agent works here, checked every time, not tied to any one act of
  work.
+ `TASKLIST.md` — the full list of open work, with a plan for when.
  A short checkbox line up top for each item, a full write-up below
  it.
+ `ROADMAP.md` — the larger plan, in phases. A short line up top for
  each phase, the detail below it.
+ `HANDOFF.md` — the hand-off to the next chat: where things stand
  right now, and the next move. Kept short; the full list lives in
  `TASKLIST.md`.

## Markdown check

+ Before you commit any markdown file, run the check and get no
  errors at all. Do not commit a markdown file that still has errors.
+ This check is two things in one: a form check that runs on every
  markdown file, and a word check for plain English that runs on
  every markdown file too, except the `_JP.md` files, which are not
  in English. `TASKLIST.md` and `ROADMAP.md` get a check of their own
  form as well. A word failed in the word check goes into `draft_words.md` if it is a plain word
  many people would use, or into `docs/standard/tech_terms.md` if it
  is a real, needed hard word with its own sense given in one line.
  Never put a new word into `basic_words.md`; that file holds only
  Ogden's own 850 words, and nothing else.
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
+ The form is `type: Verb subject`. The verb is Add or Update, and
  nothing else; the hook turns down Delete. The type is one of
  `feat`, `fix`, `refactor`, `docs`, `chore`, `test`, or `style`.
+ Keep the first line between 57 and 61 letters long. Count it with
  a command, never by eye.
+ Do not put square marks or forward lines in the message; keep it
  plain.
+ The hook also turns down the mark `-`, the name of this repository,
  and a list of padding words (`now`, `just`, `own`, `work`, `here`,
  `one`, and more; the list is in `.githooks/commit-msg`). The words
  after the verb must be Basic English.
+ Read the master's own word before every commit and push. A clear
  GO is owed each time.

## History

+ Keep the history one straight line. Do not make a commit that
  joins two lines back into one.
+ If a push is turned down because the copy on the server is ahead,
  put your work back on top of it first, then push. Do not join the
  two lines back into one.
