# game04 agent guide

**Secrets, Room's & Travels' — The Card Game.** A two-player tactical card game
in which both players always play the same deck, and in which everything inside
a match is diegetic: no numbers and no letters.

## Required reading

Before planning, discussing or changing anything:

- `docs/GAME_DESIGN.md` — the rules. The source of truth for every mechanic,
  and §15 for what is deliberately still open.
- `docs/ROADMAP.md` — the build order (gates G00–G10) and the working method.
- `docs/TASKS.md` — the tracker. Check it before "fixing" something that looks
  odd: it may already be a known finding with a decision attached.

`concepts/` holds the hand-drawn references the design keeps pointing at: the
board, the card surface, and the diegetic ability sketches.

## The working method

Two rules carry this project. Both are in `ROADMAP.md` §1–§2; they are repeated
here because neither is optional.

**Build from the simulation outward.** The rules never live in a scene. They
live in a Godot-free assembly with unit tests; the Godot side reads snapshots
and draws them. Every gate has a simulation half and a presentation half, in
that order.

**Ask before you build.** Before writing any code for a gate, write a short spec
for that gate and ask the developer its open questions. Many design decisions
are in his head on purpose and are answered per gate, not up front. An agent
that finds itself inventing a rule to keep going has found a question it should
have asked — stop and ask rather than guessing, and rather than quietly
widening the design.

Anything marked **EXPERIMENTAL** in `GAME_DESIGN.md` may be changed freely by a
later gate, and nothing outside it may depend on its current shape.

## Project layout

Created by G00; the assembly prefix is provisional until that gate's spec round
settles it. Layered strictly downwards — nothing ever references upwards.

| Project | Depends on | Holds |
| --- | --- | --- |
| `src/Cardgame.Core` | nothing | world state, commands, events, systems, snapshot projection, card and board data |
| `src/Cardgame.Server` | Core | lobby, match runner, command queue, the authoritative clock |
| `src/Cardgame.App` | Core | the Godot client: layout, drawing, animation, input plumbing |
| `src/Cardgame.Cli` | Core | headless match runner — plays a command list from a seed and prints the resulting state |
| `tests/Cardgame.Core.Tests` | Core, TestSupport | |
| `tests/Cardgame.TestSupport` | Core | fixtures shared by the test projects |

Four rules follow from this:

- **`Core` must not reference Godot.** Not the SDK, not a type, not a `using`.
  It is a plain .NET class library so that `dotnet test` runs the rules without
  an engine anywhere in sight.
- **There are no tests against `Cardgame.App`.** A test project cannot reference
  it without pulling in `Godot.NET.Sdk`. The corollary is the useful half:
  anything worth testing belongs in `Core`. If a rule is hard to test, it is in
  the wrong project.
- **The client never resolves a rule.** It sends commands and renders snapshots.
  `App` references `Core` for those types only. If the types ever have to ship
  without the rules — a web client, say — they move into a project of their own;
  until then this is a discipline, not a compiler boundary.
- **`Server` and `App` never reference each other.** They talk over the network.

## Engineering

Keep the simulation explicit and serializable: state in data, logic in systems,
intent in commands, facts in events. Card behaviour is data, never a per-card
script. Randomness goes through one seeded source, because a mirror-deck game
with a seed is reproducible and that is worth more here than anywhere else.

Make one focused slice at a time and preserve unrelated work.

## Verification

```sh
./scripts/check.sh
```

Builds every project and runs every test project. Unlike the sibling
repositories there is no cheaper path that skips the tests: the Core is small
and its tests are the first half of every gate, so a run without them would
check almost nothing. `--tests` is accepted and changes nothing.

From an agent sandbox the script cannot be run directly — request a run through
the watcher instead. `CLAUDE.md` has the exit codes and the rules for when to
stop and ask.

Two things the script does not cover, and cannot: booting the Godot editor
needs the Godot binary rather than only its NuGet SDK, and looking at the board
is not something a script does. Both stay manual, and the second one is the
whole point of the presentation half of a gate.

## Project documentation

Everything except this file and `CLAUDE.md` lives in `docs/`, because these two
are what an agent is pointed at first.

- `docs/GAME_DESIGN.md` — rules. Sections marked EXPERIMENTAL are unfinished on
  purpose; §15 lists every question carried deliberately.
- `docs/ROADMAP.md` — gates, and the working method above in full.
- `docs/TASKS.md` — open, blocked and deliberately deferred work, plus the
  settled decisions worth recording. Its own header explains what belongs there
  rather than in the other two.

Three documents, three kinds of open question, and they are not copied into each
other: §15 holds open **rules**, the roadmap holds the **plan**, `TASKS.md`
holds open **work**. Add a finding to the tracker rather than leaving it
undocumented.

When a gate answers a question, the answer goes into `GAME_DESIGN.md`. A
decision that lives only in a commit message is a decision that will be made
again.

## Version control

After every completed change, create a Git commit with a concise, descriptive
message. Review the staged diff first and make sure only the intended changes
are in it. Never stage `_to_delete/`. Do not push — the developer handles that.

Run `GIT_UNLOCK_AGE=5 ./scripts/git-unlock.sh` after each commit made from a
sandbox; `CLAUDE.md` explains why.
