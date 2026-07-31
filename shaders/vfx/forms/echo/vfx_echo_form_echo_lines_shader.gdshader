shader_type canvas_item;
render_mode blend_mix;

uniform sampler2D mask_texture : hint_default_white;

uniform vec2 pivot_offset = vec2(0.0, 0.0);

uniform vec2 scale_center = vec2(0.5, 0.5);
uniform float scale_factor = 1.1;

uniform vec2 scale_alpha_smoothstep = vec2(0.0, 1.0);

varying vec4 vertex_color;

void vertex() 
{
	VERTEX += pivot_offset * (1.0/TEXTURE_PIXEL_SIZE);
	vertex_color = COLOR;
}

vec2 scale(vec2 uv, vec2 center, float scale)
{
	vec2 scaled_uv = uv;
	scaled_uv -= center;
	scaled_uv /= scale;
	scaled_uv += center;
	
	return scaled_uv;
}

void fragment() 
{
	vec4 scaled_main_tex = texture(TEXTURE, scale(UV, scale_center, scale_factor));
	vec4 main_tex = texture(TEXTURE, UV);
	
	float mask = texture(mask_texture, UV).r;
	
	float alpha = scaled_main_tex.a - main_tex.a;
	alpha = smoothstep(scale_alpha_smoothstep.x, scale_alpha_smoothstep.y, alpha);
	alpha *= mask;
	
	COLOR = vec4(1.0);
	COLOR.a = alpha;
	COLOR *= vertex_color;	
}

//void light() {
//	// Called for every pixel for every light affecting the CanvasItem.
//	// Uncomment to replace the default light processing function with this one.
//}
