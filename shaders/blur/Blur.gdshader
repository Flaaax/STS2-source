// This is the shader used on NCard portrait & ancient portrait when it is blurred.
// It is the first pass (screen X) of a separable Gaussian blur; the second pass (screen Y) is done by the
// PortraitCanvasGroup with canvas_group_mask_blur.gdshader.

shader_type canvas_item;

#include "res://shaders/blur/portrait_blur.gdshaderinc"

// Blur radius in PortraitCanvasGroup units. Keep in sync with the canvas group blur materials.
uniform float radius = 16.0;

// Portrait's region within its atlas, in UV (xy = min, zw = max). Set per node by NCard so that taps are
// clamped to the sprite instead of bleeding in neighbouring portraits from the atlas.
instance uniform vec4 uv_region = vec4(0.0, 0.0, 1.0, 1.0);
// Converts the radius from canvas group units to this node's local units (1 / node scale).
instance uniform float radius_scale = 1.0;

varying vec2 local_pos;

void vertex() {
	local_pos = VERTEX;
}

void fragment() {
	vec2 tex_size = vec2(textureSize(TEXTURE, 0));

	// Atlas texels moved per screen pixel along screen X, and local units moved per screen pixel.
	vec2 texels_per_px = dFdx(UV) * tex_size;
	float texel_len = max(length(texels_per_px), 1e-6);
	float local_len = max(length(dFdx(local_pos)), 1e-6);

	float sigma = radius * radius_scale * BLUR_SIGMA_PER_RADIUS * (texel_len / local_len);
	// UV offset of one texel along screen X.
	vec2 dir = (texels_per_px / texel_len) / tex_size;

	// Keep bilinear taps half a texel inside the region so they never touch neighbouring sprites.
	vec2 lo = uv_region.xy + 0.5 * TEXTURE_PIXEL_SIZE;
	vec2 hi = uv_region.zw - 0.5 * TEXTURE_PIXEL_SIZE;

	vec3 sum = textureLod(TEXTURE, clamp(UV, lo, hi), 0.0).rgb;
	float wsum = 1.0;

	if (sigma > 0.5) {
		float unit = blur_unit(sigma);
		int pairs = blur_pairs(sigma, unit);
		float inv_two_sigma_sq = 1.0 / (2.0 * sigma * sigma);

		for (int k = 1; k <= BLUR_MAX_PAIRS; k++) {
			if (k > pairs) {
				break;
			}
			vec2 p = blur_pair(k, unit, inv_two_sigma_sq);
			vec2 o = dir * p.x;
			sum += p.y * textureLod(TEXTURE, clamp(UV + o, lo, hi), 0.0).rgb;
			sum += p.y * textureLod(TEXTURE, clamp(UV - o, lo, hi), 0.0).rgb;
			wsum += 2.0 * p.y;
		}
	}

	COLOR.rgb = sum / wsum;
	COLOR.a = 1.0;
}
