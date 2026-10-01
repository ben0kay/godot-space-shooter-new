shader_type spatial;

render_mode unshaded, blend_add, cull_disabled, depth_draw_never, fog_disabled;

uniform vec4 energy_color : source_color = vec4(0.65, 0.2, 1.0, 1.0);
uniform float brightness = 3.0;

varying float particle_age;

// =========================================================
// Faces each sprite toward the camera and gently enlarges older particles.
void vertex()
{
	particle_age = clamp(INSTANCE_CUSTOM.y, 0.0, 1.0);

	float scale_x = length(MODEL_MATRIX[0].xyz);
	float scale_y = length(MODEL_MATRIX[1].xyz);
	float scale_z = length(MODEL_MATRIX[2].xyz);

	vec4 view_position = VIEW_MATRIX * MODEL_MATRIX[3];

	MODELVIEW_MATRIX = mat4(
		vec4(scale_x, 0.0, 0.0, 0.0),
		vec4(0.0, scale_y, 0.0, 0.0),
		vec4(0.0, 0.0, scale_z, 0.0),
		view_position
	);

	VERTEX *= 1.0 + particle_age * 0.65;
}

// =========================================================
// Draws a soft violet halo with a brighter centre and a fading lifetime.
void fragment()
{
	vec2 centred_uv = UV * 2.0 - 1.0;
	float radius = length(centred_uv);

	float halo = pow(max(0.0, 1.0 - radius), 2.0);
	float core = exp(-radius * radius * 32.0);
	float fade = pow(1.0 - particle_age, 1.8);

	vec3 core_color = mix(
		energy_color.rgb,
		vec3(0.9, 0.8, 1.0),
		0.65
	);

	ALBEDO = (
		energy_color.rgb * halo
		+ core_color * core * 0.7
	) * brightness;

	ALPHA = clamp(
		(halo * 0.45 + core * 0.35)
		* fade
		* energy_color.a,
		0.0,
		1.0
	);
}