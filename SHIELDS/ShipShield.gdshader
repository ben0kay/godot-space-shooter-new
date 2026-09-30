shader_type spatial;

render_mode unshaded, blend_add, cull_back, depth_draw_never;

// Draws an enclosing energy shield with a bright rim, flowing lines, and hit ripples.

uniform vec4 shield_color : source_color = vec4(0.15, 0.85, 1.0, 1.0);

uniform float idle_opacity = 0.035;
uniform float rim_opacity = 0.3;
uniform float brightness = 1.4;
uniform float shield_ratio = 1.0;

uniform vec3 hit_direction = vec3(0.0, 0.0, -1.0);
uniform float hit_progress = 1.0;
uniform float hit_strength = 0.0;

uniform float collapse_progress = 0.0;

varying vec3 shell_direction;

// =========================================================
// Records coordinates on the unit sphere before it is stretched into an ellipsoid.
void vertex()
{
	shell_direction = normalize(VERTEX);
}

// =========================================================
// Draws the rim, subtle moving energy lines, and the latest impact ripple.
void fragment()
{
	vec3 direction = normalize(shell_direction);

	float facing = abs(dot(
		normalize(NORMAL),
		normalize(-VERTEX)
	));

	float rim = pow(1.0 - facing, 2.5);

	float flow = sin(
		direction.y * 55.0
		+ direction.x * 9.0
		+ TIME * 1.8
	);

	float lines = pow(0.5 + 0.5 * flow, 18.0);

	float angular_distance = acos(clamp(
		dot(direction, normalize(hit_direction)),
		-1.0,
		1.0
	));

	float ripple_radius = hit_progress * 1.5;

	float ripple = 1.0 - smoothstep(
		0.015,
		0.09,
		abs(angular_distance - ripple_radius)
	);

	float contact = exp(-angular_distance * angular_distance * 45.0);

	float impact = (
		ripple * 0.8 + contact
	) * hit_strength;

	float health_visibility = mix(
		0.65,
		1.0,
		clamp(shield_ratio, 0.0, 1.0)
	);

	float collapse = clamp(collapse_progress, 0.0, 1.0);

	float alpha = (
		idle_opacity
		+ rim * rim_opacity
		+ lines * 0.025
	) * health_visibility;

	alpha += impact * 0.6;
	alpha += collapse * (1.0 - collapse) * 1.8;

	alpha *= 1.0 - collapse;

	ALBEDO = shield_color.rgb
		* brightness
		* (0.7 + rim * 0.5 + impact);

	ALPHA = clamp(alpha, 0.0, 0.85);
}