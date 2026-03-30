#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	float steps; //== palette_size
	float palette_type;
	float quantization_type;
	float dither_spread;
} params;

layout(rgba16f, set = 0, binding = 1) uniform image2D color_image;
layout(rgba16f, set = 0, binding = 2) uniform image2D palette_hsl_image;
layout(rgba16f, set = 0, binding = 3) uniform image2D palette_oklch_image;



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



//=========================================================================================
//==========Main===========================================================================
//=========================================================================================

void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);
	vec4 base = imageLoad(color_image, uv_pixel);
	base = max(base, 0.0); //remove possible negative values, these might happen because of oklab colors from the test scene that are invalid rgb values/imaginary colors
	vec4 color = base;

	//use either oklab L or Y for quantization
	float Y;
	if(params.quantization_type == 0){
		Y = 0.2126 * base.r + 0.7152 * base.g + 0.0722 * base.b;
	}
	if(params.quantization_type == 1){
		vec3 lab = linear_srgb_to_oklab(color.xyz);
		Y = lab.x;
	}
	if (Y >= 1.0) Y = 0.999999;



	float[][] bayerMatrix = float[4][4](
		float[4](	0,	8,	2,	10	),
		float[4](	12,	4,	14,	6	),
		float[4](	3,	11,	1,	9	),
		float[4](	15,	7,	13,	5	)
	);
	float M = bayerMatrix[int(mod(uv_pixel.x, 4))][int(mod(uv_pixel.y, 4))];
	M *= 1.0 / pow(4, 2.0) - 0.5;

	Y += M * params.dither_spread;



	//quantize by lightness
	float steps = params.steps;
	Y = floor(Y * (steps)) / (steps);



	//apply palette
	if(params.palette_type == 0.0){
		color = imageLoad(palette_hsl_image, ivec2(steps * Y,0));
	}

	if(params.palette_type == 1.0){
		color = imageLoad(palette_oklch_image, ivec2(steps * Y,0));
	}



	imageStore(color_image, uv_pixel, color);
}
