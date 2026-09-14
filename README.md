# game04

**Secrets, Room's & Travels' — The Card Game.** A two-player tactical card game
in which both players always play the same deck, and in which everything inside
a match is diegetic: no numbers and no letters. Values are read from colour,
count and the art itself; abilities from animation and from what the card looks
like.

Built with Godot 4 and C#. The rules live in an engine-free core with unit
tests; the Godot side renders snapshots and sends commands.

## Where to start

| Document | What it holds |
|---|---|
| [`AGENTS.md`](AGENTS.md) | The project canon: required reading, the working method, the layout, verification and Git rules. Read this first. |
| [`CLAUDE.md`](CLAUDE.md) | Working from an agent sandbox: the check watcher, Git locks, deleting files. |
| [`docs/GAME_DESIGN.md`](docs/GAME_DESIGN.md) | The rules. Source of truth for every mechanic; §15 lists what is deliberately still open. |
| [`docs/ROADMAP.md`](docs/ROADMAP.md) | The build order, gates G00–G10. |
| [`docs/TASKS.md`](docs/TASKS.md) | Open, blocked and deferred work, and the settled decisions worth recording. |

`concepts/` holds the hand-drawn references the design keeps pointing at: the
board, the card surface, and the diegetic ability sketches.
