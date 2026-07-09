shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/flipbook.gdshaderinc"

uniform sampler2D lut;

uniform float flipbook_speed = 1;
uniform float frame_count = 9;
uniform vec2 flipbook_size = vec2(3,3);

varying float lifetime;
varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
}

void fragment() {
	vec2 flipbook_uv = flipbook(UV, flipbook_size, mod(TIME * flipbook_speed, frame_count));
	vec4 texture_color =  texture(TEXTURE, flipbook_uv);

	vec4 lut_color = texture(lut, texture_color.rr);

	vec4 final_color = lut_color * vertex_color;
	float alpha = texture_color.a * vertex_color.a;

	COLOR = vec4(final_color.rgb, alpha);
}