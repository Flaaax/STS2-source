// Shared kernel helpers for the two-pass card portrait blur (Blur.gdshader + canvas_group_mask_blur.gdshader).
//
// Both passes express the blur radius in PortraitCanvasGroup units and convert it to their own sample space
// (atlas texels for pass 1, screen pixels for pass 2) using screen-space derivatives. This keeps the blur
// isotropic and the same relative size no matter how large the card is drawn, or how it is rotated.
//
// The kernel is a true Gaussian evaluated on the fly, sampled with the "linear sampling" trick: one bilinear
// tap covers two adjacent texels with the correct combined weight, so N taps per side cover 2N texels.

// Gaussian sigma as a fraction of the blur radius.
const float BLUR_SIGMA_PER_RADIUS = 0.5;
// How far the kernel extends, in sigmas. Weight at the edge is exp(-2.5^2 / 2) ~= 4%.
const float BLUR_EXTENT_SIGMAS = 2.5;
// Max bilinear taps per side. Total taps per pass = 2 * BLUR_MAX_PAIRS + 1.
const int BLUR_MAX_PAIRS = 20;

// Spacing between texels the kernel covers. Stays at 1 (exact) until the kernel would need more than
// BLUR_MAX_PAIRS taps per side, after which it spreads out to cap the cost.
float blur_unit(float sigma) {
	return max(1.0, sigma * BLUR_EXTENT_SIGMAS / (2.0 * float(BLUR_MAX_PAIRS)));
}

int blur_pairs(float sigma, float unit) {
	return clamp(int(ceil(sigma * BLUR_EXTENT_SIGMAS / (2.0 * unit))), 0, BLUR_MAX_PAIRS);
}

// Returns (offset, weight) for a single bilinear tap covering the texels at (2k-1)*unit and (2k)*unit.
vec2 blur_pair(int k, float unit, float inv_two_sigma_sq) {
	float a = float(2 * k - 1) * unit;
	float b = float(2 * k) * unit;
	float wa = exp(-a * a * inv_two_sigma_sq);
	float wb = exp(-b * b * inv_two_sigma_sq);
	float w = wa + wb;
	return vec2((a * wa + b * wb) / w, w);
}
