# Germio Security Model

> **Version**: 2.2
> **Base rule**: G6 (Pure Key Management)
> **Meant for**: security reviewers, and anyone working with sensitive game data

---

## 1. Threat Model

### 1.1 What Germio guards against

+ **A player editing the save file**: the AES-CBC encrypted save file
  (`germio.dat`) cannot be usefully edited by hand without knowing the
  AES key.
+ **Reading spoilers or unlocked content by looking at the file directly**:
  the progression tree, flag names, and unlock conditions are all closed
  off inside the encrypted bytes.
+ **Replaying old moves through the history log**: the
  `Snapshot.history.entries` list keeps a record of every rule fired and
  every node entered, and this record is part of the encrypted data too.

### 1.2 What Germio does NOT guard against

The following threats are **out of scope** for Germio. They must be handled
at the level of the platform, or in how the Unity build itself is set up.

| Threat | What to do about it |
| --- | --- |
| reading memory while the game runs | use Unity IL2CPP, plus anti-cheat middleware |
| reverse-engineering the Unity build | use code obfuscation (Obfuscator-LLVM, and the like) |
| a save clashing across more than one device | handle this in your own cloud-save layer |
| pulling the key out of the built game | use the platform's own keystore, or a secure enclave |
| someone reading network traffic | out of scope (Germio runs on the client side only) |

---

## 2. What is used to encrypt

| What | Library | Used where |
| --- | --- | --- |
| AES-256 (CBC mode) | .NET's own `System.Security.Cryptography` | `Vault.cs` and `Storage.cs`, to encrypt the save file |
| Base64 | .NET's own `System.Convert` | writes the key as text, for the `GERMIO_AES_KEY` setting |
| PBKDF2, or any other key-building step | not used | the key is given directly, with no extra step to build it |
| HMAC, or GCM | not used | see §6 for a note on CBC against GCM |

### 2.1 The size of the key

The AES key is **48 bytes** long:

+ bytes 0-31: the AES-256 key itself (32 bytes)
+ bytes 32-47: the IV (16 bytes)

When this is written as Base64 text: `Convert.ToBase64String(key48bytes)`.

---

## 3. Key handling (`Vault.cs`)

### 3.1 The order it looks for a key

`Vault.GetKey()` looks for the key in this order:

1. **The setting** `GERMIO_AES_KEY` (Base64 text, decoding to 48 bytes)

   — checked first. Fits CI/CD, a builder's own machine, and a build made
   to run on a server.

2. **`StreamingAssets/germio_key.bin`** (raw bytes, 48 bytes)

   — Unity's StreamingAssets ship with the built game, so this fits a
   finished, shipped build.

3. **No other fallback** (it throws `InvalidOperationException`)

   — under G6, the old fallback to `PlayerPrefs` is fully taken out in
   v2.2.

### 3.2 What v2.2 took out (G6: taking out PlayerPrefs)

v1.0 and v2.0 both had a third fallback:

```text
PlayerPrefs.GetString("germio_key")  // stored the AES key in plaintext on disk
```

This was a **serious weak point**:

+ on Windows, `PlayerPrefs` is stored in the registry, as plain text
+ any program that can read the registry can pull the key out
+ on Android, `PlayerPrefs` is stored in a SharedPreferences XML file with
  no encryption at all

**G6 (Pure Key Management) takes this fallback out fully.** A missing key
now stops the game with a plain error (`InvalidOperationException`),
rather than quietly falling back to a key that is not safe.

### 3.3 Which key source to use, by target

| Where it runs | Key source to use |
| --- | --- |
| building it, on your own machine | the `GERMIO_AES_KEY` setting, held in `.env` or your shell's own settings |
| the Unity Editor's play mode | the `GERMIO_AES_KEY` setting |
| a finished Windows 11 build | `StreamingAssets/germio_key.bin` (shipped with the build) |
| a finished Android 14 build | `StreamingAssets/germio_key.bin` (inside the APK's assets) |
| a CI/CD pipeline (automated tests) | the `GERMIO_AES_KEY` setting (held as a pipeline secret) |

### 3.4 Changing the key later

Germio has no built-in way to change a key that is already in use. If the
key is thought to be found out by someone else:

1. Make a new 48-byte key.
2. Decrypt every save file already there, with the old key.
3. Encrypt them again, with the new key.
4. Ship the new `germio_key.bin`.

### 3.5 A `.gitignore` rule

`germio_key.bin` **must** be listed in `.gitignore`, and **must never** be
committed. The CI pipeline should check for this on every push.

---

## 4. Save File Layout

### 4.1 Plain-text mode (while building)

`germio.json` — UTF-8 JSON text, with no encryption at all.
`Storage.LoadAsync()` tries to read this JSON first, and only falls back
to the encrypted `germio.dat` if it is not there. Nothing is migrated
between the two (see §4.3).

### 4.2 Encrypted mode (a finished build)

`germio.dat` — bytes encrypted with AES-256-CBC.
How it works: `Storage.cs` uses `System.Security.Cryptography.Aes.Create()`
(CBC mode by default, with PKCS7 padding). The key and IV both come from
`Vault.GetKey()` (a 32-byte key plus a 16-byte IV, taken from one 48-byte
piece of data). The JSON is written as UTF-8 text, then encrypted. The IV
is **not** placed before the encrypted bytes — the loader builds it again
from that same shared 48-byte key data.

### 4.3 Keeping the schema in step, across versions

`Scenario.schema_version` (a number, `1` by default) tracks which version
of the data shape is in use. Right now, there is only one version (`1`),
and no `Migrator` is built. The old `Migrator` class was taken out during
Phase 5.8 v2, since the schema was not yet public. If `schema_version` is
ever raised past `1`, a new Migrator will need to be built again. See
`docs/save_data_spec.md` for why.

---

## 4.4 Snapshot encryption (added in Phase 5.8 v2)

`snapshot_{slot}.dat` files use the same AES-CBC encryption as
`germio.dat`.

The key (`GERMIO_AES_KEY` or `germio_key.bin`) is shared between both
kinds of file.

### 4.4.1 Why the snapshot needs encryption too

+ stops a speedrun record from being edited by hand (faking a timestamp)
+ stops save-data cheating (changing a flag or a counter by hand)
+ stops the history from being edited to skip past an achievement's
  real unlock condition

### 4.4.2 Behavior, by build mode

| Mode | Scenario | Snapshot |
| --- | --- | --- |
| while building (the Editor) | germio.json (plain text) | snapshot_{slot}.json (plain text) |
| a finished build | germio.dat (AES) | snapshot_{slot}.dat (AES) |

### 4.4.3 Keeping slots apart

More than one save slot can exist at once (`snapshot_{slot}.{json,dat}`,
one per number `slot`), each kept apart from the rest. The key is shared
across every slot, but each file still stands on its own.

## 5. What happens when something goes wrong

### 5.1 No key found

`Vault.GetKey()` throws `InvalidOperationException("No AES key source
found")`.
`Storage.LoadAsync()` passes this error on, rather than catching it.
**Why it is built this way**: the game should never start in a state
where its data is left unguarded.

### 5.2 The key data is too short

Both the setting path and the file path check that the decoded data is
exactly 48 bytes. If it is shorter, `Vault.GetKey()` throws
`InvalidOperationException`.

### 5.3 A broken or edited save file

.NET's AES-CBC decryption throws a `CryptographicException` when the
padding does not check out. `Storage.LoadAsync()` does not quietly give
back null here; the error is passed on. The game's own code should catch
`CryptographicException` at load time, and show the player a plain
message such as "the save file is broken".

### 5.4 No save file yet (the first time the game runs)

`Storage.LoadAsync()` gives back `null` if neither `germio.json` nor
`germio.dat` is found. This is the normal state on a first run; the game
starts a fresh `Scenario`, filled in with the defaults built into the
game.

---

## 6. Notes for an outside security review

Anyone reviewing Germio's security should look closely at the following.

### 6.1 CBC mode, against GCM

Germio uses AES-CBC, which carries no built-in check that the data was not
changed by someone else. A mode with such a check (AES-GCM) would catch
tampering without needing to decrypt the data first. This is a **known
gap**.

A path to close it: swap `Aes.Create()` (CBC plus PKCS7) for `AesGcm`
(which checks for tampering; needs .NET 5 or later, and is supported from
Unity 2021 on). This is tracked as something to improve after the v1.0
release.

### 6.2 How the IV is handled

.NET's own CBC build makes a fresh, random IV each time something is
encrypted, and this IV is placed before the encrypted bytes. A reviewer
should check that `Storage.SaveAsync()` never reuses the same IV across
more than one save.

### 6.3 PlayerPrefs

Check, with `grep -rn "PlayerPrefs" game/Assets/Plugins/Germio/`, that no
call to `PlayerPrefs.GetString/SetString` is left anywhere in Germio's own
code. As of v2.2, this should turn up **zero** hits.

### 6.4 How well the tests cover this

Tests that encrypt data, then decrypt it back, sit in:
`game/tests/IntegrationTests/Scripts/Core/StorageEncryptionTests.cs`

Tests for the order `Vault` looks for a key sit in:
`game/tests/IntegrationTests/Scripts/Core/VaultTests.cs`

Run `dotnet test --filter "StorageEncryption|Vault"` to check these.
