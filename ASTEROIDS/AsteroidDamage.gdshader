shader_type spatial;

// Adds layered rock colour, small surface relief, damage fissures and hit flashes.

uniform vec4 rock_color : source_color = vec4(0.28, 0.27, 0.25, 1.0);
uniform vec4 crack_color : source_color = vec4(0.025, 0.022, 0.02, 1.0);

uniform float asteroid_radius = 3.0;
uniform float damage_amount : hint_range(0.0, 1.0) = 0.0;
uniform float crack_density = 3.5;
uniform float crack_width = 0.07;
uniform vec3 pattern_offset = vec3(0.0);

uniform vec3 hit_position = vec3(0.0);
uniform float hit_radius = 1.0;
uniform float hit_strength = 0.0;

uniform float surface_scale = 1.2;
uniform float bump_strength = 0.18;

varying vec3 local_position;
varying vec3 view_position;

// =========================================================
// Produces a repeatable scalar for each noise cell.
float hash_value(vec3 p)
{
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

// =========================================================
// Interpolates noise smoothly through three-dimensional rock space.
float rock_noise(vec3 p)
{
	vec3 cell = floor(p);
	vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	return mix(
		mix(
			mix(hash_value(cell), hash_value(cell + vec3(1, 0, 0)), f.x),
			mix(hash_value(cell + vec3(0, 1, 0)),
				hash_value(cell + vec3(1, 1, 0)), f.x),
			f.y
		),
		mix(
			mix(hash_value(cell + vec3(0, 0, 1)),
				hash_value(cell + vec3(1, 0, 1)), f.x),
			mix(hash_value(cell + vec3(0, 1, 1)),
				hash_value(cell + vec3(1, 1, 1)), f.x),
			f.y
		),
		f.z
	);
}

// =========================================================
// Records coordinates that remain attached to the rotating asteroid.
void vertex()
{
	local_position = VERTEX;
	view_position = (MODELVIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

// =========================================================
// Shades rocky surfaces and reveals fissures as the asteroid takes damage.
void fragment()
{
	vec3 p = local_position * surface_scale + pattern_offset;

	float broad = rock_noise(p * 0.24);
	float medium = rock_noise(p);
	float fine = rock_noise(p * 3.1);
	float height = medium * 0.72 + fine * 0.28;

	vec3 stone = rock_color.rgb * mix(0.62, 1.32, broad);
	stone *= mix(0.78, 1.12, height);

	float damage = clamp(damage_amount, 0.0, 1.0);
	float cracks = 0.0;

	if (damage > 0.01)
	{
		vec3 crack_point = local_position
			/ max(asteroid_radius, 0.01)
			* crack_density
			+ pattern_offset;

		crack_point += vec3(
			rock_noise(crack_point + vec3(17, 0, 0)),
			rock_noise(crack_point + vec3(0, 29, 0)),
			rock_noise(crack_point + vec3(0, 0, 41))
		) * 0.8;

		vec3 cell_edge = abs(fract(crack_point) - 0.5);
		float edge = 0.5 - max(
			cell_edge.x, max(cell_edge.y, cell_edge.z)
		);

		float width = max(0.001, crack_width * mix(0.2, 1.0, damage));
		float threshold = mix(
			0.08, 0.65, hash_value(floor(crack_point))
		);

		cracks = (1.0 - smoothstep(width * 0.3, width, edge))
			* smoothstep(threshold, threshold + 0.18, damage);
	}

	ALBEDO = mix(stone, crack_color.rgb, cracks);
	ROUGHNESS = mix(0.88, 1.0, fine);
	SPECULAR = 0.18;

	// Derivative-based relief needs no UVs or extra mesh vertices.
	vec3 dpdx = dFdx(view_position);
	vec3 dpdy = dFdy(view_position);
	vec3 r1 = cross(dpdy, NORMAL);
	vec3 r2 = cross(NORMAL, dpdx);
	float determinant = dot(dpdx, r1);

	if (abs(determinant) > 0.000001)
	{
		vec3 gradient = (
			dFdx(height) * r1 + dFdy(height) * r2
		) * bump_strength;

		NORMAL = normalize(
			abs(determinant) * NORMAL
			- sign(determinant) * gradient
		);
	}

	float flash = (
		1.0 - smoothstep(
			0.0, max(hit_radius, 0.01),
			distance(local_position, hit_position)
		)
	) * hit_strength;

	EMISSION = vec3(0.65, 0.48, 0.3) * flash;
}