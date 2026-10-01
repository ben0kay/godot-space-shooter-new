shader_type spatial;
render_mode unshaded, cull_disabled, fog_disabled;

// Displays the cockpit viewport with subtle electronic screen treatment.
uniform sampler2D screen_texture : source_color, filter_linear;
uniform float brightness = 1.0;
uniform float glow_strength = 0.45;
uniform float scanline_strength = 0.035;

void fragment()
{
	vec3 screen = texture(screen_texture, UV).rgb;

	float scanline = 0.5 + 0.5 * cos(UV.y * 360.0 * 6.283185);
	screen *= 1.0 - scanline * scanline_strength;

	float edge_distance = min(
		min(UV.x, 1.0 - UV.x),
		min(UV.y, 1.0 - UV.y)
	);

	float edge = 1.0 - smoothstep(0.002, 0.005, edge_distance);
	vec3 colour = screen * brightness;
	colour += vec3(0.03, 0.25, 0.30) * edge;

	ALBEDO = colour;
	EMISSION = screen * glow_strength;
}