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
  (GAME_DESIGN.md §7.3) and, optionally, the ability name keys it plays.
- `abilities/<name-key>.json` — one file per ability. A card's
  `ability_name_keys` must each resolve to a file here; the loader rejects a
  card that references one that does not exist.
- `asset_keys.json` — the PolyTools asset keys game04 uses that no card
  names itself: the card frame (`card`) and the three totems. It is a
  reference, not a copy — the vector data lives in
  `src/Cardgame.Client/assets/polytools/`, written only by the PolyTools sync.
  The sync *reads* this file (and, from G02 on, each card's own asset key in
  `cards/*.json`) to refuse an update in which PolyTools has renamed or
  withdrawn something game04 uses; it never writes into `design/`. Not read by
  `DesignCatalogLoader`.

G00 ships this pipeline empty on purpose. `cards/` and `abilities/` do not
exist yet: Rogue and Wizard are the first real content, and they are G02
scope (ROADMAP.md), not G00's. `Cardgame.TestSupport.TestCardDesigns` holds
fixture cards for G00's own tests instead, clearly named so nobody mistakes
them for balance data.
