shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/tiling_and_offset.gdshaderinc"

instance uniform vec2 noise_initial_offset;
instance uniform float use_r_as_alpha : hint_range(0.0, 1.0, 1.0);

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform sampler2D lut;
uniform sampler2D noise : repeat_enable;

uniform vec4 noise_st;

uniform vec2 scale_multiplier_range;
uniform vec2 scale_strength = vec2(1.0, 1.0);

varying vec4 vertex_color;

void vertex() {
	vec4 noise_tex = textureLod(noise, tiling_and_offset(UV, noise_st, noise_initial_offset), 0);	
	float scale_multiplier = mix(scale_multiplier_range.x, scale_multiplier_range.y, noise_tex.r);

	vertex_color = COLOR;
	VERTEX.x = mix(VERTEX.x, VERTEX.x * scale_multiplier, scale_strength.x);
	VERTEX.y = mix(VERTEX.y, VERTEX.y * scale_multiplier, scale_strength.y);
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE) * scale_multiplier;
}

void fragment() {
	vec4 grayscale = texture(TEXTURE, UV);

	vec4 lut_tex = texture(lut, vec2(grayscale.r));
	vec4 color_sub = lut_tex * vertex_color;

	COLOR = vec4(color_sub.rgb, vertex_color.a * mix(grayscale.a, grayscale.r, use_r_as_alpha));
}