shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never, fog_disabled;

// Draws soft procedural dust clouds with faded edges and softened rock intersections.

uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;

uniform vec4 cloud_color : source_color = vec4(0.30, 0.45, 0.62, 1.0);
uniform float opacity = 0.07;
uniform vec2 noise_offset = vec2(0.0);
uniform float pattern_rotation = 0.0;
uniform float soft_distance = 80.0;
uniform float near_fade_start = 200.0;
uniform float near_fade_end = 550.0;

varying float centre_distance;

// =========================================================
// Generates repeatable cloud noise.
float hash_value(vec2 p)
{
	return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453);
}

// =========================================================
// Interpolates smooth noise between cells.
float cloud_noise(vec2 p)
{
	vec2 cell = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	return mix(
		mix(hash_value(cell), hash_value(cell + vec2(1, 0)), f.x),
		mix(hash_value(cell + vec2(0, 1)),
			hash_value(cell + vec2(1, 1)), f.x),
		f.y
	);
}

// =========================================================
// Combines broad cloud formations with smaller wisps.
float layered_noise(vec2 p)
{
	return cloud_noise(p) * 0.57
		+ cloud_noise(p * 2.03 + vec2(17.0)) * 0.28
		+ cloud_noise(p * 4.11 + vec2(43.0)) * 0.15;
}

// =========================================================
// Faces the cloud towards the camera without CPU rotation updates.
void vertex()
{
	centre_distance = length(
		(VIEW_MATRIX * MODEL_MATRIX[3]).xyz
	);

	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
		INV_VIEW_MATRIX[0],
		INV_VIEW_MATRIX[1],
		INV_VIEW_MATRIX[2],
		MODEL_MATRIX[3]
	);
}

// =========================================================
// Fades cloud edges, nearby cards and intersections with opaque surfaces.
void fragment()
{
	vec2 centred = (UV - vec2(0.5)) * 2.0;

	float cosine = cos(pattern_rotation);
	float sine = sin(pattern_rotation);

	vec2 rotated = vec2(
		centred.x * cosine - centred.y * sine,
		centred.x * sine + centred.y * cosine
	);

	vec2 p = rotated * 2.5 + noise_offset;
	vec2 warp = vec2(
		cloud_noise(p + vec2(19.0)),
		cloud_noise(p + vec2(71.0))
	);

	float formations = layered_noise(p + warp * 1.8);
	float wisps = layered_noise(p * 2.0 + warp);

	float edge = 1.0 - smoothstep(0.35, 1.0, length(centred));
	edge *= edge;

	float cloud = smoothstep(0.30, 0.72, formations)
		* mix(0.55, 1.0, wisps);

	float scene_depth = texture(depth_texture, SCREEN_UV).r;
	vec3 ndc = vec3(SCREEN_UV * 2.0 - 1.0, scene_depth);
	vec4 scene_view = INV_PROJECTION_MATRIX * vec4(ndc, 1.0);
	scene_view.xyz /= scene_view.w;

	float separation = -scene_view.z - (-VERTEX.z);
	float intersection_fade = clamp(
		separation / max(soft_distance, 0.01), 0.0, 1.0
	);

	float near_fade = smoothstep(
		near_fade_start,
		max(near_fade_start + 1.0, near_fade_end),
		centre_distance
	);

	ALBEDO = cloud_color.rgb;
	ALPHA = cloud * edge * opacity * intersection_fade * near_fade;
}