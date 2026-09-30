shader_type sky;
render_mode disable_fog;

// Draws stationary distant stars and faint nebula haze without image assets.

uniform float star_brightness = 1.4;
uniform float nebula_strength = 0.025;

// =========================================================
// Generates repeatable random values for a star cell.
vec3 star_random(vec2 cell, float seed)
{
	return fract(sin(vec3(
		dot(cell, vec2(127.1, 311.7)) + seed,
		dot(cell, vec2(269.5, 183.3)) + seed,
		dot(cell, vec2(419.2, 371.9)) + seed
	)) * 43758.5453);
}

// =========================================================
// Draws one sparse layer of stars with varied position and brightness.
vec3 star_layer(
	vec2 coordinates,
	float face,
	float density,
	float probability,
	float radius,
	float seed
)
{
	vec2 grid = coordinates * density;
	vec2 cell = floor(grid);
	vec2 within_cell = fract(grid);

	vec3 random = star_random(cell, seed + face * 37.0);

	if (random.z > probability)
	{
		return vec3(0.0);
	}

	vec2 centre = vec2(0.2) + random.xy * 0.6;
	float distance_to_star = length(within_cell - centre);

	float star = 1.0 - smoothstep(
		radius * 0.2,
		radius,
		distance_to_star
	);

	vec3 tint = mix(
		vec3(0.65, 0.78, 1.0),
		vec3(1.0, 0.88, 0.72),
		random.x
	);

	float brightness = mix(0.35, 1.0, random.y);

	return tint * star * brightness;
}

// =========================================================
// Projects the viewing direction onto a sky face and draws the space background.
void sky()
{
	vec3 direction = normalize(EYEDIR);
	vec3 absolute_direction = abs(direction);

	vec2 coordinates;
	float face;

	if (
		absolute_direction.x >= absolute_direction.y
		&& absolute_direction.x >= absolute_direction.z
	)
	{
		coordinates = direction.yz / absolute_direction.x;
		face = direction.x > 0.0 ? 0.0 : 1.0;
	}
	else if (absolute_direction.y >= absolute_direction.z)
	{
		coordinates = direction.xz / absolute_direction.y;
		face = direction.y > 0.0 ? 2.0 : 3.0;
	}
	else
	{
		coordinates = direction.xy / absolute_direction.z;
		face = direction.z > 0.0 ? 4.0 : 5.0;
	}

	coordinates = coordinates * 0.5 + 0.5;

	vec3 stars = star_layer(
		coordinates, face, 180.0, 0.025, 0.11, 11.0
	);

	stars += star_layer(
		coordinates, face, 75.0, 0.018, 0.075, 83.0
	) * 1.6;

	float blue_cloud = pow(
		max(dot(direction, normalize(vec3(-0.7, 0.3, -0.6))), 0.0),
		6.0
	);

	float purple_cloud = pow(
		max(dot(direction, normalize(vec3(0.4, -0.2, -0.9))), 0.0),
		9.0
	);

	float cloud_detail = 0.55 + 0.45 * sin(
		direction.x * 19.0
		+ sin(direction.y * 13.0)
		+ direction.z * 11.0
	);

	vec3 haze = (
		vec3(0.12, 0.2, 0.45) * blue_cloud
		+ vec3(0.3, 0.1, 0.4) * purple_cloud
	) * cloud_detail * nebula_strength;

	COLOR = haze + stars * star_brightness;
}