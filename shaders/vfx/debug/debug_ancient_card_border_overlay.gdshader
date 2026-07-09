shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D screen : hint_screen_texture, filter_linear_mipmap;
uniform float blur_amount;
uniform float add_amount;

uniform sampler2D mask;
uniform vec4 corner_color : source_color;
uniform float corner_weight : hint_range(0.0, 1.0, 0.1);

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec4 main_tex = texture(TEXTURE, UV);
	vec4 mask_tex = texture(mask, UV);
	
	vec4 screen_tex = texture(screen, SCREEN_UV, blur_amount);
	screen_tex.rgb += vec3(add_amount);
	screen_tex.a = mask_tex.a;
	
	vec4 final_color = mix(screen_tex, corner_color, mask_tex.g * corner_weight);
	
	COLOR = final_color;
	//COLOR = main_tex;
	COLOR *= vertex_color;
	COLOR *= main_tex.a * mask_tex.a;
}