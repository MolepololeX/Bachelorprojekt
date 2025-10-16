shader_type spatial;

render_mode unshaded, fog_disabled, blend_mix;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform sampler2D depth_texture : hint_depth_texture, repeat_disable, filter_nearest;
uniform sampler2D normal_texture : source_color, hint_normal_roughness_texture, filter_nearest;

uniform float depthThreshold : hint_range(0, 10) = 0.05;
uniform vec3 outlineColor : source_color;
uniform float thiccness : hint_range(0, 10) = 1.0;

void vertex() {
	POSITION = vec4(VERTEX.xy, 1.0, 1.0);
}

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

void fragment() {
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

	for(int i = 0; i < 4; i++){
		float testDepth = sampleLinearDepth(depth_uv[i], INV_PROJECTION_MATRIX);
		depthDifference += depth - testDepth;

		if(sampleLinearDepth(closestDepthUV, INV_PROJECTION_MATRIX) > testDepth){
			closestDepthUV = depth_uv[i];
		}
	}

	float depthEdge = clamp(step(depthThreshold, depthDifference), 0.0, 1.0);
	vec3 original = texture(screen_texture, SCREEN_UV).rgb;
	ALBEDO = mix(original, outlineColor, depthEdge);
}
