shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"
#include "res://shaders/vfx/_util/tiling_and_offset.gdshaderinc"

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform vec4 main_st_a;
uniform vec4 main_st_b;

uniform vec2 main_st_a_initial_offset;
uniform vec2 main_st_b_initial_offset;

uniform vec2 erosion;

uniform float mask_divisor = 0.5;

uniform sampler2D lut;
uniform sampler2D alpha_overlay;
uniform sampler2D mask;

varying vec4 modulate;

void vertex() {
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
	modulate = COLOR;
}

void fragment() {
	vec4 noise_a = texture(TEXTURE, tiling_and_offset(UV, main_st_a, main_st_a_initial_offset));
	vec4 noise_b = texture(TEXTURE, tiling_and_offset(UV, main_st_b, main_st_b_initial_offset));
	
	vec4 alpha_overlay_tex = texture(alpha_overlay, UV);
	vec4 mask_tex = texture(mask, UV);
	
	float noise_total = (noise_a.r + noise_b.r) * 0.5;
	noise_total += alpha_overlay_tex.b;
	noise_total *= mask_tex.r;
	noise_total = erosion_from_factors(erosion, noise_total);
	
	vec4 lut_tex = texture(lut, vec2(noise_total));
	
	COLOR = lut_tex;
	COLOR.a = noise_total * (1.0 - UV.r) / mask_divisor;
	
	COLOR *= modulate;
}