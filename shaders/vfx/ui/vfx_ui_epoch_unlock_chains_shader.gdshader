shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"

uniform vec4 bright_color : source_color;
uniform float bright_energy;

uniform float bright_enabled : hint_range(0.0, 1.0, 0.01);

uniform sampler2D erosion_texture;
uniform float erosion_enabled : hint_range(0.0, 1.0, 0.01);
uniform float erosion_base : hint_range(-0.1, 1.0, 0.01);
uniform float erosion_offset = 1.0;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;	
}

void fragment() {
	vec4 bright_col = bright_color * bright_energy;
	vec4 tex = texture(TEXTURE, UV);

	vec4 erosion_tex = texture(erosion_texture, UV);
	float erosion = erosion_from_factors(vec2(erosion_base, erosion_offset), erosion_tex.r);
	erosion = mix(1.0, erosion, erosion_enabled);
	
	vec4 final_color = mix(tex, bright_color, bright_enabled);
	final_color.a = tex.a;
	final_color.a *= erosion;
	
	COLOR = final_color * vertex_color;
}