using System;
using System.Runtime.Intrinsics.X86;
using Godot;

namespace BA
{
    public struct Lab { public float L; public float a; public float b; public float X => L; public float Y => a; public float Z => b; };
    public struct RGB { public float r; public float g; public float b; };
    public static class ColorHelper
    {
        public static Lab linear_srgb_to_oklab(RGB c)
        {
            float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
            float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
            float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

            float l_ = (float)Math.Pow(l, 1.0 / 3.0);
            float m_ = (float)Math.Pow(m, 1.0 / 3.0);
            float s_ = (float)Math.Pow(s, 1.0 / 3.0);

            Lab lab = new();
            lab.L = 0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_;
            lab.a = 1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_;
            lab.b = 0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_;
            return lab;
        }

        public static RGB oklab_to_linear_srgb(Lab c)
        {
            float l_ = c.L + 0.3963377774f * c.a + 0.2158037573f * c.b;
            float m_ = c.L - 0.1055613458f * c.a - 0.0638541728f * c.b;
            float s_ = c.L - 0.0894841775f * c.a - 1.2914855480f * c.b;

            float l = l_ * l_ * l_;
            float m = m_ * m_ * m_;
            float s = s_ * s_ * s_;

            RGB rgb = new();
            rgb.r = +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
            rgb.g = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
            rgb.b = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;
            return rgb;
        }

        public static float calculate_oklab_d_h(Lab c1, Lab c2)
        {
            double h1 = Math.Atan2(c1.b, c1.a);
            double h2 = Math.Atan2(c2.b, c2.a);
            if (h1 < 0) h1 += 2.0 * Math.PI;
            if (h2 < 0) h2 += 2.0 * Math.PI;

            double d_h = h2 - h1;
            if (Math.Abs(d_h) <= Math.PI)
            {
                d_h = h2 - h1;
            }
            else if (d_h > Math.PI)
            {
                d_h = h2 - h1 - Math.PI * 2.0;
            }
            else // (d_h < Math.PI)
            {
                d_h = h2 - h1 + Math.PI * 2.0;
            }
            return (float)d_h;
        }

        public static float calculate_cie_d_L(Lab cs, Lab cb)
        {
            double L_s = cs.X;
            double L_b = cb.X;
            double d_L = L_b - L_s;

            return (float)d_L;
        }

        public static float calculate_cie_d_C(Lab cs, Lab cb)
        {
            double C_star = (Math.Sqrt(cs.Y * cs.Y + cs.Z * cs.Z) + Math.Sqrt(cb.Y * cb.Y + cb.Z * cb.Z)) / 2.0;//original Chroma
            double G = 0.5 * (1.0 - Math.Sqrt(Math.Pow(C_star, 7.0) / (Math.Pow(C_star, 7.0) + Math.Pow(25.0, 7.0)))); //TODO fehler in der formel fixen

            double a_s = (1.0 + G) * cs.Y;
            double b_s = cs.Z;

            double a_b = (1.0 + G) * cb.Y;
            double b_b = cb.Z;

            double Cs = Math.Sqrt(a_s * a_s + b_s * b_s);
            double Cb = Math.Sqrt(a_b * a_b + b_b * b_b);

            double d_C = Cb - Cs;
            return (float)d_C;
        }

        public static float calculate_cie_d_H(Lab cs, Lab cb)
        {
            double C_star = (Math.Sqrt(cs.Y * cs.Y + cs.Z * cs.Z) + Math.Sqrt(cb.Y * cb.Y + cb.Z * cb.Z)) / 2.0;//original Chroma
            double G = 0.5 * (1.0 - Math.Sqrt(Math.Pow(C_star, 7.0) / (Math.Pow(C_star, 7.0) + Math.Pow(25.0, 7.0)))); //TODO fehler in der formel fixen

            double a_s = (1.0 + G) * cs.Y;
            double b_s = cs.Z;

            double a_b = (1.0 + G) * cb.Y;
            double b_b = cb.Z;

            double Cs = Math.Sqrt(a_s * a_s + b_s * b_s);
            double Cb = Math.Sqrt(a_b * a_b + b_b * b_b);

            double hs = Math.Atan2(b_s, a_s);
            double hb = Math.Atan2(b_b, a_b);

            if (hs < 0) hs += 2.0 * Math.PI;
            if (hb < 0) hb += 2.0 * Math.PI;

            double d_h = hb - hs;
            if (Cs * Cb == 0.0)
            {
                d_h = 0.0;
            }
            else
            {
                if (Math.Abs(d_h) <= Math.PI)
                {
                    d_h = hb - hs;
                }
                else if (d_h > Math.PI)
                {
                    d_h = hb - hs - Math.PI * 2.0;
                }
                else // (d_h < Math.PI)
                {
                    d_h = hb - hs + Math.PI * 2.0;
                }
            }
            //cannot use d_H since it is the distance between the two points on the color wheel not their angle difference
            // double d_H = 2.0 * Math.Sqrt(Cb * Cs) * Math.Sin(d_h / 2.0);
            return (float)d_h;
        }

        private static double DegToRad(double deg) => deg * Math.PI / 180.0;

        public static float calculate_cie_de_2000(Lab cs, Lab cb)
        {
            double C_star = (Math.Sqrt(cs.Y * cs.Y + cs.Z * cs.Z) + Math.Sqrt(cb.Y * cb.Y + cb.Z * cb.Z)) / 2.0;//original Chroma
            double G = 0.5 * (1.0 - Math.Sqrt(Math.Pow(C_star, 7.0) / (Math.Pow(C_star, 7.0) + Math.Pow(25.0, 7.0)))); //TODO fehler in der formel fixen

            double L_s = cs.X;
            double a_s = (1.0 + G) * cs.Y;
            double b_s = cs.Z;

            double L_b = cb.X;
            double a_b = (1.0 + G) * cb.Y;
            double b_b = cb.Z;

            double Cs = Math.Sqrt(a_s * a_s + b_s * b_s);
            double Cb = Math.Sqrt(a_b * a_b + b_b * b_b);

            double hs = Math.Atan2(b_s, a_s);
            double hb = Math.Atan2(b_b, a_b);

            if (hs < 0) hs += 2.0 * Math.PI;
            if (hb < 0) hb += 2.0 * Math.PI;

            double d_L = L_b - L_s;
            double d_C = Cb - Cs;

            double d_h = hb - hs;
            if (Cs * Cb == 0.0)
            {
                d_h = 0.0;
            }
            else
            {
                if (Math.Abs(d_h) <= Math.PI)
                {
                    d_h = hb - hs;
                }
                else if (d_h > Math.PI)
                {
                    d_h = hb - hs - Math.PI * 2.0;
                }
                else // (d_h < Math.PI)
                {
                    d_h = hb - hs + Math.PI * 2.0;
                }
            }

            double d_H = 2.0 * Math.Sqrt(Cb * Cs) * Math.Sin(d_h / 2.0);

            double kL = 1.0;
            double kC = 1.0;
            double kH = 1.0;

            double m_L = (L_b + L_s) / 2.0;
            double m_C = (Cb + Cs) / 2.0;

            double m_h = (hs + hb) / 2.0;

            double diff = Math.Abs(hs - hb);
            if (Cs * Cb == 0.0)
            {
                m_h = hs + hb;
            }
            else
            {
                if (diff <= Math.PI)
                {
                    // m_h = (hs + hb) / 2.0;
                }
                else if ((hs + hb) < 2.0 * Math.PI)
                {
                    m_h = (hs + hb + 2.0 * Math.PI) / 2.0;
                }
                else //((hs+hb) < 2.0 * Math.PI)
                {
                    m_h = (hs + hb - 2.0 * Math.PI) / 2.0;
                }
            }

            double sL = 1.0 + (0.015 * Math.Pow(m_L - 50.0, 2.0)) / (Math.Sqrt(20.0 + Math.Pow(m_L - 50.0, 2.0)));
            double sC = 1.0 + 0.045 * m_C;

            double T = 1.0
                - 0.17 * Math.Cos(m_h - DegToRad(30.0))
                + 0.24 * Math.Cos(2.0 * m_h)
                + 0.32 * Math.Cos(3.0 * m_h + DegToRad(6.0))
                - 0.20 * Math.Cos(4.0 * m_h - DegToRad(63.0));

            double sH = 1.0 + 0.015 * m_C * T;

            double d_0 = DegToRad(30.0) * Math.Exp(-Math.Pow((m_h - DegToRad(275.0)) / DegToRad(25.0), 2.0));
            double Rc = 2.0 * Math.Sqrt(Math.Pow(m_C, 7.0) / (Math.Pow(m_C, 7.0) + Math.Pow(25.0, 7.0)));
            double Rt = -Math.Sin(2.0 * d_0) * Rc;

            double d_E_00 = Math.Sqrt(Math.Pow(d_L / (kL * sL), 2.0) + Math.Pow(d_C / (kC * sC), 2.0) + Math.Pow(d_H / (kH * sH), 2.0) + (Rt * (d_C / (kC * sC)) * (d_H / (kH * sH))));
            return (float)d_E_00;
        }

        private static float cie_f(float I)
        {
            if (I > Math.Pow(6.0 / 29.0, 3.0))
            {
                return (float)Math.Pow(I, 1.0 / 3.0);
            }
            else
            {
                return (float)((841.0 / 108.0) * I + (16.0 / 116.0));
            }
        }

        public static Lab linear_srgb_to_cielab(RGB rgb)
        {
            Vector3 d65 = new Vector3(95.014f, 100.0f, 108.827f);

            Vector3 xyz = new Vector3(
                0.4124f * rgb.r + 0.3576f * rgb.g + 0.1805f * rgb.b,
                0.2126f * rgb.r + 0.7152f * rgb.g + 0.0722f * rgb.b,
                0.0193f * rgb.r + 0.1192f * rgb.g + 0.9505f * rgb.b
            );
            xyz *= 100.0f; //multiplying by 100 here since we use a base 100 d65 whitepoint

            float Xx = xyz.X / d65.X;
            float Yy = xyz.Y / d65.Y;
            float Zz = xyz.Z / d65.Z;

            float L = 116.0f * cie_f(Yy) - 16.0f;
            float a = 500.0f * (cie_f(Xx) - cie_f(Yy));
            float b = 200.0f * (cie_f(Yy) - cie_f(Zz));

            return new Lab { L = L, a = a, b = b };
        }
    }
}