shader_type spatial;

render_mode unshaded, blend_add, cull_disabled, depth_draw_never;

// Animates a segmented beam cylinder with flowing displacement and energy bands.

uniform vec4 beam_color : source_color = vec4(0.15, 0.85, 1.0, 1.0);

uniform float emission_energy = 1.0;
uniform float opacity = 0.5;
uniform float release_alpha = 1.0;

uniform float beam_length = 1.0;
uniform float beam_diameter = 0.12;

uniform float wobble_amount = 0.035;
uniform float wobble_wavelength = 10.0;
uniform float wobble_speed = 9.0;

uniform float pulse_amount = 0.08;
uniform float pulse_speed = 12.0;

uniform float band_spacing = 8.0;
uniform float band_speed = 32.0;
uniform float band_strength = 0.35;

varying float distance_along;

// =========================================================
// Displaces the beam sideways while keeping both endpoints anchored.
void vertex()
{
	// The cylinder's positive Y end becomes the muzzle after rotation.
	float progress = clamp(0.5 - VERTEX.y, 0.0, 1.0);
	distance_along = progress * beam_length;

	float envelope = sin(progress * 3.14159265);

	float phase =
		distance_along * 6.2831853 / max(wobble_wavelength, 0.01)
		- TIME * wobble_speed;

	vec2 displacement = vec2(
		sin(phase) + 0.35 * sin(phase * 1.73 + TIME * 1.2),
		cos(phase * 0.83 + TIME * 0.7)
	);

	// Convert world-space wobble to the scaled cylinder's local units.
	VERTEX.xz += displacement
		* envelope
		* wobble_amount
		/ max(beam_diameter, 0.001);

	float pulse = 1.0
		+ sin(TIME * pulse_speed - distance_along * 0.6)
		* pulse_amount;

	// Pulse the radius without moving the centre of the cylinder.
	// Wobble remains subtle enough that this also reads as flowing energy.
	VERTEX.xz *= pulse;
}

// =========================================================
// Moves brighter energy bands from the muzzle toward the endpoint.
void fragment()
{
	float travel = (
		distance_along - TIME * band_speed
	) / max(band_spacing, 0.01);

	float wave = 0.5 + 0.5 * cos(travel * 6.2831853);
	float band = pow(wave, 12.0);

	float brightness = 0.7 + band * band_strength;

	ALBEDO = vec3(0.0);
	EMISSION = beam_color.rgb * emission_energy * brightness;
	ALPHA = opacity * release_alpha;
}