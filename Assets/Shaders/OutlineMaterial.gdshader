// NOTE: Shader automatically converted from Godot Engine 4.5.stable.mono's StandardMaterial3D.

shader_type spatial;
render_mode blend_mix, depth_draw_opaque, cull_back, diffuse_toon, specular_toon;

uniform float depthThreshold : hint_range(0, 10) = 0.05;
uniform float normalThreshold : hint_range(0, 10) = 0.05;
uniform float outlineDarkness : hint_range(-1, 1) = 0.75;
uniform float outlineLightness : hint_range(-10, 10) = 2.0;
uniform float thiccness : hint_range(0, 10) = 1.0;

uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D normal_texture : source_color, hint_normal_roughness_texture, filter_nearest;
uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;

uniform vec4 albedo : source_color;
uniform sampler2D texture_albedo : source_color, filter_linear_mipmap, repeat_enable;
uniform ivec2 albedo_texture_size;
uniform float point_size : hint_range(0.1, 128.0, 0.1);

uniform float roughness : hint_range(0.0, 1.0);
uniform sampler2D texture_metallic : hint_default_white, filter_linear_mipmap, repeat_enable;
uniform vec4 metallic_texture_channel;
uniform sampler2D texture_roughness : hint_roughness_r, filter_linear_mipmap, repeat_enable;

uniform float specular : hint_range(0.0, 1.0, 0.01);
uniform float metallic : hint_range(0.0, 1.0, 0.01);

uniform vec3 uv1_scale;
uniform vec3 uv1_offset;
uniform vec3 uv2_scale;
uniform vec3 uv2_offset;

varying mat4 inv_proj_mat;

float sampleLinearDepth(vec2 uv, mat4 inv_proj_matrix){
	//from godot documentation
	float depth = texture(depth_texture, uv).x;
	#if CURRENT_RENDERER == RENDERER_COMPATIBILITY
	vec3 ndc = vec3(SCREEN_UV, depth) * 2.0 - 1.0;
	#else
	vec3 ndc = vec3(uv * 2.0 - 1.0, depth);
	#endif
	vec4 view = inv_proj_matrix * vec4(ndc, 1.0);
  	view.xyz /= view.w;
  	return -view.z;
}

void vertex() {
	UV = UV * uv1_scale.xy + uv1_offset.xy;

	inv_proj_mat = INV_PROJECTION_MATRIX;
}

void fragment() {
	vec2 base_uv = UV;

	vec4 albedo_tex = texture(texture_albedo, base_uv);
	ALBEDO = albedo.rgb * albedo_tex.rgb;

	float metallic_tex = dot(texture(texture_metallic, base_uv), metallic_texture_channel);
	METALLIC = metallic_tex * metallic;
	SPECULAR = specular;

	vec4 roughness_texture_channel = vec4(1.0, 0.0, 0.0, 0.0);
	float roughness_tex = dot(texture(texture_roughness, base_uv), roughness_texture_channel);
	ROUGHNESS = roughness_tex * roughness;

	ALBEDO = vec3(sampleLinearDepth(SCREEN_UV, inv_proj_mat) / 100.0);
	
	vec2 texelSize = 1.0 / VIEWPORT_SIZE.xy;
	vec3 normal = texture(normal_texture, SCREEN_UV).xyz * 2.0 - 1.0;
	float depth = sampleLinearDepth(SCREEN_UV, INV_PROJECTION_MATRIX);
	vec2[4] depth_uv;

	depth_uv[0] = SCREEN_UV + vec2(1.0, 0.0) * texelSize * thiccness;
	depth_uv[1] = SCREEN_UV + vec2(-1.0, 0.0) * texelSize * thiccness;
	depth_uv[2] = SCREEN_UV + vec2(0.0, -1.0) * texelSize * thiccness;
	depth_uv[3] = SCREEN_UV + vec2(0.0, 1.0) * texelSize * thiccness;

	float depthDifference = 0.0;
	vec2 closestDepthUV = SCREEN_UV;
	float normalSum = 0.0;

	for(int i = 0; i < 4; i++){
		float testDepth = sampleLinearDepth(depth_uv[i], INV_PROJECTION_MATRIX);
		depthDifference += depth - testDepth;

		if(sampleLinearDepth(closestDepthUV, INV_PROJECTION_MATRIX) > testDepth){
			closestDepthUV = depth_uv[i];
		}

		vec3 n = texture(normal_texture, depth_uv[i]).xyz * 2.0 - 1.0;
		vec3 normalDiff = normal - n;

		vec3 normalEdgeBias = vec3(1,1,1);
		float normalBiasDiff = dot(normalDiff, normalEdgeBias);
		float normalIndicator = smoothstep(-0.01, 0.01, normalBiasDiff);

		normalSum += dot(normalDiff, normalDiff) * normalIndicator;
	}

	float indicator = sqrt(normalSum);
	float normalEdge = step(normalThreshold, indicator);

	float depthEdge = clamp(step(depthThreshold, depthDifference), 0.0, 1.0);

	vec3 original = texture(screen_texture, SCREEN_UV).rgb;
	vec3 outline = texture(screen_texture, closestDepthUV).rgb * outlineDarkness;

	//float steps = 256.0;
//
	//int originalR = int(original.r * steps);
	//int originalG = int(original.g * steps);
	//int originalB = int(original.b * steps);
//
	//original = vec3(float(originalR) / steps, float(originalG) / steps, float(originalB) / steps);

	//if(depthEdge > 0.0){
		//ALBEDO = mix(original, outline, depthEdge);
		//ALBEDO = vec3(0.0);
		////ALPHA = depthEdge;
	//} else{
		ALBEDO = mix(original, original * outlineLightness, normalEdge);
		//ALPHA = normalEdge;
	//}
}
