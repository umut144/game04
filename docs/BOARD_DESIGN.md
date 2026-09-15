# Board Design — layout geometry

**Status:** presentation reference, not rules. The board's *topology* — 6 unit
slots per side, 3 totems per side each spanning 2 columns, adjacency — is
`GAME_DESIGN.md` §3; this document is only how that topology is measured out
on screen. Nothing here constrains or is constrained by simulation code:
`Cardgame.Core.Board.BoardSide` knows slot counts and column pairs, never
pixels.

Implemented in `src/Cardgame.Client/Presentation/BoardLayoutSpec.cs`, which holds
these numbers as anchor fractions (0..1 of the viewport), not raw pixels, so
the layout carries over unchanged at any resolution sharing the same 16:10
aspect. The pixel figures below are all against a 1920x1200 reference
viewport, chosen only because the arithmetic is easier at that size — it is
exactly 3/4 of the original 2560x1600 design viewport from the G01 spec
round, same aspect, same resulting fractions.

## The two footprints

Two props set the whole grid: the Card and the Totem. Only their *aspect
ratio* matters here — the centimeter figures fix a shape, not an absolute
on-screen scale, since the actual render size is whatever the grid math below
produces.

| Prop | Declared footprint | Ratio |
|---|---|---|
| Card | 60cm × 90cm | 2:3 |
| Totem | 160cm × 128cm | 5:4 |

## The grid

The field is 1200px wide, centered horizontally in the 1920px-wide viewport
(360px margin left and right, reserved for hand/HUD chrome). Split into 6
equal unit columns, each column is 1200/6 = **200px** wide. A totem cell is
always exactly 2 unit columns — **400px** wide — because `board_field.JPG`'s
grid lines run continuously from the totem row into the unit rows beneath it;
column width and totem-cell width can't be chosen independently of each
other.

Row heights are chosen so each footprint fits its own row with zero
distortion:

- A unit/card row is **300px** tall. A 200×300 cell holds the card's 2:3
  ratio exactly — 200:300 reduces to 2:3 — with no padding and no overlap.
- A totem row is **240px** tall. At the totem's 5:4 ratio that comes out
  **300px** wide inside the 400px-wide cell, leaving a clean, centered
  **50px** margin on each side.

Two totem rows (240 each) and two unit rows (300 each) sum to 1080px, 120px
short of the 1200px-tall viewport. Rather than pad each unit row internally,
that 120px sits *outside* the field as a **60px top and 60px bottom margin**,
symmetric with the 360px side margins.

```text
top margin        60px    ───────────────────────────────────────
Opponent totems  240px    [   400   ][   400   ][   400   ]        3 totem cells, 300x240 each, 50px margin either side
Opponent units   300px    [200][200][200][200][200][200]           6 unit slots, 200x300 each
                           ─────────────────── mid-line ───────────────────
Own units        300px    [200][200][200][200][200][200]           6 unit slots, 200x300 each
Own totems       240px    [   400   ][   400   ][   400   ]        3 totem cells, 300x240 each, 50px margin either side
bottom margin     60px    ───────────────────────────────────────
                  ◀──────────────────── 1200px ────────────────────▶
                  ◀── 360px ──▶                       ◀── 360px ──▶
                  (left margin)                       (right margin)
```

## Final numbers (1920×1200 reference)

| Quantity | Value |
|---|---|
| Side margin (left/right) | 360px each |
| Top/bottom margin | 60px each |
| Unit column width | 200px |
| Totem cell width | 400px |
| Unit/card row height | 300px |
| Totem row height | 240px |
| Card render size | 200×300 (exact 2:3, zero padding/overlap) |
| Totem render size | 300×240, centered in its 400×240 cell (50px margin each side) |
| Field size | 1200×1080 |

Anywhere else at the same 16:10 aspect, multiply by (viewport width / 1920)
— the fractions in `BoardLayoutSpec.cs` do this automatically.

## Open, tracked in `docs/TASKS.md`

- `ASSET-01`: the exported totems currently carry 11/8/9 components against a
  design of body + 7 segments, which will move their authored bounding box.
  The 5:4 target ratio here is what the totem is *meant* to render at once
  corrected, not a measurement of today's export.
- `SYNC-01`: game04 isn't a PolyTools Consumer Sync target yet, so none of
  this renders with real art today — `BoardGridPreview.tscn` shows placeholder
  colored cells at these exact proportions.
