shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/flipbook.gdshaderinc"
#include "res://shaders/vfx/_util/hue_shift.gdshaderinc" 
#include "res://shaders/vfx/_util/erosion_from_factors.gdshaderinc"

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform float frame_count = 4;
uniform vec2 flipbook_size = vec2(2,2);

uniform sampler2D erosion_curve : hint_default_black;
uniform float erosion_offset = 0.1;

uniform float bright_offset : hint_range(0.0, 1.0, 0.05);
uniform sampler2D bright_lut : repeat_disable;

varying float lifetime;
varying float flipbook_offset;
varying vec4 vertex_color;

varying vec2 flipbook_uv;

instance uniform float hue_shift;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
	flipbook_offset = INSTANCE_CUSTOM.z;
	
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
	
	flipbook_uv = flipbook(UV, flipbook_size, (frame_count * flipbook_offset) * 0.99);
}

void fragment() {
	vec4 main_tex = texture(TEXTURE, flipbook_uv);
	
	float erosion_curve_val = texture(erosion_curve, vec2(lifetime)).r;
	float erosion = erosion_from_factors(vec2(erosion_curve_val, erosion_offset), main_tex.r);
	
	float bright_erosion = erosion_from_factors(vec2(erosion_curve_val, bright_offset), main_tex.r);
	
	vec4 bright_color = texture(bright_lut, vec2(bright_erosion));
	bright_color = shift_hue(bright_color, hue_shift);
	
	COLOR = vec4(bright_color.rgb * vertex_color.rgb, main_tex.a * vertex_color.a * erosion);
}
