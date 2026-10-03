shader_type spatial;
render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;

// Draws faint greenish cloud wisps using smoothly warped, layered noise.
// Additive blending preserves the stars behind the cloud patches.

varying vec4 cloud_color;
varying vec2 cloud_seed;

// =========================================================
// Receives per-patch colour and noise variation.
// =========================================================
void vertex()
{
	cloud_color = COLOR;
	cloud_seed = INSTANCE_CUSTOM.xy * 17.0;
}

// =========================================================
// Produces repeatable noise values without a large sine-based hash.
// =========================================================
float hash_value(vec2 point)
{
	vec3 value = fract(vec3(point.xyx) * 0.1031);
	value += dot(value, value.yzx + 33.33);

	return fract((value.x + value.y) * value.z);
}

// =========================================================
// Interpolates all four cell corners with smooth quintic blending.
// =========================================================
float smooth_noise(vec2 point)
{
	vec2 cell = floor(point);
	vec2 fraction = fract(point);

	vec2 blend = fraction * fraction * fraction
		* (fraction * (fraction * 6.0 - 15.0) + 10.0);

	float bottom_left = hash_value(cell);
	float bottom_right = hash_value(cell + vec2(1.0, 0.0));
	float top_left = hash_value(cell + vec2(0.0, 1.0));
	float top_right = hash_value(cell + vec2(1.0, 1.0));

	float bottom = mix(bottom_left, bottom_right, blend.x);
	float top = mix(top_left, top_right, blend.x);

	return mix(bottom, top, blend.y);
}

// =========================================================
// Combines rotated noise layers to reduce visible grid structure.
// =========================================================
float layered_noise(vec2 point)
{
	mat2 rotation = mat2(
		vec2(0.8, 0.6),
		vec2(-0.6, 0.8)
	);

	float value = smooth_noise(point) * 0.57;

	point = rotation * point * 2.03 + vec2(7.1, 3.8);
	value += smooth_noise(point) * 0.29;

	point = rotation * point * 2.07 + vec2(2.6, 9.3);
	value += smooth_noise(point) * 0.14;

	return value;
}

// =========================================================
// Warps broad haze into flowing wisps and fades every card edge.
// =========================================================
void fragment()
{
	vec2 centred_uv = (UV - vec2(0.5)) * 2.0;

	float angle = cloud_seed.x;
	float cosine = cos(angle);
	float sine = sin(angle);

	vec2 rotated_uv = vec2(
		cosine * centred_uv.x - sine * centred_uv.y,
		sine * centred_uv.x + cosine * centred_uv.y
	);

	vec2 point = rotated_uv * vec2(2.0, 3.8) + cloud_seed;
	point += vec2(TIME * 0.012, TIME * 0.006);

	vec2 warp = vec2(
		layered_noise(point * 0.7 + vec2(4.2, 1.3)),
		layered_noise(point * 0.7 + vec2(9.7, 6.4))
	);

	vec2 flowing_point = point + (warp - vec2(0.5)) * 2.4;

	float broad_noise = layered_noise(flowing_point);
	float fine_noise = smooth_noise(
		flowing_point * 3.1 + vec2(5.2, 8.7)
	);

	float haze = smoothstep(0.22, 0.78, broad_noise);

	float filament = 1.0 - smoothstep(
		0.025,
		0.2,
		abs(broad_noise - 0.52)
	);

	// Broad fade guarantees transparent edges regardless of noise.
	float radius_squared = dot(centred_uv, centred_uv);

	float envelope =
		exp(-radius_squared * 2.3)
		* (1.0 - smoothstep(0.45, 1.0, radius_squared));

	float density =
		haze * 0.55
		+ filament * fine_noise * 0.45;

	ALBEDO = cloud_color.rgb;
	ALPHA = density * envelope * cloud_color.a;
}