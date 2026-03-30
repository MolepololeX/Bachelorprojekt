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

        L /= 100.0f;
        a /= 100.0f;
        b /= 100.0f;

        return new Lab { L = L, a = a, b = b };
    }
}
