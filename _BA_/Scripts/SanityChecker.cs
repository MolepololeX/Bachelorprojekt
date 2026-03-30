using System;
using Godot;
using static GenerateColorGrid;

[Tool]
public partial class SanityChecker : Node
{
    [Export(PropertyHint.ColorNoAlpha)] public Color colorSRGB = Colors.Red;
    [ExportToolButton("SanityCheck")] public Callable CheckSanityPls => Callable.From(SanityCheck);

    public void SanityCheck()
    {
        RGB rgb = new RGB { r = colorSRGB.R, g = colorSRGB.G, b = colorSRGB.B };
        GD.Print("RGB: ");
        GD.Print("r: " + rgb.r);
        GD.Print("g: " + rgb.g);
        GD.Print("b: " + rgb.b);
        Lab oklab = linear_srgb_to_oklab(rgb);
        GD.Print("OKLCh: ");
        GD.Print("L: " + oklab.L);
        GD.Print("C: " + new Vector2(oklab.a, oklab.b).Length());
        GD.Print("h: " + Mathf.RadToDeg(Math.Atan2(oklab.b, oklab.a)));
        Lab cielab = linear_srgb_to_cielab(rgb);
        GD.Print("Cielab: ");
        GD.Print("L: " + cielab.L);
        GD.Print("a: " + cielab.a);
        GD.Print("b: " + cielab.b);

        Lab c2 = new Lab{L = 0, a = 0, b = 0};
        GD.Print("CIEDE2000E: " + calculate_cie_de_2000(new Vector3(cielab.L, cielab.a, cielab.b), new Vector3(c2.L, c2.a, c2.b)));
    }

    float calculate_cie_de_2000(Vector3 cs, Vector3 cb){
        double C_star = (Math.Sqrt(cs.Y * cs.Y + cs.Z * cs.Z) + Math.Sqrt(cb.Y * cb.Y + cb.Z * cb.Z)) / 2.0;//original Chroma
        double G = 0.5 * (1.0 - Math.Sqrt( 	Math.Pow(C_star, 7.0) / ( Math.Pow(C_star, 7.0) + Math.Pow(25.0, 7.0) )	)); //TODO fehler in der formel fixen

        double L_s = cs.X;
        double a_s = (1.0 + G)*cs.Y;
        double b_s = cs.Z;

        double L_b = cb.X;
        double a_b = (1.0 + G)*cb.Y;
        double b_b = cb.Z;

        double Cs = Math.Sqrt(a_s * a_s + b_s * b_s);
        double Cb = Math.Sqrt(a_b * a_b + b_b * b_b);

        double hs = Math.Atan2(b_s , a_s);
        double hb = Math.Atan2(b_b , a_b);

        double d_h = hb - hs;
        d_h = Math.Atan2(Math.Sin(d_h), Math.Cos(d_h));
        double d_L = L_b - L_s;
        double d_C = Cb - Cs;
        double d_H = 2.0 * Math.Sqrt(Cb * Cs) * Math.Sin(d_h / 2.0);

        double kL = 1.0;
        double kC = 1.0;
        double kH = 1.0;

        double m_L = (L_b + L_s) / 2.0;
        double m_C = (Cb + Cs) / 2.0;
        double m_h = (hs + hb) / 2.0;

        double sL = 1.0 + (0.015 * Math.Pow(m_L - 50.0, 2.0)) / (Math.Sqrt(20.0 + Math.Pow(m_L - 50.0, 2.0)));
        double sC = 1.0 + 0.045 * m_C;
        double T = 1.0 - 0.17 * Math.Cos(m_h - 30.0) + 0.24 * Math.Cos(2.0 * m_h) + 0.32 * Math.Cos(3.0 * m_h + 6.0) - 0.20 * Math.Cos(4.0 * m_h - 63);
        double sH = 1.0 + 0.015 * m_C * T;

        double d_0 = 30.0 * Math.Exp(-Math.Pow((m_h - 275.0) / 25.0, 2.0));
        double Rc = 2.0 * Math.Sqrt(Math.Pow(m_C, 7.0) / (Math.Pow(m_C, 7.0) + Math.Pow(25.0, 7.0)));
        double Rt = -Math.Sin(2.0 * d_0) * Rc;

        double d_E_00 = Math.Sqrt(Math.Pow(d_L / (kL * sL), 2.0) + Math.Pow(d_C / (kC * sC), 2.0) + Math.Pow(d_H / (kH * sH), 2.0) +		( Rt * (d_C / (kC * sC)) * (d_H / (kH * sH)) )		);
        return (float)d_E_00;
    }

    float cie_f(float I)
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

    Lab linear_srgb_to_cielab(RGB rgb)
    {
        Vector3 d65 = new Vector3(95.014f, 100.0f, 108.827f);

        Vector3 xyz = new Vector3(
            0.4124f * rgb.r + 0.3576f * rgb.g + 0.1805f * rgb.b,
            0.2126f * rgb.r + 0.7152f * rgb.g + 0.0722f * rgb.b,
            0.0193f * rgb.r + 0.1192f * rgb.g + 0.9505f * rgb.b
        );
        xyz *= 100.0f; //multiplying by 100 here since we use a base 100 d65 whitepoint

        GD.Print("XYZ: ");
        GD.Print("X: " + xyz.X);
        GD.Print("Y: " + xyz.Y);
        GD.Print("Z: " + xyz.Z);

        float Xx = xyz.X / d65.X;
        float Yy = xyz.Y / d65.Y;
        float Zz = xyz.Z / d65.Z;

        float L = 116.0f * cie_f(Yy) - 16.0f;
        float a = 500.0f * (cie_f(Xx) - cie_f(Yy));
        float b = 200.0f * (cie_f(Yy) - cie_f(Zz));

        return new Lab { L = L, a = a, b = b };
    }
}
