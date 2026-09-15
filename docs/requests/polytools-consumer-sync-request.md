# game04: Entscheidungen zum Consumer Sync

Dein Plan passt, mit diesen Änderungen:

1. **Pfad:** Das Godot-Projekt heißt jetzt `src/Cardgame.Client/` (vorher
   `Cardgame.App` – macOS hat den Ordner als Programm angezeigt). Ziel ist
   `src/Cardgame.Client/assets/polytools/<typ>/<key>/manifest.json` plus
   `catalog.json`.
2. **Umfang:** Keine Auswahlliste. Alle Asset-Typen, aber **nur Singles** –
   Sets und Palettes nicht. Abbrechen, wenn ein Single auf ein Set oder eine
   Palette verweist (die wären dann nicht da).
3. **Key-Prüfung:** Die Keys, die game04 nutzt, stehen in
   `design/asset_keys.json` (`.asset_keys[]`, derzeit `card` und die drei
   Totems). Ab G02 kommt pro Karte ein eigener Key in `design/cards/*.json`
   dazu – das Feld gibt es noch nicht, das Skript sollte es aber später
   leicht mitlesen können. Umbenannt oder zurückgezogen → Abbruch vor dem
   Austausch, mit denselben Meldungen wie bei world01. `design/` wird nur
   gelesen, nie geschrieben.
4. **Commit:** Die gesyncten Dateien werden in game04 committet. Die
   `.gitignore` ignoriert `.polytools-staging.*/` und `.polytools-backup.*/`
   bereits – die Namen also bitte so lassen.
5. **Unverändert:** eigener Orchestrator `sync_game04_consumers.sh`, der
   Button startet beide Skripte als getrennte Prozesse (das zweite läuft
   auch, wenn das erste scheitert), fünf Zeilen in der Checkliste, rot
   sobald eine rot ist. Standardpfad `../game04`, überschreibbar über
   `GAME04_PROJECT_DIR`. World bleibt `worlds/world01/`,
   `POLYTOOLS_WORLD_DIR` überschreibbar. `.gdignore` in den temporären
   Ordnern ist ok. `ASSET-01` ist kein Sync-Thema.

Details stehen in game04s `docs/TASKS.md` unter `SYNC-02`.
