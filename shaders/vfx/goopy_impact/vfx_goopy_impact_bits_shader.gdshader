shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/flipbook.gdshaderinc"
#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"

uniform sampler2D lut;

uniform float frame_count = 4;
uniform vec2 flipbook_size = vec2(2,2);

uniform vec2 distortion_panning_speed;
uniform float distortion_strength;

uniform sampler2D erosion_curve;
uniform float erosion_offset = 0.25;

varying float lifetime;
varying float flipbook_offset;
varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
	flipbook_offset = INSTANCE_CUSTOM.z;
}

void fragment() {
	vec4 distortion_tex = texture(TEXTURE, UV + (TIME * distortion_panning_speed));
	float distortion_val = (distortion_tex.b - 0.5) * 2.0;

	vec2 flipbook_uv = flipbook(UV, flipbook_size, frame_count * flipbook_offset * 0.99);
	vec4 flipbook_texture = texture(TEXTURE, flipbook_uv + (distortion_val * distortion_strength));

	vec4 lut_color = texture(lut, flipbook_texture.rr);

	float erosion_curve_val = texture(erosion_curve, vec2(lifetime)).r;
	float erosion_val = erosion_from_factors(vec2(erosion_curve_val, erosion_offset), flipbook_texture.g);

	COLOR = vec4(lut_color.rgb, erosion_val) * vertex_color;
}