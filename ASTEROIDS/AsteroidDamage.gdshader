shader_type spatial;

// Adds procedural surface cracks and a brief local impact flash.

uniform vec4 rock_color : source_color = vec4(0.4, 0.4, 0.42, 1.0);
uniform vec4 crack_color : source_color = vec4(0.055, 0.045, 0.04, 1.0);

uniform float asteroid_radius = 3.0;
uniform float damage_amount : hint_range(0.0, 1.0) = 0.0;

uniform float crack_density = 3.5;
uniform float crack_width = 0.07;
uniform vec3 pattern_offset = vec3(0.0);

uniform vec3 hit_position = vec3(0.0);
uniform float hit_radius = 1.0;
uniform float hit_strength = 0.0;

varying vec3 local_position;

// =========================================================
// Generates a repeatable point inside each procedural crack cell.
vec3 cell_random(vec3 cell)
{
	return fract(sin(vec3(
		dot(cell, vec3(127.1, 311.7, 74.7)),
		dot(cell, vec3(269.5, 183.3, 246.1)),
		dot(cell, vec3(113.5, 271.9, 124.6))
	)) * 43758.5453);
}

// =========================================================
// Records surface coordinates before the asteroid's world rotation.
void vertex()
{
	local_position = VERTEX;
}

// =========================================================
// Reveals cell-boundary fissures as health falls and brightens the last hit.
void fragment()
{
	vec3 point = local_position
		/ max(asteroid_radius, 0.01)
		* crack_density
		+ pattern_offset;

	vec3 cell = floor(point);
	vec3 within_cell = fract(point);

	float nearest = 100.0;
	float second_nearest = 100.0;
	vec3 nearest_cell = cell;

	for (int x = -1; x <= 1; x++)
	{
		for (int y = -1; y <= 1; y++)
		{
			for (int z = -1; z <= 1; z++)
			{
				vec3 neighbour = vec3(float(x), float(y), float(z));
				vec3 candidate_cell = cell + neighbour;

				vec3 offset = neighbour
					+ cell_random(candidate_cell)
					- within_cell;

				float distance_squared = dot(offset, offset);

				if (distance_squared < nearest)
				{
					second_nearest = nearest;
					nearest = distance_squared;
					nearest_cell = candidate_cell;
				}
				else if (distance_squared < second_nearest)
				{
					second_nearest = distance_squared;
				}
			}
		}
	}

	float boundary = sqrt(second_nearest) - sqrt(nearest);

	float damage = clamp(damage_amount, 0.0, 1.0);

	float width = crack_width * mix(0.2, 1.0, damage);
	float fissure = 1.0 - smoothstep(width * 0.35, width, boundary);

	// Different areas reveal their cracks at different damage levels.
	float threshold = mix(
		0.08,
		0.65,
		cell_random(nearest_cell + vec3(19.0)).x
	);

	float revealed = smoothstep(
		threshold,
		threshold + 0.18,
		damage
	);

	float cracks = fissure * revealed;

	float variation = mix(
		0.85,
		1.08,
		cell_random(nearest_cell).y
	);

	ALBEDO = mix(
		rock_color.rgb * variation,
		crack_color.rgb,
		cracks
	);

	ROUGHNESS = 1.0;
	SPECULAR = 0.15;

	float impact_distance = distance(local_position, hit_position);

	float flash = (
		1.0 - smoothstep(0.0, max(hit_radius, 0.01), impact_distance)
	) * hit_strength;

	EMISSION = vec3(0.65, 0.48, 0.3) * flash;
}