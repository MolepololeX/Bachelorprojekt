#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	float base_exposure;
	float tonemapper_mode;
	float tonemapper_exposure;
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


float tonemap(float l){
	return -exp(-params.tonemapper_exposure*l) + 1;
}

vec4 tonemap(vec4 l){
	return -exp(-params.tonemapper_exposure*l) + 1;
}

//oklab delta_h
float get_oklab_delta_h(vec3 c1, vec3 c2){
	float hue_1 = atan(c1.y, c1.z);
	float hue_2 = atan(c2.y, c2.z);
	return hue_2 - hue_1;
}

//c1 and c2 need to be in Lab format
float get_cie76_delta_E(vec3 c1, vec3 c2){
	return sqrt(pow((c2.x - c1.x), 2.0) + pow((c2.y - c1.y), 2.0) + pow((c2.z - c1.z), 2.0));
}

//cs and cb need to be in CIELAB and Lab Format
float calculate_cie_de_2000(vec3 cs, vec3 cb){


	float m_C_ = (sqrt(cs.y * cs.y + cs.z * cs.z) + sqrt(cb.y * cb.y + cb.z * cb.z)) / 2.0;
	float G = 0.5 * (1.0 - sqrt( 	pow(m_C_, 7.0) / ( pow(m_C_, 7.0) + pow(25.0, 7.0) )	)); //TODO fehler in der formel fixen

	float Ls = cs.x;
	float as = (1.0 + G)*cs.y;
	float bs = cs.z;

	float Lb = cb.x;
	float ab = (1.0 + G)*cb.y;
	float bb = cb.z;

	float Cs = sqrt(as * as + bs * bs);
	float Cb = sqrt(ab * ab + bb * bb);

	float hs = atan(bs / as);
	float hb = atan(bb / ab);

	float d_h = hb - hs;
	float d_L = Lb - Ls;
	float d_C = Cb - Cs;
	float d_H = 2.0 * sqrt(Cb * Cs) * sin(d_h / 2.0);

	float kL = 1.0;
	float kC = 1.0;
	float kH = 1.0;

	float m_L = (Lb + Ls) / 2.0;
	float m_C = (Cb + Cs) / 2.0;
	float m_h = (hs + hb) / 2.0;

	float sL = 1.0 + (0.015 * pow(m_L - 50.0, 2.0)) / (sqrt(20.0 + pow(m_L - 50.0, 2.0)));
	float sC = 1.0 + 0.045 * m_C;
	float T = 1.0 - 0.17 * cos(m_h - 30.0) + 0.24 * cos(2.0 * m_h) + 0.32 * cos(3.0 * m_h + 6.0) - 0.20 * cos(4.0 * m_h - 63);
	float sH = 1.0 + 0.015 * m_C * T;

	float d_0 = 30.0 * exp(-pow((m_h - 275.0) / 25.0, 2.0));
	float Rc = 2.0 * sqrt(pow(m_C, 7.0) / (pow(m_C, 7.0) + pow(25.0, 7.0)));
	float Rt = -sin(2.0 * d_0) * Rc;

	float d_E_00 = sqrt(pow(d_L / (kL * sL), 2.0) + pow(d_C / (kC * sC), 2.0) + pow(d_H / (kH * sH), 2.0) +		( Rt * (d_C / (kC * sC)) * (d_H / (kH * sH)) )		);
	return d_E_00;
}

void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);

	vec4 base = imageLoad(color_image, uv_pixel);

	base = base * params.base_exposure;

	vec4 color = base;

	vec4 pre;
	vec4 post;

	pre = color;


	//directly on sRGB values
	if(params.tonemapper_mode == 1.0){
		color = tonemap(color);
	}
	//using oklab L
	if(params.tonemapper_mode == 2.0){
		color = vec4(linear_srgb_to_oklab(color.xyz),1.0);
		color.x = tonemap(color.x);
		color = vec4(oklab_to_linear_srgb(color.xyz),1.0);
	}

	post = color;


	//normal image
	if(params.draw_mode == 1.0){
		imageStore(color_image, uv_pixel, color);
	}

	//oklab delta_h
	if(params.draw_mode == 2.0){

		float hue_diff = get_oklab_delta_h(linear_srgb_to_oklab(pre.xyz), linear_srgb_to_oklab(post.xyz));

		vec4 hue_diff_mask = vec4(vec3(0.0), 1.0);
		if(hue_diff < 0.0){
			hue_diff_mask.r = abs(hue_diff);
		}else{
			hue_diff_mask.b = hue_diff;
		}

		imageStore(color_image, uv_pixel, hue_diff_mask);
	}

	//cie76
	if(params.draw_mode == 3.0){

		float diff = get_cie76_delta_E(linear_srgb_to_oklab(pre.xyz), linear_srgb_to_oklab(post.xyz));

		vec4 diff_mask = vec4(vec3(0.0), 1.0);
		if(diff < 0.0){
			diff_mask.r = abs(diff);
		}else{
			diff_mask.b = diff;
		}

		imageStore(color_image, uv_pixel, diff_mask);
	}
}