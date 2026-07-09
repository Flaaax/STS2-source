shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/polar_coordinates.gdshaderinc"

uniform vec2 tiling;
uniform vec2 x_offsets;

uniform sampler2D offset_curve : repeat_disable;

varying vec4 vertex_color;
varying float lifetime;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
}

void fragment() {
	float interpolation = texture(offset_curve, vec2(lifetime)).r;

	vec2 polar_uv = polar_coordinates(UV, vec2(0.5, 0.5), 1.0, 1.0);
	polar_uv *= tiling;
	polar_uv += vec2(mix(x_offsets.x, x_offsets.y, interpolation), 0.0);

	vec4 main_tex = texture(TEXTURE, UV);
	vec4 main_tex_polar = texture(TEXTURE, polar_uv);

	vec3 sub_color = vertex_color.rgb;
	float alpha = vertex_color.a * main_tex_polar.r * main_tex.a;

	COLOR = vec4(sub_color, alpha);
}