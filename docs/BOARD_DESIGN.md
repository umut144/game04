# Board Design — layout geometry

**Status:** presentation reference, not rules. The board's *topology* — 6 unit
slots per side, 3 totems per side each spanning 2 columns, adjacency — is
`GAME_DESIGN.md` §3; this document is only how that topology is measured out
on screen. Nothing here constrains or is constrained by simulation code:
`Cardgame.Core.Board.BoardSide` knows slot counts and totem places, never
pixels.

Implemented in `src/Cardgame.Client/Presentation/BoardLayoutSpec.cs`, which
holds the layout as anchor fractions (0..1 of the viewport), not raw pixels,
so it carries over unchanged at any resolution sharing the same 16:10 aspect.
The pixel figures below are against a 1920×1200 reference viewport — exactly
3/4 of the 2560×1600 design viewport named in `ROADMAP.md`, same aspect, same
fractions. Settled as `LAYOUT-01` in `TASKS.md`.

## The layout

The field fills the full height and is centred horizontally. Each side margin
is exactly one card wide, so the hand fits on the left and the deck on the
right.

```text
            ◀ 240 ▶◀──────────────────── 1440 ────────────────────▶◀ 240 ▶
Opp. totems  257.1         [     480     ][     480     ][     480     ]        totem 321.4×257.1, 79.3 free either side
Opp. cards   342.9         [240][240][240][240][240][240]                       card 240×342.9, fills its slot
                           ──────────────── mid-line ────────────────
Own cards    342.9  [Hand] [240][240][240][240][240][240] [Deck]                one card in each side margin
Own totems   257.1         [     480     ][     480     ][     480     ]
```

| Quantity | Pixels (1920×1200) | Fraction of viewport |
|---|---|---|
| Side margin (left/right) | 240 each = one card | 1/8 of the width |
| Top/bottom margin | 0 | — |
| Unit column = card width | 240 | 1/8 of the width |
| Totem cell | 480 | 1/4 of the width |
| Card row = card height | 342.9 | 2/7 of the height |
| Totem row | 257.1 | 3/14 of the height |
| Card | 240×342.9 | fills its slot, 7:10 |
| Totem | 321.4×257.1 | 300/448 of its cell, centred, 5:4 |
| Field | 1440×1200 | 3/4 × 1 |

The pixel sizes are not all whole numbers at this resolution. That is
expected: the layout is defined by the fractions, and the anchors place
everything from them at whatever the actual window size is.

## Asset scale: PolyTools's proportions, game04's size

PolyTools's geometry is the source of truth, and each consumer scales it for
its own purpose — SceneMaker scales the totems up by 5 to use them as MOBA
buildings. game04 applies constant x/y factors per asset from
`design/asset_presentation.json`, and the result is game04's own truth:

| Asset | PolyTools (fill) | Factors x / y | In game04 |
|---|---|---|---|
| Card | 60 × 90 cm | 1.12 / 16⁄15 | 67.2 × 96 cm, 7:10 |
| Totem footprint | 160 × 128 cm | 9⁄16 / 9⁄16 | 90 × 72 cm |
| Totem of Life | 105 × 128 cm | 9⁄16 / 9⁄16 | 59.1 × 72 cm |
| Totem of Mana | 150 × 128 cm | 9⁄16 / 9⁄16 | 84.4 × 72 cm |
| Totem of Time | 155 × 128 cm | 9⁄16 / 9⁄16 | 87.2 × 72 cm |

The card is stretched on purpose: the card slots are 7:10, the PolyTools card
is 2:3. The totems keep their proportions.

One factor then turns game04 meters into pixels for the whole board: a card
fills one column, so pixels per meter = column width / 0.672 m — 357.1 at the
1920×1200 reference. At that factor a card is 240 × 342.9 and every totem is
257.1 tall, exactly its row, standing bottom-centre in its cell (Life 211,
Mana 301, Time 311 wide; the footprint 321.4). Both anchors, the card's and the
totems', are their bottom centre.

A component's position is built by chaining the transforms of its parents,
then its own position, rotation and scale; the asset pivot is subtracted last.
The exported vertices already have the component pivot taken off, so it is not
subtracted again (`Cardgame.Assets.AssetGeometry`, tested against the synced
card: its corner glyphs land in its corners).

Colours, from the same file: cards have crystal-blue strokes, totems strokes in
their own colour — Life bordeaux, Mana sky blue, Time emerald — and everything
is filled paper white.

## Breathing room

Cards and totems are drawn at **96 %** of the size above, centred in their
cells (`cell_fill` in `design/asset_presentation.json`), so their outlines do not sit on the cell
edges. The grid itself does not change. At the 1920×1200 reference:

| | cell | drawn at 96 % | free on each side |
|---|---|---|---|
| Card (also hand and deck) | 240 × 342.9 | 230.4 × 329.1 | 4.8 left/right, 6.9 top/bottom |
| Totem | row 257.1 tall | 246.9 tall (Life 202.5, Mana 289.3, Time 298.9 wide) | 5.1 top/bottom |

The outline is drawn centred on the fill's edge, so it reaches about 4 px past
a card's fill left and right and 2 px past a totem's; two cards side by side
keep roughly 1.5 px between their outlines.

## The footprints

Only the *aspect ratios* of the two props set the grid; the centimetre
figures fix a shape, not an on-screen scale.

| Prop | Footprint | Ratio |
|---|---|---|
| Card | 67.2cm × 96cm (see below) | 7:10 |
| Totem | 160cm × 128cm | 5:4 |

A totem cell is always exactly 2 unit columns wide: `board_field.JPG`'s grid
lines run continuously from the totem row into the unit rows, so column width
and totem-cell width cannot be chosen independently.

## How the layout was found

Every step was worked out before scaling, on the 1920×1200 reference, and then
scaled so the field touches the top and bottom edge.

**1. 60×90 cards, 60px top and bottom.** A 1200-wide field of six 200-wide
columns; totem cells 400. Card rows 300 tall hold a 200×300 card (2:3)
exactly; totem rows 240 tall hold a 300×240 totem (5:4) with 50 free on each
side. Field 1200×1080: 360 margin left and right, 60 top and bottom.

**2. No top or bottom margin.** The whole field scaled by 1200/1080 = 10/9:
cards 222.2×333.3, totems 333.3×266.7, side margins 293.3. In general the
factor is 1200 divided by the unscaled field height.

**3. Larger, wider cards.** Cards changed to 70×100 cm — larger and wider in
proportion (7:10 instead of 2:3) — at the same scale as step 1 (60 cm =
200 px). The totems deliberately stayed at 300×240. Before scaling: columns
233.3, card rows 333.3, field 1400×1146.7, the growth taken out of the
margins. Scaled by 45/43: cards 244.2×348.8, totems 314.0×251.2, side
margins 227.4. Preferred over step 2 on the client.

**4. Side margins exactly one card wide — the chosen layout.** Step 3's rules
plus one more condition: after scaling, each side margin holds exactly one
card, for the hand and the deck. With a card width *c* before scaling, the
field is 6*c* wide and 2·240 + 2·(10/7)*c* tall, and the factor is
*f* = 1200 / that height. Six cards in the field and one in each margin fill
the width, so 8·*cf* = 1920 and the final card is **240** wide. Solving
240·(480 + 20*c*/7) = 1200*c* gives:

| | before scaling | after scaling (15/14 ≈ 1.071) |
|---|---|---|
| Unit column = card width | 224 | 240 |
| Card row = card | 320 (224×320) | 342.9 (240×342.9) |
| Totem row | 240 | 257.1 |
| Totem cell | 448 | 480 |
| Totem | 300×240, 74 free each side | 321.4×257.1, 79.3 free each side |
| Field | 1344×1120 | 1440×1200 |
| Side margin | 288 | 240 = one card |
| Top/bottom margin | 40 | 0 |

At step 1's scale the card is 67.2×96 cm, still 7:10. Chosen because the
widths come out exactly — columns, cards and margins are all 1/8 of the
width — and because the totems end up slightly larger than in step 3
(321.4×257.1 against 314.0×251.2), which reads right next to the larger
cards.

## Notes

- The 5:4 totem ratio is a target for all three totems, not a measurement of
  the exports; the Totem of Time carries one segment more than the other two
  (`GAME_DESIGN.md` §5).
- `BoardScreen` (`src/Cardgame.Client/Presentation/`) draws the synced art
  (`SYNC-02`) for totems, cards, hand and deck. It reads the files from disk
  through the project folder, which works in the editor but not yet in an
  exported build.
