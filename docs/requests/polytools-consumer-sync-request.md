Subject: game04 needs Consumer Sync too

Hey — game04 needs the `card` and the three totem props synced from
PolyTools, same as world01 already gets. Two things:

1. It needs its own sync script, fully separate from world01/SceneMaker's
   `sync_world01_consumers.sh` — no dependency either direction. If world01
   or SceneMaker are broken or missing, game04's sync should still run; and
   game04 syncing shouldn't touch or block them either.

2. Shape-wise, just copy what world01's `sync_polytools_characters.sh`
   already does: read `POLYTOOLS_WORLD_DIR/catalog.json`, validate schema,
   check our own design-data keys resolve, validate each manifest, atomic
   swap into `game04/assets/...`. Nothing new to invent — a game04 copy of
   that script plus its own tiny orchestrator.

Open questions before starting:

- Does game04 get its own PolyTools World, or keep sharing `worlds/world01/`
  where `card`/`totem_of_*` already live?
- Is SceneMaker Godot? If so its render-from-manifest code might save us
  writing our own from scratch.
- The totems currently have the wrong segment count (11/8/9 vs. the 7 the
  design wants — tracked as `ASSET-01`). Fine to sync now and re-sync once
  that's fixed.

That's it — just wire game04 in as a second, independent consumer.
