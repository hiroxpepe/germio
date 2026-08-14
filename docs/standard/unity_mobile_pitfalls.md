# What to Keep Clear of, in a Unity Phone or Tablet Game

> Every line in this list is checked against Unity's own manual,
> or against more than one, separate, outside source. This is not
> a guess, or a hand-me-down rule — each line holds a source.

## Why this matters more, on a phone or tablet

Unity's own garbage collector is not "generational" — it cannot
sweep out small, frequent, temporary room made, on the heap, in a
smart, quick way; each true collection walks the whole, live set.
Unity's own manual states the true goal plainly: keep the room
made, on the heap, each frame, as close to zero bytes as it can be
made. On a phone or tablet, this matters even more, since a lower-
end chip both makes room, and cleans it up, at a slower speed than
a desktop machine.

## The list, each line with its own source

| What to keep clear of | Why | Source |
| --- | --- | --- |
| `LINQ`, on any path run every frame, or every note, or every tick | Most `LINQ` methods make fresh room, on the heap, each call (one true count found 29 of them making 32 to 88 bytes each); `LINQ` also leans on a closure, itself a fresh, held object | Unity's own manual, in full words, tells a writer to keep clear of `LINQ` in running code, most of all inside `Update`, `FixedUpdate`, and other paths run every frame |
| boxing (turning a plain value, such as an `int`, into an object) | C#'s own tools give no warning at all, when this happens, though it still makes fresh room, on the heap, each time | Unity's own manual names this "one of the most common sources of a room made, on the heap, with no one meaning to" |
| joining strings together (`+`, or reading a value into a string), inside a path run every frame | Each join makes a whole, new string, thrown away right after | Unity's own manual, and more than one, outside source, name this by name, as a true, common cause |
| a closure, or a method passed as a value, inside a path run every frame | A method, passed this way, is itself a reference type in C#, so it too makes fresh room, on the heap, each time it is built | Unity's own manual states this holds true, whether the method is named, or written right there, on the spot |
| calling `GetComponent` inside `Update` (or any path run every frame) | The true cost is not room made on the heap, but true, wasted CPU time, spent searching, again and again, for the same answer | Unity's own manual, and more than one, outside checklist, both call for this to be called once (in `Awake` or `Start`), and the answer held, from then on |
| a fresh `WaitForSeconds`, or a fresh yield value, inside a loop | Each fresh one makes fresh room, on the heap; held, and reused, it does not | More than one, outside source, names this by name |
| `Instantiate` and `Destroy`, called again and again, for a short-lived thing (a bullet, an effect) | Each call carries its own, true cost, apart from any room made on the heap at all; a pool of things, made once, and turned on and off, avoids this cost, in full | Named, by more than one, outside source, as the single most telling fix, for this one problem |
| reflection, on any path run often | Slow, in itself, apart from any room made on the heap | Unity's own manual names this too, as its own kind of cost |
