shader_type canvas_item;

uniform vec2 source_pos;
uniform float angle_offset;

uniform sampler2D angle_curve;

void vertex() {
	
}

void fragment() {
	vec2 delta = UV - source_pos; 
	float angle = atan(delta.y, delta.x) / (2.0 * PI);
	angle += angle_offset;
	angle = fract(angle);
	
	float curve_tex = texture(angle_curve, vec2(angle)).r;
	
	COLOR = vec4(curve_tex);
}
