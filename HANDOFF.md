# HANDOFF

> New chat starts here. Read this file first, before anything else.

## Where things stand

The `germio.json` editor tool (`Editor/`, a plain browser page, no
Unity needed) is built and working, checked in a real browser on a
real machine. `Command.request_notify` (a free word, one-moment
notify signal) is built, tested, and shipped, fixing the level-clear
words timing bug in `stemic` and `flugi`. Every event across the
whole code now ends in a plain past word form, checked by a new
`ConventionRules.cs` rule.

## Next move

See `TASKLIST.md` for the full list. The biggest open items right now
are the Scene wiring checker and moving germio from a git submodule
into a Unity Package.
