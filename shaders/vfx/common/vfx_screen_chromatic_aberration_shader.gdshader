shader_type canvas_item;
render_mode blend_mix;

instance uniform float base_intensity = 1.0;

uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D intensity_curve : repeat_disable;
uniform sampler2D distance_curve : repeat_disable;

varying vec4 vertex_color;
varying float lifetime;

void vertex() {
	vertex_color = COLOR;
	lifetime = INSTANCE_CUSTOM.y / INSTANCE_CUSTOM.w;
}

void fragment() {
	vec4 main_tex = texture(TEXTURE, UV);
	
	vec2 dir = normalize(vec2(0.5) - UV);
	
	float dist = distance(UV, vec2(0.5));
	float dist_curve = texture(distance_curve, vec2(dist)).r;
	float intensity = texture(intensity_curve, vec2(lifetime)).r * base_intensity * dist_curve * main_tex.r;
	
	vec4 screen_tex_r = texture(screen, SCREEN_UV + (dir/2.0 * intensity));
	vec4 screen_tex_g = texture(screen, SCREEN_UV + (0.0 * intensity));
	vec4 screen_tex_b = texture(screen, SCREEN_UV + (dir * intensity));
	
	COLOR = vec4(screen_tex_r.r, screen_tex_g.g, screen_tex_b.b, 1.0);
}