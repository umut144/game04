shader_type canvas_item;

// Shows only what lies below `cutoff`, in the asset's own meters (UV carries
// the mesh position): the Totem of Time drains from the top (G04-05).
uniform float cutoff = 1000.0;

void fragment() {
	if (UV.y > cutoff) {
		discard;
	}
}
