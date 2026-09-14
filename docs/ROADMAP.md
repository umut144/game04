# Roadmap — Secrets, Room's & Travels' — The Card Game

**Document version:** 1.000
**Status:** source of truth for build order. Rules live in
[`GAME_DESIGN.md`](GAME_DESIGN.md).

---

## 1. The one principle

> Build the game from the simulation outward.

That sentence is already in `cardgame-ref/Documents/GAME_ARCHITECTURE.md`. The
previous attempt had it written down and did not follow it, and the design
drifted inside the Godot scene until the prototype could no longer be judged.
This roadmap exists mainly to make the sentence binding.

Concretely: **the rules never live in a scene.** They live in a Godot-free core
assembly with unit tests. The Godot side reads snapshots and draws them.

## 2. How a gate is run

### 2.1 Every gate has two halves

Each gate delivers a **Simulation half** and a **Presentation half**. Neither is
optional and the order inside a gate is fixed: simulation first, with tests,
then presentation.

The gate list below was originally written as presentation milestones only —
"build the board", "draw a card", "watch two cards trade blows". The simulation
half was implicit. It is now spelled out, because "look at it and see whether
the health points went down correctly" is not a check, it is a hope. A test
that asserts 3 attack against 5 health leaves 2 is the check; the animation is
what you enjoy afterwards.

### 2.2 The spec question round — mandatory

Many design decisions are in the developer's head and are deliberately not
written down in advance. Therefore:

**Before writing any code for a gate, the implementing agent writes a short
spec for that gate and asks the developer its open questions. Work starts only
after the answers are in.**

The spec is short — the decisions the gate needs, nothing more. It names:

- what the gate's simulation half must be able to do,
- what the presentation half must show,
- the questions whose answers the gate cannot be built without,
- what counts as done.

Answers go back into `GAME_DESIGN.md`; the spec itself is scratch and does not
need to be kept. Questions already listed under `GAME_DESIGN.md` §15 are asked
at the gate that needs them, not all at once and not early. They are referenced
by name there rather than by number, so answering one does not renumber the
others.

An agent that finds itself inventing a rule to keep going has found a question
it should have asked. Stop and ask.

### 2.3 Definition of done

A gate is done when:

1. the simulation half passes its tests,
2. the presentation half shows exactly what the simulation reports — no rule
   lives only in the scene,
3. `GAME_DESIGN.md` records the decisions the gate answered,
4. anything still open is written down rather than carried in someone's head.

### 2.4 Experimental sections

Anything marked **EXPERIMENTAL** in `GAME_DESIGN.md` may be changed freely by a
later gate. Nothing outside it may depend on its current shape. Mastery Stats
and totem presentation are experimental today.

## 3. The gates

The numbering differs from the first sketch of the roadmap; the mapping is in
§4. Gates are ordered by dependency, not by appeal.

### G00 — Core and data model

No presentation. The foundation the other gates stand on.

- Godot-free core assembly; world state, commands, events, systems, snapshot
  projection; seeded deterministic RNG.
- Data model: board geometry (6 slots + 3 × 2 totem columns per side), card
  definition and card instance, the three tiers, the four corner values, and a
  **type** field (§7.6) — nine types exist in the fiction, one is specified, and
  rules already hang off it, so the field belongs in the model from the start.
- Test harness, and a plain-text dump of the world state as the only "view".

Done when a match can be set up, serialised and asserted against without Godot
ever being started.

### G01 — The board

- **Sim:** board state, slot occupancy, totem placement and its randomisation,
  the two mirror modes, match setup.
- **Pres:** the board of `concepts/board_field.JPG`, at a 2560 × 1600 (16:10)
  design viewport.

One warning for the presentation half: use the design viewport as a *reference*
and lay the board out from the grid definition with anchors, not from
hard-coded pixel arithmetic. A board that is only correct at one aspect ratio
will have to be rebuilt for the first player on 16:9.

### G02 — Cards and the diegetic value language

- **Sim:** Rogue and Wizard as card definitions, tiers 1–3, play cost, hand,
  paying mana.
- **Pres:** the card surface, all four corner glyphs across all three
  intensities, tier selection, and the **debug number overlay**. The top-left
  wedge carries two currencies — mana in blue, Coins in copper/silver/gold
  (§7.1) — so build it as cost *plus* currency from the start, not as a blue
  wedge that gets a second skin later.

The overlay is part of this gate, not a later convenience. G03 is measured
against it.

### G03 — Legibility

A gate with no new rules and a real possibility of failure.

Protocol: show a card for about two seconds and have a test person name cost,
bounty, attack and health. Repeat across values, intensities and both card
types. The pass mark is set before the test, not after it.

Two cases have to be in the set deliberately, because they are where the system
is weakest: a **Coin-cost card**, whose top-left and top-right wedges are both
in the copper/silver/gold family and mean opposite things, and a **split band**
such as the Rogue ♀'s half-red/half-poison attack (§7.2).

If it fails, G02 is reopened. That is the point of doing it after two cards
rather than after fifty. This is the project's largest single risk and the
cheapest moment to discover it.

### G04 — Round loop, mana, time

- **Sim:** alternating turns with a per-turn clock, rounds, turn order from
  Speed, +1 mana per round to a maximum of 7, draw 1 per round, hand limit 8,
  the 10 s base + 20 s bonus clock.
- **Pres:** the resource totems as the HUD — mana segments extinguishing as
  mana is spent, the Totem of Time draining and flipping into bonus time.

The rules of this gate are settled in `GAME_DESIGN.md` §6. What is still open
is the presentation of an eighth mana point on a seven-segment totem (§15,
*The eighth mana point*) — it can be deferred to the Mastery gate, since only
Mastery produces one.

### G05 — Unit combat

- **Sim:** range and attack patterns rebased to 6 slots, damage, destruction
  (one kind only, §8.3), the roster of §8.2 as data. Tests prove the
  arithmetic.
- **Sim:** Bounty and **Coins** (§10). Coins are not a Merchant feature: they
  are earned on every destruction, and thrown cards already spend them, so they
  belong here and G08 is only a second sink. Note that a consumed card pays its
  Bounty on use, not on destruction.
- **Sim:** the Arcane rules (§7.6) — the totem ban and the 1 mana refunded on a
  kill. The refund is a combat trigger that pays into the economy of G04, so it
  is written here and tested against the mana system, not bolted on later.
- **Pres:** target and affected-field preview, the attack itself, damage and
  destruction feedback.

Reach is settled in `GAME_DESIGN.md` §8.1 and it is **two** concepts: *Range*
names enemy fields only, *Radius* names fields on either side including the
unit's own. Both belong in the data model from the first line, together with
*affected area* as a third, separate thing — Wizard, Warrior and Hammerer all
differ in reach versus area, thrown cards use Radius, and a single "range" field
holds none of it.

### G06 — Totems under attack

- **Sim:** the column protection rule, the Rogue bypass, the diagonal surcharge
  for reaching a totem (§8.1.1), damage to Life, Mana and Time and their effect
  on the following round, win and draw conditions.
  Model protection per column from the start: two later rules — stunned units
  and Totemic Taunt (§4.2, §4.3) — both cut through it, and a pair-wide
  boolean will not survive them. A totem is one target reachable through either
  of its columns, never partially reachable. Area effects do not pay the
  surcharge (§8.1.1) — the Bomb reaching a totem no unit could aim at is a
  wanted outcome and needs a test that pins it, so a later cleanup does not
  quietly remove it.
- **Pres:** totem damage states, and the resource loss made legible — the
  player must see that next round will be poorer.

### G07 — Card effects

The largest gate; the effect system's shape is locked here.

- **Sim:** the trigger → effect → value symbol structure, OnPlay, OnDeath,
  Taunt, Stealth, Rush. Effects are data, never per-card scripts.
- **Sim:** the status effects of §9 — Stun, Poison, Burn, Protect — with their
  different clocks. Poison ticks per round, Burn per turn, Protect is a charge;
  name the exact checkpoints (§15, *Tick checkpoints*) and test them against
  each other, because an alternating turn order makes the two schedules easy to
  confuse. Cures (§9.5) always remove the whole stack and belong in this gate
  with the effects they remove — a poison stack without an antidote is not
  balanced, it is only dangerous.
- **Pres:** the shared diegetic vocabulary, superposed display, and the
  automatic effect sequence with manual stepping.

Worth splitting into two gates if the symbol vocabulary turns out to be large.

### G08 — Merchant

- **Sim:** merchant scheduling, offers, the phase with alternating priority and
  its own clock. Bounty and Coins themselves already exist from G05.
- **Pres:** the merchant overlay, collapsible over the board.

### G09 — Mastery Stats — **EXPERIMENTAL**

- **Sim:** 6 points across 4 stats, max 3 each, each point a distinct effect
  rather than a scaling number, layered on top of rules that are already
  complete without them. Totemic Taunt and the Stamina 3 totem placement reach
  back into the protection rule and the match setup of G01/G06; Protect and
  death protection reach into damage resolution. The placement rule also
  overrides Perfect Mirror for the player who took it (§2) — match setup has to
  allow one side to deviate from a mirrored layout.
- **Pres:** the pre-match allocation screen (meta UI — text allowed), and the
  totem colour changes that Health and Mana produce.

Late on purpose. Everything before it must be playable at 0 points everywhere.

### G10 — Infrastructure

Player-hosted servers, lobbies, deck composition and the authored deck library —
the `world01` MOBA model, and where `cardgame-ref`'s lobby systems return.

### Explicitly not in the MVP

Ghost and Puppet units (`GAME_DESIGN.md` §7.6) are written down as preparation
and no gate builds them. They are on record so the type system and the reach
model are designed against what is coming — a Ghost that dies to a unit played
on top of it, and a Shaman with infinite radius, both stress the same two
systems. Building to them early is out of scope; being unable to express them
later would be a design failure.

### Later, not scheduled

Colour-blind mode with hatched wedges (`GAME_DESIGN.md` §11); replay from seed,
which a mirror-deck game gets almost for free and which is worth a great deal
for both testing and the "study a line" fantasy.

## 4. Mapping from the first sketch

| First sketch                | Now                                      |
|-----------------------------|------------------------------------------|
| —                           | G00 core and data model                  |
| gate01 board                | G01 board                                |
| gate02 cards, tiers, corners| G02 cards, plus G03 legibility           |
| —                           | G04 round loop, mana, time               |
| gate03 two cards trading    | G05 unit combat                          |
| gate04 attacking totems     | G06 totems under attack                  |
| gate05 effects              | G07 card effects                         |
| gate06 merchant             | G08 merchant (bounty and Coins in G05)    |
| gate07 mastery stats        | G09 mastery stats                        |
| gate09 infrastructure       | G10 infrastructure                       |

Three things were inserted: the core gate, the round loop, and the legibility
test. Nothing was dropped.
