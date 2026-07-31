vec2 flipbook(vec2 uv, vec2 size, float progress) 
{
	float h_frames = float(size.x);
	float v_frames = float(size.y);
	float particle_total_frames = float(size.x * size.y);
	float particle_frame = floor(progress);
	particle_frame = mod(particle_frame, particle_total_frames);
	
	vec2 return_uv = uv/vec2(h_frames, v_frames);
	return_uv += vec2(mod(particle_frame, h_frames) / h_frames, floor((particle_frame + 0.5) / h_frames) / v_frames);
	
	return return_uv;
}