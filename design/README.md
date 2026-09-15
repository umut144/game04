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

G00 ships this pipeline empty on purpose. `cards/` and `abilities/` do not
exist yet: Rogue and Wizard are the first real content, and they are G02
scope (ROADMAP.md), not G00's. `Cardgame.TestSupport.TestCardDesigns` holds
fixture cards for G00's own tests instead, clearly named so nobody mistakes
them for balance data.
