# Request: PolyTools Consumer Sync for game04

Author: Umut (via Claude, game04 session), 2026-09-15
Audience: PolyTools maintainer (i.e. the same developer, wearing the PolyTools hat)
Status: approved by the developer, 2026-09-15 — ready to act on. The open
questions at the end still need an answer before implementation starts, but
the request itself is final.

## Why

`card` and the three `totem_of_life` / `totem_of_mana` / `totem_of_time`
props already exist as authored, exported PolyTools Assets — they are
published in `worlds/world01/catalog.json` today, with current
`PolyToolsRuntimeExports/<asset_key>/manifest.json` packages (schema 22).
game04 (the Godot/C# card game) wants to render them, but game04 is not a
Consumer Sync target: `scripts/sync_world01_consumers.sh` only knows about
world01 and SceneMaker, by name and by hardcoded relative path
(`../../BevyProjects/world01`, `../SceneMaker`). There is no equivalent step,
and no `POLYTOOLS_WORLD_DIR`-reading script, on the game04 side.

**game04 must not depend on world01 or SceneMaker in any direction.** When
PolyTools has exported, game04 should be able to sync directly — its own
sync must not be gated on world01's or SceneMaker's steps, and it must not
abort or skip just because world01 or SceneMaker are missing, broken, or
fail their own sync. This is a hard requirement, not a preference: the two
games share nothing except the PolyTools Catalog they both read.

## What Consumer Sync already does, for reference

Read directly from `scripts/sync_world01_consumers.sh` and
`BevyProjects/world01/scripts/sync_polytools_characters.sh`, so this section
is a description of the current mechanism, not a guess:

- The sync is a 4-step, dependency-ordered batch, not a flat sequence. Two
  steps are independent siblings (both need only the published Catalog:
  "PolyTools -> world01 content" and "PolyTools -> SceneMaker"); a third step
  re-exports SceneMaker's own scene once its catalog copy is current; a
  fourth step ("SceneMaker -> world01 map") waits on both the first and the
  third. A step that is missing its input is skipped and says which step it
  was waiting for, rather than the whole run stopping. Nothing is atomic
  end-to-end — each step commits as it goes — so the report always names
  which consumer ended up on new content and which is still on its previous
  state.
- The world01 asset-content step (`sync_polytools_characters.sh`) is a good
  template for what a game04 step would need to do:
  1. Read `${POLYTOOLS_WORLD_DIR:-.../worlds/world01}/catalog.json`.
  2. Validate the catalog's own shape with `jq` (schema_version == 3, every
     asset has a non-empty `asset_key`/`asset_type`/`runtime_package`/
     `asset_id`, `asset_type` is one of the known categories, `asset_id`s are
     unique, retired assets don't collide with live ones, `asset_key`s are
     unique).
  3. Cross-check that every asset_key the *consumer's own hand-authored
     design data* names still resolves in that catalog — world01 pulls this
     list from its `crates/design/*.json`/`*.toml` files. A name that no
     longer resolves gets a specific diagnosis (renamed to X, retired as
     asset Y, or never existed), not a generic "not found".
  4. Per referenced asset, validate the actual `manifest.json` against a
     fairly detailed `jq` contract (schema_version == 22, component/region
     shape, asset_reference components resolving to a real asset_id+asset_key
     pair, category-specific rules for `set`/`palette`, and a couple of
     per-asset-type rules such as required attachment_frame roles on the
     hammer weapon).
  5. Stage everything into a fresh temp directory, then swap it into place
     atomically (old content moved to a backup dir first, removed only after
     the swap succeeds), split both by asset type (`characters/`, `props/`,
     `weapons/`, ...) and as one flat `assets/catalog.json`.
- The contract itself (`docs/RUNTIME_EXPORT_CONTRACT.md`) is engine-neutral:
  meters, Y-up, `T(position) * R(rotation) * S(scale) * T(-pivot)`, one Tool
  unit = 0.1 m, Component meshes always at `scale = [1,1]` after Scale
  Rebase. Nothing in the contract assumes Bevy or Rust.

## What's already true for the two game04 assets

- `card` (props/single, `asset_51`) is a flat single-body prop with glyph
  sub-components (mana/bounty/health/attack glyphs x3 each) — no nested
  segment transforms to speak of. A rough check of its authored geometry
  (untransformed component vertices only, not a rigorous bounding box) came
  out close to the stated 60x90 cm card footprint, which is reassuring but
  not a substitute for reading the real bounding box once a consumer
  actually needs an exact number.
- `totem_of_life` / `totem_of_mana` / `totem_of_time` (props/single) are the
  right *kind* of asset, not a naming coincidence: `totem_of_life`'s
  components are a `body` plus several numbered `segmentNN` children laid
  out along local X, which matches game04's own model of a totem as a
  segmented HP/supply track rather than a flat icon. This is already a
  tracked mismatch on the game04 side, not a new finding: `docs/TASKS.md`
  `ASSET-01` (blocked on G06) notes that `GAME_DESIGN.md` §5 expects body +
  **7** segments per totem, while the current exports carry 11 (Life), 8
  (Mana) and 9 (Time) — Mana is the only one that already matches. That
  reconciliation is out of scope here (G06, not G01), but it does mean the
  totem's *authored bounding box* will shift once segment counts are
  corrected, so the game04 board-layout math (see `docs/TASKS.md`'s G01
  thread) treats the declared 160x128cm footprint as the target ratio to
  design the grid around, not as a measurement of the current export.

## The concrete ask

Give game04 its own Consumer Sync path, independent of
`scripts/sync_world01_consumers.sh` end to end — not a new step wedged into
that script's dependency chain, since that script is world01/SceneMaker's
own orchestrator by name and by design, and wiring game04 into it would be
exactly the coupling that must not exist. Concretely:

1. A new, standalone orchestrator script, e.g. `scripts/sync_game04_consumers.sh`,
   living beside (not inside) `sync_world01_consumers.sh`. Its only step —
   "PolyTools -> game04 content" — needs nothing but the published Catalog,
   so it has no dependency on world01's content step, SceneMaker's catalog
   step, or SceneMaker's scene export, and its failure or absence must not
   affect them either. This mirrors the independent-sibling shape
   `sync_world01_consumers.sh` already uses for its own first two steps, just
   without sharing a run with them.
2. Whatever triggers PolyTools's export today (the `Sync Consumers` action in
   the Runtime Export workspace) should run *both* orchestrators — or
   `sync_game04_consumers.sh` gets its own trigger — but either way, one
   failing must never block or skip the other. If it's simpler to keep a
   single "Sync Consumers" button, it should call each orchestrator and
   report both results independently, the same way `sync_world01_consumers.sh`
   already reports per-step rather than failing as one lump.
3. A game04-side sync script (e.g. `game04/scripts/sync_polytools_props.sh`,
   invoked as `env POLYTOOLS_WORLD_DIR=<world dir> <script>` by the new
   orchestrator), shaped like `sync_polytools_characters.sh` but for a
   Godot/C# consumer instead of Bevy/Rust: catalog + manifest validation
   mirroring its `jq` contract (schema_version checks, asset_id/asset_key
   resolution, region/component shape) — game04 doesn't need the
   character-specific attachment-frame rules, but does need whatever
   `props`-category rules apply.
4. A game04-side design-data asset-key list to cross-check against, the
   equivalent of world01's `crates/design/*.json` scrape. game04's own
   `design/cards/*.json` schema (see `docs/TASKS.md`, decision CORE-08) would
   need a field naming the PolyTools `asset_key` a card's visual art comes
   from, once that's added — this doesn't exist yet and is new scope on the
   game04 side, not just the PolyTools side.
5. Staged output the Godot client can actually load — most likely
   `game04/assets/props/<asset_key>/manifest.json` plus a flat
   `assets/catalog.json`, matching world01's `assets/<type>/<key>/` shape,
   but the exact directory convention is PolyTools's call since it already
   owns this pattern for two consumers.
6. Something on the Godot side has to turn a manifest's 2D vector geometry
   (`contour_stroke_mesh`, `closed_region_mesh`, per-component `mesh`) into
   actually-rendered Godot nodes (`Polygon2D`/`MeshInstance2D`, or a custom
   importer). World01's Rust-side interpreter
   (`crates/content/src/manifest.rs`, `derived.rs`) can't be reused directly
   from C#. **If SceneMaker is itself a Godot project**, its own consumer
   script (`sync_polytools_world.sh` + however it renders what it receives)
   is a much closer precedent than world01's Rust code and worth comparing
   before game04 writes its own manifest interpreter from scratch — this
   wasn't checked yet because SceneMaker wasn't part of this session. (This
   would only be a *precedent to read*, not a dependency: game04 still ends
   up with its own script and its own copy of whatever logic it needs.)

## Open questions (need an answer before scoping this properly)

- **Which PolyTools World holds game04's assets?** `card` and the three
  totems currently live in `worlds/world01/` only because that was the only
  World that existed when they were authored. Does game04 get its own
  `worlds/game04/`, or does it keep sharing `worlds/world01/` as a general
  asset library? This changes what `POLYTOOLS_WORLD_DIR` the new script
  defaults to and whether "world01" the PolyTools World and "world01" the
  Bevy game need to be told apart more explicitly going forward.
- **Should Consumer Sync wait for `ASSET-01`'s segment-count reconciliation**
  (7 segments per design, vs. today's 11/8/9), or sync the totems as they
  are now and let a later re-sync pick up the corrected geometry? Since
  `ASSET-01` is blocked on G06, syncing now and re-syncing later is likely
  fine — Consumer Sync already treats "current content" as a moving target
  the client re-pulls, not a one-time snapshot.
- **Is SceneMaker a Godot project**, and if so, does its existing sync/render
  path already solve "manifest -> Godot node" in a way game04 can reuse
  instead of writing a second implementation of the same problem?
- **Where does the game04-side asset-key list live** once
  `design/cards/*.json` grows a PolyTools-asset reference field — is that
  field mandatory per card (every card has art) or optional (some cards
  ship without art initially)?
