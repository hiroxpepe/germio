# Change Log

## [Next]

### New

+ `WorldNames` (`Scripts/Core/WorldNames.cs`), a table of the kind and id of each thing in the world, built once at scene load, with 5 tests.
+ `Sight` (`Scripts/Sight.cs`), a plain data holder for how far, and how wide, one character sees.
+ `V037`, a check on each Need name, that runs only where the names each persona holds are handed in from outside.
+ A function that reads a whole `germio.json` string, turns it into a `Scenario`, and runs `Validate` on it, with no Unity call at all.
+ `ShaderSetUp` (`Scripts/Editor/ShaderSetUp.cs`), which keeps every shader under `Shaders/` in the list of shaders always included.

### Changed

+ `ShaderRegistrar` is now `ShaderSetUp`. The package type now includes runtime shader assets, and the `.meta` files of the whole package were made new at one time.

## [0.1.0] - 2026-08-23

### Added

+ `package.json`, `Scripts/Germio.asmdef`, `Scripts/Editor/Germio.Editor.asmdef` — the files a Unity Package needs.
+ `actor` on `Rule`, `request_deed` and `update_need` on `Command`, and four questions a deed puts to its own past on `Target` (`not_in_memory`, `not_given_to`, `keep_from`, `new_again_after`).
+ `TargetMark`, for putting a found id in place of the `$target` mark.
+ `SpeechSize`, the sums behind a line spoken over a character's head.
+ `Tests~/ModelTests` and `Tests~/CoreTests` — this build now checks itself, with 744 tests, rather than leaning on a game to do it.

### Fixed

+ `AndNode`, `OrNode` and `NotNode` held their own sides private, so a `history.*` call inside `&&`, `||` or `!` always gave back `false`. Both sides are now held open, and history is read at any depth.
