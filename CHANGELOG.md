# Change Log

## [0.1.0] - 2026-08-23

### Added

+ `package.json`, `Scripts/Germio.asmdef`, `Scripts/Editor/Germio.Editor.asmdef` — the files a Unity Package needs.
+ `actor` on `Rule`, `request_deed` and `update_need` on `Command`, and four questions a deed puts to its own past on `Target` (`not_in_memory`, `not_given_to`, `keep_from`, `new_again_after`).
+ `TargetMark`, for putting a found id in place of the `$target` mark.
+ `SpeechSize`, the sums behind a line spoken over a character's head.
+ `Tests~/ModelTests` and `Tests~/CoreTests` — this build now checks itself, with 744 tests, rather than leaning on a game to do it.

### Fixed

+ `AndNode`, `OrNode` and `NotNode` held their own sides private, so a `history.*` call inside `&&`, `||` or `!` always gave back `false`. Both sides are now held open, and history is read at any depth.
