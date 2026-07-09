shader_type canvas_item;

//set these values within code (ouside of this shader)
group_uniforms Uniforms_For_Code;
uniform float current_mana = 0.5;
uniform float max_mana = 1.0;
uniform float rank_advance_mana = 1.0;

uniform float inset = .8;

//Border Values
group_uniforms Outer;
uniform float groove_opacity = 0.5;
uniform float groove_speed = 0.1;
uniform float groove_width = 0.075;
uniform float groove_distance = 0.3;
uniform sampler2D noise;
uniform sampler2D groovy_noise;
uniform vec4 border_color : source_color;
uniform float border_width = 0.1;
uniform float border_speed = 0.02;


//Inner Values
group_uniforms Inner;
uniform float glow_strength = 0.5;
uniform vec4 mana_color : source_color;


mat2 rotate2d(float _angle){
    return mat2(vec2(cos(_angle),-sin(_angle)),
                vec2(sin(_angle),cos(_angle)));
}


float circle(float d, float radius, float circlewidth){
	return step(d,radius+circlewidth)-step(d,radius);
}


float lines(vec2 st, float d, float w){
	return 1.0 - step( w, mod( st.y, d));
}


float inverse_lerp(float a, float b, float c){
    return (c - a)/(b - a);
}


float smoothcircle(float d, float radius, float circlewidth, float smoothing){
	float outer = smoothstep(radius-smoothing,radius,d);
	float inner = smoothstep(radius+circlewidth,radius+circlewidth+smoothing,d);
	return outer - inner;
	return smoothstep(radius,radius+smoothing,d+circlewidth) * smoothstep(d,d+smoothing,radius);
}


void fragment() {
	vec2 st = (UV - .5) * 2.0;
	
	//polar coordinate from center
	float d = length(st);
	float angle = atan( st.x, st.y) / TAU;
	
	//border mask
	float ratio = min( 1.0, max_mana / rank_advance_mana);
	ratio *= inset;
	float noise_value1 = texture( noise,
			vec2( fract( TIME * border_speed), fract(angle - TIME * border_speed))).r - .5;
	noise_value1 /= 10.0;
	float noise_value2 = texture( noise, 
			vec2( fract( angle + TIME * border_speed * 5.0), fract(TIME * border_speed))).r - .5; 
	noise_value2 /= 12.0;
	float noise_value = noise_value1 + noise_value2;
	float max_mana_mask = circle( d, ratio + noise_value, border_width);
	float lerp = ( inverse_lerp(ratio + noise_value, ratio + noise_value + border_width, d) );
	max_mana_mask *= lerp * lerp;
	
	//groove mask
	float groovy_mask = circle( d, 0.0, ratio + noise_value + border_width);
	float groovy_noise_value = texture( groovy_noise, fract(UV+TIME * groove_speed)).x;
	groovy_noise_value = lines( rotate2d(groovy_noise_value * 3.0) 
			* (st + vec2( 0.0, sin(TIME * groove_speed))), groove_distance, groove_width);
	groovy_mask *= groovy_noise_value;
	
	//mana mask
	float mana_ratio = (current_mana / max_mana) * ratio * 1.05;
	float mana_mask = smoothcircle( d - noise_value1 * d * 2.0, -0.05, mana_ratio + .1, 0.05) 
		* float(d < ratio + noise_value + border_width  / 1.5) ;
	
	//blending colors
	vec4 color = vec4(0.0);
	if(max_mana_mask > 0.0){
		color = border_color;
		color.a *= max_mana_mask;
	}
	if(mana_mask > 0.0 && current_mana > 0.0){
		vec4 f = mana_color * (d / mana_ratio) * glow_strength;
		color = mix(color * max_mana_mask,mana_color + f, mana_mask);
		color.rgb = mana_color.rgb;
		color.rgb += f.rgb;
		color.a = mana_mask * mana_color.a;
	}
	if(groovy_mask > 0.0){
		color = mix(color * max(mana_mask,max_mana_mask) ,border_color,groove_opacity);
	}
	COLOR = color;
}
