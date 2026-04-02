using System;
using System.Threading.Tasks;
using Godot;
using static BA.ColorHelper;

namespace BA
{
	[Tool]
	public partial class DataManager : Node
	{
		internal class PaletteData
		{
			public PaletteData() { }
			public PalettePlotType plotType;
			public string labelX = "x axis";
			public int decimalsX = 1;
			public float xMin = 0.0f;
			public float xMax = 1.0f;
			public string labelY = "y axis";
			public int decimalsY = 2;
			public float yMin = -1.0f;
			public float yMax = 1.0f;
		}

		internal enum PalettePlotType
		{
			oklab_L,
			oklab_C,
			oklab_h,
			oklab_E,
			cie_L,
			cie_C,
			cie_h,
			cie_E
		}


		[ExportCategory("Image Analysis")]
		[ExportGroup("SSIM")]
		[ExportToolButton("Calculate SSIM")] public Callable CalcSSIM => Callable.From(Calculate_SSIM);
		[ExportGroup("Percentiles")]
		[ExportToolButton("Calculate Average")] public Callable CalcAverage => Callable.From(Calculate_Average);
		// [ExportToolButton("Calculate Percentiles")] public Callable CalcPercentile => Callable.From(Calculate_All_Percentile);


		[ExportCategory("Graphs")]
		[ExportGroup("Manual Graph Settings")]
		[Export] private Texture2D _digitImage09;
		[Export] private Texture2D _characterImageAZ;
		[Export] private int _digitScale = 2;
		[Export] private float _scaleYRangeBottom = -1.0f;
		[Export] private float _scaleYRangeTop = 1.0f;
		[Export] private float _scaleXRangeBottom = 0.0f;
		[Export] private float _scaleXRangeTop = 1.0f;
		[Export] private string _labelX = "L";
		[Export] private string _labelY = "L";
		[Export] private int _numDecimalsX = 1;
		[Export] private int _numDecimalsY = 0;
		[Export] private Color _backGroundColor = Colors.Black;
		[Export] private int _gridThickness = 2;
		[Export] private Color _gridColor = Colors.DarkGray;
		[Export] private int _gridCenterLineThickness = 3;
		[Export] private Color _gridCenterColor = Colors.Black;
		[Export] private int _gridLines_X = 10;
		[Export] private int _gridLines_Y = 20;
		[Export] private int _graphRes = 2048;
		[Export] private int _pointRadius = 9;
		[Export] private WorldEnvironment _env = null;
		[Export] private string _fileNameAtt = "";
		// [ExportToolButton("Create Chart Delta By Hue")] public Callable CreateDeltaHGraph => Callable.From(Create_Chart_Delta_By_Hue);
		// [ExportToolButton("Create Chart Chroma By Brightness")] public Callable CreateChromaBrightness => Callable.From(Create_Chart_Chroma_Brightness);
		// [ExportToolButton("Create Chart Hue By Brightness")] public Callable CreateHueBrightness => Callable.From(Create_Chart_Hue_Brightness);
		// [ExportToolButton("Create All Graphs")] public Callable CreateGraphs => Callable.From(CreateAllGraphs);


		[ExportGroup("Palettes")]
		[Export] private PalettePlotType _palettePlotType = PalettePlotType.oklab_L;
		[ExportToolButton("Plot Palette Delta Selected")] public Callable PlotPaletteDelta => Callable.From(GenPaletteGraphSelected);
		[ExportToolButton("Plot All Palettes")] public Callable PlotAllPaletteDelta => Callable.From(GenAllPaletteGraphs);
		[Export] private Godot.Collections.Array<Texture2D> _palettes;


		// [ExportCategory("Images")]
		// [ExportToolButton("CapturePrePost")] public Callable CaptureBoth => Callable.From(CapturePrePost);
		// [ExportToolButton("CaptureBaseImage")] public Callable CaptureIngameImage => Callable.From(CaptureViewportPre);
		// [ExportToolButton("CaptureComparisonImage")] public Callable CaptureIngameImageComparison => Callable.From(CaptureViewportPost);
		[ExportCategory("Test")]
		[ExportToolButton("Test_Calc_D65")] public Callable CalcD65 => Callable.From(Test_CalcD65);
		[Export] private bool _autoCapturePrePost = true;
		[Export] private bool _plotByOKLCh = true;
		// [Export] public Texture2D image_base;
		// [Export] public Texture2D image_compare;

		//cannot export to godot sadly
		private readonly PaletteData[] PALETTE_DATASET =
		{
			new PaletteData
			{
				plotType = PalettePlotType.oklab_L,
				labelX = "original oklab lightness",
				labelY = "palette oklab lightness"
			},

			new PaletteData
			{
				plotType = PalettePlotType.oklab_C,
				labelX = "original oklab lightness",
				labelY = "palette oklab chroma",
				yMin = -0.25f,
				yMax = 0.25f,
				decimalsY = 3
			},

			new PaletteData
			{
				plotType = PalettePlotType.oklab_h,
				labelX = "original oklab lightness",
				labelY = "palette oklab hue",
				yMin = -180,
				yMax = 180,
				decimalsY = 0
			},

			new PaletteData
			{
				plotType = PalettePlotType.oklab_E,
				labelX = "original oklab lightness",
				labelY = "palette oklab delta e from zero"
			},

			new PaletteData
			{
				plotType = PalettePlotType.cie_L,
				labelX = "original oklab lightness",
				labelY = "palette cielab lightness"
			},

			new PaletteData
			{
				plotType = PalettePlotType.cie_C,
				labelX = "original oklab lightness",
				labelY = "palette cielab Chroma"
			},

			new PaletteData
			{
				plotType = PalettePlotType.cie_h,
				labelX = "original oklab lightness",
				labelY = "palette cielab hue",
				yMin = -180,
				yMax = 180,
				decimalsY = 0
			},

			new PaletteData
			{
				plotType = PalettePlotType.cie_E,
				labelX = "original oklab lightness",
				labelY = "palette cielab e"
			},
		};

		Image baseImage;
		Image comparisonImage;

		public void GenAllPaletteGraphs()
		{
			foreach (Texture2D palette in _palettes)
			{
				foreach (PaletteData data in PALETTE_DATASET)
				{
					GenPaletteGraph(palette, data.plotType, data.labelX, data.decimalsX, data.xMin, data.xMax, data.labelY, data.decimalsY, data.yMin, data.yMax);
				}
			}
		}

		public void GenPaletteGraphSelected()
		{
			foreach (Texture2D palette in _palettes)
			{
				GenPaletteGraph(palette, _palettePlotType, _labelX, _numDecimalsX, _scaleXRangeBottom, _scaleXRangeTop, _labelY, _numDecimalsY, _scaleYRangeBottom, _scaleYRangeTop);
			}
		}

		private void GenPaletteGraph(Texture2D palette, PalettePlotType palettePlotType, string labelX, int numDecimalsX, float scaleXRangeBottom, float scaleXRangeTop, string labelY, int numDecimalsY, float scaleYRangeBottom, float scaleYRangeTop)
		{
			if (palette == null)
			{
				GD.PushWarning("Missing Palette...");
			}
			var img = palette.GetImage();
			img.Decompress();
			int M = img.GetWidth();
			int N = img.GetHeight();

			Image graph = Image.CreateEmpty(_graphRes, _graphRes, false, Image.Format.Rgb8);
			graph.Fill(_backGroundColor);


			DrawHelper.DrawGridDigitsLabel(
				graph,
				_digitImage09,
				_characterImageAZ,
				_graphRes,
				_gridLines_X,
				_gridLines_Y,
				scaleXRangeBottom,
				scaleXRangeTop,
				scaleYRangeBottom,
				scaleYRangeTop,
				_gridThickness,
				_gridCenterLineThickness,
				_gridColor,
				_gridCenterColor,
				_digitScale,
				numDecimalsX,
				numDecimalsY,
				labelX,
				labelY
				);


			for (int x = 0; x < M; x++)
			{
				// can only use x axis since the color palette is one dimensional
				// for (int y = 0; y < N; y++)
				// {
				Color c = img.GetPixel(x, 0);
				c = c.SrgbToLinear(); //highly necessary

				float delta = GetPlotDelta(palettePlotType, c);
				DrawColorPointOnGraph(M, graph, x, c, delta);
				// }
			}


			//linear to srgb so the colors in the chart look correct
			graph.LinearToSrgb();


			string palette_name = "";
			palette_name = palette.ResourcePath.Substring(palette.ResourcePath.LastIndexOf("/") + 1);
			palette_name = palette_name.Remove(palette_name.LastIndexOf("."));
			palette_name = "Palette_" + palette_name;
			string imagePath = "res://_Messdaten/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "__" + palette_name + "_" + "_plot_" + palettePlotType.ToString() + ".png";
			GD.Print("Saving chart to: " + imagePath);
			GD.Print(graph.SavePng(imagePath));
		}

		private static float GetPlotDelta(PalettePlotType palettePlotType, Color c)
		{
			float delta = 0.0f;
			Lab c_lab;
			RGB hue_base = new RGB { r = 1.0f, g = 0, b = 0 };

			switch (palettePlotType)
			{
				case PalettePlotType.oklab_L:

					c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
					delta = c_lab.L;

					break;
				case PalettePlotType.oklab_C:

					c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
					double C = new Vector2(c_lab.a, c_lab.b).Length();
					delta = (float)C * 4.0f;

					break;
				case PalettePlotType.oklab_h:

					c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
					delta = (float)(calculate_oklab_d_h(linear_srgb_to_oklab(hue_base), c_lab) / Math.PI);

					break;
				case PalettePlotType.oklab_E:

					c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
					double e = new Vector3(c_lab.L, c_lab.a, c_lab.b).Length();
					delta = (float)e;

					break;
				case PalettePlotType.cie_L:

					c_lab = linear_srgb_to_cielab(new RGB { r = c.R, g = c.G, b = c.B });
					double d_L = calculate_cie_d_L(new Lab { L = 0, a = 0, b = 0 }, c_lab);
					delta = (float)(d_L / 100.0);
					break;

				case PalettePlotType.cie_C:

					c_lab = linear_srgb_to_cielab(new RGB { r = c.R, g = c.G, b = c.B });
					double d_C = calculate_cie_d_C(new Lab { L = 0, a = 0, b = 0 }, c_lab);
					delta = (float)(d_C / 100.0);
					break;

				case PalettePlotType.cie_h:

					c_lab = linear_srgb_to_cielab(new RGB { r = c.R, g = c.G, b = c.B });
					double d_H = calculate_cie_d_H(linear_srgb_to_cielab(hue_base), c_lab);
					delta = (float)(d_H / Math.PI);
					break;

				case PalettePlotType.cie_E:

					c_lab = linear_srgb_to_cielab(new RGB { r = c.R, g = c.G, b = c.B });
					double d_E = calculate_cie_de_2000(linear_srgb_to_cielab(new RGB { r = 0, g = 0, b = 0 }), c_lab);
					delta = (float)(d_E / 100.0);
					break;
			}

			return delta;
		}

		private void DrawColorPointOnGraph(int M, Image graph, int x, Color c, float delta)
		{
			int ix = (int)(((float)x / (float)M) * _graphRes);
			int iy = 0;

			if (delta <= 0.0)
			{
				iy = _graphRes - ((int)(delta * ((_graphRes - 1) / 2)) + ((_graphRes - 1) / 2));
			}
			else
			{
				iy = _graphRes - (int)(delta * ((_graphRes - 1) / 2)) - ((_graphRes - 1) / 2);
			}

			if (ix >= _graphRes) ix = _graphRes - 1;
			if (ix < 0) ix = 0;
			if (iy >= _graphRes) iy = _graphRes - 1;
			if (iy < 0) iy = 0;

			for (int rx = -_pointRadius; rx <= _pointRadius; rx++)
			{
				for (int ry = -_pointRadius; ry <= _pointRadius; ry++)
				{
					if (new Vector2(rx, ry).Length() >= _pointRadius) continue;
					if (rx + ix >= _graphRes || rx + ix < 0) continue;
					if (ry + iy >= _graphRes || ry + iy < 0) continue;
					graph.SetPixel(ix + rx, iy + ry, c);
				}
			}
		}

		public override void _Process(double delta)
		{
		}


		public void Calculate_All_Percentile(Image img)
		{
			Calculate_Percentile(img, 0.01f);
			Calculate_Percentile(img, 0.1f);
			Calculate_Percentile(img, 0.5f);
			Calculate_Percentile(img, 0.9f);
			Calculate_Percentile(img, 0.99f);
			Calculate_Percentile(img, 0.999f);
		}


		public void Calculate_Percentile(Image img, float p)
		{
			int M = img.GetWidth();
			int N = img.GetHeight();

			float[] data_R = new float[M * N];
			float[] data_G = new float[M * N];
			float[] data_B = new float[M * N];

			for (int x = 0; x < M; x++)
			{
				for (int y = 0; y < N; y++)
				{
					data_R[x * N + y] = img.GetPixel(x, y).R;
					data_G[x * N + y] = img.GetPixel(x, y).G;
					data_B[x * N + y] = img.GetPixel(x, y).B;
					// data[x + y + (M * N)] = -img.GetPixel(x, y).B;
					//g channel is unused
				}
			}

			Array.Sort(data_R);
			Array.Sort(data_G);
			Array.Sort(data_B);

			double percentile_R = data_R[(int)Math.Round(data_R.Length * p)];
			double percentile_G = data_G[(int)Math.Round(data_G.Length * p)];
			double percentile_B = data_B[(int)Math.Round(data_B.Length * p)];

			GD.Print("Image " + (int)Math.Round(p * 100.0) + "th Percentile R: " + percentile_R);
			// GD.Print("Image " + (int)Math.Round(p * 100.0) + "th Percentile G: " + percentile_G);
			GD.Print("Image " + (int)Math.Round(p * 100.0) + "th Percentile B: " + percentile_B);
		}

		public void Test_CalcD65()
		{
			double T = 6504;
			double x1 = (-4.6070 * Math.Pow(10.0, 9.0)) / Math.Pow(T, 3.0);
			double x2 = (2.9678 * Math.Pow(10.0, 6.0)) / Math.Pow(T, 2.0);
			double x3 = (0.09911 * Math.Pow(10.0, 3.0)) / T;
			double x4 = 0.244063;
			double x = x1 + x2 + x3 + x4;
			GD.Print("x: " + x);

			double y1 = -3.0 * Math.Pow(x, 2.0);
			double y2 = 2.87 * x;
			double y3 = -0.275;
			double y = y1 + y2 + y3;
			GD.Print("y: " + y);
		}

		

		public void Calculate_Average()
		{
			var img = GetViewport().GetTexture().GetImage();
			int M = img.GetWidth();
			int N = img.GetHeight();

			double total_R = 0.0;
			double total_G = 0.0;
			double total_B = 0.0;
			for (int x = 0; x < M; x++)
			{
				for (int y = 0; y < N; y++)
				{
					total_R += img.GetPixel(x, y).R;
					total_G += img.GetPixel(x, y).G;
					total_B += img.GetPixel(x, y).B;
				}
			}
			double average_R = total_R / (M * N);
			double average_G = total_G / (M * N);
			double average_B = total_B / (M * N);
			GD.Print("Image Average R: " + average_R);
			// GD.Print("Image Average G: " + average_G);
			GD.Print("Image Average B: " + average_B);
		}

		//nutzt rgb 0...255
		public double Calculate_SSIM()
		{
			// Image i = image_base.GetImage();
			// Image I = image_compare.GetImage();
			baseImage.Decompress();
			comparisonImage.Decompress();

			double C1 = Math.Pow(0.01 * 255.0, 2.0);
			double C2 = Math.Pow(0.03 * 255.0, 2.0);
			double C3 = C2 / 2.0;

			int M = baseImage.GetWidth();
			int N = baseImage.GetHeight();
			int O = 3;

			double mue_i = 0.0;
			double mue_I = 0.0;
			for (int x = 0; x < M; x++)
			{
				for (int y = 0; y < N; y++)
				{
					for (int z = 0; z < O; z++)
					{
						// if((x + y + z) % 10000 == 0) GD.Print(x + "/" + M + ", " + y + "/" + N + ", " + z + "/" + O);
						mue_i += baseImage.GetPixel(x, y)[z] * 255.0; //check if format correct
						mue_I += comparisonImage.GetPixel(x, y)[z] * 255.0; //check if format correct
					}
				}
			}
			mue_i /= M * N * O;
			mue_I /= M * N * O;

			double sig_i2 = 0.0;
			double sig_I2 = 0.0;
			double sig_iI = 0.0;
			for (int x = 0; x < M; x++)
			{
				for (int y = 0; y < N; y++)
				{
					for (int z = 0; z < O; z++)
					{
						sig_i2 += Math.Pow(baseImage.GetPixel(x, y)[z] * 255.0 - mue_i, 2.0); //check if format correct
						sig_I2 += Math.Pow(comparisonImage.GetPixel(x, y)[z] * 255.0 - mue_I, 2.0); //check if format correct

						sig_iI += (baseImage.GetPixel(x, y)[z] * 255.0 - mue_i) * (comparisonImage.GetPixel(x, y)[z] * 255.0 - mue_I); //check if format correct
					}
				}
			}
			sig_i2 /= M * N * O;
			sig_I2 /= M * N * O;
			sig_iI /= M * N * O;

			double l = (2.0 * mue_i * mue_I + C1) / (Math.Pow(mue_i, 2.0) + Math.Pow(mue_I, 2.0) + C1);
			double c = (2.0 * Math.Sqrt(sig_i2) * Math.Sqrt(sig_I2) + C2) / (sig_i2 + sig_I2 + C2);
			double s = (sig_iI + C3) / (Math.Sqrt(sig_i2) * Math.Sqrt(sig_I2) + C3);

			double SSIM = l * c * s;

			GD.Print("SSIM: " + SSIM);
			GD.Print("luminance: " + l);
			GD.Print("contrast: " + c);
			GD.Print("structure: " + s);
			return SSIM;
		}
	}
}