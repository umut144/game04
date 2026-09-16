shader_type canvas_item;

// Diagonal stripes in the asset's own meters (UV carries the mesh position):
// lost health is drawn hatched (G05-04).
uniform float spacing = 0.025;
uniform float width = 0.45;

void fragment() {
	float band = fract((UV.x + UV.y) / spacing);
	if (band > width) {
		discard;
	}
}
