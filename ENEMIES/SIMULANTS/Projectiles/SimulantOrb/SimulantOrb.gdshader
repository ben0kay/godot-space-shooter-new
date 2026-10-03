shader_type spatial;
render_mode unshaded, blend_add, cull_back, depth_draw_never;

// Animated purple plasma body with a bright centre and soft outer shell.
uniform vec4 energy_color : source_color = vec4(0.55, 0.12, 1.0, 1.0);
uniform vec4 core_color : source_color = vec4(0.85, 0.65, 1.0, 1.0);
uniform float brightness = 2.0;
uniform float opacity = 0.75;

varying vec3 orb_position;

// =========================================================
// Stores local coordinates and adds subtle surface movement.
// =========================================================
void vertex()
{
	orb_position = VERTEX;

	float ripple =
		sin(VERTEX.x * 7.0 + TIME * 2.4)
		* sin(VERTEX.y * 6.0 - TIME * 1.8)
		* sin(VERTEX.z * 7.0 + TIME * 2.0);

	VERTEX += NORMAL * ripple * 0.035;
}

// =========================================================
// Draws moving energy bands and fades the silhouette.
// =========================================================
void fragment()
{
	vec3 point = orb_position * 5.0;

	float flow =
		sin(point.x + sin(point.z + TIME * 1.7))
		* sin(point.y - TIME * 2.1)
		* sin(point.z + TIME * 1.3);

	float veins = 1.0 - smoothstep(0.03, 0.24, abs(flow));

	float facing = clamp(
		dot(normalize(NORMAL), normalize(VIEW)),
		0.0,
		1.0
	);

	float centre = pow(facing, 4.0);
	float pulse = 0.9 + sin(TIME * 4.0) * 0.1;

	vec3 color = mix(
		energy_color.rgb,
		core_color.rgb,
		clamp(centre * 0.8 + veins * 0.25, 0.0, 1.0)
	);

	ALBEDO = color;
	EMISSION = color * brightness * pulse;
	ALPHA = opacity * smoothstep(0.0, 0.45, facing)
		* (0.35 + veins * 0.35 + centre * 0.3);
}