// NOTE: Shader automatically converted from Godot Engine 4.5.stable.mono's StandardMaterial3D.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_burley, specular_schlick_ggx, unshaded;

uniform vec4 tint : source_color;
uniform sampler2D viewport_texture : source_color, filter_nearest, repeat_disable;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform float alpha : hint_range(0.0, 1.0, 0.001) = 1.0;



uniform vec2 rimThreshold0 = vec2(0.0, 0.0277);
uniform vec2 rimThreshold1 = vec2(0.0, 0.0277);
uniform vec2 rimThreshold2 = vec2(0.0, 0.0277);

uniform vec3 rimColor : source_color;

uniform vec2 noiseScale = vec2 (1.0, 1.0);
uniform float timeScale = 1.0;
uniform float impact = 0.01;
uniform bool useBlurr = false;
uniform float blurrStength = 1.0;
uniform sampler2D noiseTexture;

uniform mat4 obliqueProjection;



varying float test;

void vertex() {
	//y billboard temp solution for sampling noise not by screen-uv
	
	// Billboard Mode: Y-Billboard
	MODELVIEW_MATRIX = VIEW_MATRIX * mat4(
			vec4(normalize(cross(vec3(0.0, 1.0, 0.0), MAIN_CAM_INV_VIEW_MATRIX[2].xyz)), 0.0),
			vec4(0.0, 1.0, 0.0, 0.0),
			vec4(normalize(cross(MAIN_CAM_INV_VIEW_MATRIX[0].xyz, vec3(0.0, 1.0, 0.0))), 0.0),
			MODEL_MATRIX[3]);

	// Billboard Keep Scale: Enabled
	MODELVIEW_MATRIX = MODELVIEW_MATRIX * mat4(
			vec4(length(MODEL_MATRIX[0].xyz), 0.0, 0.0, 0.0),
			vec4(0.0, length(MODEL_MATRIX[1].xyz), 0.0, 0.0),
			vec4(0.0, 0.0, length(MODEL_MATRIX[2].xyz), 0.0),
			vec4(0.0, 0.0, 0.0, 1.0));
	MODELVIEW_NORMAL_MATRIX = mat3(MODELVIEW_MATRIX);
}

float linearizeDepth(vec2 uv, float depth, mat4 inv_proj_matrix){
	//from godot documentation
	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	#else
	vec3 ndc = vec3(uv * 2.0 - 1.0, depth);
	#endif
	vec4 view = inv_proj_matrix * vec4(ndc, 1.0);
  	view.xyz /= view.w;
  	return -view.z;
}

vec3 sampleBlurredColor(vec2 uTexelSize, vec2 base_uv){
	vec2 offsets[9] = vec2[](
        vec2(-uTexelSize.x, -uTexelSize.y),
        vec2(0.0,           -uTexelSize.y),
        vec2( uTexelSize.x, -uTexelSize.y),
        vec2(-uTexelSize.x,  0.0),
        vec2(0.0,            0.0),
        vec2( uTexelSize.x,  0.0),
        vec2(-uTexelSize.x,  uTexelSize.y),
        vec2(0.0,            uTexelSize.y),
        vec2( uTexelSize.x,  uTexelSize.y)
    );

    float kernel[9] = float[](
        1.0/16.0, 2.0/16.0, 1.0/16.0,
        2.0/16.0, 4.0/16.0, 2.0/16.0,
        1.0/16.0, 2.0/16.0, 1.0/16.0
    );

    vec3 color = vec3(0.0);
    for (int i = 0; i < 9; i++) {
        color += texture(viewport_texture, base_uv + offsets[i]).rgb * kernel[i];
    }
	return color;
}

void fragment() {
	
	
	vec2 base_uv = SCREEN_UV;
	float noise = texture(noiseTexture, (UV * noiseScale) + (TIME * timeScale)).r;
	float n = noise * 2.0 - 1.0;
	base_uv.x += n * impact;
	
	vec4 albedo_tex = texture(viewport_texture, base_uv);
	
	//blurr testing
	vec2 uTexelSize = 1.0 / VIEWPORT_SIZE.xy;
	uTexelSize *= blurrStength;
	vec3 blurredCol = sampleBlurredColor(uTexelSize, base_uv);
	
	if(useBlurr){
		ALBEDO = tint.rgb * blurredCol;
	}else{
		ALBEDO = tint.rgb * albedo_tex.rgb;
	}

	ALPHA = alpha;
	
	//thin line at the shore
	float depth = texture(depth_texture, SCREEN_UV).r;
	depth = linearizeDepth(SCREEN_UV, depth, INV_PROJECTION_MATRIX);
	float fragDepth = FRAGCOORD.z;
	fragDepth = linearizeDepth(SCREEN_UV, fragDepth, INV_PROJECTION_MATRIX);
	
	
	
	
	float diff = depth - fragDepth;
	if(
		(diff < rimThreshold0.y && diff > rimThreshold0.x) ||
		(diff < rimThreshold1.y && diff > rimThreshold1.x) ||
		(diff < rimThreshold2.y && diff > rimThreshold2.x)
		){
		ALBEDO *= rimColor;
	}
	//ALBEDO = vec3();
	//ALBEDO = vec3(noise);
	//ALBEDO = vec3(test, 0.0, 0.0);
}