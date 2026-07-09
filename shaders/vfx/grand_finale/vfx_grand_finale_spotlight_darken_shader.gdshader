shader_type canvas_item;
render_mode blend_sub;

uniform vec2 source_pos;
uniform float angle_offset;
uniform float invert : hint_range(0.0, 1.0, 0.1);

uniform vec2 angle_parameters;
uniform vec2 erosion;

varying vec4 vertex_color;

void vertex() {
	vertex_color = COLOR;
}

void fragment() {
	vec2 delta = UV - source_pos; 
	float angle = atan(delta.y, delta.x) / (2.0 * PI);
	angle += angle_offset;
	angle = fract(angle);
	
	float angle_dist = abs(angle - angle_parameters.x) / angle_parameters.y;
	angle_dist = clamp(angle_dist, 0.0, 1.0);
	angle_dist = mix(angle_dist, 1.0 - angle_dist, invert);
	
	angle_dist = smoothstep(erosion.x, erosion.x + erosion.y, angle_dist);
	
	COLOR = vec4(vec3(angle_dist), 1.0) * vertex_color;
}