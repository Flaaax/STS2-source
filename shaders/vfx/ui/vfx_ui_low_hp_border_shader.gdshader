shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/tiling_and_offset.gdshaderinc"
#include "res://shaders/vfx/_util/polar_coordinates.gdshaderinc"

uniform float alpha = 1.0;
uniform float alpha_multiplier = 1.0;
uniform float inner_radius = 0.0;
uniform float outer_radius = 1.0;

uniform sampler2D noise : repeat_enable;
uniform vec2 noise_tiling = vec2(1.0, 1.0);
uniform vec2 noise_panning = vec2(0.0, 0.0);
uniform vec2 noise_initial_offset = vec2(0.0, 0.0);
uniform float noise_additional_offset;

uniform vec2 smoothstep_factors = vec2(0.0, 1.0);

uniform vec4 main_color : source_color;

varying vec4 vertex_color;

void vertex()
{
	vertex_color = COLOR;
}

void fragment()
{
	vec2 polar_uv = polar_coordinates(UV, vec2(0.5,0.5), 1.0, 1.0);
	vec4 noise_tex = texture(noise, tiling_and_offset(polar_uv, vec4(noise_tiling, noise_panning), vec2(noise_additional_offset, 0)));

	float x = abs(UV.r-.5)*2.0;
	float y = abs(UV.g-.5)*2.0;

	float q = 1.0-(1.0-sqrt(x*x+y*y)/outer_radius)/(1.0-inner_radius);
	float final_alpha = q * alpha + (noise_tex.r * 0.25);
	final_alpha = smoothstep(
		clamp(smoothstep_factors.x, 0, 1),
		clamp(smoothstep_factors.x + smoothstep_factors.y, 0, 1),
		final_alpha
		) * clamp(alpha_multiplier, 0.0, 1.0);

	COLOR = vec4(main_color.rgb, final_alpha) * vertex_color;
}