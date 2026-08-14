# Badge Convention

> The one place that fixes the README badges for every repository in this
> maker's game stack. Opinio, the section chief, reads these badges to report
> where each repository stands. When every repository shows the same badges in
> the same shape, the reports line up and the phase is plain to see.
>
> The badges are written in simple words, so a reader whose first language is
> not English, and an agent too, can take them in. This document follows
> `writing_standard.md`.

Before this convention, each repository set its own badges. Some had a phase
badge, most did not. Versions carried an `-alpha` tag that no one had asked
for. An agent, left with no rule, put numbers on by habit, and the numbers
drifted apart. This convention removes that drift: it says which badges to
show, in what order, and what each one means.

---

## 1. The badges, in order

Every repository shows these badges at the top of its README, in this order.

+ **Runtime** — Unity or .NET, at the version the repository builds against.
+ **Phase** — the phase the work is in now.
+ **Version** — the version, as `vX.Y.Z` (see section 3).
+ **License** — MIT, for a public repository only.

A private repository (for example, a paid product) drops the License badge.
It keeps Runtime, Phase, and Version.

### 1.1 Runtime

Show the runtime the repository builds against, at its version.

+ A Unity repository shows `Unity 6 LTS` (or the LTS in use).
+ A repository whose core does not depend on Unity shows the .NET version, such
  as `.NET 8`.
+ Always move this badge to the newest runtime as soon as the repository builds
  on it. The badge is there to keep the habit of staying current.

### 1.2 Phase

Show the phase the work is in now, such as `Phase 5`. Opinio reads this badge
first to fill the phase column of its dashboard. Every repository must carry it,
so no repository shows a blank phase.

### 1.3 License

Show `MIT` only for a public repository. A private repository does not show a
License badge, because it is not published and needs no license in the open.

---

## 2. The example

A public Unity repository:

```text
[![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?logo=unity)](https://unity.com/)
[![Phase](https://img.shields.io/badge/phase-5-blue)]()
[![Version](https://img.shields.io/badge/version-v0.5.42-orange)]()
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
```

A private product repository (no License badge):

```text
[![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?logo=unity)](https://unity.com/)
[![Phase](https://img.shields.io/badge/phase-3-blue)]()
[![Version](https://img.shields.io/badge/version-v0.3.88-orange)]()
```

---

## 3. The version — `vX.Y.Z`

The version has three parts. Each part moves on one event only, and on nothing
else. This keeps an agent from moving the numbers by its own guess.

+ **X — the release.** X stays `0` for the whole of the build. It turns to `1`
  on one event only: the day the game ships on Google Play. Nothing else moves
  X. Until that day, X is `0`.
+ **Y — the phase.** Y holds the phase number. But an agent's split of the work
  into phases is not to be trusted, so Y does not move on an agent's judgement.
  On the release (when X turns to `1`), Y resets to `0`. Through the build, Y
  stays as it is; it is not the number an agent bumps.
+ **Z — the commit.** Z moves on every commit. Commit, and Z goes up by one, no
  questions asked. Z has no ceiling: it may pass `999`, and it does not carry
  over into Y. Z just counts commits.

So through the build, the version reads `v0.Y.Z`, and only Z climbs, one step
per commit. X waits for the Google Play release; Y waits with it.

### 3.1 No maturity tag

The version carries no `-alpha`, `-beta`, or `-rc` tag. No one asked for those
tags; an agent added them by habit. Maturity is told by the Phase badge, not by
a tag on the version. Drop the tag.

---

## 4. How Opinio reads this

Opinio scans each repository and fills its dashboard. For the phase, it reads
the Phase badge in the README. Because every repository carries the Phase badge
in the same shape, every row of the dashboard shows a phase, and the phases can
be set side by side.

Opinio only reads. It does not write these badges back. The repository is the
truth; Opinio reports it.
