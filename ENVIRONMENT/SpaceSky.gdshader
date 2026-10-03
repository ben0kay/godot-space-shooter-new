shader_type sky;
render_mode disable_fog;

// Draws distant stars, an aligned sun and a procedurally shaded planet.

uniform float star_brightness = 0.9;
uniform float nebula_strength = 0.06;

uniform vec3 sun_direction = vec3(-0.65, 0.35, -0.67);
uniform vec4 sun_color : source_color = vec4(1.0, 0.91, 0.76, 1.0);
uniform float sun_radius = 0.01135;

uniform bool planet_enabled = true;
uniform vec3 planet_direction = vec3(0.55, 0.25, -1.0);
uniform float planet_radius = 0.95;

// =========================================================
// Generates repeatable values for star positions.
vec3 star_random(vec2 cell, float seed)
{
	return fract(sin(vec3(
		dot(cell, vec2(127.1, 311.7)) + seed,
		dot(cell, vec2(269.5, 183.3)) + seed,
		dot(cell, vec2(419.2, 371.9)) + seed
	)) * 43758.5453);
}

// =========================================================
// Draws a sparse star layer with varied brightness.
vec3 star_layer(vec2 coordinates, float face, float density, float seed)
{
	vec2 grid = coordinates * density;
	vec3 random = star_random(floor(grid), seed + face * 37.0);

	if (random.z > 0.025)
		return vec3(0.0);

	float distance_to_star = length(
		fract(grid) - (vec2(0.2) + random.xy * 0.6)
	);

	float star = 1.0 - smoothstep(0.02, 0.10, distance_to_star);
	vec3 tint = mix(vec3(0.65, 0.78, 1.0), vec3(1.0, 0.88, 0.72), random.x);

	return tint * star * mix(0.3, 1.0, random.y);
}

// =========================================================
// Produces repeatable planetary noise.
float hash_value(vec3 p)
{
	return fract(sin(dot(p, vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

// =========================================================
// Interpolates three-dimensional noise without seams around the planet.
float noise_value(vec3 p)
{
	vec3 i = floor(p);
	vec3 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	return mix(
		mix(
			mix(hash_value(i), hash_value(i + vec3(1, 0, 0)), f.x),
			mix(hash_value(i + vec3(0, 1, 0)),
				hash_value(i + vec3(1, 1, 0)), f.x), f.y
		),
		mix(
			mix(hash_value(i + vec3(0, 0, 1)),
				hash_value(i + vec3(1, 0, 1)), f.x),
			mix(hash_value(i + vec3(0, 1, 1)),
				hash_value(i + vec3(1, 1, 1)), f.x), f.y
		), f.z
	);
}

// =========================================================
// Combines broad formations with smaller cloud detail.
float layered_noise(vec3 p)
{
	return noise_value(p) * 0.57
		+ noise_value(p * 2.03 + vec3(17.0)) * 0.28
		+ noise_value(p * 4.11 + vec3(31.0)) * 0.15;
}

// =========================================================
// Shades the planet using the same sun direction as the sector lighting.
vec3 draw_planet(vec3 ray, vec3 background)
{
	if (!planet_enabled)
		return background;

	vec3 centre = normalize(planet_direction) * 3.0;
	float radius = clamp(planet_radius, 0.1, 2.0);
	float projected = dot(ray, centre);
	float discriminant = projected * projected
		- dot(centre, centre) + radius * radius;

	if (projected <= 0.0 || discriminant <= 0.0)
		return background;

	float distance_to_surface = projected - sqrt(discriminant);
	vec3 normal = normalize(ray * distance_to_surface - centre);
	float illumination = max(dot(normal, normalize(sun_direction)), 0.0);

	float land = smoothstep(
		0.47, 0.59, layered_noise(normal * 4.0 + vec3(12.0))
	);

	vec3 surface = mix(
		vec3(0.025, 0.065, 0.12),
		vec3(0.17, 0.22, 0.19),
		land
	);

	vec3 cloud_point = normal * vec3(13.0, 20.0, 13.0);
	cloud_point.x += normal.y * 7.0;

	float clouds = smoothstep(
		0.48, 0.67, layered_noise(cloud_point + vec3(47.0))
	);

	surface = mix(surface, vec3(0.65, 0.72, 0.76), clouds * 0.85);

	float limb = pow(1.0 - max(dot(normal, -ray), 0.0), 4.0);

	return surface * (0.025 + illumination * 0.95)
		+ vec3(0.08, 0.35, 0.68) * limb * (0.04 + illumination);
}

// =========================================================
// Composes stars, restrained haze, sunlight and the distant planet.
void sky()
{
	vec3 direction = normalize(EYEDIR);
	vec3 absolute_direction = abs(direction);
	vec2 coordinates;
	float face;

	if (absolute_direction.x >= absolute_direction.y
		&& absolute_direction.x >= absolute_direction.z)
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

	vec3 stars = star_layer(coordinates, face, 180.0, 11.0);
	stars += star_layer(coordinates, face, 75.0, 83.0) * 1.3;

	float haze = pow(
		max(dot(direction, normalize(vec3(-0.7, 0.3, -0.6))), 0.0),
		6.0
	);

	vec3 background = vec3(0.001, 0.002, 0.004)
		+ vec3(0.10, 0.16, 0.27) * haze * nebula_strength
		+ stars * star_brightness;

	float angle = acos(clamp(
		dot(direction, normalize(sun_direction)), -1.0, 1.0
	));

	float disc = 1.0 - smoothstep(sun_radius * 0.85, sun_radius, angle);
	float halo = exp(-angle * 22.0) * 0.22;
	float outer_glow = exp(-angle * 6.0) * 0.025;

	background += sun_color.rgb * (disc * 7.0 + halo + outer_glow);

	// Drawing the planet last lets it obscure the sun when aligned.
	COLOR = draw_planet(direction, background);
}