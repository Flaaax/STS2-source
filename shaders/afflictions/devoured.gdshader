shader_type canvas_item;
render_mode blend_premul_alpha;

// Animated void/hunger effect for the Devoured affliction.
// Dark void creeps inward from card edges, suggesting the card is being consumed.

uniform float edge_intensity : hint_range(0.0, 1.0) = 0.8;
uniform float pulse_speed : hint_range(0.1, 4.0) = 0.7;
uniform float void_width : hint_range(0.0, 0.5) = 0.175;
uniform float noise_scale : hint_range(1.0, 20.0) = 5.0;
uniform float void_stretch : hint_range(1.0, 6.0) = 3.0;
uniform vec4 void_color : source_color = vec4(0.08, 0.0, 0.12, 1.0);
uniform vec4 void_edge_color : source_color = vec4(0.25, 0.02, 0.35, 1.0);
uniform float corner_radius : hint_range(0.0, 0.15) = 0.055;

float hash(vec2 p) {
	vec3 p3 = fract(vec3(p.xyx) * 0.1031);
	p3 += dot(p3, p3.yzx + 33.33);
	return fract((p3.x + p3.y) * p3.z);
}

float value_noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	f = f * f * (3.0 - 2.0 * f);

	float a = hash(i);
	float b = hash(i + vec2(1.0, 0.0));
	float c = hash(i + vec2(0.0, 1.0));
	float d = hash(i + vec2(1.0, 1.0));

	return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p) {
	float value = 0.0;
	float amplitude = 0.5;
	for (int i = 0; i < 4; i++) {
		value += amplitude * value_noise(p);
		p *= 2.1;
		amplitude *= 0.5;
	}
	return value;
}

float rounded_rect_sdf(vec2 p, vec2 half_size, float radius) {
	vec2 d = abs(p) - half_size + vec2(radius);
	return length(max(d, vec2(0.0))) + min(max(d.x, d.y), 0.0) - radius;
}

void fragment() {
	vec2 uv = UV;
	float t = TIME * pulse_speed;

	// Card shape mask
	vec2 centered = uv - 0.5;
	float sdf = rounded_rect_sdf(centered, vec2(0.5), corner_radius);
	float card_mask = 1.0 - smoothstep(-0.003, 0.0, sdf);
	float edge_dist = max(-sdf, 0.0);

	// Determine nearest-edge direction for void stretching.
	// edge_blend: 0 = nearest edge is horizontal (top/bottom), 1 = vertical (left/right)
	float dist_h = min(uv.y, 1.0 - uv.y);
	float dist_v = min(uv.x, 1.0 - uv.x);
	float edge_blend = smoothstep(0.0, 0.05, dist_h - dist_v);

	// Stretch noise along the edge so round blobs become elongated fingers.
	// Near left/right edges: compress X, stretch Y. Near top/bottom: compress Y, stretch X.
	vec2 stretch_lr = vec2(1.0, void_stretch);
	vec2 stretch_tb = vec2(void_stretch, 1.0);
	vec2 stretch = mix(stretch_tb, stretch_lr, edge_blend);

	// Warp noise coordinates in place using sin/cos oscillation (no scrolling)
	vec2 noise_uv = uv * noise_scale * stretch;
	vec2 warp = vec2(
		sin(t * 0.7 + uv.y * 4.0) * 0.4 + cos(t * 0.5 + uv.x * 3.0) * 0.3,
		cos(t * 0.6 + uv.x * 4.0) * 0.4 + sin(t * 0.4 + uv.y * 3.0) * 0.3
	) * stretch;

	// Two noise layers with different warp phases for organic morphing
	float n1 = fbm(noise_uv + warp);
	float n2 = fbm(noise_uv * 1.4 + warp * 0.7 + 5.0);
	float combined_noise = mix(n1, n2, 0.45);

	// Void reaches inward from edges, shaped by noise
	float reach = void_width + combined_noise * 0.18;
	float void_mask = smoothstep(reach, 0.0, edge_dist);

	// Sharpen for more definition
	void_mask = smoothstep(0.1, 0.6, void_mask);

	// Breathing pulse that varies along the edge
	float breath = 0.8 + 0.2 * sin(t * 1.3 + combined_noise * 3.0);
	void_mask *= breath;

	// Color gradient: deep void at edge, lighter at the tips
	float edge_color_blend = smoothstep(0.08, 0.0, edge_dist);
	vec3 effect_color = mix(void_edge_color.rgb, void_color.rgb, edge_color_blend);

	// Faint flickering specks within the void
	float speck = value_noise(noise_uv * 3.5 + warp * 2.0);
	speck = step(0.9, speck) * void_mask * 0.35;
	effect_color += vec3(0.3, 0.1, 0.5) * speck;

	float alpha = void_mask * edge_intensity * card_mask;
	COLOR = vec4(effect_color * alpha, alpha);
}
