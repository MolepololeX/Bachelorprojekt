#version 450

// Invocations in the (x, y, z) dimension
layout(local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

layout(set = 0, binding = 0, std430) readonly buffer Params {
	float base_exposure;
	float tonemapper_mode;
	float tonemapper_exposure;
	float tonemapper_saturation;
	float draw_mode;
} params;

layout(rgba16f, set = 0, binding = 1) uniform image2D color_image;
layout(rgba16f, set = 0, binding = 2) uniform image2D image_before;


//=========================================================================================
//==========OKLAB Gamut Clipping By Bjoern Ottosson========================================
//=========================================================================================
struct Lab {float L; float a; float b;};
struct RGB {float r; float g; float b;};

const float FLT_MAX = 1.0;

Lab linear_srgb_to_oklab(RGB c)
{
	float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
	float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
	float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

	float l_ = pow(l, 1.0/3.0);
	float m_ = pow(m, 1.0/3.0);
	float s_ = pow(s, 1.0/3.0);

	return Lab(
		0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
		1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
		0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_
	);
}

RGB oklab_to_linear_srgb(Lab c)
{
    float l_ = c.L + 0.3963377774f * c.a + 0.2158037573f * c.b;
    float m_ = c.L - 0.1055613458f * c.a - 0.0638541728f * c.b;
    float s_ = c.L - 0.0894841775f * c.a - 1.2914855480f * c.b;

    float l = l_ * l_ * l_;
    float m = m_ * m_ * m_;
    float s = s_ * s_ * s_;

    return RGB(
        +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s,
        -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s,
        -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s
    );
}


// Finds the maximum saturation possible for a given hue that fits in sRGB
// Saturation here is defined as S = C/L
// a and b must be normalized so a^2 + b^2 == 1
float compute_max_saturation(float a, float b)
{
    // Max saturation will be when one of r, g or b goes below zero.

    // Select different coefficients depending on which component goes below zero first
    float k0, k1, k2, k3, k4, wl, wm, ws;

    if (-1.88170328f * a - 0.80936493f * b > 1)
    {
        // Red component
        k0 = +1.19086277f; k1 = +1.76576728f; k2 = +0.59662641f; k3 = +0.75515197f; k4 = +0.56771245f;
        wl = +4.0767416621f; wm = -3.3077115913f; ws = +0.2309699292f;
    }
    else if (1.81444104f * a - 1.19445276f * b > 1)
    {
        // Green component
        k0 = +0.73956515f; k1 = -0.45954404f; k2 = +0.08285427f; k3 = +0.12541070f; k4 = +0.14503204f;
        wl = -1.2684380046f; wm = +2.6097574011f; ws = -0.3413193965f;
    }
    else
    {
        // Blue component
        k0 = +1.35733652f; k1 = -0.00915799f; k2 = -1.15130210f; k3 = -0.50559606f; k4 = +0.00692167f;
        wl = -0.0041960863f; wm = -0.7034186147f; ws = +1.7076147010f;
    }

    // Approximate max saturation using a polynomial:
    float S = k0 + k1 * a + k2 * b + k3 * a * a + k4 * a * b;

    // Do one step Halley's method to get closer
    // this gives an error less than 10e6, except for some blue hues where the dS/dh is close to infinite
    // this should be sufficient for most applications, otherwise do two/three steps 

    float k_l = +0.3963377774f * a + 0.2158037573f * b;
    float k_m = -0.1055613458f * a - 0.0638541728f * b;
    float k_s = -0.0894841775f * a - 1.2914855480f * b;

    {
        float l_ = 1.f + S * k_l;
        float m_ = 1.f + S * k_m;
        float s_ = 1.f + S * k_s;

        float l = l_ * l_ * l_;
        float m = m_ * m_ * m_;
        float s = s_ * s_ * s_;

        float l_dS = 3.f * k_l * l_ * l_;
        float m_dS = 3.f * k_m * m_ * m_;
        float s_dS = 3.f * k_s * s_ * s_;

        float l_dS2 = 6.f * k_l * k_l * l_;
        float m_dS2 = 6.f * k_m * k_m * m_;
        float s_dS2 = 6.f * k_s * k_s * s_;

        float f  = wl * l     + wm * m     + ws * s;
        float f1 = wl * l_dS  + wm * m_dS  + ws * s_dS;
        float f2 = wl * l_dS2 + wm * m_dS2 + ws * s_dS2;

        S = S - f * f1 / (f1*f1 - 0.5f * f * f2);
    }

    return S;
}

// finds L_cusp and C_cusp for a given hue
// a and b must be normalized so a^2 + b^2 == 1
struct LC { float L; float C; };
LC find_cusp(float a, float b)
{
	// First, find the maximum saturation (saturation S = C/L)
	float S_cusp = compute_max_saturation(a, b);

	// Convert to linear sRGB to find the first point where at least one of r,g or b >= 1:
	RGB rgb_at_max = oklab_to_linear_srgb(Lab( 1, S_cusp * a, S_cusp * b ));
	float L_cusp = pow(1.f / max(max(rgb_at_max.r, rgb_at_max.g), rgb_at_max.b), 1.0/3.0);
	float C_cusp = L_cusp * S_cusp;

	return LC( L_cusp , C_cusp );
}

// Finds intersection of the line defined by 
// L = L0 * (1 - t) + t * L1;
// C = t * C1;
// a and b must be normalized so a^2 + b^2 == 1
float find_gamut_intersection(float a, float b, float L1, float C1, float L0)
{
	// Find the cusp of the gamut triangle
	LC cusp = find_cusp(a, b);

	// Find the intersection for upper and lower half seprately
	float t;
	if (((L1 - L0) * cusp.C - (cusp.L - L0) * C1) <= 0.f)
	{
		// Lower half

		t = cusp.C * L0 / (C1 * cusp.L + cusp.C * (L0 - L1));
	}
	else
	{
		// Upper half

		// First intersect with triangle
		t = cusp.C * (L0 - 1.f) / (C1 * (cusp.L - 1.f) + cusp.C * (L0 - L1));

		// Then one step Halley's method
		{
			float dL = L1 - L0;
			float dC = C1;

			float k_l = +0.3963377774f * a + 0.2158037573f * b;
			float k_m = -0.1055613458f * a - 0.0638541728f * b;
			float k_s = -0.0894841775f * a - 1.2914855480f * b;

			float l_dt = dL + dC * k_l;
			float m_dt = dL + dC * k_m;
			float s_dt = dL + dC * k_s;

			
			// If higher accuracy is required, 2 or 3 iterations of the following block can be used:
			{
				float L = L0 * (1.f - t) + t * L1;
				float C = t * C1;

				float l_ = L + C * k_l;
				float m_ = L + C * k_m;
				float s_ = L + C * k_s;

				float l = l_ * l_ * l_;
				float m = m_ * m_ * m_;
				float s = s_ * s_ * s_;

				float ldt = 3 * l_dt * l_ * l_;
				float mdt = 3 * m_dt * m_ * m_;
				float sdt = 3 * s_dt * s_ * s_;

				float ldt2 = 6 * l_dt * l_dt * l_;
				float mdt2 = 6 * m_dt * m_dt * m_;
				float sdt2 = 6 * s_dt * s_dt * s_;

				float r = 4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s - 1;
				float r1 = 4.0767416621f * ldt - 3.3077115913f * mdt + 0.2309699292f * sdt;
				float r2 = 4.0767416621f * ldt2 - 3.3077115913f * mdt2 + 0.2309699292f * sdt2;

				float u_r = r1 / (r1 * r1 - 0.5f * r * r2);
				float t_r = -r * u_r;

				float g = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s - 1;
				float g1 = -1.2684380046f * ldt + 2.6097574011f * mdt - 0.3413193965f * sdt;
				float g2 = -1.2684380046f * ldt2 + 2.6097574011f * mdt2 - 0.3413193965f * sdt2;

				float u_g = g1 / (g1 * g1 - 0.5f * g * g2);
				float t_g = -g * u_g;

				float b = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s - 1;
				float b1 = -0.0041960863f * ldt - 0.7034186147f * mdt + 1.7076147010f * sdt;
				float b2 = -0.0041960863f * ldt2 - 0.7034186147f * mdt2 + 1.7076147010f * sdt2;

				float u_b = b1 / (b1 * b1 - 0.5f * b * b2);
				float t_b = -b * u_b;

				t_r = u_r >= 0.f ? t_r : FLT_MAX;
				t_g = u_g >= 0.f ? t_g : FLT_MAX;
				t_b = u_b >= 0.f ? t_b : FLT_MAX;

				t += min(t_r, min(t_g, t_b));
			}
		}
	}

	return t;
}

// float clamp(float x, float min, float max)
// {
// 	if (x < min)
// 		return min;
// 	if (x > max)
// 		return max;

// 	return x;
// }

float sgn(float x)
{
	return float(0.f < x) - float(x < 0.f);
}

RGB gamut_clip_preserve_chroma(RGB rgb)
{
	if (rgb.r < 1 && rgb.g < 1 && rgb.b < 1 && rgb.r > 0 && rgb.g > 0 && rgb.b > 0)
		return rgb;

	Lab lab = linear_srgb_to_oklab(rgb);

	float L = lab.L;
	float eps = 0.00001f;
	float C = max(eps, sqrt(lab.a * lab.a + lab.b * lab.b));
	float a_ = lab.a / C;
	float b_ = lab.b / C;

	float L0 = clamp(L, 0, 1);

	float t = find_gamut_intersection(a_, b_, L, C, L0);
	float L_clipped = L0 * (1 - t) + t * L;
	float C_clipped = t * C;

	return oklab_to_linear_srgb(Lab( L_clipped, C_clipped * a_, C_clipped * b_ ));
}

RGB gamut_clip_project_to_0_5(RGB rgb)
{
	if (rgb.r < 1 && rgb.g < 1 && rgb.b < 1 && rgb.r > 0 && rgb.g > 0 && rgb.b > 0)
		return rgb;

	Lab lab = linear_srgb_to_oklab(rgb);

	float L = lab.L;
	float eps = 0.00001f;
	float C = max(eps, sqrt(lab.a * lab.a + lab.b * lab.b));
	float a_ = lab.a / C;
	float b_ = lab.b / C;

	float L0 = 0.5;

	float t = find_gamut_intersection(a_, b_, L, C, L0);
	float L_clipped = L0 * (1 - t) + t * L;
	float C_clipped = t * C;

	return oklab_to_linear_srgb(Lab( L_clipped, C_clipped * a_, C_clipped * b_ ));
}

RGB gamut_clip_project_to_L_cusp(RGB rgb)
{
	if (rgb.r < 1 && rgb.g < 1 && rgb.b < 1 && rgb.r > 0 && rgb.g > 0 && rgb.b > 0)
		return rgb;

	Lab lab = linear_srgb_to_oklab(rgb);

	float L = lab.L;
	float eps = 0.00001f;
	float C = max(eps, sqrt(lab.a * lab.a + lab.b * lab.b));
	float a_ = lab.a / C;
	float b_ = lab.b / C;

	// The cusp is computed here and in find_gamut_intersection, an optimized solution would only compute it once.
	LC cusp = find_cusp(a_, b_);

	float L0 = cusp.L;

	float t = find_gamut_intersection(a_, b_, L, C, L0);

	float L_clipped = L0 * (1 - t) + t * L;
	float C_clipped = t * C;

	return oklab_to_linear_srgb(Lab( L_clipped, C_clipped * a_, C_clipped * b_ ));
}

// RGB gamut_clip_adaptive_L0_0_5(RGB rgb, float alpha = 0.05f)
RGB gamut_clip_adaptive_L0_0_5(RGB rgb, float alpha)
{
	if (rgb.r < 1 && rgb.g < 1 && rgb.b < 1 && rgb.r > 0 && rgb.g > 0 && rgb.b > 0)
		return rgb;

	Lab lab = linear_srgb_to_oklab(rgb);

	float L = lab.L;
	float eps = 0.00001f;
	float C = max(eps, sqrt(lab.a * lab.a + lab.b * lab.b));
	float a_ = lab.a / C;
	float b_ = lab.b / C;

	float Ld = L - 0.5f;
	float e1 = 0.5f + abs(Ld) + alpha * C;
	float L0 = 0.5f*(1.f + sign(Ld)*(e1 - sqrt(e1*e1 - 2.f *abs(Ld))));

	float t = find_gamut_intersection(a_, b_, L, C, L0);
	float L_clipped = L0 * (1.f - t) + t * L;
	float C_clipped = t * C;

	return oklab_to_linear_srgb(Lab( L_clipped, C_clipped * a_, C_clipped * b_ ));
}

// RGB gamut_clip_adaptive_L0_L_cusp(RGB rgb, float alpha = 0.05f)
RGB gamut_clip_adaptive_L0_L_cusp(RGB rgb, float alpha)
{
	if (rgb.r < 1 && rgb.g < 1 && rgb.b < 1 && rgb.r > 0 && rgb.g > 0 && rgb.b > 0)
		return rgb;

	Lab lab = linear_srgb_to_oklab(rgb);

	float L = lab.L;
	float eps = 0.00001f;
	float C = max(eps, sqrt(lab.a * lab.a + lab.b * lab.b));
	float a_ = lab.a / C;
	float b_ = lab.b / C;

	// The cusp is computed here and in find_gamut_intersection, an optimized solution would only compute it once.
	LC cusp = find_cusp(a_, b_);

	float Ld = L - cusp.L;
	float k = 2.f * (Ld > 0 ? 1.f - cusp.L : cusp.L);

	float e1 = 0.5f*k + abs(Ld) + alpha * C/k;
	float L0 = cusp.L + 0.5f * (sign(Ld) * (e1 - sqrt(e1 * e1 - 2.f * k * abs(Ld))));

	float t = find_gamut_intersection(a_, b_, L, C, L0);
	float L_clipped = L0 * (1.f - t) + t * L;
	float C_clipped = t * C;

	return oklab_to_linear_srgb(Lab( L_clipped, C_clipped * a_, C_clipped * b_ ));
}

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

//Note: these are not normalized but ~ in the range -128...128 for a and b and 0...100 for Luminance since deltaE2000 breaks if used with normalized cie values
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


vec3 Tonemap_Aces(vec3 color) {

	// ACES filmic tonemapper with highlight desaturation ("crosstalk").
	// Based on the curve fit by Krzysztof Narkowicz.
	// https://knarkowicz.wordpress.com/2016/01/06/aces-filmic-tone-mapping-curve/

	const float slope = 12.0f; // higher values = slower rise.

	// Store grayscale as an extra channel.
	vec4 x = vec4(
		// RGB
		color.r, color.g, color.b,
		// Luminosity
		(color.r * 0.299) + (color.g * 0.587) + (color.b * 0.114)
	);
	
	// ACES Tonemapper
	const float a = 2.51f;
	const float b = 0.03f;
	const float c = 2.43f;
	const float d = 0.59f;
	const float e = 0.14f;

	vec4 tonemap = clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
	float t = x.a;
	
	t = t * t / (slope + t);

	// Return after desaturation step.
	return mix(tonemap.rgb, tonemap.aaa, t);
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
	float G = 0.5 * (1.0 - sqrt(pow(C_star, 7.0) / (pow(C_star, 7.0) + pow(25.0, 7.0))));

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
	float G = 0.5 * (1.0 - sqrt(pow(C_star, 7.0) / (pow(C_star, 7.0) + pow(25.0, 7.0))));

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

	float d_h;
	if(Cs * Cb == 0.0){
		d_h = 0.0;
	}
	else
	{
		float diff = hb - hs;
		if (abs(diff) <= PI)
		{
			d_h = diff;
		}
		else if (diff > PI)
		{
			d_h = diff - PI * 2.0;
		}
		else // (d_h < -Math.PI)
		{
			d_h = diff + PI * 2.0;
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

	float d_h;
	if(Cs * Cb == 0.0){
		d_h = 0.0;
	}
	else
	{
		float diff = hb - hs;
		if (abs(diff) <= PI)
		{
			d_h = diff;
		}
		else if (diff > PI)
		{
			d_h = diff - PI * 2.0;
		}
		else // (d_h < -Math.PI)
		{
			d_h = diff + PI * 2.0;
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
	imageStore(image_before, uv_pixel, base);
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
		// // ==== old approach of only tonemapping the luminance ====
		// -> has clipping
        // float Y = 0.2126 * base.r + 0.7152 * base.g + 0.0722 * base.b;
		// float Yt = tonemap(Y);
		// color *= Yt / max(Y, 1e-5);

		// rheinhard basic curve -> hue distortions but elimintates clippping
		color = color / (1.0 + color);

		// aces unused since it has the same problems as rheinhard but is more complex
		// color = vec4(Tonemap_Aces(color.xyz),1.0);

		// my curve, same ups and downs as rheinhard
		// color = tonemap(color);

	}

	//tonemap oklab L
	if(params.tonemapper_mode == 2.0){

		vec3 lab = linear_srgb_to_oklab(color.xyz);

		float L = lab.x;
		float C = length(lab.yz);
		float h = atan(lab.z, lab.y);

		L = tonemap(L);
		// L = L / (1.0 + L);

		// scale chroma slightly by new/old L to avoid clipping out of valid OKLAB or sRGB Chroma, will still happen but reduces it noticably, would need correct gamut mapping
		// C *= (L / max(lab.x, 1e-5));
		// better scaling
		C *= (-exp(params.tonemapper_saturation * L - params.tonemapper_saturation) + 1);

		lab.x = L;
		lab.y = C * cos(h);
		lab.z = C * sin(h);

		color = vec4(oklab_to_linear_srgb(lab), 1.0);

		// clip chroma to the nearest valid value
		// using the pure chroma clipping version since lightness can be perfectly preserved since its already tonemapped
		RGB colClipped = gamut_clip_preserve_chroma(RGB(color.r, color.g, color.b));
		color = vec4(colClipped.r, colClipped.g, colClipped.b, 1.0);
	}



	if(params.draw_mode == 0.0){
		if((color.r > 1.0) || (color.g > 1.0) || (color.b > 1.0))
		{
			post = vec4(color.r - 1,color.g - 1,color.b - 1,1.0);
		}else{
			post = vec4(0.0,0.0,0.0,1.0);
		// post = color;
		}
		imageStore(color_image, uv_pixel, post);
		return;
	}


	//clamp color here so measurement functions can measure it
	post = clamp(color, 0.0, 1.0);



	//color image
	if(params.draw_mode == 1.0){
		imageStore(color_image, uv_pixel, post);
		return;
	}



	float delta = 0.0;



	//oklab delta_h
	if(params.draw_mode == 2.0){

		delta = oklab_delta_h(
			linear_srgb_to_oklab(pre.xyz), 
			linear_srgb_to_oklab(post.xyz)
		);
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
		delta = oklab_delta_L(
			linear_srgb_to_oklab(pre.xyz),
			linear_srgb_to_oklab(post.xyz)
		);
	}

	//oklab deltaE
	if(params.draw_mode == 5.0){
		delta = oklab_delta_E(
			linear_srgb_to_oklab(pre.xyz),
			linear_srgb_to_oklab(post.xyz)
		);
	}



	//cie delta_h
	if(params.draw_mode == 6.0){
		delta = calculate_cie_de_2000_H(
			linear_srgb_to_cielab(pre.xyz), 
			linear_srgb_to_cielab(post.xyz)
		);
		delta /= PI;
	}

	//cie delta_C
	if(params.draw_mode == 7.0){
		delta = calculate_cie_de_2000_C(
			linear_srgb_to_cielab(pre.xyz), 
			linear_srgb_to_cielab(post.xyz)
		);
		delta /= 100.0;
	}

	//cie delta_L
	if(params.draw_mode == 8.0){
		delta = calculate_cie_de_2000_L(
			linear_srgb_to_cielab(pre.xyz),
			linear_srgb_to_cielab(post.xyz)
		);
		delta /= 100.0;
	}

    //ciede2000
	if(params.draw_mode == 9.0){
		delta = calculate_cie_de_2000(
			linear_srgb_to_cielab(pre.xyz),
			linear_srgb_to_cielab(post.xyz)	
		);
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
