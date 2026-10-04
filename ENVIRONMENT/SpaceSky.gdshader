shader_type sky;
render_mode disable_fog;

// Draws deep blue space, broad nebula clouds, sunlight and planetary atmosphere.

uniform float star_brightness = 0.9;
uniform float nebula_strength = 0.65;

uniform vec4 background_color : source_color = vec4(0.003, 0.006, 0.012, 1.0);
uniform vec4 nebula_color : source_color = vec4(0.25, 0.42, 0.62, 1.0);
uniform vec3 nebula_direction = vec3(-0.25, 0.18, -1.0);

uniform vec3 sun_direction = vec3(-0.65, 0.35, -0.67);
uniform vec4 sun_color : source_color = vec4(1.0, 0.91, 0.76, 1.0);
uniform float sun_radius = 0.01135;

uniform bool planet_enabled = true;
uniform vec3 planet_direction = vec3(0.55, 0.25, -1.0);
uniform float planet_radius = 0.95;
uniform float atmosphere_strength = 0.8;

// =========================================================
// Generates repeatable noise values without a large sine calculation.
float hash_value(vec3 p)
{
	p = fract(p * 0.1031);
	p += dot(p, p.yzx + vec3(33.33));

	return fract((p.x + p.y) * p.z);
}

// =========================================================
// Interpolates seamless three-dimensional noise.
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
// Combines broad shapes and finer wisps.
float layered_noise(vec3 p)
{
	return noise_value(p) * 0.53
		+ noise_value(p * 2.03 + vec3(17.0)) * 0.27
		+ noise_value(p * 4.11 + vec3(31.0)) * 0.14
		+ noise_value(p * 8.17 + vec3(53.0)) * 0.06;
}

// =========================================================
// Draws surrounding faint nebulae with several stronger cloud regions.
vec3 draw_nebula(vec3 ray)
{
	vec3 p = ray * vec3(2.5, 3.5, 2.5);

	vec3 warp = vec3(
		noise_value(p + vec3(11.0)),
		noise_value(p + vec3(37.0)),
		noise_value(p + vec3(71.0))
	) - vec3(0.5);

	vec3 cloud_point = p + warp * 0.7;
	float formations = layered_noise(cloud_point + vec3(19.0));
	float wisps = layered_noise(cloud_point * 2.0 + vec3(43.0));

	float coverage = smoothstep(0.25, 0.80, formations);
	float detail = mix(0.75, 1.0, wisps);

	// Broad formations distributed around different parts of the sky.
	float primary = smoothstep(
		-0.25, 0.95,
		dot(ray, normalize(nebula_direction))
	);

	float opposite = smoothstep(
		-0.20, 0.95,
		dot(ray, normalize(vec3(0.65, -0.10, 1.0)))
	);

	float upper = smoothstep(
		0.0, 0.95,
		dot(ray, normalize(vec3(-0.75, 0.65, 0.35)))
	);

	// A faint baseline prevents the opposite hemisphere becoming empty.
	float region = 0.22
		+ primary * primary * 0.78
		+ opposite * opposite * 0.65
		+ upper * upper * 0.35;

	region = min(region, 1.0);

	vec3 tint = mix(
		vec3(0.025, 0.045, 0.075),
		nebula_color.rgb * 0.35,
		smoothstep(0.30, 0.75, formations)
	);

	return tint * coverage * detail * region * nebula_strength;
}

// =========================================================
// Generates varied star positions.
vec3 star_random(vec2 cell, float seed)
{
	return fract(sin(vec3(
		dot(cell, vec2(127.1, 311.7)) + seed,
		dot(cell, vec2(269.5, 183.3)) + seed,
		dot(cell, vec2(419.2, 371.9)) + seed
	)) * 43758.5453);
}

// =========================================================
// Draws one sparse layer of distant stars.
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
	vec3 tint = mix(
		vec3(0.65, 0.78, 1.0),
		vec3(1.0, 0.88, 0.72),
		random.x
	);

	return tint * star * mix(0.3, 1.0, random.y);
}

// =========================================================
// Draws the planet and a soft atmosphere extending beyond its silhouette.
vec3 draw_planet(vec3 ray, vec3 background)
{
	if (!planet_enabled)
		return background;

	vec3 planet_axis = normalize(planet_direction);
	vec3 centre = planet_axis * 3.0;
	vec3 sunlight = normalize(sun_direction);

	float radius = clamp(planet_radius, 0.1, 2.0);
	float projected = dot(ray, centre);
	float discriminant = projected * projected
		- dot(centre, centre) + radius * radius;

	float angular_radius = asin(radius / 3.0);
	float angular_distance = acos(clamp(dot(ray, planet_axis), -1.0, 1.0));

	// Estimate illumination around the limb to favour the sun-facing edge.
	vec3 tangent = ray - planet_axis * dot(ray, planet_axis);
	vec3 limb_normal = length(tangent) > 0.0001
		? normalize(tangent) : vec3(0.0, 1.0, 0.0);

	float limb_light = 0.12 + 0.88 * max(dot(limb_normal, sunlight), 0.0);

	if (projected <= 0.0 || discriminant <= 0.0)
	{
		float outside = max(angular_distance - angular_radius, 0.0);
		float close_haze = exp(-outside * 90.0);
		float broad_haze = exp(-outside * 22.0);

		vec3 atmosphere = vec3(0.20, 0.48, 0.82)
			* (close_haze * 0.28 + broad_haze * 0.045)
			* limb_light * atmosphere_strength;

		return background + atmosphere;
	}

	float distance_to_surface = projected - sqrt(discriminant);
	vec3 normal = normalize(ray * distance_to_surface - centre);
	float illumination = max(dot(normal, sunlight), 0.0);

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

	float limb = pow(
		1.0 - max(dot(normal, -ray), 0.0),
		3.0
	);

	vec3 planet = surface * (0.035 + illumination * 0.95);
	vec3 atmosphere = vec3(0.14, 0.40, 0.74)
		* limb * (0.08 + illumination)
		* atmosphere_strength;

	return planet + atmosphere;
}

// =========================================================
// Composes the blue background, nebulae, stars, sun and planet.
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

	vec3 nebula = draw_nebula(direction);
	float star_attenuation = clamp(
		1.0 - length(nebula) * 1.5, 0.45, 1.0
	);

	vec3 background = background_color.rgb
		+ nebula
		+ stars * star_brightness * star_attenuation;

	float angle = acos(clamp(
		dot(direction, normalize(sun_direction)), -1.0, 1.0
	));

	float disc = 1.0 - smoothstep(sun_radius * 0.85, sun_radius, angle);
	float inner_glow = exp(-angle * 18.0) * 0.25;
	float outer_glow = exp(-angle * 4.5) * 0.045;

	background += sun_color.rgb * (
		disc * 7.0 + inner_glow + outer_glow
	);

	COLOR = draw_planet(direction, background);
}