#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	float bloom_threshold;
    float bloom_strength;
	float blurr_kernelsize;
    float blurr_kernelspacing;
	float draw_mode;
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

//from https://www.shadertoy.com/view/XljGzV
vec3 rgb_to_hsl(vec3 c){
  float h = 0.0;
	float s = 0.0;
	float l = 0.0;
	float r = c.r;
	float g = c.g;
	float b = c.b;
	float cMin = min( r, min( g, b ) );
	float cMax = max( r, max( g, b ) );

	l = ( cMax + cMin ) / 2.0;
	if ( cMax > cMin ) {
		float cDelta = cMax - cMin;
        
		s = l < .0 ? cDelta / ( cMax + cMin ) : cDelta / ( 2.0 - ( cMax + cMin ) );
		if ( r == cMax ) {
			h = ( g - b ) / cDelta;
		} else if ( g == cMax ) {
			h = 2.0 + ( b - r ) / cDelta;
		} else {
			h = 4.0 + ( r - g ) / cDelta;
		}
		if ( h < 0.0) {
			h += 6.0;
		}
		h = h / 6.0;
	}
	return vec3( h, s, l );
}

void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);
	vec4 base = imageLoad(color_image, uv_pixel);

	//none
	if(params.draw_mode == 0.0){
		// imageStore(color_image, uv_pixel, base);
	}

    //srgb bloom
    if(params.draw_mode == 1.0){
        int kernelSize = int(params.blurr_kernelsize);
        vec3 color = vec3(0.0);
        for(int i = -kernelSize; i <= kernelSize; i++){
            for(int j = -kernelSize; j <= kernelSize; j++){
                vec4 col = imageLoad(color_image, uv_pixel + ivec2(i * params.blurr_kernelspacing, j * params.blurr_kernelspacing));
                vec3 colHSL = rgb_to_hsl(col.xyz);
                if (colHSL.z < params.bloom_threshold){
                    col = vec4(vec3(0.0), 1.0);
                }
                color += col.xyz;
            }
        }
        color /= pow(kernelSize + kernelSize + 1, 2.0);

        imageStore(color_image, uv_pixel, base + vec4(color.xyz, 1.0) * params.bloom_strength);
    }

    //srgb bloom mask
    if(params.draw_mode == 2.0){
        vec3 baseHSL = rgb_to_hsl(base.xyz);
        if (baseHSL.z >=params.bloom_threshold){
            imageStore(color_image, uv_pixel, base);
        }else{
            imageStore(color_image, uv_pixel, vec4(vec3(0.0), 1.0));
        }
    }

    //oklab bloom
    if(params.draw_mode == 3.0){
        int kernelSize = int(params.blurr_kernelsize);
        vec3 color = vec3(0.0);
        for(int i = -kernelSize; i <= kernelSize; i++){
            for(int j = -kernelSize; j <= kernelSize; j++){
                vec4 col = imageLoad(color_image, uv_pixel + ivec2(i * params.blurr_kernelspacing, j * params.blurr_kernelspacing));
                vec3 colLAB = linear_srgb_to_oklab(col.xyz);
                if (colLAB.x < params.bloom_threshold){
                    col = vec4(vec3(0.0), 1.0);
                }
                color += col.xyz;
            }
        }
        color /= pow(kernelSize + kernelSize + 1, 2.0);

        imageStore(color_image, uv_pixel, base + vec4(color.xyz, 1.0) * params.bloom_strength);
    }

    //oklab bloom mask
    if(params.draw_mode == 4.0){
        vec3 baseLAB = linear_srgb_to_oklab(base.xyz);
        if (baseLAB.x >=params.bloom_threshold){
            imageStore(color_image, uv_pixel, base);
        }else{
            imageStore(color_image, uv_pixel, vec4(vec3(0.0), 1.0));
        }
    }
}
