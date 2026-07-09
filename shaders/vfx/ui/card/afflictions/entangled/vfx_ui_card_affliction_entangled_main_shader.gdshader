shader_type canvas_item;
render_mode blend_mix;

uniform vec4 distortion_st;
uniform float distortion_intensity;

uniform sampler2D vine_lut : repeat_disable;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec2 distortion_uv = UV * distortion_st.xy + vec2(distortion_st.z, TIME * distortion_st.w);
	vec4 distortion_tex = texture(TEXTURE, distortion_uv);
	float distortion = distortion_tex.b * distortion_intensity;
	
	vec4 main_tex = texture(TEXTURE, UV);
	vec4 main_tex_distorted = texture(TEXTURE, UV + vec2(distortion * main_tex.g, 0.0));
	
	vec4 lut_tex = texture(vine_lut, vec2(main_tex_distorted.r));
	
	COLOR = vec4(vertex_color.rgb * lut_tex.rgb, vertex_color.a * main_tex_distorted.a);
}
