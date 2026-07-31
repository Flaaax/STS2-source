shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/tiling_and_offset.gdshaderinc"

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform vec4 distortion_st;
uniform vec2 distortion_initial_offset;

uniform vec2 distortion_dir;
uniform float distortion_strength;

uniform sampler2D lut : repeat_disable;

varying vec4 modulate; 

void vertex() {
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
	modulate = COLOR;
}

void fragment() {
	float distortion_mask = texture(TEXTURE, UV).b;
	
	vec4 distortion_tex = texture(TEXTURE, tiling_and_offset(UV, distortion_st, distortion_initial_offset));
	vec2 distortion = 
		(distortion_tex.g - 0.5) * 2.0 
		* distortion_strength 
		* distortion_mask 
		* distortion_dir;
	
	vec4 main_tex = texture(TEXTURE, UV + distortion);
	
	vec4 lut_tex = texture(lut, vec2(main_tex.r));
	
	COLOR = lut_tex;
	COLOR.a = main_tex.a;
	COLOR *= modulate;
}