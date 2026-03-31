using System;
using System.IO;
using Godot;
using static BA.ColorHelper;

namespace BA
{
    [Tool]
    public partial class SanityChecker : Node
    {
        [Export(PropertyHint.ColorNoAlpha)] public Color colorSRGB_0 = Colors.Red;
        [Export(PropertyHint.ColorNoAlpha)] public Color colorSRGB_1 = Colors.Green;
        [ExportToolButton("SanityCheck")] public Callable CheckSanityPls => Callable.From(SanityCheck);

        public void SanityCheck()
        {
            Color lin_c0 = colorSRGB_0.SrgbToLinear();
            Color lin_c1 = colorSRGB_1.SrgbToLinear();
            RGB rgb_0 = new RGB { r = lin_c0.R, g = lin_c0.G, b = lin_c0.B };
            RGB rgb_1 = new RGB { r = lin_c1.R, g = lin_c1.G, b = lin_c1.B };
            // GD.Print("RGB: ");
            // GD.Print("r: " + rgb.r);
            // GD.Print("g: " + rgb.g);
            // GD.Print("b: " + rgb.b);
            // Lab oklab = linear_srgb_to_oklab(rgb);
            // GD.Print("OKLCh: ");
            // GD.Print("L: " + oklab.L);
            // GD.Print("C: " + new Vector2(oklab.a, oklab.b).Length());
            // GD.Print("h: " + Mathf.RadToDeg(Math.Atan2(oklab.b, oklab.a)));
            // GD.Print("Cielab: ");
            // GD.Print("L: " + cielab.L);
            // GD.Print("a: " + cielab.a);
            // GD.Print("b: " + cielab.b);

            Lab c1 = linear_srgb_to_cielab(rgb_0);
            Lab c2 = linear_srgb_to_cielab(rgb_1);

            GD.Print("=====CIE2000 TESTING=====");
            GD.Print("C1 L: " + c1.L);
            GD.Print("C1 a: " + c1.a);
            GD.Print("C1 b: " + c1.b);
            GD.Print("C2 L: " + c2.L);
            GD.Print("C2 a: " + c2.a);
            GD.Print("C2 b: " + c2.b);
            GD.Print("CIEDE2000E: " + calculate_cie_de_2000(c1, c2));

            Lab a = new Lab{L=50, a=2.6772f, b=-79.7751f};
            Lab b = new Lab{L=50, a=0.0f, b=-82.7485f};
            GD.Print("CIEDE2000E: " + calculate_cie_de_2000(a, b));

            // GD.Print("=====TEST=====");
            c2 = linear_srgb_to_cielab(new RGB { r = colorSRGB_1.R, g = colorSRGB_1.G, b = colorSRGB_1.B });
            // double h1 = Math.Atan2(cielab.b, cielab.a);
            double h2 = Math.Atan2(c2.b, c2.a);
            // GD.Print("Hue 1: " + h1);
            // GD.Print("Hue 2: " + h2);

            // h1 += Math.PI;
            // h2 += Math.PI;

            // // GD.Print("Hue 1 transformed: " + h1);
            // // GD.Print("Hue 2 transformed: " + h2);

            // double d_h = Math.Abs(h2 - h1);
            // if (d_h <= Math.PI)
            // {
            //     d_h = h2 - h1;
            // }
            // else if (d_h > Math.PI && h2 <= h1)
            // {
            //     d_h = h2 - h1 + Math.PI * 2.0;
            // }
            // else if (d_h > Math.PI && h2 > h1)
            // {
            //     d_h = h2 - h1 - Math.PI * 2.0;
            // }

            // GD.Print("Delta Hue: " + d_h);
            GD.Print(Math.PI);
            GD.Print(Math.PI * 2.0);
        }
    }
}