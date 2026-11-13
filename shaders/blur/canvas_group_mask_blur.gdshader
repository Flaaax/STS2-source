// This is the shader used on NCard PortraitCanvasGroup when it is blurred.
// It is the second pass (screen Y) of a separable Gaussian blur; the first pass (screen X) is done by
// Blur.gdshader on the portrait itself.
// It supports masking the CanvasGroup with the rounded ancient card border.

shader_type canvas_item;

#include "res://shaders/blur/portrait_blur.gdshaderinc"

// This should be set to the max size of the contents of the canvas group.
const vec2 size = vec2(598, 842);

// Linear filtering is required: the blur relies on bilinear taps to cover two pixels at once.
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_linear;

uniform sampler2D mask_texture : repeat_disable;
uniform vec4 mask_region;
uniform vec2 mask_offset;

// Blur radius in PortraitCanvasGroup units. Keep in sync with card_portrait_blur_material.
uniform float radius = 32.0;

uniform bool mask;
uniform bool blur;

// --- Ancient glass border (replaces the separate AncientBorderGlassOverlay screen-reading shader) ---
uniform bool glass = false;
// R/A = border band, G = corner highlights. Same texture the old overlay shader used.
uniform sampler2D glass_mask : repeat_disable, filter_linear;
// Where the glass quad sits inside the canvas group, in 0-1 group UVs (xy = offset, zw = size).
uniform vec4 glass_rect = vec4(0.01517, 0.01019, 0.98751, 0.98814);
// Blur radius, in canvas group texels (so it scales with the card).
uniform float glass_blur = 6.0;
uniform float glass_add = 0.15;
uniform vec4 glass_corner_color : source_color = vec4(1.0);
uniform float glass_corner_weight : hint_range(0.0, 1.0, 0.1) = 0.2;

varying float rotation;
varying vec2 local_pos;

// Samples the backbuffer at the correct position for the canvas group. Taken from the Godot docs:
// https://docs.godotengine.org/en/stable/classes/class_canvasgroup.html
vec4 sample(vec2 uv)
{
	vec4 col = textureLod(screen_texture, uv, 0.0);

	if (col.a > 0.0001) {
		col.rgb /= col.a;
	}
	
	return col;
}
// When the CanvasGroup is rotated, the renderer enlarges for some reason (seems like no reason...)
// https://forum.godotengine.org/t/canvasgroup-shader-uv-is-too-large-with-rotated-node/124571/12
// This method returns the corrected UVs that tightly enclose the rotated group.
vec2 shrink_rotated_canvas_group_uv(vec2 uv) {
	float pixel_total_width = size.x + sin(2.0 * abs(rotation)) * size.y;
	float pixel_total_height = size.y + sin(2.0 * abs(rotation)) * size.x;
	
	// Turn pixel values to shader values
	float shader_total_width = pixel_total_width / size.x;
	float shader_total_height = pixel_total_height / size.y;
	
	float shader_offset_x = (shader_total_width - 1.0) / 2.0;
	float shader_offset_y = (shader_total_height - 1.0) / 2.0;
	
	return uv * vec2(shader_total_width, shader_total_height) - vec2(shader_offset_x, shader_offset_y);
}

// Cheap disc blur of the group's own backbuffer. Alpha-weighted so the transparent area outside
// the portrait doesn't darken the edge.
vec3 glass_sample(vec2 uv, vec2 r)
{
	const vec2 TAPS[12] = vec2[](
		vec2(1.0, 0.0), vec2(0.5, 0.866), vec2(-0.5, 0.866), vec2(-1.0, 0.0), vec2(-0.5, -0.866), vec2(0.5, -0.866),
		vec2(0.433, 0.25), vec2(0.0, 0.5), vec2(-0.433, 0.25), vec2(-0.433, -0.25), vec2(0.0, -0.5), vec2(0.433, -0.25));
	vec4 acc = textureLod(screen_texture, uv, 0.0);
	for (int i = 0; i < 12; i++)
	{
		acc += textureLod(screen_texture, uv + TAPS[i] * r, 0.0);
	}
	return acc.a > 0.0001 ? acc.rgb / acc.a : acc.rgb;
}

void vertex() {
	vec4 rot_checker = MODEL_MATRIX * vec4(1, 0, 0, 0);
	rotation = atan(rot_checker.y, rot_checker.x);
	local_pos = VERTEX;
}

// Accumulates one premultiplied backbuffer tap, `px` screen pixels along screen Y. Taps that land outside the
// canvas group's quad are dropped: the backbuffer is only cleared there, beyond it is unrelated screen content.
void blur_tap(vec2 screen_uv, vec2 uv, float px, float w, vec2 screen_step, vec2 uv_step, inout vec4 sum) {
	vec2 tap_uv = uv + uv_step * px;
	if (all(greaterThanEqual(tap_uv, vec2(0.0))) && all(lessThanEqual(tap_uv, vec2(1.0)))) {
		sum += w * textureLod(screen_texture, screen_uv + screen_step * px, 0.0);
	}
}

void fragment() {
	if (blur)
	{
		// Screen pixels per canvas group unit, so the radius matches pass 1 regardless of card scale.
		float local_len = max(length(dFdy(local_pos)), 1e-6);
		float sigma = radius * BLUR_SIGMA_PER_RADIUS / local_len;

		vec2 screen_step = vec2(0.0, SCREEN_PIXEL_SIZE.y);
		vec2 uv_step = dFdy(UV);

		// The backbuffer is premultiplied, so accumulating it as-is gives an alpha-weighted blur. Dividing by the
		// accumulated alpha at the end means transparent pixels around the portrait don't darken its edges.
		vec4 sum = textureLod(screen_texture, SCREEN_UV, 0.0);

		if (sigma > 0.5) {
			float unit = blur_unit(sigma);
			int pairs = blur_pairs(sigma, unit);
			float inv_two_sigma_sq = 1.0 / (2.0 * sigma * sigma);

			for (int k = 1; k <= BLUR_MAX_PAIRS; k++) {
				if (k > pairs) {
					break;
				}
				vec2 p = blur_pair(k, unit, inv_two_sigma_sq);
				blur_tap(SCREEN_UV, UV, p.x, p.y, screen_step, uv_step, sum);
				blur_tap(SCREEN_UV, UV, -p.x, p.y, screen_step, uv_step, sum);
			}
		}

		COLOR.rgb = sum.a > 0.0001 ? sum.rgb / sum.a : vec3(0.0);

		// Keep the portrait's own coverage, otherwise the rest of the group's quad is drawn black.
		// (The mask path below already does this.)
		if (!mask)
		{
			COLOR.a *= textureLod(screen_texture, SCREEN_UV, 0.0).a;
		}
	}
	else
	{
		COLOR.rgb = sample(SCREEN_UV).rgb;
	}
	
	if (mask)
	{
		vec2 maskSize = vec2(textureSize(mask_texture, 0));
		vec2 texSize = vec2(textureSize(TEXTURE, 0));
		vec4 screenSample = sample(SCREEN_UV);
		
		vec2 adjustedUv = shrink_rotated_canvas_group_uv(UV);
		adjustedUv = clamp(adjustedUv, 0, 1);
		
		vec2 maskOffset = mask_offset / maskSize;
		vec2 maskRegionSizeUv = mask_region.zw / maskSize + maskOffset;
		vec2 maskRegionUv = mask_region.xy / maskSize;
		vec2 maskUv = adjustedUv * maskRegionSizeUv + maskRegionUv - maskOffset;
		maskUv = clamp(maskUv, maskRegionUv, maskRegionUv + maskRegionSizeUv);
		vec4 maskSample = texture(mask_texture, maskUv);
		COLOR.a *= screenSample.a * maskSample.a;
	}

	if (glass)
	{
		vec2 groupUv = clamp(shrink_rotated_canvas_group_uv(UV), 0.0, 1.0);
		vec4 g = texture(glass_mask, (groupUv - glass_rect.xy) / glass_rect.zw);

		// When the portrait is already blurred (locked/unknown cards), COLOR.rgb is the finished
		// two-pass blur, so just frost that. Otherwise blur the sharp portrait a little.
		vec3 behind = COLOR.rgb;
		if (!blur)
		{
			// Screen pixels per group texel, so the blur radius follows card scale and rotation.
			float texel = 1.0 / (length(vec2(dFdx(UV.x), dFdy(UV.x))) * size.x);
			behind = glass_sample(SCREEN_UV, glass_blur * texel * SCREEN_PIXEL_SIZE);
		}

		// Same maths as debug_ancient_card_border_overlay.gdshader, composited in place.
		float cornerMix = g.g * glass_corner_weight;
		vec3 glassRgb = mix(behind + vec3(glass_add), glass_corner_color.rgb, cornerMix) * g.a;
		float glassA = mix(g.a, glass_corner_color.a, cornerMix) * g.a;
		COLOR.rgb = mix(COLOR.rgb, glassRgb, glassA);
	}
}
