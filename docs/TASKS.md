# game04 Tasks

This file tracks only current, next, blocked or deliberately deferred outcomes.
Completed implementation history stays in Git.

**What belongs here, and what does not.** Three documents carry three different
kinds of open question, and they must not be copied into each other:

- `GAME_DESIGN.md` §15 holds the open **rules** questions — what a mechanic
  does. They are answered in the gate that needs them, and the answer goes back
  into the design document.
- `ROADMAP.md` holds the **plan**: the gates, their order, and the working
  method. It is not a tracker.
- This file holds the open **work**: decisions the implementation has to make,
  findings that need acting on, and what was deferred on purpose.

A rule question that turns out to need engineering gets a row here as well; a
row here that turns out to be a rule question moves to §15 instead.

Read `AGENTS.md` first — its rules are what every row has to stay inside.

**Where this stands.** Nothing is built yet. The design and the roadmap are
written, the repository is set up, and the next step is the spec round for
**G00 — Core and data model**, whose questions are the `CORE-*` rows below.

## Active Tasks

| ID | Area | Outcome | Status |
|---|---|---|---|
| `CORE-01` | Architecture | Decide where the turn clock lives. The 10 s + 20 s split is a rule, the Time totem is game state, and the clock is attackable — all of which argues for Core. A real clock is also the one thing a deterministic, testable core does not want. `cardgame-ref` put a `ServerTimeSystem` in `Core/Simulation`; whether that was right is the question. | **Open — G00 spec round** |
| `CORE-02` | Architecture | Settle the snapshot: full state every time or deltas, and what a player's projection may contain. Visibility is the part that cannot be retrofitted — the opponent's hand must never leave the server, and a projection built for convenience first will leak it. | **Open — G00 spec round** |
| `CORE-03` | Architecture | Settle the randomness contract: one seeded source, where the seed lives, and how Perfect Mirror consumes it so both sides provably get the same deck order and totem layout. This is also what makes `REPLAY-01` almost free later, so it is worth getting right before anything calls a random number. | **Open — G00 spec round** |
| `CORE-04` | Architecture | Name the assemblies. `Cardgame.*` is provisional throughout `AGENTS.md`; `scripts/check.sh` discovers projects rather than naming them, so nothing breaks either way, but the first file needs a decision. Test framework follows SceneMaker unless there is a reason: xunit with `Microsoft.NET.Test.Sdk`. | **Open — G00 spec round** |
| `CORE-05` | Architecture | Decide whether G00 creates a Godot project at all or whether the first `project.godot` arrives with G01. G00 is Godot-free by definition, so the honest answer is probably G01 — but `check.sh` builds whatever root project exists and needs to be told which world it is in. | **Open — G00 spec round** |
| `ASSET-01` | Assets | Reconcile the totem segment counts. The design says body plus **7** segments for each totem (`GAME_DESIGN.md` §5); the PolyTools exports carry 11 components for Life, 8 for Mana and 9 for Time. Mana matches, the other two do not. The segment count is a gameplay value — Life's HP and Mana's supply are read off it — so this is not a modelling detail that can be settled in the presentation gate. | **Blocked on G06** — wanted before the totems are drawn |
| `TEST-01` | Combat | Pin the Human bomb bonus with a test. That +1 Radius does **not** extend a thrown card's reach against totems is a deliberate balance decision (`GAME_DESIGN.md` §8.4), but it is *emergent*: it falls out of `floor(2R/3)` being equal for 3 and 4. Change the surcharge formula and the decision changes silently. A comment will not hold it; an assertion will. | **Blocked on G06** |
| `DOC-01` | Documentation | Decide how the data-allowance notice reaches an agent that starts at `AGENTS.md`. In all four repositories the link runs one way — every `CLAUDE.md` says to read `AGENTS.md` in full, and no `AGENTS.md` requires `CLAUDE.md` in return — so an agent following the AGENTS convention never sees it. Either repeat the notice in each `AGENTS.md`, or add one line making `CLAUDE.md` required reading and keep the text in one place. Spans all four repositories, not just this one. | **Open** |

## Optional Later

| ID | Area | Outcome | Status |
|---|---|---|---|
| `REPLAY-01` | Tooling | Replay a match from its seed and a command list. A mirror-deck game with one seeded source (`CORE-03`) gets this nearly for free, and it pays twice: as the strongest regression test the simulation can have, and as the "study a line" fantasy that Perfect Mirror is built around. `src/Cardgame.Cli` is where it lands. | **Optional / Later** |
| `A11Y-01` | Accessibility | Colour-blind mode distinguishing the corner wedges by hatching as well as colour (`GAME_DESIGN.md` §13). Attack is red and Health is green in a game with no numbers to fall back on, and the Rogue ♀'s split band puts both inside one wedge. The wedges are already drawn as bands, so hatching is a second channel rather than a retrofit — but it is a card-art decision, and the longer it waits the more art it touches. | **Optional / Later** |
| `REF-01` | Housekeeping | Retire `cardgame-ref/`. Its architecture lessons are written down in `GAME_DESIGN.md` §14 and the only code still worth reading is the lobby and server side, wanted at G10. Once that gate is past, the folder has no reader left. It is git-ignored, so retiring it means deleting it from the machine, not from the history. | **Blocked on G10** |

## Settled, recorded so it is not re-litigated

| ID | Area | Decision |
|---|---|---|
| `ENGINE-01` | Platform | **Godot 4 with a C# core**, decided 2026-09-14. Server cost, the developer's first concern, turned out not to be a factor: a turn-based card game's server is a command validator with no tick loop, so Rust's advantage lands on a base of a few dozen kilobytes per match. Three things decided it instead. PolyTools is Godot and exports `.tscn`, which Godot reads directly while Bevy needs the importer world01 had to build. The product is largely presentation — animation, shaders, layered card state — and Godot ships what would otherwise be built by hand. And for agent-driven work, Bevy's API churn and compile times cost more than C# gives up, which for a turn-based game with no concurrency is little. world01 stays Bevy; a MOBA with its own simulation is a different product. Note what this does *not* decide: the engine binds only the presentation half, because the rules live in an engine-free core. |
| `WEB-01` | Platform | No browser client for now — the developer's goal is not the browser, which is what settled Godot with a C# core. The architecture keeps the door open rather than closing it: the client resolves no rules (`AGENTS.md`), so a thin client in another language could render snapshots without the rules engine ever shipping to it. Reopen only with the product decision, never as a technical drift. |

## Tracker rules

- Keep at most one task **In progress**.
- Describe an observable outcome, not an implementation diary.
- Add acceptance detail only when it is needed to decide whether the task is done.
- A row is one entry with its reasoning; the reasoning does not move to a
  separate notes file, because in this project it fits.
- Remove completed rows after the immediate handoff; Git preserves their
  history. A decision that closes a question moves to **Settled** rather than
  being deleted, if it is the kind that gets asked again.
- Add a finding here rather than leaving it undocumented.
