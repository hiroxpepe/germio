# Developing Standard

> A short list to run through while you work, so the same slips do not
> come back. Read it at the start of a session, and again before every
> commit. It is not a stand-in for `HANDOFF.md` (where things stand).
> It is how to move once you know where you are.

## Before you write any code

+ Read `HANDOFF.md` first, for the part you touch.
+ Do not start to code until there is a clear GO. Check the plan, not
  just the wish.
+ Split the work into phases and tasks of even size before you start.
  Settle the design questions at split time. Do not leave a "which
  way?" to come up mid-task. A task with no real work in it means the
  split was wrong; drop it, do not pad it.

## TDD, one task at a time

+ Write the failing test first. Run it and see it fail (red) for the
  right reason. A build error, since the type is not there yet, is a
  fair red.
+ Write the least code that makes it pass (green).
+ Keep judgement in a pure part of the code; leave any outside layer
  (a screen, an OS call) a thin caller. Do not test that outside
  layer straight; stand in for it with a mock, all the way down.
+ Name what you name in `snake_case`; leave an outside name (a data
  base column, a platform type) as it is.

## Before every commit — run the tests

+ Run the full test set for this repository.
+ All green, the naming-rule gate too. Never commit a change without
  a test run first.

## Be honest about what is checked

+ Say plainly what is checked here, in this sandbox, and what is not
  (a real device run, a full build on another machine). The owner's
  own real run is the last, real check.
+ When a real run confirms a thing, put the fact in `HANDOFF.md`; a
  runtime log itself is not put in as a commit.

## Commit and push

+ Commit per phase, not per tiny step. Stage only the files the
  change touched; never add a build's own output folder (the
  `.gitignore` file covers this; check the status first).
+ Wait for a clear "commit GO". A catch-phrase alone is not commit
  approval.
+ Commit and push are one move. Once the GO is given, push in the
  same step.

## Style reminders that came up

+ No personal names in code, in a comment, in a log, or in output;
  use a role word instead.
+ Every script that touches data has a `--dry-run`; start from a dry
  run, never straight to a real one.
+ Answer plainly and short; do not argue a point again once it is
  made.
