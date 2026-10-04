shader_type spatial;

render_mode unshaded, blend_add, cull_back, depth_draw_never;

// Draws a subtle shield rim, clear interior, expanding impact ripples, and collapse flash.

uniform vec4 shield_color : source_color = vec4(0.15, 0.85, 1.0, 1.0);

uniform float idle_opacity = 0.001;
uniform float rim_opacity = 0.12;
uniform float brightness = 1.1;
uniform float shield_ratio = 1.0;

uniform vec3 hit_direction = vec3(0.0, 0.0, -1.0);
uniform float hit_progress = 1.0;
uniform float hit_strength = 0.0;

uniform float collapse_progress = 0.0;

varying vec3 shell_direction;

// =========================================================
// Records the unit sphere coordinates before the mesh becomes an ellipsoid.
void vertex()
{
	shell_direction = normalize(VERTEX);
}

// =========================================================
// Keeps the shield interior clear and reveals energy around its rim and impacts.
void fragment()
{
	vec3 direction = normalize(shell_direction);

	float facing = clamp(abs(dot(
		normalize(NORMAL),
		normalize(-VERTEX)
	)), 0.0, 1.0);

	// A narrow rim rather than a broad cloudy surface.
	float rim = pow(1.0 - facing, 7.0);

	float angular_distance = acos(clamp(
		dot(direction, normalize(hit_direction)),
		-1.0,
		1.0
	));

	float progress = clamp(hit_progress, 0.0, 1.0);

	float ripple_radius = progress * 1.7;
	float ripple_width = mix(0.035, 0.075, progress);

	float ripple = 1.0 - smoothstep(
		ripple_width * 0.25,
		ripple_width,
		abs(angular_distance - ripple_radius)
	);

	// A softer second ring trails the main ripple.
	float trailing_radius = max(0.0, ripple_radius - 0.13);

	float trailing = 1.0 - smoothstep(
		0.01,
		0.045,
		abs(angular_distance - trailing_radius)
	);

	float contact = exp(
		-angular_distance * angular_distance * 65.0
	) * (1.0 - progress);

	float impact = (
		ripple
		+ trailing * 0.3
		+ contact * 1.2
	) * hit_strength;

	float health_visibility = mix(
		0.5,
		1.0,
		clamp(shield_ratio, 0.0, 1.0)
	);

	float collapse = clamp(collapse_progress, 0.0, 1.0);

	float collapse_flash = sin(collapse * 3.14159265);

	float alpha = (
		idle_opacity
		+ rim * rim_opacity
	) * health_visibility;

	alpha += impact * 0.55;
	alpha += collapse_flash * 0.22;

	alpha *= 1.0 - collapse;

	ALBEDO = shield_color.rgb
		* brightness
		* (0.8 + impact * 0.8 + collapse_flash * 0.5);

	ALPHA = clamp(alpha, 0.0, 0.75);
}