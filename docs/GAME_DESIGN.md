# Secrets, Room's & Travels' — The Card Game

**Document version:** 3.001
**Status:** design source of truth for rules. Build order lives in [`ROADMAP.md`](ROADMAP.md).

This document replaces the design in `cardgame-ref/Documents/`. That project's
*architecture* is still a reference and is partly reused (see §14); its *game
design* — Lineages, Archetypes, Relics, Deck Powers, five card kinds, eight
fields per side — is discarded.

Sections marked **EXPERIMENTAL** are deliberately incomplete. They are expected
to change once they have been played, and nothing else in the design may come
to depend on their current shape.

---

## 1. What the game is

A two-player tactical card game in which both players always play the **same
deck**. There are no factions, no deckbuilding advantage and no meta arms race.
The asymmetry between two players comes from three places only:

1. which of the three tiers they choose when they play a card,
2. where they place units on the board,
3. their Mastery Stats, set once before the match.

Everything inside a match is **diegetic**: no numbers and no letters. Values are
read from colour, count and the art itself; abilities are read from animation
and from changes to the card's appearance.

Diegetic is a rule for the *match*. Meta screens — lobby, server browser, deck
composition, Mastery Stat allocation — may use text.

## 2. Mirror modes

Every match is a mirror match. Two modes:

- **Shuffled Mirror** — same card pool, decks shuffled independently, different
  starting hands, and the **totem layout rolled independently per side**. This
  is the situation drawn in `concepts/board_field.JPG`, where the two sides
  carry the three totems in different orders.
- **Perfect Mirror** — same cards, same starting hand, same deck order, and the
  **same totem layout on both sides**. "Perfect" must be literally true; a
  randomised layout would be the one remaining asymmetry and is therefore
  mirrored as well.

**One rule overrides Perfect Mirror:** a player at Stamina 3 places their own
Totem of Time themselves (§12). Their side then deviates from the planned
layout while the opponent still receives exactly what the perfect mirror
intended. If both players took Stamina 3, both place, and the mirror is
knowingly bent in that one respect. This is a deliberate exception, not an
oversight — it is the price of the strongest defensive point in the game, and
it is paid in the mode's own promise.

A deck is a subset of one large, growing card pool. Deck composition — choosing
or authoring a deck — belongs to the infrastructure gate, not to the MVP.

## 3. The board

Per side, from the opponent's view inward:

```text
Opponent back:   [ Totem A ][ Totem B ][ Totem C ]     3 totems, 2 columns each
Opponent front:   1   2   3   4   5   6               6 unit slots
                 ───────────────────────────
Own front:        1   2   3   4   5   6               6 unit slots
Own back:        [ Totem ][ Totem ][ Totem ]           3 totems, 2 columns each
```

- 6 unit slots per side, numbered 1–6, side-local.
- 3 totems per side, each occupying 2 back-row columns: columns 1–2, 3–4, 5–6.
- The three totems are **Totem of Life**, **Totem of Mana** and **Totem of
  Time**. Their assignment to the three column pairs is randomised at match
  start (see §2 for how the two sides relate).
- Adjacency is horizontal within one side. Slot 4 opposes slot 4.

Canonical layout reference: `concepts/board_field.JPG`.

## 4. Totem protection

**A totem is protected by the units standing in its own two columns.**

Totem A covers columns 1–2, Totem B covers 3–4, Totem C covers 5–6. As long as
at least one unit stands in a totem's column pair, that totem cannot be
attacked. The other two totems are unaffected by it.

This is the rule that gives the board its meaning. The earlier idea — any
single unit anywhere protects all three totems — was rejected: it makes every
slot interchangeable, reduces the match to "keep one chump unit alive" and
makes a full board wipe the only path to victory.

**Protection is a property of the slot being occupied, not of who put the
occupant there.** Anything standing in the column closes it — including a Ghost
the *opponent* placed on this side of the board. Playing a Ghost onto the
enemy's board therefore shields the enemy's totem, and that is deliberate: it is
the price of being allowed to place on both sides at all.

Whether the same holds for a Wall is not yet decided (§8.4).

A column is **open** when its slot is empty. Two things cut through protection:

### 4.1 Rogue bypass

The Rogue ignores protection **only through the slot directly in front of
himself**. A Rogue on own slot 3 may strike the totem covering opposing columns
3–4 if opposing slot 3 is open — even when opposing slot 4 is occupied.

So the bypass is a hole in a specific column, not a general exception. It makes
the arrangement of a defender's units, not merely their number, the thing that
matters.

### 4.2 Stunned units

A stunned unit (§9.1) protects only its **own** column. It no longer extends
protection across the column pair, so if the other column of that pair is open,
any attacker that can reach through it may hit the totem — not only a Rogue.

### 4.3 Totemic Taunt

A Stamina Mastery investment can force every totem attack onto the Totem of
Time while that totem is undamaged. See §12, Stamina.

## 5. The three totems

All three totems are board objects. They can be damaged, they can be healed,
and they are simultaneously the player's **HUD**: a totem does not display a
resource next to itself, it *is* the display. Damaging a totem visibly
shortens the thing the player reads their own state from.

Each totem is built as a body plus **7 segments**. **EXPERIMENTAL** — the
presentation of all three totems needs testing, especially in combination with
Mastery Stats.

### 5.1 Totem of Life — the win condition

Destroying the opponent's Totem of Life wins the match. Each segment is one HP
step; base total is 7. Health Mastery adds 2 HP for its first point (§12).

Higher HP does not add segments, it makes each segment carry more steps, shown
by a stronger colour. A segment then darkens through its colour stages as it
takes damage — dark red → red → grey — until it is spent.

### 5.2 Totem of Mana

The 7 segments glow blue, one per available mana point. Spending mana
extinguishes segments to grey; the round refills them. Damage to the totem
reduces what the round can refill, and keeps reducing it until the totem is
healed.

Maximum mana is **7**, and **8** with the third Mana Mastery point. How an
eighth point is shown on a seven-segment totem is an open presentation
question (§15).

### 5.3 Totem of Time

The colour drains from top to bottom like an hourglass. A turn has **10 seconds
of base time and 20 seconds of bonus time**. When the base time is spent, the
totem flips and the bonus time runs down in a more intense colour.

Only the **bonus time** can be attacked. Base time is a floor and cannot be
taken away. Damage to the Totem of Time reduces the bonus time the next round
grants, in proportion to the segments still standing.

The floor exists because this game asks more of a player's reading time than a
normal card game does, not less: no numbers, plus a three-way tier choice on
every card played.

### 5.4 Why this matters

The three totems give three ways to win. Destroy Life and the match ends now.
Attack Mana or Time and the opponent is not killed but **strangled**: every
following round they have less to spend and less time to spend it in, while a
healthy attacker keeps their full budget. An unhealed resource totem compounds.

## 6. Turn structure and economy

- **Alternating turns**, each with its own clock. A **turn** is one player's
  turn; a **round** is both players having had one turn.
- The turn clock is 10 s base + 20 s bonus (§5.3).
- **Mana** grows by **1 per round**, up to maximum mana (7, or 8 with Mana
  Mastery 3).
- Unspent mana is normally lost at the end of the round. Mana Mastery carries 1
  or 2 points over into the next round (§12).
- **Draw** is 1 card per round.
- **Hand limit is 8.** Hand size and hand limit are the same thing; this
  document uses "hand limit". A player with a full hand simply does not draw —
  no card is burned, nothing is lost, the deck is untouched.

Who takes the first turn is decided by Speed (§12).

## 7. Cards

### 7.1 The card surface

Reference: `concepts/card_example.JPG`.

The four corners carry the four values, always in the same position:

| Corner       | Value      | Colour family                              |
|--------------|------------|--------------------------------------------|
| top left     | Play cost  | **mana:** light blue → blue → dark blue, or **Coins:** copper → silver → gold |
| top right    | Bounty     | copper → silver → gold                     |
| bottom left  | Attack     | light red → red → dark red                 |
| bottom right | Health     | light green → green → dark green           |

### 7.2 The value glyph system

Each corner is a triangular wedge divided into **3 bands**, read together with
**3 colour intensities**. Value = (intensity − 1) × 3 + filled bands, giving
1 through 9. 9 is the maximum value in the game.

```text
light  + 1..3 bands = 1, 2, 3
medium + 1..3 bands = 4, 5, 6
dark   + 1..3 bands = 7, 8, 9
```

A value of 0 is an empty wedge. The band structure stays visible when unfilled,
so the player reads "empty" rather than "missing". Reference:
`concepts/diegetic_examples/attack-power-example/`.

This supersedes the 1–12 (3 × 4) glyph system in `cardgame-ref`. Three bands
are quicker to count at a glance than four, and a 9-point range is enough.

**The top-left wedge also carries the currency.** Most cards cost mana and are
blue there. A card that costs **Coins** instead — the Bomb, for example — shows
the copper/silver/gold family in that same corner. Position says *what the
number is for*; colour family says *which resource pays it*.

This is the one place in the system where the same colour family appears twice
on one card with opposite meanings: a Coin cost top-left is what you pay, a
Bounty top-right is what your opponent collects. Only position separates them.
The legibility gate has to test a Coin-cost card specifically, not just a mana
one.

**A band may be split to show damage type.** The Rogue ♀ attacks for 1 physical
and 2 poison: her attack wedge shows band 1 half red / half poison green, and
band 2 fully poison green. The glyph therefore encodes composition, not only
magnitude — an extension the legibility gate has to test as well.

### 7.3 Tiers

Almost every card offers a choice of **Tier 1, Tier 2 or Tier 3** at the moment
it is played. The three tiers are shown side by side before the player commits.

Tier 3 is not an upgrade. It is more complex, riskier and usually more
expensive. A higher tier may carry a drawback as part of its identity — a
Rogue with Rush and 5 Bounty hands the opponent a large reward for killing it.
The **Stone** (§8.4) is the clearest case: its Tier 2 is free and stronger than
Tier 1, and pays the opponent for the privilege.

Tiers are the main branching point of a mirror match and must be readable fast,
because the turn clock is short and can be attacked. Tier selection needs a
keyboard path, not only pointer targeting.

### 7.4 Abilities

Abilities are shown diegetically — through animation, through a change to the
card, or through a change to the art. Reference:
`concepts/diegetic_examples/`.

- **Rush** — red eyes.
- **Stealth** — the figure sits inside a scribbled cloud of smoke.
- **OnDeath / explode** — the figure is drawn as a dashed outline with red
  bursts in all four corners.
- **Burn applied** — a burning shader effect around the attack wedge.
- **Taunt**, **Blink** and others have their own sketches in the same folder.

Some abilities share a common visual vocabulary; others are individual to one
card and will need individual presentation. The aim is **superposition**:
everything readable at once. When a card carries more effects than can be shown
simultaneously, a short sequence plays above the card automatically, and the
player can also step it with a key.

### 7.5 Taunt

A taunting unit on slot *i* forces the three opposing front columns *i−1*, *i*
and *i+1* to attack it. A taunter on slot 1 forces opposing 1 and 2; a taunter
on slot 4 forces opposing 3, 4 and 5.

`cardgame-ref` had removed Taunt because it collided with pattern targeting.
It is back, and the rule above is what resolves that collision: Taunt
constrains *who may be chosen as the primary target*, not which fields a
pattern covers.

### 7.6 Types

Units carry a **type** from the world of *Secrets, Room's & Travels'*. There are
nine. Rules hang off them one at a time, as a card actually needs them, rather
than being specified up front.

### Arcane — Mage, Sorcerer, Wizard

- An Arcane unit **cannot damage totems at all**, at any range.
- When an Arcane unit kills something, its controller is **refunded 1 mana**,
  usable immediately in the same round.

### Human — Warrior, Hammerer

- A card thrown from a Human launch point has **+1 Range** (§8.4).

### Goblin — Rogue, Rogue ♀

- A card thrown from a Goblin launch point costs **1 Coin less** (§8.4).

### Ghost — Enchantris, Romina · **post-MVP**

- May be played on **either side** of the board.
- Attack value 0. Ghosts do not strike; they work through **curses**.
- Having no physical body, a Ghost is **driven off its slot when another unit is
  played on top of it**. This is not a destruction at all: it **vanishes**. No
  Bounty is paid, it is not a kill, it never enters the list of destroyed cards,
  and it cannot be found in the match any more. Ghost-specific mechanics may one
  day bring it back; that is out of scope here.
- A Ghost that is **killed** dies like anything else (§8.3): Bounty is paid, it
  counts as a kill for the Arcane refund, and it joins the destroyed list.
- The two removals are therefore not interchangeable. Pushing a Ghost off its
  slot costs the attacker nothing and pays them nothing; killing it pays them
  its Bounty. Removing a Ghost the cheap way is also the merciful way.
- If the unit played there is **another Ghost**, the first does not die: it
  dodges to a random free slot on the board.
- Throw radius 0 (§8.4).

### Puppet — Shaman · **post-MVP**

- Throw radius 0 (§8.4).
- The most individual type in the world: Puppet rules live on the card, not on
  the type. Expect little that generalises.
- The **Shaman** attacks with a feather of **infinite radius** — any field on
  the board, on either side.

### Wild — Glavier · Kobold — Barde

No rules of their own yet.

### Nature, Bird — no units assigned yet

Ghost and Puppet above are written down as **preparation only**. Neither is part
of the MVP and no gate should build them; they are here so that the type system
is designed against what is actually coming, rather than being widened later.

The nine are the nine Lineages of `cardgame-ref` under a new name: **Arcane,
Human, Goblin, Ghost, Wild, Kobold, Nature, Puppet, Bird**. Only *Magical*
was renamed, to Arcane. They are not the same mechanic, though: a Lineage was a
deckbuilding identity, a type is nothing but a hook that specific rules hang
off.

Nature and Bird have no units yet, and the Archer has no type yet. Both are
deliberate gaps, filled after the MVP stands.

Arcane's first rule is what keeps the totem game physical: the three units with
the longest reach are exactly the three forbidden to touch a totem. It leaves the
Archer as the only unit that can threaten a totem from lateral distance, and it
turns the Rogue's bypass — which at +0 range looked like the weakest ability in
the roster — into one of the few reliable ways in. The mana refund is what
Arcane gets in exchange: it is paid in tempo rather than in reach.

## 8. Combat, range and the roster

Position is a real resource. Each unit defines how it reaches, not merely how
hard it hits.

### 8.1 Reach: Range and Radius

There are two kinds of reach, and the difference is which fields they may name.

**Range** points at the enemy. It is a **lateral column offset** from the
attacker's own slot index.
**Range +0 means frontal only** — the opposing slot with the same index. A unit
with Range +R on own slot *i* may target opposing slots *i−R* … *i+R*, clipped
to 1–6.

Worked examples: an Archer at +4 standing on slot 1 reaches opposing slots 1–5;
a Mage at +3 on slot 1 reaches 1–4; an Archer at +4 on slot 2 reaches 1–6.

**Radius** points at the board. It is the same lateral offset, but measured
around the unit **including its own field**, and it may name a field on
**either side**. Radius 0 therefore means "my own slot, and nothing else" — a
real value, not a way of saying "cannot".

Radius is what thrown cards use (§8.4), and an attack may use it too: the
Shaman's feather has infinite radius and can name any field on the board.

The diagonal surcharge below applies to a totem row on either side, so a unit
with a radius **can bomb its own totems**. That is allowed: it is the price of
the freedom, not a loophole to be closed.

**Reach and affected area are two different things.** The Wizard aims at a
target within +2 and his attack then radiates to that target's neighbours, so
the outermost slot he can affect lies 3 columns away — but his range is +2,
because that is what he may aim at. The same separation applies to Warrior and
Hammerer, who are +0 but affect three slots.

### 8.1.1 Reaching a totem — the diagonal surcharge

Totems stand in the back row, so reaching one costs more the further sideways
the attacker has to go. **Every 2 columns of lateral offset cost 1 extra
range.**

```text
cost(offset) = offset + ceil(offset / 2)
a totem column is reachable when cost(offset) <= range
```

The intent is to approximate a diagonal: going one row deeper is cheap straight
ahead and expensive from across the board. The practical effect is that no unit
can snipe a far-corner totem from safety — to threaten a totem you have to
commit units to that part of the board.

Resulting reach, in maximum lateral offset to a totem column:

| Range | +0 | +1 | +2 | +3 | +4 |
|-------|----|----|----|----|----|
| max offset to a totem | 0 | 0 | 1 | 2 | 2 |

The closed form is `floor(2 × range / 3)`, which is the cheaper check to
implement and is equivalent across the whole range table.

Worked examples:

- Archer at +4 on slot 1 reaches units on 1–5, but totem columns only 1–3 —
  so the totem spanning columns 3–4, and no further.
- Archer at +4 on slot 2 reaches units on 1–6, but totem columns only 1–4. A
  Totem of Time on columns 5–6 is out of his reach entirely.
- Rogue ♀ at +2 on slot 3 reaches totem columns 2–4.
- Glavier at +1 and everything at +0 reach only the totem column directly in
  front of them.

Mage, Sorcerer and Wizard are Arcane and may not damage totems at any range
(§7.6), so the reach table above applies to them for units only.

**A totem is one target, not two.** It is attackable when *either* of its two
columns is within reach. There is no such thing as reaching a totem partially.

**Area effects do not pay the surcharge.** It applies to what an attack *aims
at*. Damage that spreads from a chosen point reaches whatever its shape covers,
free of charge.

That produces a deliberate trick. The **Bomb** card has Radius 3 and spreads to
−1/+1 around its target. On the totem row, Radius 3 loses only 1 and Radius 4
loses 2, so both land on offset 2 — and a Bomb thrown from slot 2 can aim at
totem column 4 (offset 2, cost 3) and spread into column 5. Column 5 belongs to
the totem spanning 5–6, which costs 5 to aim at and which *no* thrower at
Radius 3 or 4 could have reached directly. A Totem of Time behind Totemic Taunt, safe
from every attacker on the board, is still not safe from a Bomb.

Tricks of this kind are wanted, not accidents to be patched out. A mechanic
that decides a match and is written nowhere on the board is what the game is
named after.

Range is measured from the **launch unit's** slot; §8.4 has the throwing rules.

### 8.2 The roster

| Unit | Type | Range | Affects | Special |
|------|------|-------|---------|---------|
| Rogue | Goblin | +0 | single | bypasses to the totem through an open column in front of him (§4.1) |
| Rogue ♀ | Goblin | +2 | single | 1 damage even when buffed, plus 2 Poison |
| Archer | — | +4 | single | — |
| Mage | Arcane | +3 | single | 2 consecutive hits |
| Wizard | Arcane | +2 | 3 slots around the target | — |
| Sorcerer | Arcane | — | both front rows | damages his own units too, but never himself, and never the totem row |
| Warrior | Human | +0 | 3 slots | full damage in front, half to the neighbours (Cleave) |
| Hammerer | Human | +0 | 3 slots | equal damage on all three (Splash); *i−1* and *i+1* are Stunned for 1 round |
| Glavier | Wild | +1 | single | applies exactly 1 Burn per attack |
| Barde | Kobold | own side | adjacent own units | heals, cleanses, and buffs +1 HP for 1 round |

The Archer has no type yet (§7.6). The Sorcerer's "whole
board" is both **front** rows only — he is Arcane, so the totem row is out of
his reach by type, not by geometry.

The `FieldPattern` / `AttackPattern` vocabulary of `cardgame-ref` (Single,
Cleave, Splash, Range, and the board-wide patterns) is reused, rebased from 8
fields to 6. Affected fields must be previewable before a command is submitted.

> **Open (§15, *Barde*):** is the Barde's heal, cleanse and buff an attack
> replacement, an on-play effect, or something that happens every turn?

### 8.3 Destruction

There is exactly one way for a card to leave the board: it is **destroyed**.
Whether it fell to 0 health or was removed by an effect makes no difference,
and there is no graveyard as a zone.

Consequences:

- **Bounty is always paid** to the opponent when a card is destroyed.
- `OnDeath` triggers fire on destruction, whatever the cause.
- Reborn and recursion effects simply read a **list of destroyed cards**.

This deliberately drops the four-way distinction `cardgame-ref` made between
lethal damage, destroy effects, move-to-graveyard and remove-from-game. It was
too complicated for what it bought.

> **One exception exists, and it is post-MVP.** A displaced Ghost (§7.6) does
> not leave the board by being destroyed — it **vanishes**, which is a different
> thing and carries none of destruction's consequences: no Bounty, no kill, no
> entry in the destroyed list, no way back for a reborn effect. So there are two
> ways off the board and only one of them has any consequences. The sentence
> above holds for everything in the MVP; when Ghosts are built, vanishing has to
> be a named state of its own.

### 8.4 Thrown cards

Some cards are **thrown** rather than played onto the board. Playing one is two
choices in order:

1. pick one of your **own units** as the **launch point**,
2. pick a target field on the opponent's side.

**Throwing uses Radius, not Range** (§8.1). A thrown card may therefore land on
the thrower's **own** field or anywhere on their own side, which is how a board
is swept clear of something the opponent put there.

**Throwing is an ability of the unit's type, not of the card.** The card is
asked what it does on impact; the launch unit is asked how far it can throw it.
A unit's own *attack* range has nothing to do with it — a Rogue at +0 throws as
far as his type allows, not as far as he stabs.

| Type | Throwing |
|------|----------|
| Human — Warrior, Hammerer | +1 Radius |
| Goblin — Rogue, Rogue ♀ | −1 Coin on the throw's cost |
| Ghost | Radius 0 — it can only drop a card on its own field |
| Puppet | Radius 0 |
| all others | base radius, base cost |

Radius 0 is a value, not a prohibition: a Ghost still throws, it simply throws
at its own feet. Bombing itself to clear the Walls around it is a real play, and
one that looks absurd until it wins a board. Two cards that appear to have
nothing to do with each other turning out to combine is the kind of thing this
world is named for.

> **Modelling note:** Human carries a *modifier* (+1) while Ghost and Puppet
> carry an *absolute* (0). No unit has two types, so the two never collide
> today; if they ever do, decide then whether 0 is an override or a cap.

#### The Bomb

Costs **Coins**, not mana (§7.1). Base Radius 3, spreading to −1/+1 around the
target. Its use against totems, and why the spread ignores the diagonal
surcharge, is worked through in §8.1.1.

**The Human +1 does not extend the Bomb's reach against totems** — and that is
deliberate. The totem row collapses Radius 3 and Radius 4 onto the same offset
of 2, so a Human launch point changes the aimable totem columns at no slot on the
board; only the front-row reach grows. The Bomb would be too strong otherwise.
This is a secret that is meant not to weigh much.

It is worth knowing that this property is *emergent*, not written: it falls out
of `floor(2R/3)` being equal for 3 and 4. Change the surcharge formula and this
balance decision silently changes with it, so it needs a test that pins it,
not a comment.

#### The Stone

The canonical demonstration that a higher tier is not a better tier (§7.3).

| Tier | Cost | Effect |
|------|------|--------|
| 1 | free | low damage, no Bounty |
| 2 | free | more damage, but carries **Bounty** — the opponent receives the Coins the moment it is thrown |
| 3 | mana | **Magic Stone**: more damage |

Tier 1 is free and clean. Tier 2 is free and *funds the opponent*. Tier 3 costs
the resource the rest of the turn wants. None of the three is simply the best,
and choosing between them is the whole exercise.

#### The Wall — **fragment**

Not a thrown card, but the reason thrown cards can aim at one's own side: a Wall
occupies a slot, a player may find one sitting on their own board, and clearing
it is what a Bomb aimed at one's own field is for.

Beyond that, deliberately unwritten. It serves **primarily as a blockade**, and
everything else — which side it may be placed on, what it costs, what its tiers
give, whether a unit can displace one, and whether a Wall in a column counts as
protection for the totem behind it (§4) — is open (§15, *The Wall*).

An earlier draft of this section specified all of it. It was withdrawn as
premature rather than kept as a guess: a rule that looks decided is worse than a
gap, because nobody asks about it again.

## 9. Status effects

Three status effects exist today, plus Protect. They differ in their clock, and
that difference has to be exact in the simulation: with alternating turns a
round is two turns, so "per turn" and "per round" are not the same schedule.
Each effect names its checkpoint.

**Bleed** is named as a future status effect and is not yet specified.

### 9.1 Stun

Source: Hammerer, on the two units beside its primary target. Duration 1 round.

A stunned unit **cannot attack**, and **protects only its own column** — it no
longer covers the other column of its totem's pair (§4.2).

### 9.2 Poison

Source: Rogue ♀, 2 stacks per hit.

Poison ticks **per round**. At the checkpoint it first deals damage equal to
the current stack count, and only then drops by 1 stack. 4 poison deals 4 and
falls to 3, then deals 3 and falls to 2, and so on.

Total damage from N stacks is therefore N(N+1)/2 — 2 stacks deal 3, 3 deal 6,
4 deal 10, against a maximum health of 9. Poison is meant to be that
frightening; the answer to a large stack is not to out-heal it but to remove it
(§9.5).

### 9.3 Burn

Source: Glavier, exactly 1 stack per attack, by definition — never more. Also
reachable from a Fireball-style ability in hand.

Burn expires after one **turn**. Each stack deals a flat **4 damage**.

Stacking Burn is meant to be hard: two Glaviers on one target, or a Glavier
plus a Fireball. The flat 4 against a maximum health of 9 is what makes a
second stack worth building toward.

Diegetic: a burning shader effect around the attacker's bottom-left attack
wedge.

### 9.4 Protect

Source: Health Mastery, second point (§12). Card effects may also place Protect
on units, so it is not a totem-only property.

Protect is **charge-based, not permanent**. When an attack would land, the
damage is reduced to exactly 1 and the charge is spent. Without a charge the
attack lands in full.

### 9.5 Removing status effects

**Every cure removes the whole stack.** There is no partial cleanse: one
Antidote clears 4 poison exactly as it clears 1. This is the counterplay that
poison's quadratic curve is balanced against, so the real balance lever is how
readily cures are available, not the formula itself (§15, *Damage scale*).

Sources:

- **Barde** — cleanses, alongside healing and buffing adjacent own units.
- **Heal-Kit** — removes nearly every status effect: Burn, Poison, and Bleed
  once it exists.
- **Cream** — either buffs +1 HP, or removes Burn or Bleed and then heals 2.
- **Antidote** — removes Poison and converts it into a damage buff for the
  cured unit.

## 10. Bounty and Coins

**Coins** are the currency. A **Bounty** is an amount of Coins — the two are the
same thing named from two sides, not two resources.

A card's Bounty is paid **to the opponent**:

- for a unit on the board, when it is destroyed (§8.3),
- for a card that is used and consumed, **immediately on use** — the Tier 2
  Stone hands the opponent its Coins the moment it is thrown (§8.4).

Bounty is therefore a genuine cost of playing a strong or cheap card, and it is
the only value on the card that the *owner* does not want to be high.

Coins are kept — they do not refresh each round. They are spent at the Merchant
(§11) and on cards that cost Coins instead of mana, such as the Bomb (§8.4).

Bounty is inert until the Merchant exists, but it is part of the card surface
from the first card that is drawn, because the glyph must be read and tested
alongside the other three.

## 11. The Merchant

At a random round a merchant appears for both players. The Coins earned from
destroying enemy cards buys cards, healing applied directly to the board, or
services such as returning a card to hand.

The Merchant is a scheduled phase with its own short clock, not a real-time
race. The player with more Speed decides first; after that, first choice
**alternates**, so that Speed is an advantage and not a monopoly.

The phase model of `cardgame-ref` (§21 there) is reused largely unchanged.

## 12. Mastery Stats — **EXPERIMENTAL**

The only decision a player makes before the match. **6 points**, distributed
across **4 stats**, at most **3 points per stat**.

Each point is a distinct effect, not a scaling number. The third point in a
stat is usually a different kind of thing from the first two.

### Health — all effects apply to the Totem of Life

| Point | Effect |
|-------|--------|
| 1 | +2 HP |
| 2 | **Protect**, one charge — the next incoming attack is reduced to exactly 1 damage and the charge is spent (§9.4) |
| 3 | **Death protection, once** — on lethal damage the totem is restored to 2 HP instead of being destroyed |

### Mana

| Point | Effect |
|-------|--------|
| 1 | +1 mana carried over into the next round |
| 2 | +1 further carry-over (2 total) |
| 3 | **instead** +1 maximum mana — 7 becomes 8 |

### Stamina

| Point | Effect |
|-------|--------|
| 1 | more turn time |
| 2 | more turn time, and takes time from the opponent |
| 3 | **Totemic Taunt** |

**Totemic Taunt:** while the Totem of Time is at **full HP**, every totem
attack must target the Totem of Time. The other two totems cannot be attacked
at all. The moment the Totem of Time takes any damage, the taunt is gone for
the rest of the match.

Example: with the Totem of Time on columns 5–6, the opponent cannot touch the
totems on columns 1–4 — he has to find a way onto columns 5 or 6 first, and the
diagonal surcharge (§8.1.1) makes that a real journey. An Archer at +4 standing
on slot 2 reaches totem columns only up to 4 and cannot touch it at all; he has
to advance to slot 4 or further. A Rogue has to stand on slot 5 or 6 and find
an open column in front of him.

**Stamina 3 also grants placement.** A player at Stamina 3 *chooses* where their
own Totem of Time sits before the match; their other two totems are then rolled
again, in Perfect Mirror as well.
This removes the variance that would otherwise decide how much Totemic Taunt is
worth, and together with the surcharge it lets the player build a genuinely
remote fortress. In Perfect Mirror the choice overrides the mirrored layout for
that player only (§2).

### Speed

| Point | Effect |
|-------|--------|
| 1 | nothing beyond the flag itself: starts the match, and first choice at the Merchant |
| 2 | +1 extra mana at match start |
| 3 | +2 cards in hand at match start |

### Notes

- Speed's second and third points fix the earlier problem that Speed was purely
  relative — that only "more than the opponent" did anything. Points 2 and 3
  now have absolute value regardless of what the opponent took.
- **Totemic Taunt is by a distance the strongest single point in the game.** It
  gates the win condition behind one totem, the placement lets the player put
  that totem where it is hardest to reach, and the diagonal surcharge makes
  reaching it cost board position. Three rules compound here, and they were
  each decided for good reasons on their own. Acknowledged as such: this is the
  first thing to measure once the stats are playable, and the most likely thing
  to need turning down.
- The exact seconds for Stamina 1 and 2 have to be restated against the 10 + 20
  split in §5.3; the old 20 → 30 → 40 numbers predate it.

The whole section is experimental on purpose and is scheduled late (see
`ROADMAP.md`). Nothing in §3–§11 may assume a particular Mastery configuration;
the base rules must be complete and playable at 0 points in every stat.

## 13. Accessibility

Attack is red and Health is green, in a game with no numbers to fall back on.
That is the classic red/green collision with no safety net — and the Rogue ♀'s
split red/poison-green band puts both colours inside the same wedge.

Planned answer: a **colour-blind mode** that distinguishes the corner wedges by
**hatching pattern** in addition to colour. Because the wedges are already
drawn as bands, hatching is a natural second channel rather than a retrofit.

A debug overlay that prints the real numbers over every card and totem is built
in the same gate as the cards themselves. It is needed for development, it is
what the legibility test is measured against, and it is the fallback of last
resort.

## 14. What is reused from `cardgame-ref`

`cardgame-ref/` is **not part of this repository**. It stays on the developer's
machine as a read-only reference and is excluded by `.gitignore`, so a fresh
clone will not contain it. Everything worth carrying forward is listed below; if
you are reading this without the folder, the list is the whole of it.

**Keep:**

- the layering `Core/Simulation` — `World`, `Commands`, `Events`, `Systems`,
  `Projection` — and the Godot-free core assembly,
- the command → validation → systems → events → snapshot flow,
- `CardInstance` with stable entity IDs,
- the Merchant phase model with alternating priority,
- `FieldPattern` / `AttackPattern` and targeting previews, rebased to 6 slots,
- the lobby systems, for the infrastructure gate,
- the server-authoritative, headless-first, test-first principle.

**Discard:**

- Lineages, Archetypes, Relics, Deck Powers,
- the five card kinds (Magical/Physical Abilities, Items, Objects),
- 8 fields per side and the single-totem win condition,
- field biomes,
- the 12-point stat allocation,
- the 1–12 value glyph system, replaced by §7.2,
- free deckbuilding — the mirror deck replaces it,
- **the four-way distinction between lethal damage, destroy, move-to-graveyard
  and remove-from-game**, and the graveyard as a zone. Only "destroyed"
  remains (§8.3).

## 15. Open questions

Carried deliberately, to be answered at the gate that needs them
(see `ROADMAP.md` §2). Referenced by name rather than by number, so that
answering one does not renumber the rest.

- **Stamina seconds.** Points 1 and 2 restated against 10 s + 20 s; the old
  20 → 30 → 40 numbers predate the split.
- **Barde.** Is the heal, cleanse and buff an attack replacement, an on-play
  effect, or a per-turn one? Which units count as adjacent?
- **Taunt precedence.** A unit Taunt forces the three opposing columns onto the
  taunter, which stops them attacking any totem; Totemic Taunt restricts which
  totem may be attacked. The reading is that they never conflict because they
  constrain different target classes — confirm it.
- **The Wall.** Its whole specification (§8.4) beyond "primarily a blockade":
  which side it may be placed on, cost and tiers, whether a unit can displace
  one, and whether it counts as totem protection in its column (§4).
- **Vanishing as a state.** Post-MVP, but on record: displacing a Ghost is a
  second way off the board that is not a destruction, and it needs a name of its
  own in the model (§8.3, §7.6).
- **The Archer's type.** The only named unit still without one (§7.6).
- **Units for Ghost, Nature, Puppet and Bird**, and rules for Wild and Kobold —
  after the MVP.
- **Damage scale.** Poison is quadratic and Burn is a flat 4, both against a
  maximum health of 9, with full-stack cures as the counterplay (§9.5). The
  lever is how readily cures appear in a deck — a tuning pass once combat and
  the cure cards are both playable.
- **Bleed.** Named as a future status effect; clock, damage and sources unspecified.
- **Protect on units.** Which cards or effects grant it, and how many charges.
- **Deck size and card pool size.** No fixed minimum or maximum yet.
- **Tick checkpoints.** Exactly where in the turn and round the poison and burn
  ticks resolve.
- **The eighth mana point.** How maximum mana of 8 is shown on a totem with
  seven segments.

## 16. Assets

The unit and totem geometry already exists in PolyTools under
`worlds/world01/geometry/`: `Totem_of_Life`, `Totem_of_Mana`, `Totem_of_Time`,
and the cast — `Rogue`, `Wizard`, `Warrior`, `Sorcerer`, `ArcherF`, `Glavier`,
`Mage`, `Barde`, `Monk`, `Hammerer` — plus item shapes such as `Heart`,
`Potion`, `Orb`, `Bomb`, `Dagger`, `Bow`, `Arrow`, `Ankh`, `Lightning_Bolt`.

The exported totems carry 11 (Life), 8 (Mana) and 9 (Time) components against a
design of body + 7 segments. Reconcile before the totem presentation gate: the
segment count is a gameplay value, not only a model detail.
