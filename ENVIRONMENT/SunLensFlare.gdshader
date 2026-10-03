shader_type canvas_item;
render_mode unshaded, blend_add;

// Draws broad sunlight glare, a wide horizontal streak and subtle lens reflections.

uniform vec2 sun_position = vec2(0.5);
uniform float aspect_ratio = 1.0;
uniform float visibility : hint_range(0.0, 1.0) = 0.0;
uniform vec4 flare_color : source_color = vec4(1.0, 0.85, 0.62, 1.0);

// =========================================================
// Draws one softly fading lens reflection.
float ghost(vec2 point, vec2 centre, float radius)
{
	float distance_to_centre = length(point - centre) / radius;
	return exp(-distance_to_centre * distance_to_centre * 3.0);
}

// =========================================================
// Composes a wide streak, diffuse glare and restrained coloured reflections.
void fragment()
{
	vec2 scale = vec2(aspect_ratio, 1.0);
	vec2 point = (UV - vec2(0.5)) * scale;
	vec2 sun = (sun_position - vec2(0.5)) * scale;
	vec2 delta = point - sun;

	float distance_to_sun = length(delta);

	float close_glare = exp(-distance_to_sun * 22.0) * 0.18;
	float broad_glare = exp(-distance_to_sun * 7.0) * 0.035;

	float thin_streak = exp(-abs(delta.y) * 650.0)
		* exp(-abs(delta.x) * 2.3)
		* 0.32;

	float soft_streak = exp(-abs(delta.y) * 100.0)
		* exp(-abs(delta.x) * 3.2)
		* 0.065;

	float reflection_a = ghost(point, -sun * 0.3, 0.055);
	float reflection_b = ghost(point, -sun * 0.7, 0.10);
	float reflection_c = ghost(point, -sun * 1.15, 0.035);

	vec3 reflections =
		vec3(0.30, 0.65, 0.85) * reflection_a * 0.025
		+ vec3(0.45, 0.40, 0.75) * reflection_b * 0.018
		+ flare_color.rgb * reflection_c * 0.035;

	vec3 glare = flare_color.rgb * (
		close_glare + broad_glare + thin_streak + soft_streak
	);

	COLOR = vec4(glare + reflections, visibility);
}