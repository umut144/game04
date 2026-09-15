# game04: Pivot-Formel im Runtime Export Contract

Die Zeile `component_transform: "T(position) * R(rotation) * S(scale) * T(-pivot)"`
liest sich wie eine Anweisung an den Konsumenten. Die exportierten Vertices
haben den Component-Pivot aber schon abgezogen — wer `T(-pivot)` noch einmal
anwendet, verschiebt alles.

Beispiel `card`, `mana_glyph03`: position (−0.275, 0.425), component_pivot
(−0.275, 0.875), Vertices ±0.025 um (0, 0), asset_pivot (0, −0.45).

- Formel wörtlich: Glyph landet bei (0, 0), unten mittig auf der Karte.
- Ohne `T(-pivot)`: (−0.275, 0.875), oben links — richtig.

game04 rechnet deshalb `Parent-Transforms · T(position) · R · S · vertex −
asset_pivot`, ohne den Component-Pivot; Tests gegen die echte Karte bestätigen
das.

Bitte:

1. Die Formel im Contract klarstellen, z. B.: „Eine Komponente wird mit
   `T(position) * R(rotation) * S(scale)` auf ihre exportierten Vertices
   gesetzt; der Component-Pivot ist in den Vertices bereits abgezogen.“ Gleiches
   für `closed_region_mesh`, `contour_stroke_mesh` und
   `projection_depth_corners`.
2. Hinweis für world01: Dessen Renderer (`apps/client/src/polytools.rs`) liest
   noch das alte Feld `local_pivot`, das Schema 23 nicht mehr hat, zieht also 0
   ab und liegt damit nur zufällig richtig. Wer dort auf `component_pivot`
   umstellt, verschiebt alle Figuren.

Kein Datenfehler — nur die Beschreibung ist missverständlich.
