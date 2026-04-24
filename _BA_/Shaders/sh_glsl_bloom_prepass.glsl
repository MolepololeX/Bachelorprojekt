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





//=========================================================================================
//==========Conversion Functions===========================================================
//=========================================================================================

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

//from https://github.com/flixel-gdx/flixel-gdx/blob/master/flixel-core/src/org/flixel/data/shaders/blend/luminosity.glsl
vec4 RGBToHSL(vec4 color)
{
	vec4 hsl; // init to 0 to avoid warnings ? (and reverse if + remove first part)
	
	float fmin = min(min(color.r, color.g), color.b);    //Min. value of RGB
	float fmax = max(max(color.r, color.g), color.b);    //Max. value of RGB
	float delta = fmax - fmin;             //Delta RGB value

	hsl.z = (fmax + fmin) / 2.0; // Luminance

	if (delta == 0.0)		//This is a gray, no chroma...
	{
		hsl.x = 0.0;	// Hue
		hsl.y = 0.0;	// Saturation
	}
	else                                    //Chromatic data...
	{
		if (hsl.z < 0.5)
			hsl.y = delta / (fmax + fmin); // Saturation
		else
			hsl.y = delta / (2.0 - fmax - fmin); // Saturation
		
		float deltaR = (((fmax - color.r) / 6.0) + (delta / 2.0)) / delta;
		float deltaG = (((fmax - color.g) / 6.0) + (delta / 2.0)) / delta;
		float deltaB = (((fmax - color.b) / 6.0) + (delta / 2.0)) / delta;

		if (color.r == fmax )
			hsl.x = deltaB - deltaG; // Hue
		else if (color.g == fmax)
			hsl.x = (1.0 / 3.0) + deltaR - deltaB; // Hue
		else if (color.b == fmax)
			hsl.x = (2.0 / 3.0) + deltaG - deltaR; // Hue

		if (hsl.x < 0.0)
			hsl.x += 1.0; // Hue
		else if (hsl.x > 1.0)
			hsl.x -= 1.0; // Hue
	}

	return hsl;
}

float gaussian(float r, float o){
   return (1.0 / (o * sqrt(2.0 * 3.14159265359)))   *   pow(2.71828182845, - ( (pow(r,2.0)) / (2.0*pow(o,2.0)) ));
}



//=========================================================================================
//==========Main===========================================================================
//=========================================================================================

void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);
	vec4 base = imageLoad(color_image, uv_pixel * int(params.image_size_x / params.bloom_size_x)); //TODO maybe make this a sampler aswell
	// vec4 base = imageLoad(color_image, uv_pixel * 2); //TODO maybe make this a sampler aswell

    //srgb bloom
    if(params.draw_mode == 1.0){
        vec3 baseHSL = RGBToHSL(base).xyz;
        if (baseHSL.z < params.bloom_threshold){
            base = vec4(vec3(0.0), 1.0);
        }
    }

    //oklab bloom
    if(params.draw_mode == 2.0){
        vec3 baseLAB = linear_srgb_to_oklab(base.xyz);
        if (baseLAB.x < params.bloom_threshold){
            base = vec4(vec3(0.0), 1.0);
        }
    }

    //srgb Y bloom
    if(params.draw_mode == 3.0){
        float Y = 0.2126 * base.r + 0.7152 * base.g + 0.0722 * base.b;
        if (Y < params.bloom_threshold){
            base = vec4(vec3(0.0), 1.0);
        }
    }

    imageStore(ping_texture, uv_pixel, base);
}
