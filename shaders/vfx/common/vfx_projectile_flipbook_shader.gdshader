shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/flipbook.gdshaderinc"
#include "res://shaders/vfx/_util/hue_shift.gdshaderinc"

instance uniform vec2 flipbook_size;
instance uniform float frame_count;
instance uniform float flipbook_speed;
instance uniform float flipbook_offset;
uniform sampler2D hue_shift_curve : repeat_disable, hint_default_black;

uniform sampler2D lut : repeat_disable;

varying vec4 vertex_color;
varying vec2 flipbook_uv;

void vertex() {
	vertex_color = COLOR;
	
	float flipbook_progress = flipbook_offset + (TIME * flipbook_speed);
	flipbook_uv = flipbook(UV, flipbook_size, ((frame_count * flipbook_offset) + (frame_count * flipbook_progress)) * 0.99);
}

void fragment() {
	vec4 flipbook_texture_color =  texture(TEXTURE, flipbook_uv);
	float alpha = flipbook_texture_color.a * vertex_color.a;

	vec4 lut_color = texture(lut, flipbook_texture_color.rr);
	vec4 initial_color = lut_color * vertex_color;
	
	COLOR = vec4(initial_color.rgb, alpha);
}