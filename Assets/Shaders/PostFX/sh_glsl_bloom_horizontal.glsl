#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	float bloom_threshold;
    float bloom_strength;
    float bloom_weight;
	float blurr_kernelsize;
    float blurr_kernelspacing;
	float draw_mode;
    float image_size_x;
    float image_size_y;
    float bloom_size_x;
    float bloom_size_y;
} params;

layout(rgba16f, set = 0, binding = 1) uniform image2D color_image;
layout(set = 0, binding = 2) uniform sampler2D ping_sampler;
layout(set = 0, binding = 3) uniform sampler2D pong_sampler;
layout(rgba16f, set = 0, binding = 4) uniform image2D ping_texture;
layout(rgba16f, set = 0, binding = 5) uniform image2D pong_texture;



float gaussian(float r, float o){
   return (1.0 / (o * sqrt(2.0 * 3.14159265359)))   *   pow(2.71828182845, - ( (pow(r,2.0)) / (2.0*pow(o,2.0)) ));
}



void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);
    vec2 imageSize = vec2(params.bloom_size_x, params.bloom_size_y);
    vec2 uv = uv_pixel / imageSize;
	vec4 base = texture(ping_sampler, uv);

    int kernelSize = int(params.blurr_kernelsize);
    vec3 color = vec3(0.0);

    for(int i = -kernelSize; i <= kernelSize; i++){
        float g = gaussian(float(i), params.bloom_weight);
        vec2 offset = vec2(i * params.blurr_kernelspacing, 0);
        vec4 col = texture(ping_sampler, uv + (offset / imageSize));
        color += col.xyz * g;
    }

    base = vec4(color.xyz, 1.0);

    imageStore(pong_texture, uv_pixel, base);
}
