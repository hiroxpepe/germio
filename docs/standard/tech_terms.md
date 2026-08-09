# Technical Terms

> The one place where the technical terms are given their sense.
> The writing standard turns off the simple-word rule only for words in this
> list. If a term is not here, do not use it in a document — add it here first.
> The sense of each term is given in simple words, so a reader whose first
> language is not English, and an agent too, can take it in.

Each entry has a short sense, and where it helps, a note on how the term is
used in real work. This list is tuned to this repository: it keeps the terms
that any repository needs, and adds the terms of level construction work.

---

## Version control (Git)

**repository** — The place where all the files of a project are kept, together
with the full record of their changes over time. Often said in the short form
"repo".

**commit** — To put a set of changes into the record of the repository, as one
step with a note on what changed. Also the name of that one saved step.

**push** — To send the commits made on your own machine up to the shared
repository, so others (and agents) get them too.

**pull** — To get the newest commits from the shared repository down to your
own machine.

**branch** — A line of commits that goes its own way, apart from the main line,
so work can go on without touching the main line. The main line is often named
`master` or `main`.

**PAT** — Short for "personal access token". A secret key that lets a tool act
on a repository in your name, without a password each time.
*In use: keep it out of the repository; never commit it.*

---

## Language models and agents

**LLM** — Short for "large language model". A model that takes in words and
gives back words. It is not sure or fixed: the same input may give a different
output. For this reason its work is kept to judgment, not to sure steps.

**agent** — An LLM set up to do work on its own: it reads, it makes a choice,
it takes an act (such as running a tool), and it goes on by steps toward an
end.

**prompt** — The words given to an LLM to set it to work.

---

## Programs and interfaces

**API** — Short for "application programming interface". A fixed way for one
program to ask another program to do something or give data.

**JSON** — A plain-text way to write down data as names and values. This tool
turns a scene into JSON, and JSON back into a scene.

**Unity** — A program for making games in 3D. This tool is an add-on that runs
inside its editor.

**C#** — The language this tool is written in.

---

## Level construction

**scene** — The set of all the things placed in one level of a game: the
blocks, their spots, and their turns. Unity keeps a level as a scene.

**block** — One piece placed in a level, such as a wall or a floor tile. A
level is built from many blocks.

**prefab** — A ready-made block kept as a template. You place copies of a
prefab in a scene, so every wall of one kind is the same.

**grid** — The set of fixed spots a block can sit on, spaced by a set step.
Blocks snap to the grid, so they line up clean.

**serialize** — To turn a scene into JSON: to write down every block, its spot,
and its turn, so the whole level is kept as text.

**import** — To read a JSON file and build the scene from it, placing each
block back where the JSON says.

**export** — To take the current scene and write it out as JSON.

**round-trip** — To export a scene to JSON and import it back, and get the same
scene. A clean round-trip proves no data was lost.

---

**mock** — A stand-in object used in a test in place of a real one, so the test
can drive the code under known, made-up conditions.

**node** — A single point in a graph or tree, joined to others by edges.

**region** — A named area of a larger space, marked off for its own handling.

## Encryption

**encrypt** — To turn readable data into a form that cannot be read without
a key. To **decrypt** is to turn it back.

**key** — A secret piece of data, needed to encrypt or decrypt. Without it,
encrypted data cannot be read.

**AES** — A common, well-tested way to encrypt data. AES-256 means the key
is 256 bits long.

**IV** (initialization vector) — A piece of data, not secret, mixed in with
the key each time data is encrypted, so that encrypting the same data twice
gives two different results.

**CBC** — One way of using AES on data longer than one block. Does not, on
its own, tell you if the encrypted data has been changed by someone else.

**GCM** — Another way of using AES, newer than CBC, that also tells you if
the encrypted data has been changed by someone else (this is called
"authenticated encryption").

**padding** — Extra bytes added to data before encrypting it, so its length
comes out even, in full blocks. **PKCS7** is one common way to pick these
extra bytes.

**hash** — A short, fixed-length code made from data, such that even a tiny
change to the data gives a very different hash. Used to check data has not
changed, without needing to keep the whole data around.

**HMAC** — A hash made using a secret key, so only someone with the key
could have made it. Used to check both that data is unchanged, and that it
truly came from whoever holds the key.

**salt** — Extra, non-secret data mixed in before hashing a password, so
the same password does not always give the same hash.

**Base64** — A way to write raw bytes as plain, printable text.

## Germio scene generation

**Node** — In a `germio.json` file, one entry in the tree under `root`. It
holds an `id`, a `name`, a `kind`, a `scene` value, a list of `children`, a
list of `next` targets, and a list of `rules`.

**leaf Node** — A Node with no `children` and a non-empty `scene` value. It
stands for one Unity Scene.

**Scene class** — In this document, a C# class under
`Assets/Scripts/Scenes/`, not a Unity Scene file. When the two could be
mixed up, the text says "Unity Scene" or "Scene class" in full.

**handler** — A method in a Scene class marked with the attribute
`[GermioSceneHandler(id: "...")]`. When its `id` matches the current Node,
Germio calls it.

**Generator** — The tool this document is about (class name
`SceneCodeSyncer`). It reads `germio.json` and makes the Scene class files
match it.

**orphan** — A Scene class whose handler `id` no longer matches any Node in
`germio.json`.

**idempotent** — Said of a tool that gives the same result no matter how
many times it is run on the same input. A core need for the Generator.

**GUID** — A long, near-unique code Unity puts in each `.meta` file. It is
how Unity tracks a file's identity even after the file is renamed or moved.

**PascalCase** — A way to write a name with no spaces, where each word
starts with a capital letter, e.g. `LevelOne`.

+ One term, one sense. Give the sense in one place only — here.
+ Keep the sense in simple words, by the writing standard.
+ Add a term **before** it is first used in any document.
+ When a term is no longer used anywhere, it may be taken out.
