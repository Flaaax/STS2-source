shader_type canvas_item;
render_mode blend_sub;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec4 main = texture(TEXTURE, UV);
	COLOR = main * vertex_color;
}