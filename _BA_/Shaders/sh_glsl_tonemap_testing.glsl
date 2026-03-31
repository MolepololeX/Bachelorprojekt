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




//=========================================================================================
//==========Conversion Functions===========================================================
//=========================================================================================

float cie_f(float I){
    if(I > pow(6.0/29.0, 3.0)){
        return pow(I, 1.0/3.0);
    }else{
        return (841.0/108.0) * I + (16.0/116.0);
    }
}

//Note: these are not normalized but in the range -100...100 for a and b and 0...100 for Luminance since deltaE2000 breaks if used with normalized cie values
vec3 linear_srgb_to_cielab(vec3 rgb){
    vec3 d65 = vec3(95.014, 100, 108.827);

	// mat3 rgb_to_xyz = mat3(
	// 	0.4124,	0.3576, 0.1805,
	// 	0.2126, 0.7152, 0.0722,
	// 	0.0193, 0.1192, 0.9505
	// );
	// vec3 xyz = rgb_to_xyz * rgb;

	vec3 xyz = vec3(
		0.4124 * rgb.r + 0.3576 * rgb.g + 0.1805 * rgb.b,
		0.2126 * rgb.r + 0.7152 * rgb.g + 0.0722 * rgb.b,
		0.0193 * rgb.r + 0.1192 * rgb.g + 0.9505 * rgb.b
	);
	xyz *= 100.0;


    float Xx = xyz.x / d65.x;
    float Yy = xyz.y / d65.y;
    float Zz = xyz.z / d65.z;

    float L = 116.0 * cie_f(Yy) - 16.0;
    float a = 500 * (cie_f(Xx) - cie_f(Yy));
    float b = 200 * (cie_f(Yy) - cie_f(Zz));

    return vec3(L, a, b);
}

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




//=========================================================================================
//==========Tonemapping Functions==========================================================
//=========================================================================================

float tonemap(float l){
	return -exp(-params.tonemapper_exposure*l) + 1;
}

vec4 tonemap(vec4 l){
	return -exp(-params.tonemapper_exposure*l) + 1;
}





//=========================================================================================
//==========Measurement Functions==========================================================
//=========================================================================================

const float PI = 3.141592653589793;
const float TwoPI = 6.283185307179586;

float oklab_delta_h(vec3 c1, vec3 c2){
	float h1 = atan(c1.z, c1.y);
	float h2 = atan(c2.z, c2.y);
	if (h1 < 0) h1 += 2.0 * PI;
	if (h2 < 0) h2 += 2.0 * PI;

	float d_h = h2 - h1;
	if (abs(d_h) <= PI)
	{
		d_h = h2 - h1;
	}
	else if (d_h > PI)
	{
		d_h = h2 - h1 - PI * 2.0;
	}
	else // (d_h < PI)
	{
		d_h = h2 - h1 + PI * 2.0;
	}
	return d_h;
}

float oklab_delta_C(vec3 c1, vec3 c2){
	float chroma_1 = length(vec2(c1.y, c1.z));
	float chroma_2 = length(vec2(c2.y, c2.z));
	return chroma_2 - chroma_1;
}

float oklab_delta_L(vec3 c1, vec3 c2){
	return c2.x - c1.x;
}

float oklab_delta_E(vec3 c1, vec3 c2){
	return length(c2 - c1);
}





float calculate_cie_de_2000_C(vec3 cs, vec3 cb){
	float C_star = (sqrt(cs.y * cs.y + cs.z * cs.z) + sqrt(cb.y * cb.y + cb.z * cb.z)) / 2.0;//original Chroma
	float G = 0.5 * (1.0 - sqrt(pow(C_star, 7.0) / (pow(C_star, 7.0) + pow(25.0, 7.0)))); //TODO fehler in der formel fixen

	float a_s = (1.0 + G) * cs.y;
	float b_s = cs.z;

	float a_b = (1.0 + G) * cb.y;
	float b_b = cb.z;

	float Cs = sqrt(a_s * a_s + b_s * b_s);
	float Cb = sqrt(a_b * a_b + b_b * b_b);

	float d_C = Cb - Cs;
	return d_C;
}

float calculate_cie_de_2000_H(vec3 cs, vec3 cb){
	float C_star = (sqrt(cs.y * cs.y + cs.z * cs.z) + sqrt(cb.y * cb.y + cb.z * cb.z)) / 2.0;//original Chroma
	float G = 0.5 * (1.0 - sqrt(pow(C_star, 7.0) / (pow(C_star, 7.0) + pow(25.0, 7.0)))); //TODO fehler in der formel fixen

	float a_s = (1.0 + G) * cs.y;
	float b_s = cs.z;

	float a_b = (1.0 + G) * cb.y;
	float b_b = cb.z;

	float Cs = sqrt(a_s * a_s + b_s * b_s);
	float Cb = sqrt(a_b * a_b + b_b * b_b);

	float hs = atan(b_s, a_s);
	float hb = atan(b_b, a_b);

	if (hs < 0) hs += 2.0 * PI;
	if (hb < 0) hb += 2.0 * PI;

	float d_h = hb - hs;
	if(Cs * Cb == 0.0){
		d_h = 0.0;
	}
	else
	{
		if (abs(d_h) <= PI)
		{
			d_h = hb - hs;
		}
		else if (d_h > PI)
		{
			d_h = hb - hs - PI * 2.0;
		}
		else // (d_h < Math.PI)
		{
			d_h = hb - hs + PI * 2.0;
		}
	}

	// float d_H = 2.0 * sqrt(Cb * Cs) * sin(d_h / 2.0);
	return d_h;
}

float calculate_cie_de_2000_L(vec3 cs, vec3 cb){
	float L_s = cs.x;
	float L_b = cb.x;
	float d_L = L_b - L_s;
	return d_L;
}

float DegToRad(float deg) {
	return deg * PI / 180.0;
}

float calculate_cie_de_2000(vec3 cs, vec3 cb)
{
	float C_star = (sqrt(cs.y * cs.y + cs.z * cs.z) + sqrt(cb.y * cb.y + cb.z * cb.z)) / 2.0;//original Chroma
	float G = 0.5 * (1.0 - sqrt(pow(C_star, 7.0) / (pow(C_star, 7.0) + pow(25.0, 7.0)))); 

	float L_s = cs.x;
	float a_s = (1.0 + G) * cs.y;
	float b_s = cs.z;

	float L_b = cb.x;
	float a_b = (1.0 + G) * cb.y;
	float b_b = cb.z;

	float Cs = sqrt(a_s * a_s + b_s * b_s);
	float Cb = sqrt(a_b * a_b + b_b * b_b);

	float hs = atan(b_s, a_s);
	float hb = atan(b_b, a_b);

	if (hs < 0) hs += 2.0 * PI;
	if (hb < 0) hb += 2.0 * PI;

	float d_L = L_b - L_s;
	float d_C = Cb - Cs;

	float d_h = hb - hs;
	if(Cs * Cb == 0.0){
		d_h = 0.0;
	}
	else
	{
		if (abs(d_h) <= PI)
		{
			d_h = hb - hs;
		}
		else if (d_h > PI)
		{
			d_h = hb - hs - PI * 2.0;
		}
		else // (d_h < Math.PI)
		{
			d_h = hb - hs + PI * 2.0;
		}
	}

	float d_H = 2.0 * sqrt(Cb * Cs) * sin(d_h / 2.0);

	float kL = 1.0;
	float kC = 1.0;
	float kH = 1.0;

	float m_L = (L_b + L_s) / 2.0;
	float m_C = (Cb + Cs) / 2.0;

	float m_h = (hs + hb) / 2.0;

	if(Cs * Cb == 0.0){
		m_h = hs + hb;
	}
	else
	{
		float diff = abs(hs - hb);
		if (diff <= PI)
		{
			// m_h = (hs + hb) / 2.0;
		}
		else if ((hs + hb) < 2.0 * PI)
		{
			m_h = (hs + hb + 2.0 * PI) / 2.0;
		}
		else //((hs+hb) < 2.0 * Math.PI)
		{
			m_h = (hs + hb - 2.0 * PI) / 2.0;
		}
	}

	float sL = 1.0 + (0.015 * pow(m_L - 50.0, 2.0)) / (sqrt(20.0 + pow(m_L - 50.0, 2.0)));
	float sC = 1.0 + 0.045 * m_C;

	float T = 1.0
		- 0.17 * cos(m_h - DegToRad(30.0))
		+ 0.24 * cos(2.0 * m_h)
		+ 0.32 * cos(3.0 * m_h + DegToRad(6.0))
		- 0.20 * cos(4.0 * m_h - DegToRad(63.0));

	float sH = 1.0 + 0.015 * m_C * T;

	float d_0 = DegToRad(30.0) * exp(-pow((m_h - DegToRad(275.0)) / DegToRad(25.0), 2.0));
	float Rc = 2.0 * sqrt(pow(m_C, 7.0) / (pow(m_C, 7.0) + pow(25.0, 7.0)));
	float Rt = -sin(2.0 * d_0) * Rc;

	float d_E_00 = sqrt(pow(d_L / (kL * sL), 2.0) + pow(d_C / (kC * sC), 2.0) + pow(d_H / (kH * sH), 2.0) + (Rt * (d_C / (kC * sC)) * (d_H / (kH * sH))));
	return d_E_00;
}





//=========================================================================================
//==========Main===========================================================================
//=========================================================================================

void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);

	vec4 base = imageLoad(color_image, uv_pixel);
	base = base * params.base_exposure;
	base = max(base, 0.0); //remove possible negative values, these might happen because of oklab colors from the test scene that are invalid rgb values/imaginary colors
	vec4 color = base;

	vec4 pre;
	vec4 post;

	pre = color;


	//testing
	if(params.draw_mode == 0.0){
	}

	//tonemap srgb Y
	if(params.tonemapper_mode == 1.0){
        float Y = 0.2126 * base.r + 0.7152 * base.g + 0.0722 * base.b;
		float Yt = tonemap(Y);
		color *= Yt / max(Y, 1e-5);
	}

	//tonemap oklab L
	if(params.tonemapper_mode == 2.0){

		vec3 lab = linear_srgb_to_oklab(color.xyz);

		float L = lab.x;
		float C = length(lab.yz);
		float h = atan(lab.z, lab.y);

		L = tonemap(L);

		// scale chroma slightly by new/old L to avoid clipping out of valid OKLAB or sRGB Chroma, will still happen but reduces it noticably
		C *= L / max(lab.x, 1e-5);

		lab.x = L;
		lab.y = C * cos(h);
		lab.z = C * sin(h);

		color = vec4(oklab_to_linear_srgb(lab), 1.0);
	}




	post = color;



	//color image
	if(params.draw_mode == 1.0){
		imageStore(color_image, uv_pixel, color);
		return;
	}



	float delta = 0.0;



	//oklab delta_h
	if(params.draw_mode == 2.0){

		float hue_diff = oklab_delta_h(
			linear_srgb_to_oklab(pre.xyz), 
			linear_srgb_to_oklab(post.xyz)
		);
		//correct normalization 1 ... -1
		delta /= PI;
	}

	//oklab delta_C
	if(params.draw_mode == 3.0){

		delta = oklab_delta_C(
			linear_srgb_to_oklab(pre.xyz), 
			linear_srgb_to_oklab(post.xyz)
		);
	}

	//oklab delta_L
	if(params.draw_mode == 4.0){
		//corrected delta L for tonemapping
		vec3 corrected_pre = linear_srgb_to_oklab(pre.xyz);
		corrected_pre.x = tonemap(corrected_pre.x);

		delta = oklab_delta_L(
			corrected_pre, 
			linear_srgb_to_oklab(post.xyz)
		);
	}

	//oklab deltaE
	if(params.draw_mode == 5.0){
		vec3 corrected_pre = linear_srgb_to_oklab(pre.xyz);
		corrected_pre.x = tonemap(corrected_pre.x);

		delta = oklab_delta_E(
			corrected_pre,
			linear_srgb_to_oklab(post.xyz)
		);
	}



	//cie delta_H
	if(params.draw_mode == 6.0){

		delta = calculate_cie_de_2000_H(
			linear_srgb_to_cielab(pre.xyz), 
			linear_srgb_to_cielab(post.xyz)
		);

		//normalization from CIE range to -1...1
		delta /= 100.0;
	}

	//cie delta_C
	if(params.draw_mode == 7.0){
		
		delta = calculate_cie_de_2000_C(
			linear_srgb_to_cielab(pre.xyz), 
			linear_srgb_to_cielab(post.xyz)
		);

		//normalization from CIE range to -1...1
		delta /= 100.0;
	}

	//cie delta_L
	if(params.draw_mode == 8.0){

		vec3 corrected_pre = linear_srgb_to_cielab(pre.xyz);
		//normalize before adjusting the tonemapping
		corrected_pre.x /= 100.0;
		corrected_pre.x = tonemap(corrected_pre.x);
		corrected_pre.x *= 100.0;

		delta = calculate_cie_de_2000_L(
			corrected_pre,
			linear_srgb_to_cielab(post.xyz)
		);

		//normalization from CIE range to -1...1
		delta /= 100.0;
	}

    //ciede2000
	if(params.draw_mode == 9.0){

		vec3 corrected_pre = linear_srgb_to_cielab(pre.xyz);
		//normalize before adjusting the tonemapping
		corrected_pre.x /= 100.0;
		corrected_pre.x = tonemap(corrected_pre.x);
		corrected_pre.x *= 100.0;

		delta = calculate_cie_de_2000(
			corrected_pre, 
			linear_srgb_to_cielab(post.xyz)	
		);

		//normalization from CIE range to -1...1
		delta /= 100.0;
	}



	//negative delta values R channel, positive B channel
	vec4 delta_mask = vec4(vec3(0.0), 1.0);
	if(delta < 0.0){
		delta_mask.r = abs(delta);
	}else{
		delta_mask.b = delta;
	}

	imageStore(color_image, uv_pixel, delta_mask);
}
