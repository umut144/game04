# design/

Data-driven card and ability content (G00 spec round, CORE-07/CORE-09).
Behaviour stays in `Cardgame.Core`; these files only ever carry data plus the
*keys* that name behaviour, per AGENTS.md ("card behaviour is data, never a
per-card script").

Loaded and validated by `Cardgame.Core.Design.DesignCatalogLoader`, which is
also the schema documentation: see that type and `Cardgame.Core.Design.CardDesign`
/ `AbilityDesign` for the exact shape and `schema_version` currently
understood.

Layout, once populated:

- `cards/<id>.json` — one file per card. Every card names three tiers
  (GAME_DESIGN.md §7.3), the ability name keys it plays, and `asset_key`, the
  PolyTools character drawn on it.
- `abilities/<name-key>.json` — one file per ability. A card's
  `ability_name_keys` must each resolve to a file here; the loader rejects a
  card that references one that does not exist.
- `decks/<id>.json` — a deck as card ids with counts; `starter.json` is the
  20-card mirror deck both sides play until deck composition exists (G10).
- `asset_keys.json` — the PolyTools asset keys game04 uses that no card
  names itself: the card frame (`card`) and the three totems. It is a
  reference, not a copy — the vector data lives in
  `src/Cardgame.Client/assets/polytools/`, written only by the PolyTools sync.
  The sync *reads* this file (and, from G02 on, each card's own asset key in
  `cards/*.json`) to refuse an update in which PolyTools has renamed or
  withdrawn something game04 uses; it never writes into `design/`. Not read by
  `DesignCatalogLoader`.
- `asset_presentation.json` — how game04 shows each of those assets: constant
  x/y factors applied to PolyTools's geometry (PolyTools is the source of
  truth; the scaled result is game04's), and the fill and stroke colours, since
  the manifests carry none. `cell_fill` (0.96) is how much of its board cell a
  card or totem fills, drawn centred. Read by `Cardgame.Assets`
  (`BOARD_DESIGN.md`).

The cards and abilities are G02's first set: rough values to get moving, no
balance claim (`docs/TASKS.md`, `G02-01`).
