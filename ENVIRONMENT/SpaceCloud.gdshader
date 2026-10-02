shader_type spatial;
render_mode unshaded, blend_mix, cull_disabled, depth_draw_never, fog_disabled;

// Draws faint, irregular cloud patches without external textures.

varying vec4 cloud_color;
varying vec2 cloud_seed;

// =========================================================
// Receives each patch's colour and unique noise offset.
// =========================================================
void vertex()
{
	cloud_color = COLOR;
	cloud_seed = INSTANCE_CUSTOM.xy * 80.0;
}

// =========================================================
// Generates a repeatable pseudo-random value.
// =========================================================
float hash_value(vec2 point)
{
	return fract(sin(dot(point, vec2(127.1, 311.7))) * 43758.5453);
}

// =========================================================
// Interpolates noise smoothly between neighbouring cells.
// =========================================================
float cloud_noise(vec2 point)
{
	vec2 cell = floor(point);
	vec2 fraction = fract(point);
	vec2 blend = fraction * fraction * (3.0 - 2.0 * fraction);

	return mix(
		mix(hash_value(cell), hash_value(cell + vec2(1.0, 0.0)), blend.x),
		mix(hash_value(cell + vec2(0.0, 1.0)),
			hash_value(cell + vec2(1.0, 1.0)), blend.x),
		blend.y
	);
}

// =========================================================
// Builds soft wisps with transparent edges and slow internal motion.
// =========================================================
void fragment()
{
	vec2 point = UV * vec2(5.0, 3.0) + cloud_seed;
	point += vec2(TIME * 0.025, TIME * 0.012);

	float noise =
		cloud_noise(point) * 0.65
		+ cloud_noise(point * 2.1) * 0.35;

	float radius = length((UV - vec2(0.5)) * 2.0);
	float edge = 1.0 - smoothstep(0.2, 1.0, radius);
	float wisp = smoothstep(0.35, 0.75, noise);

	ALBEDO = cloud_color.rgb;
	ALPHA = edge * wisp * cloud_color.a;
}