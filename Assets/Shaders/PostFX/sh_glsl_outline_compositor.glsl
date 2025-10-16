#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	vec2 raster_size;
	vec2 reserved;
	mat4 inv_proj_mat;
} params;

layout(rgba16f, set = 0, binding = 1) uniform image2D color_image;
layout(set = 0, binding = 2) uniform sampler2D depth_texture;
layout(set = 0, binding = 3) uniform sampler2D normal_texture;

//from this guy: https://www.youtube.com/watch?v=jCWyXKJdHCk
vec4 normalRoughnessCompatibility(vec4 p_normal_roughness) {
	float roughness = p_normal_roughness.w;
	if (roughness > 0.5) {
		roughness = 1.0 - roughness;
	}
	roughness /= (127.0 / 255.0);
	vec4 normal_comp = vec4(normalize(p_normal_roughness.xyz * 2.0 - 1.0) * 0.5 + 0.5, roughness);
	normal_comp = normal_comp * 2.0 - 1.0;
	return normal_comp;
}

vec4 getNormal(vec2 uv){
	// vec4 normal = texture(normal_texture, uv + offset);//TODO: maybe das mit dem offset nochmal anschauen
	return normalRoughnessCompatibility(texture(normal_texture, uv));
}

// From godot documentation
float sampleLinearDepth(vec2 uv){
	float depth = texture(depth_texture, uv).x;
	vec3 ndc = vec3(uv * 2.0 - 1.0, depth);
	vec4 view = params.inv_proj_mat * vec4(ndc, 1.0);
  	view.xyz /= view.w;
  	return -view.z;
}



void main() {
	//TODO: uniforms
	// vec2 VIEWPORT_SIZE = vec2(320, 180);
	float thiccness = 1.0;
	vec3 normalEdgeBias = vec3(1,1,1);
	float normalThreshold = 0.9;
	float depthThreshold = 0.5;
	float outlineDarkness = 0.5;
	float outlineLightness = 1.1;
	//

	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);
	vec2 size = vec2(params.raster_size);
	vec2 UV = uv_pixel / size;
	vec2 texel_size = 0.5 / vec2(size);
	vec2 snapped_uv = floor(UV / texel_size + 0.5) * texel_size;

	if (uv_pixel.x >= size.x || uv_pixel.y >= size.y) {
		return;
	}

	// float mask = texture(normal_texture, UV).a;
	// mask = ceil(mask);
	// vec4 color = texture(normal_texture, uv);
	// vec4 color = vec4(d);
    //color *= 0.1;
	// color = vec4(getNormal(UV).xyz, 1.0);

	vec2 texelSize = 1.0 / size.xy;
	vec3 normal = getNormal(UV).xyz * 2.0 - 1.0;
	float depth = sampleLinearDepth(UV);
	vec2[4] depth_uv;
	vec2 offset = vec2(0.001);

	depth_uv[0] = UV + vec2(1.0, 0.0) * texelSize * thiccness  - offset;
	depth_uv[1] = UV + vec2(-1.0, 0.0) * texelSize * thiccness - offset;
	depth_uv[2] = UV + vec2(0.0, -1.0) * texelSize * thiccness - offset;
	depth_uv[3] = UV + vec2(0.0, 1.0) * texelSize * thiccness  - offset;

	float depthDifference = 0.0;
	vec2 closestDepthUV = UV;
	float normalSum = 0.0;

	for(int i = 0; i < 4; i++){
		float testDepth = sampleLinearDepth(depth_uv[i]);
		depthDifference += depth - testDepth;

		if(sampleLinearDepth(closestDepthUV) > testDepth){
			closestDepthUV = depth_uv[i];
		}

		vec3 n = getNormal(depth_uv[i]).xyz * 2.0 - 1.0;
		vec3 normalDiff = normal - n;

		float normalBiasDiff = dot(normalDiff, normalEdgeBias);
		float normalIndicator = smoothstep(-0.01, 0.01, normalBiasDiff);

		normalSum += dot(normalDiff, normalDiff) * normalIndicator;
	}

	float indicator = sqrt(normalSum);
	float normalEdge = step(normalThreshold, indicator);

	float depthEdge = clamp(step(depthThreshold, depthDifference), 0.0, 1.0);

	vec4 original = imageLoad(color_image, uv_pixel);
	vec4 outline = vec4(imageLoad(color_image, uv_pixel).rgb * outlineDarkness, 1.0);

	if(depthEdge > 0.0){
		original = mix(original, outline, depthEdge);
	} else{
		original = mix(original, original * outlineLightness, normalEdge);
	}

	//original *= vec4(getNormal(UV).rgb * 2.0 - 1.0, 1.0);
	original = imageLoad(color_image, uv_pixel);
	float packed = uintBitsToFloat(packHalf2x16(original.xy));
	// vec2 unpacked = unpackHalf2x16(floatBitsToUint(packed));
	original = vec4(packed, original.y, original.z ,sampleLinearDepth(snapped_uv) / 100.0);
	// original = vec4(UV.x, UV.y, 0.0, 1.0);

	imageStore(color_image, uv_pixel, original);
}