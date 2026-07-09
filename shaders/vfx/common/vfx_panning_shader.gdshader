shader_type canvas_item;
render_mode blend_mix;

#include "res://shaders/vfx/_util/tiling_and_offset.gdshaderinc"

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform sampler2D lut : hint_default_white, repeat_disable;
uniform sampler2D mask : hint_default_white;
uniform sampler2D erosion_over_lifetime : hint_default_black;

uniform float erosion_offset = 1.0;
uniform float energy = 1.0;

uniform vec2 initial_offset_multiplier;
uniform vec4 main_st = vec4(1.0, 1.0, 0.0, 0.0);

uniform float use_red_channel_as_alpha : hint_range(0.0, 1.0, 1.0) = 0.0;

varying float lifetime;
varying float instance_offset;
varying vec4 vertex_color;

void vertex() {
	lifetime = INSTANCE_CUSTOM.y/INSTANCE_CUSTOM.w;
	instance_offset = INSTANCE_CUSTOM.z;
	vertex_color = COLOR;
	
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
}

void fragment() {
	vec2 adjusted_uv = tiling_and_offset(UV, main_st, instance_offset * initial_offset_multiplier / main_st.xy);
	
	vec4 main_tex = texture(TEXTURE, adjusted_uv);
	vec4 lut_tex = texture(lut, vec2(main_tex.r));
	vec4 erosion_tex = texture(erosion_over_lifetime, vec2(lifetime));
	
	float erosion = smoothstep(erosion_tex.r, erosion_tex.r + erosion_offset, mix(main_tex.a, main_tex.r, use_red_channel_as_alpha));
	vec4 mask_tex = texture(mask, UV);
	
	COLOR = vec4(lut_tex.rgb * vertex_color.rgb * energy, erosion * vertex_color.a * mask_tex.r); 
}