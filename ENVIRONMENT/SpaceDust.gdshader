shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;

// Draws small soft dust specks using the instance colour and opacity.

varying vec4 dust_color;

// =========================================================
// Receives the per-instance colour from the MultiMesh.
// =========================================================
void vertex()
{
	dust_color = COLOR;
}

// =========================================================
// Softens the particle edges without requiring a texture.
// =========================================================
void fragment()
{
	float radius = length((UV - vec2(0.5)) * 2.0);
	float softness = 1.0 - smoothstep(0.15, 1.0, radius);

	ALBEDO = dust_color.rgb;
	ALPHA = softness * dust_color.a;
}