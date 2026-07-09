shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"

uniform sampler2D lut;

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform sampler2D erosion_curve;
uniform float erosion_offset = 0.25;
uniform float bright_erosion_offset = 0.35;

uniform float distortion_strength = 1.0;
uniform float distortion_speed = 1.0;

varying float lifetime;
varying float flipbook_offset;
varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
	flipbook_offset = INSTANCE_CUSTOM.z;
	
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
}

void fragment() {
	vec2 distortion_uv_offset = vec2(flipbook_offset);
	vec4 distortion_color = texture(TEXTURE, UV + vec2(distortion_speed * TIME) + distortion_uv_offset);
	float distortion = distortion_color.b;
	distortion -= 0.5; 
	
	vec4 texture_color =  texture(TEXTURE, UV + vec2(distortion * distortion_strength, 0.0));
	
	float erosion_curve_val = texture(erosion_curve, vec2(lifetime)).r;
	float erosion_val = erosion_from_factors(vec2(erosion_curve_val, erosion_offset), texture_color.a);
	float bright_erosion_val = erosion_from_factors(vec2(erosion_curve_val, bright_erosion_offset), texture_color.a);
	
	vec4 lut_color = texture(lut, vec2(bright_erosion_val));

	COLOR = vec4(lut_color.rgb, erosion_val) * vertex_color;
}