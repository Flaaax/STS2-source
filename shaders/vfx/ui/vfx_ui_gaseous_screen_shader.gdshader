shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/bloat.gdshaderinc"
#include "res://shaders/vfx/_util/tiling_and_offset.gdshaderinc"
#include "res://shaders/vfx/_util/rotate.gdshaderinc"
#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"

uniform sampler2D noise : repeat_enable;
uniform vec4 noise_st_a = vec4(1.0, 1.0, 0, 0);
uniform vec2 noise_a_static_offset = vec2(0, 0);
uniform vec4 noise_st_b = vec4(1.0, 1.0, 0, 0);
uniform vec2 noise_b_static_offset = vec2(0, 0);
uniform float noise_layer_lerp : hint_range(0.0, 1.0, 0.1);
uniform float noise_panning_multiplier = 1.0;
uniform float noise_alpha_multiplier = 1.0;

uniform sampler2D distortion : repeat_enable, hint_default_black;
uniform vec4 distortion_st = vec4(1.0, 1.0, 0, 0);
uniform float distortion_intensity = 0.1;

uniform float min_base_alpha = 0.1;
uniform float alpha_multiplier = 1.0;

uniform float erosion_base = 0.25;
uniform float erosion_offset = 0.5;

uniform float bloat_intensity = 0.0;
uniform vec2 bloat_center = vec2(0.5, 0.5);

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec2 bloated_uv = bloat(UV, bloat_center, bloat_intensity);
	vec2 rotated_uv = rotate(bloated_uv, vec2(0.5, 0.5), 0);
	
	vec2 distortion_uv = tiling_and_offset(rotated_uv, distortion_st, vec2(0.0));
	float distortion_val = texture(distortion, distortion_uv).r;
	distortion_val = (distortion_val - 0.5) * 2.0;
	distortion_val *= distortion_intensity;
	
	vec2 noise_uv_a = rotated_uv;
	noise_uv_a = tiling_and_offset(noise_uv_a, noise_st_a, noise_a_static_offset + vec2(0.0, distortion_val));
	
	vec2 noise_uv_b = rotated_uv;
	noise_uv_b = tiling_and_offset(noise_uv_b, noise_st_b, noise_b_static_offset + vec2(0.0, distortion_val));
	
	vec4 noise_a_tex = texture(noise, noise_uv_a);
	vec4 noise_b_tex = texture(noise, noise_uv_b);
	
	float combined_noise = mix(noise_a_tex, noise_b_tex, noise_layer_lerp).r;
	combined_noise = erosion_from_factors(vec2(erosion_base, erosion_offset), combined_noise);
	combined_noise *= noise_alpha_multiplier;
	
	float sub_alpha = vertex_color.a * combined_noise;
	sub_alpha = max(sub_alpha, min_base_alpha);
	
	COLOR = vec4(vertex_color.rgb, sub_alpha * alpha_multiplier);
}