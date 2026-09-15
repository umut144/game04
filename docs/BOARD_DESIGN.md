# Board Design — layout geometry

**Status:** presentation reference, not rules. The board's *topology* — 6 unit
slots per side, 3 totems per side each spanning 2 columns, adjacency — is
`GAME_DESIGN.md` §3; this document is only how that topology is measured out
on screen. Nothing here constrains or is constrained by simulation code:
`Cardgame.Core.Board.BoardSide` knows slot counts and totem places, never
pixels.

Implemented in `src/Cardgame.Client/Presentation/BoardLayoutSpec.cs`, which
holds these numbers as anchor fractions (0..1 of the viewport), not raw
pixels, so the layout carries over unchanged at any resolution sharing the
same 16:10 aspect. The pixel figures below are against a 1920×1200 reference
viewport — exactly 3/4 of the 2560×1600 design viewport named in
`ROADMAP.md`, same aspect, same fractions.

## The two footprints

Two props set the whole grid: the Card and the Totem. Only their *aspect
ratio* matters here — the centimetre figures fix a shape, not an on-screen
scale.

| Prop | Declared footprint | Ratio |
|---|---|---|
| Card | 60cm × 90cm | 2:3 |
| Totem | 160cm × 128cm | 5:4 |

## The grid, step by step

**1. Proportions.** Worked out on a 1200-wide field: 6 equal unit columns of
**200**; a totem cell is always exactly 2 columns, **400** — `board_field.JPG`'s
grid lines run continuously from the totem row into the unit rows, so the two
widths cannot be chosen independently. Row heights are then chosen so each
footprint fits with zero distortion:

- A unit row is **300** tall. A 200×300 slot holds the card's 2:3 exactly — no
  padding, no overlap.
- A totem row is **240** tall. At 5:4 the totem is **300** wide inside its
  400-wide cell, leaving a deliberate, centred **50** on each side.

Two totem rows and two unit rows make the field **1200×1080**.

**2. Scale to the full height.** The board has no top or bottom margin: the
1080-tall field is scaled by **1200 / 1080 = 10/9 ≈ 1.111** so that it
touches both edges, and stays centred horizontally. Everything keeps its
proportions; the side margins shrink from 360 to 293.3.

In general, for a top/bottom margin *m*: factor = (1200 − 2*m*) / 1080.

```text
Opponent totems  266.7   [    444.4    ][    444.4    ][    444.4    ]   totem 333.3×266.7, 55.6 free either side
Opponent units   333.3   [222.2][222.2][222.2][222.2][222.2][222.2]      card 222.2×333.3, fills its slot
                          ─────────────────── mid-line ───────────────────
Own units        333.3   [222.2][222.2][222.2][222.2][222.2][222.2]
Own totems       266.7   [    444.4    ][    444.4    ][    444.4    ]
                 ◀──────────────────── 1333.3 ────────────────────▶
                 293.3 margin on the left and on the right; none at top or bottom
```

## Final numbers (1920×1200 reference)

| Quantity | Pixels | Fraction of viewport |
|---|---|---|
| Side margin (left/right) | 293.3 each | 11/72 of the width |
| Top/bottom margin | 0 | — |
| Unit column width | 222.2 | 25/216 of the width |
| Totem cell width | 444.4 | 25/108 of the width |
| Unit row height | 333.3 | 5/18 of the height |
| Totem row height | 266.7 | 2/9 of the height |
| Card render size | 222.2×333.3 | fills its slot (2:3) |
| Totem render size | 333.3×266.7 | 3/4 of its cell's width, centred (5:4) |
| Field size | 1333.3×1200 | 25/36 × 1 |

The pixel sizes are not whole numbers at this resolution. That is expected:
the layout is defined by the fractions, and the anchors place everything from
them at whatever the actual window size is.

## Candidate: 70×100 cards (layout 2)

Under comparison with the layout above (`TASKS.md`, `LAYOUT-02`);
`BoardScreen` switches between the layouts with keys 1 to 3.

The cards grow to 70×100 cm — larger, and wider in proportion (7:10 instead
of 2:3) — at the same scale as before (60 cm = 200 px, so 3⅓ px per cm). The
totems deliberately stay as they are. Before scaling, at 1920×1200:

| | Layout 1 (60×90) | Layout 2 (70×100) |
|---|---|---|
| Unit column | 200 | 233.3 |
| Card row = card | 300 (200×300) | 333.3 (233.3×333.3) |
| Totem cell | 400 | 466.7 |
| Totem | 300×240, 50 free each side | 300×240, 83.3 free each side |
| Field | 1200×1080 | 1400×1146.7 |
| Side / top-bottom margin | 360 / 60 | 260 / 26.7 |

The growth comes out of the margins. Scaling to the full height then takes
**1200 / 1146.7 = 45/43 ≈ 1.047** instead of 10/9:

| | Layout 1 | Layout 2 |
|---|---|---|
| Card | 222.2×333.3 | 244.2×348.8 |
| Totem row | 266.7 | 251.2 |
| Totem cell | 444.4 | 488.4 |
| Totem | 333.3×266.7 (75 % of its cell) | 314.0×251.2 (64 % of its cell) |
| Field | 1333.3×1200 | 1465.1×1200 |
| Side margin | 293.3 | 227.4 |
| Fractions | totem row 2/9, card row 5/18, column 25/216 | totem row 9/43, card row 25/86, column 175/1376 |

So the cards gain about 10 % in width and 5 % in height against layout 1,
while the totems end up about 6 % smaller and with more room around them.

## Candidate: margin-sized cards (layout 3)

Like layout 2 — 7:10 cards, totems unchanged at 300×240 before scaling, full
height — plus one more condition: after scaling, each side margin is exactly
one card wide, so the hand fits on the left and the deck on the right.

With a card width *c* before scaling, the field is 6*c* wide and
2·240 + 2·(10/7)*c* tall, and the factor is *f* = 1200 / that height. A side
margin one card wide means 1920 = 6*cf* + 2*cf*, so the final card width
*cf* is 1920/8 = **240**. Solving 240·(480 + 20*c*/7) = 1200*c* gives
**c = 224**, a field height of 1120 and **f = 15/14 ≈ 1.071**.

| | before scaling | after scaling (15/14) |
|---|---|---|
| Unit column = card width | 224 | 240 |
| Card row = card | 320 (224×320) | 342.9 (240×342.9) |
| Totem row | 240 | 257.1 |
| Totem cell | 448 | 480 |
| Totem | 300×240, 74 free each side | 321.4×257.1, 79.3 free each side |
| Field | 1344×1120 | 1440×1200 |
| Side margin | 288 | **240 = one card** |
| Top/bottom margin | 40 | 0 |

At the old scale of 3⅓ px per cm the card is 67.2×96 cm, still 7:10. The
fractions are the cleanest of the three: columns, cards and side margins are
each 1/8 of the width; totem rows 3/14 and card rows 2/7 of the height.

Against layout 2 the cards are slightly narrower (240 instead of 244.2) and
shorter (342.9 instead of 348.8); the totems are slightly larger (321.4×257.1
instead of 314.0×251.2).

`BoardScreen` marks the hand and the deck with one card-sized placeholder in
each margin, level with the own card row, in every layout.

## Notes

- The 5:4 totem ratio is a target for all three totems, not a measurement of
  the exports; the Totem of Time carries one segment more than the other two
  (`GAME_DESIGN.md` §5).
- The art is synced (`SYNC-02`, `src/Cardgame.Client/assets/polytools/`), but
  nothing draws a manifest yet — `BoardScreen` (`src/Cardgame.Client/Presentation/`)
  shows the totems and cards as placeholders at these exact proportions.
