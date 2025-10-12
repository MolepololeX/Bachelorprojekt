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
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);
	vec2 size = ivec2(params.raster_size);
	vec2 UV = uv_pixel / size;

	if (uv_pixel.x >= size.x || uv_pixel.y >= size.y) {
		return;
	}

	vec4 color = imageLoad(color_image, uv_pixel);

	// float mask = texture(normal_texture, UV).a;
	// mask = ceil(mask);
	// vec4 color = texture(normal_texture, uv);
	// vec4 color = vec4(d);
    //color *= 0.1;
	color = vec4(getNormal(UV).xyz, 1.0);

	imageStore(color_image, uv_pixel, color);
}