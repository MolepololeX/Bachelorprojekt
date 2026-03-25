using Godot;
using System;

[Tool]
public partial class GenerateColorGrid : Node
{
	internal struct Lab { public float L; public float a; public float b; };
	internal struct RGB { public float r; public float g; public float b; };

	[Export] private Node3D _startPos;
	[Export] private Mesh _nodeMesh;
	[Export] private float _nodeSpacing = 5.0f;
	[Export] private int _hueSteps = 8;
	[Export] private int _lightnessSteps = 8;
	[Export] private float _chroma = 1.0f;
	[ExportToolButton("GenerateColorGrid")] public Callable GenGrid => Callable.From(GenColorGrid);

	[ExportCategory("Texture Gen")]
	[Export] private int _textureHueSteps = 8;
	[Export] private int _textureLightnessSteps = 8;
	[ExportToolButton("GenerateColorTexture")] public Callable GenTex => Callable.From(GenColorTexture);

    public override void _Ready()
	{
		GenColorGrid();
	}

	public void GenColorGrid()
	{
		if (_startPos.GetChildren().Count > 0)
		{
			foreach (Node n in _startPos.GetChildren())
			{
				n.Free();
			}
		}

		for (int i = 0; i < _lightnessSteps; i++)
		{
			for (int j = 0; j < _hueSteps; j++)
			{
				MeshInstance3D mesh = new MeshInstance3D();
				mesh.Mesh = _nodeMesh;
				var mat = new StandardMaterial3D();

				float C = _chroma;
				float h = j / (float)_hueSteps * (float)Math.PI * 2.0f;
				Lab colOK = new()
				{
					L = i / (float)_lightnessSteps * 1.05f,
					a = C * (float)Math.Cos(h),
					b = C * (float)Math.Sin(h)
				};
				RGB colRGB = oklab_to_linear_srgb(colOK);
				Color matColor = new()
				{
					R = colRGB.r,
					G = colRGB.g,
					B = colRGB.b
				};

				mat.AlbedoColor = matColor;
				mat.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
				mesh.SetSurfaceOverrideMaterial(0, mat);

				mesh.Position = new Vector3(
					mesh.Position.X + (float)i * _nodeSpacing,
					mesh.Position.Y,
					mesh.Position.Z + (float)j * _nodeSpacing
				);
				_startPos.AddChild(mesh);
			}
		}
	}

	public void GenColorTexture()
	{
		Image img = Image.CreateEmpty(_textureLightnessSteps, _textureHueSteps, false, Image.Format.Rgb8);
		for (int i = 0; i < _textureLightnessSteps; i++)
		{
			for (int j = 0; j < _textureHueSteps; j++)
			{
				float C = _chroma;
				float h = j / (float)_textureHueSteps * (float)Math.PI * 2.0f;
				Lab colOK = new()
				{
					L = i / (float)_textureLightnessSteps * 1.05f,
					a = C * (float)Math.Cos(h),
					b = C * (float)Math.Sin(h)
				};
				RGB colRGB = oklab_to_linear_srgb(colOK);
				Color matColor = new()
				{
					R = colRGB.r,
					G = colRGB.g,
					B = colRGB.b
				};

				img.SetPixel(i, j, matColor);
			}
		}

		img.SavePng( "res://color_palette_oklab.png");
	}


	Lab linear_srgb_to_oklab(RGB c)
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

	RGB oklab_to_linear_srgb(Lab c)
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
}
