shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"
#include "res://shaders/vfx/_util/hue_shift.gdshaderinc" 

uniform sampler2D erosion_texture : repeat_enable;
instance uniform float erosion_texture_x_offset : hint_range(0.0, 1.0, 0.01);

instance uniform float erosion_base : hint_range(-0.2, 1.0, 0.001);
uniform float erosion_offset : hint_range(0.0, 1.0, 0.05);

uniform float bright_offset : hint_range(0.0, 1.0, 0.05);
uniform sampler2D bright_lut : repeat_disable;

instance uniform float hue_shift;

varying vec4 vertex_color; 

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec4 main_tex = texture(TEXTURE, UV);
	vec4 erosion_tex = texture(erosion_texture, UV + vec2(erosion_texture_x_offset, 0.0));
	
	//float erosion = erosion_from_factors(vec2(erosion_base, erosion_offset), erosion_tex.r);
	float erosion = step(erosion_base, erosion_tex.r);
	float bright_erosion = erosion_from_factors(vec2(erosion_base, bright_offset), erosion_tex.r);
	
	vec4 bright_color = texture(bright_lut, vec2(bright_erosion));
	bright_color = shift_hue(bright_color, hue_shift);
	
	vec3 main_color = mix(main_tex, bright_color, 1.0 - bright_erosion).rgb;
	
	COLOR = vec4(main_color * vertex_color.rgb, main_tex.a * vertex_color.a * erosion);
}