#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	vec2 raster_size;
	vec2 reserved;
	mat4 inv_proj_mat;
} params;

layout(rgba16f, set = 0, binding = 1) uniform image2D color_image;

vec3 linear_srgb_to_oklab(vec3 c) 
{
    float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
	float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
	float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

    float l_ = pow(l, 1.0/3.0);
    float m_ = pow(m, 1.0/3.0);
    float s_ = pow(s, 1.0/3.0);

    return vec3(
        0.2104542553f*l_ + 0.7936177850f*m_ - 0.0040720468f*s_,
        1.9779984951f*l_ - 2.4285922050f*m_ + 0.4505937099f*s_,
        0.0259040371f*l_ + 0.7827717662f*m_ - 0.8086757660f*s_
    );
}

// L | c.x
// A | c.y
// B | c.z
vec3 oklab_to_linear_srgb(vec3 c) 
{
    float l_ = c.x + 0.3963377774f * c.y + 0.2158037573f * c.z;
    float m_ = c.x - 0.1055613458f * c.y - 0.0638541728f * c.z;
    float s_ = c.x - 0.0894841775f * c.y - 1.2914855480f * c.z;

    float l = l_*l_*l_;
    float m = m_*m_*m_;
    float s = s_*s_*s_;

    return vec3(
		+4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
		-1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
		-0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s
    );
}


float tonemap(float l){
	float exposure = -1.5;
	return -exp(exposure*l) + 1;
}

vec4 tonemap(vec4 l){
	float exposure = -1.5;
	return -exp(exposure*l) + 1;
}


void main() {
	float base_exposure = 1.0;

	int mode = 1;

	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);

	vec4 base = imageLoad(color_image, uv_pixel);

	base = base * base_exposure;

	vec4 color = base;

	vec4 test1 = color;
	vec4 test2 = color;

	float L = 0.0;

	if(mode == 0){
	}
	if(mode == 1){
		color = tonemap(color);
	}
	if(mode == 2){
		color = vec4(linear_srgb_to_oklab(color.xyz),1.0);
		color.x = tonemap(color.x);
		color = vec4(oklab_to_linear_srgb(color.xyz),1.0);
	}

	// if(color.r > 1.0 || color.g > 1.0 || color.b > 1.0){
	// 	color = vec4(0.0);
	// }
	// if(L > 0.0){
	// 	color = vec4(L);
	// }
	// else{
	// 	color = vec4(0.0);
	// }

	imageStore(color_image, uv_pixel, color);
}