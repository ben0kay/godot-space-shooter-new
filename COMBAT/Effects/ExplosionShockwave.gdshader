shader_type spatial;

render_mode unshaded, blend_add, cull_back, depth_draw_never, fog_disabled;

// Shared by the expanding shell and the brief central flash.
uniform vec4 energy_color : source_color = vec4(1.0, 0.55, 0.15, 1.0);
uniform float brightness = 3.0;
uniform float opacity = 0.45;
uniform float rim_power = 7.0;
uniform float rim_mix = 1.0;
uniform float distortion = 0.025;
uniform float age = 0.0;

varying vec3 shell_direction;

// =========================================================
// Adds subtle irregularity to the sphere without a noise texture.
void vertex()
{
	shell_direction = normalize(VERTEX);

	float ripple =
		sin(shell_direction.x * 19.0 + age * 12.0)
		* sin(shell_direction.y * 23.0 - age * 9.0)
		* sin(shell_direction.z * 17.0 + age * 7.0);

	VERTEX *= 1.0 + ripple * distortion;
}

// =========================================================
// Shows a narrow outer rim, with a gently uneven energy intensity.
void fragment()
{
	float facing = clamp(
		abs(dot(normalize(NORMAL), normalize(VIEW))),
		0.0,
		1.0
	);

	float rim = pow(
		1.0 - facing,
		max(rim_power, 0.01)
	);

	float variation = 0.88 + 0.12 * sin(
		shell_direction.x * 29.0
		+ shell_direction.y * 17.0
		+ shell_direction.z * 23.0
		- age * 14.0
	);

	float coverage = mix(1.0, rim * variation, rim_mix);

	ALBEDO = energy_color.rgb * brightness;
	ALPHA = clamp(coverage * opacity * energy_color.a, 0.0, 1.0);
}