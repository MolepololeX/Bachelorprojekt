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

vec3 oklab_to_xyz(vec3 c){
    float l_ = c.x + 0.3963377774f * c.y + 0.2158037573f * c.z;
    float m_ = c.x - 0.1055613458f * c.y - 0.0638541728f * c.z;
    float s_ = c.x - 0.0894841775f * c.y - 1.2914855480f * c.z;

    float l = l_*l_*l_;
    float m = m_*m_*m_;
    float s = s_*s_*s_;

    vec3 lms = vec3(l, m, s);

    // mat3 lms_to_xyz = mat3(
    //     1.2270, -0.5575, 0.2810,
    //     -0.0406, 1.1030, -0.0125,
    //     -0.0171, -0.0709, 0.9427
    // );

	// https://www.uni-weimar.de/fileadmin/user/fak/medien/professuren/Computer_Graphics/3-ima-color-spaces17.pdf
    mat3 lms_to_xyz = mat3(
		1.9102,  -1.1121,  0.2019,
		0.371,   0.6291,   0.0,
		0.0,     0.0,      1.0
    );

    vec3 xyz = lms * lms_to_xyz;

    return xyz;
}

float f(float I){
    if(I > pow(6.0/29.0, 3.0)){
        return pow(I, 1.0/3.0);
    }else{
        return (841.0/108.0) * I + (16.0/116.0);
    }
}

vec3 xyz_to_cielab(vec3 xyz){
    vec3 d65 = vec3(94.811, 100, 107.304);

    float Yy = xyz.y / d65.y;
    float Xx = xyz.x / d65.x;
    float Zz = xyz.z / d65.z;

    float L = 116.0 * f(Yy) - 16.0;
    float a = 500 * (f(Xx) - f(Yy));
    float b = 200 * (f(Yy) - f(Zz));

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

//from https://github.com/flixel-gdx/flixel-gdx/blob/master/flixel-core/src/org/flixel/data/shaders/blend/luminosity.glsl
float HueToRGB(float f1, float f2, float hue)
{
	if (hue < 0.0)
		hue += 1.0;
	else if (hue > 1.0)
		hue -= 1.0;
	float res;
	if ((6.0 * hue) < 1.0)
		res = f1 + (f2 - f1) * 6.0 * hue;
	else if ((2.0 * hue) < 1.0)
		res = f2;
	else if ((3.0 * hue) < 2.0)
		res = f1 + (f2 - f1) * ((2.0 / 3.0) - hue) * 6.0;
	else
		res = f1;
	return res;
}

//from https://github.com/flixel-gdx/flixel-gdx/blob/master/flixel-core/src/org/flixel/data/shaders/blend/luminosity.glsl
vec4 HSLToRGB(vec4 hsl)
{
	vec4 rgb;
	
	if (hsl.y == 0.0)
		rgb = vec4(hsl.z); // Luminance
	else
	{
		float f2;
		
		if (hsl.z < 0.5)
			f2 = hsl.z * (1.0 + hsl.y);
		else
			f2 = (hsl.z + hsl.y) - (hsl.y * hsl.z);
			
		float f1 = 2.0 * hsl.z - f2;
		
		rgb.r = HueToRGB(f1, f2, hsl.x + (1.0/3.0));
		rgb.g = HueToRGB(f1, f2, hsl.x);
		rgb.b = HueToRGB(f1, f2, hsl.x - (1.0/3.0));
	}	
	return rgb;
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

float calculate_oklab_delta_h(vec3 c1, vec3 c2){
	float hue_1 = atan(c1.z, c1.y);
	float hue_2 = atan(c2.z, c2.y);
	return hue_2 - hue_1;
}

float calculate_oklab_delta_C(vec3 c1, vec3 c2){
	float chroma_1 = sqrt(pow(c1.y, 2.0) + pow(c1.z, 2.0));
	float chroma_2 = sqrt(pow(c2.y, 2.0) + pow(c2.z, 2.0));
	return chroma_2 - chroma_1;
}

float calculate_oklab_delta_E(vec3 c1, vec3 c2){
	return sqrt(pow((c2.x - c1.x), 2.0) + pow((c2.y - c1.y), 2.0) + pow((c2.z - c1.z), 2.0));
}

float calculate_cie_de_2000_C(vec3 cs, vec3 cb){
	float m_C_ = (sqrt(cs.y * cs.y + cs.z * cs.z) + sqrt(cb.y * cb.y + cb.z * cb.z)) / 2.0;
	float G = 0.5 * (1.0 - sqrt( 	pow(m_C_, 7.0) / ( pow(m_C_, 7.0) + pow(25.0, 7.0) )	));

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

	return d_C;
}

float calculate_cie_de_2000_H(vec3 cs, vec3 cb){
	float m_C_ = (sqrt(cs.y * cs.y + cs.z * cs.z) + sqrt(cb.y * cb.y + cb.z * cb.z)) / 2.0;
	float G = 0.5 * (1.0 - sqrt( 	pow(m_C_, 7.0) / ( pow(m_C_, 7.0) + pow(25.0, 7.0) )	));

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

	return d_H;
}

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





//=========================================================================================
//==========Main===========================================================================
//=========================================================================================

void main() {
	ivec2 uv_pixel = ivec2(gl_GlobalInvocationID.xy);

	vec4 base = imageLoad(color_image, uv_pixel);
	base = base * params.base_exposure;
	vec4 color = base;

	vec4 pre;
	vec4 post;

	pre = color;



	//tonemap srgb
	if(params.tonemapper_mode == 1.0){
		// color = RGBToHSL(color);
		color = vec4(tonemap(color).xyz, 1.0);
		// color = HSLToRGB(color);
	}
	//using oklab L
	if(params.tonemapper_mode == 2.0){
		color = vec4(linear_srgb_to_oklab(color.xyz),1.0);
		color.x = tonemap(color.x);
		color = vec4(oklab_to_linear_srgb(color.xyz),1.0);
	}



	post = color;



	//color image
	if(params.draw_mode == 1.0){
		imageStore(color_image, uv_pixel, color);
	}

	//oklab delta_h
	if(params.draw_mode == 2.0){

		float hue_diff = calculate_oklab_delta_h(linear_srgb_to_oklab(pre.xyz), linear_srgb_to_oklab(post.xyz));

		vec4 hue_diff_mask = vec4(vec3(0.0), 1.0);
		if(hue_diff < 0.0){
			hue_diff_mask.r = abs(hue_diff);
		}else{
			hue_diff_mask.b = hue_diff;
		}

		imageStore(color_image, uv_pixel, hue_diff_mask);
	}
	
	//oklab delta_C
	if(params.draw_mode == 3.0){
		float hue_diff = calculate_oklab_delta_C(linear_srgb_to_oklab(pre.xyz), linear_srgb_to_oklab(post.xyz));

		vec4 hue_diff_mask = vec4(vec3(0.0), 1.0);
		if(hue_diff < 0.0){
			hue_diff_mask.r = abs(hue_diff);
		}else{
			hue_diff_mask.b = hue_diff;
		}

		imageStore(color_image, uv_pixel, hue_diff_mask);
	}

	//oklab deltaE
	if(params.draw_mode == 4.0){

		float diff = calculate_oklab_delta_E(linear_srgb_to_oklab(pre.xyz), linear_srgb_to_oklab(post.xyz));

		vec4 diff_mask = vec4(vec3(0.0), 1.0);
		if(diff < 0.0){
			diff_mask.r = abs(diff);
		}else{
			diff_mask.b = diff;
		}

		imageStore(color_image, uv_pixel, diff_mask);
	}

	//cie delta_H
	if(params.draw_mode == 5.0){
		vec3 c1 = linear_srgb_to_oklab(pre.xyz);
        vec3 c2 = linear_srgb_to_oklab(post.xyz);
        c1 = oklab_to_xyz(c1);
        c2 = oklab_to_xyz(c2);
        c1 = xyz_to_cielab(c1);
        c2 = xyz_to_cielab(c2);
		float diff = calculate_cie_de_2000_H(c1, c2);

		//cielab normalisierung
		diff/=100.0;

		vec4 diff_mask = vec4(vec3(0.0), 1.0);
		if(diff < 0.0){
			diff_mask.r = abs(diff);
		}else{
			diff_mask.b = diff;
		}

		imageStore(color_image, uv_pixel, diff_mask);
	}

	//cie delta_C
	if(params.draw_mode == 6.0){
		vec3 c1 = linear_srgb_to_oklab(pre.xyz);
        vec3 c2 = linear_srgb_to_oklab(post.xyz);
        c1 = oklab_to_xyz(c1);
        c2 = oklab_to_xyz(c2);
        c1 = xyz_to_cielab(c1);
        c2 = xyz_to_cielab(c2);
		float diff = calculate_cie_de_2000_C(c1, c2);

		//cielab normalisierung
		diff/=100.0;

		vec4 diff_mask = vec4(vec3(0.0), 1.0);
		if(diff < 0.0){
			diff_mask.r = abs(diff);
		}else{
			diff_mask.b = diff;
		}

		imageStore(color_image, uv_pixel, diff_mask);
	}

    //ciede2000
	if(params.draw_mode == 7.0){
        vec3 c1 = linear_srgb_to_oklab(pre.xyz);
        vec3 c2 = linear_srgb_to_oklab(post.xyz);
        c1 = oklab_to_xyz(c1);
        c2 = oklab_to_xyz(c2);
        c1 = xyz_to_cielab(c1);
        c2 = xyz_to_cielab(c2);
		float diff = calculate_cie_de_2000(c1, c2);

		//cielab normalisierung
		diff/=100.0;

		vec4 diff_mask = vec4(vec3(0.0), 1.0);
		if(diff < 0.0){
			diff_mask.r = abs(diff);
		}else{
			diff_mask.b = diff;
		}

		imageStore(color_image, uv_pixel, diff_mask);
	}
}
