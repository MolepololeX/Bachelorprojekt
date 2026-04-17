using Godot;
using System;
using static BA.ColorHelper;

namespace BA
{
	[Tool]
	public partial class GenerateColorGrid : Node
	{
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

					h = j / (float)_hueSteps;
					matColor = Color.FromHsv(h, 1.0f, 1.0f);

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

			img.SavePng("res://color_palette_oklab.png");
		}
	}
}