shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;

// Animates thin energy wisps flowing backwards along curved ship ribbons.

uniform vec4 flow_color : source_color = vec4(0.45, 0.9, 1.0, 1.0);

uniform float opacity = 0.0;
uniform float flow_speed = 3.0;
uniform float width_scale = 1.0;
uniform float length_scale = 1.0;

uniform float base_width = 0.22;
uniform float nose_z = -2.6;

// =========================================================
// Widens and lengthens the existing ribbon without rebuilding its mesh.
// =========================================================
void vertex()
{
	VERTEX += NORMAL
		* (UV.y - 0.5)
		* base_width
		* (width_scale - 1.0);

	VERTEX.z = nose_z + (VERTEX.z - nose_z) * length_scale;
}

// =========================================================
// Scrolls narrow, wavering strands from the nose towards the tail.
// =========================================================
void fragment()
{
	float phase = UV.x * 22.0 - TIME * flow_speed * 6.0;

	float centre =
		0.5
		+ sin(phase * 0.65) * 0.1
		+ sin(phase * 1.3 + 2.0) * 0.035;

	float strand = 1.0 - smoothstep(
		0.025,
		0.22,
		abs(UV.y - centre)
	);

	float envelope =
		smoothstep(0.0, 0.12, UV.x)
		* (1.0 - smoothstep(0.65, 1.0, UV.x));

	float pulse = 0.65 + 0.35 * sin(phase);

	ALBEDO = flow_color.rgb;
	EMISSION = flow_color.rgb * 0.7;

	ALPHA = strand * envelope * pulse * opacity * flow_color.a;
}