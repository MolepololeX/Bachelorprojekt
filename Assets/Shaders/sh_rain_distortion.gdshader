shader_type spatial;

render_mode unshaded, blend_mix, particle_trails, cull_disabled;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform float alpha : hint_range(0.0, 1.0, 0.01) = 0.5;
uniform vec2 noiseScale = vec2 (1.0, 1.0);
uniform float timeScale = 1.0;
uniform float impact = 0.01;
uniform bool useColorOverride = false;
uniform vec3 colorOverride : source_color;
uniform sampler2D noiseTexture;

void vertex() {
	// Billboard Mode: Enabled
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			MAIN_CAM_INV_VIEW_MATRIX[0],
			MAIN_CAM_INV_VIEW_MATRIX[1],
			MAIN_CAM_INV_VIEW_MATRIX[2],
			MODEL_MATRIX[3]);

	// Billboard Keep Scale: Enabled
	MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(
			vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0),
			vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
			vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0),
			vec4(0.0, 0.0, 0.0, 1.0));
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}

void fragment() {
	
	vec2 base_uv = SCREEN_UV;
	float noise = texture(noiseTexture, (SCREEN_UV * noiseScale) + (TIME * timeScale)).r;
	float n = noise * 2.0 - 1.0;
	base_uv.x += n * impact;
	
	
	
	ALBEDO = texture(screen_texture, base_uv).rgb;
	ALPHA = alpha;
	
	// Vertex Color Use as Albedo: Enabled
	ALBEDO *= COLOR.xyz;
	
	if(useColorOverride)
		ALBEDO *= colorOverride;
	// Called for every pixel the material is visible on.
}

//void light() {
//	// Called for every pixel for every light affecting the material.
//	// Uncomment to replace the default light processing function with this one.
//}
