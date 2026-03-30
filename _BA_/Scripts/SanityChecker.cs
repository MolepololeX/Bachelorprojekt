using System;
using Godot;
using static BA.ColorHelper;

namespace BA
{
    [Tool]
    public partial class SanityChecker : Node
    {
        [Export(PropertyHint.ColorNoAlpha)] public Color colorSRGB = Colors.Red;
        [Export(PropertyHint.ColorNoAlpha)] public Color color2 = Colors.Green;
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

            Lab c2 = linear_srgb_to_cielab(new RGB { r = color2.R, g = color2.G, b = color2.B });
            GD.Print("CIEDE2000E: " + calculate_cie_de_2000(cielab, c2));
        }
    }
}