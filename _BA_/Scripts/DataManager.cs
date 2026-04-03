using System;
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

		internal struct SSIM
		{
			public double luminance;
			public double contrast;
			public double structure;
			public double ssim;
		}


		[ExportCategory("Image Analysis")]
		[ExportGroup("SSIM")]
		[ExportToolButton("Calculate SSIM")] public Callable CalcSSIM => Callable.From(Calculate_SSIM);
		[Export] private Texture2D _image_before_linear;
		[Export] private Texture2D[] _images_after_linear;
		// [ExportGroup("Percentiles")]
		// [ExportToolButton("Calculate Average")] public Callable CalcAverage => Callable.From(Calculate_Average);
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

		[ExportGroup("Tonemapping")]
		[Export] private string _path_to_raw_image_linear_data;
		[Export] private Texture2D _delta_texture_linear;
		[Export(PropertyHint.None, "if true will plot by cie lightness, otherwise plots by oklab lightness")] private bool _plot_by_cie_L = false;
		[Export] private bool _generate_percentile_data = true;
		[ExportToolButton("Plot Test Delta")] public Callable PlotTestDelta => Callable.From(GenTestDeltaGraph);
		[Export] private bool _drawTonemapperReference = false;
		[Export] private float _tonemapperReferenceExposure = 1.2f;
		[Export] private int _referenceLineWidth = 1;


		// [ExportCategory("Images")]
		// [ExportToolButton("CapturePrePost")] public Callable CaptureBoth => Callable.From(CapturePrePost);
		// [ExportToolButton("CaptureBaseImage")] public Callable CaptureIngameImage => Callable.From(CaptureViewportPre);
		// [ExportToolButton("CaptureComparisonImage")] public Callable CaptureIngameImageComparison => Callable.From(CaptureViewportPost);
		// [ExportCategory("Test")]
		// [ExportToolButton("Test_Calc_D65")] public Callable CalcD65 => Callable.From(Test_CalcD65);
		// [Export] private bool _autoCapturePrePost = true;
		// [Export] private bool _plotByOKLCh = true;
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
				DrawColorPointOnGraph(M, graph, x, c, delta, _pointRadius);
				// }
			}


			//linear to srgb so the colors in the chart look correct
			graph.LinearToSrgb();


			string palette_name = "";
			palette_name = palette.ResourcePath.Substring(palette.ResourcePath.LastIndexOf("/") + 1);
			palette_name = palette_name.Remove(palette_name.LastIndexOf("."));
			palette_name = "Palette_" + palette_name;
			string imagePath = "res://_BA_/_Messdaten_/Palettes/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "__" + palette_name + "_" + "_plot_" + palettePlotType.ToString() + ".png";
			GD.Print("Saving chart to: " + imagePath);
			GD.Print(graph.SavePng(imagePath));
		}

		private void GenTestDeltaGraph()
		{
			if (_delta_texture_linear == null)
			{
				GD.PushWarning("Missing delta image...");
				return;
			}
			var size_ref = _delta_texture_linear.GetImage();
			var size = size_ref.GetSize();

			var data_file = FileAccess.Open(_path_to_raw_image_linear_data, FileAccess.ModeFlags.Read);
			if (data_file == null)
			{
				GD.PushError("Failed to open raw image data file\nPlease verify path: " + _path_to_raw_image_linear_data);
				return;
			}
			var raw_image = Image.CreateFromData(
				size.X,
				size.Y,
				false,
				Image.Format.Rgbah,
				data_file.GetBuffer((long)data_file.GetLength()));
			data_file.Close();

			GenDeltaGraph(
				raw_image,
				_delta_texture_linear,
				"original oklab L",
				1,
				0.0f,
				3.0f,
				"delta something",
				2,
				-1.0f,
				1.0f,
				_drawTonemapperReference,
				_plot_by_cie_L
				);
		}

		private void GenDeltaGraph(Image original_raw_image, Texture2D deltaMask, string labelX, int numDecimalsX, float scaleXRangeBottom, float scaleXRangeTop, string labelY, int numDecimalsY, float scaleYRangeBottom, float scaleYRangeTop, bool drawTonemapperReference = false, bool plotByCieL = false)
		{
			if (deltaMask == null)
			{
				GD.PushWarning("Missing delta image...");
				return;
			}
			var img = deltaMask.GetImage();
			img.Decompress();


			if (_generate_percentile_data)
			{
				GeneratePercentiles(img, deltaMask.ResourcePath);
			}


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
				for (int y = 0; y < N; y++)
				{
					Color deltaColor = img.GetPixel(x, y);
					float delta = deltaColor.B - deltaColor.R;
					delta = (float)Math.Clamp(delta, -1.0, 1.0);


					Color rawColor = original_raw_image.GetPixel(x, y);
					Lab rawLab;
					float L;
					if (plotByCieL)
					{
						rawLab = linear_srgb_to_cielab(new RGB { r = rawColor.R, g = rawColor.G, b = rawColor.B });
						L = rawLab.L / 100.0f;
					}
					else
					{
						rawLab = linear_srgb_to_oklab(new RGB { r = rawColor.R, g = rawColor.G, b = rawColor.B });
						L = rawLab.L;
					}


					// bring L from 0..inf to 0..1
					L = (float)Math.Clamp(L, 0.0, 3.0);
					L = L / 3.0f;

					DrawColorPointOnGraph(M, graph, (int)(L * M), rawColor, delta, _pointRadius);
				}

				if (drawTonemapperReference)
				{
					// draw tmo delta as reference
					float fx = x / (float)M;
					fx *= 3.0f;
					float tmo_delta = (float)(-Math.Exp(_tonemapperReferenceExposure * -fx) + 1) - fx;
					DrawColorPointOnGraph(M, graph, x, Colors.Black, tmo_delta, _referenceLineWidth);
				}
			}


			//linear to srgb so the colors in the chart look correct
			graph.LinearToSrgb();


			string delta_mask_name = "";
			delta_mask_name = deltaMask.ResourcePath.Substring(deltaMask.ResourcePath.LastIndexOf("/") + 1);
			delta_mask_name = delta_mask_name.Remove(delta_mask_name.LastIndexOf("."));
			delta_mask_name = "Palette_" + delta_mask_name;
			string imagePath = "res://_BA_/_Messdaten_/Tonemapping/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "__" + delta_mask_name + "_" + "_plot_" + ".png";
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

		private void DrawColorPointOnGraph(int M, Image graph, int x, Color c, float delta, int pointRadius)
		{
			int ix = (int)(((float)x / (float)M) * _graphRes);
			int iy = 0;

			if (delta <= 0.0)
			{
				// iy = _graphRes - (int)(delta * (_graphRes / 2) + (_graphRes / 2));
				iy = (int)(Math.Abs(delta) * (_graphRes / 2) + ((_graphRes - 1) / 2));
			}
			else
			{
				// iy = _graphRes - (int)(delta * (_graphRes / 2) - (_graphRes / 2));
				iy = -(int)(Math.Abs(delta) * (_graphRes / 2)) + ((_graphRes - 1) / 2);
			}

			if (ix >= _graphRes) ix = _graphRes - 1;
			if (ix < 0) ix = 0;
			if (iy >= _graphRes) iy = _graphRes - 1;
			if (iy < 0) iy = 0;

			for (int rx = -pointRadius; rx <= pointRadius; rx++)
			{
				for (int ry = -pointRadius; ry <= pointRadius; ry++)
				{
					if (new Vector2(rx, ry).Length() >= _pointRadius) continue;
					if (rx + ix >= _graphRes || rx + ix < 0) continue;
					if (ry + iy >= _graphRes || ry + iy < 0) continue;
					graph.SetPixel(ix + rx, iy + ry, c);
				}
			}
		}

		public void GeneratePercentiles(Image img, string imgResourcePath)
		{
			int M = img.GetWidth();
			int N = img.GetHeight();

			float[] data_delta = new float[M * N];

			for (int x = 0; x < M; x++)
			{
				for (int y = 0; y < N; y++)
				{
					data_delta[x * N + y] = img.GetPixel(x, y).B - img.GetPixel(x, y).R;
				}
			}

			Array.Sort(data_delta);

			string data_string = "";

			float min = data_delta[0];
			data_string += "Minimum:	" + min + "\n";

			float max = data_delta[data_delta.Length - 1];
			data_string += "Maximum:	" + max + "\n";

			double average = 0.0;
			for (int i = 0; i < data_delta.Length; i++)
			{
				average += data_delta[i];
			}
			average /= data_delta.Length;
			data_string += "Average:	" + average + "\n";

			float p_001 = CalculatePercentile(data_delta, 0.001f);
			data_string += "0.1 %:	" + p_001 + "\n";

			float p_010 = CalculatePercentile(data_delta, 0.01f);
			data_string += "1 %:		" + p_010 + "\n";

			float p_100 = CalculatePercentile(data_delta, 0.1f);
			data_string += "10 %:		" + p_100 + "\n";

			float p_median = CalculatePercentile(data_delta, 0.5f);
			data_string += "Median:	" + p_median + "\n";

			float p_900 = CalculatePercentile(data_delta, 0.9f);
			data_string += "90%:		" + p_900 + "\n";

			float p_990 = CalculatePercentile(data_delta, 0.99f);
			data_string += "99%:		" + p_990 + "\n";

			float p_999 = CalculatePercentile(data_delta, 0.999f);
			data_string += "99.9%:	" + p_999 + "\n";

			float CalculatePercentile(float[] data, float p)
			{
				return data[(int)Math.Round(data.Length * p)];
			}

			string delta_mask_name = "";
			delta_mask_name = imgResourcePath.Substring(imgResourcePath.LastIndexOf("/") + 1);
			delta_mask_name = delta_mask_name.Remove(delta_mask_name.LastIndexOf("."));
			delta_mask_name = "Palette_" + delta_mask_name;
			string path = "res://_BA_/_Messdaten_/Tonemapping/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "__" + delta_mask_name + "_" + "_plot_" + ".txt";
			var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
			if (file == null)
			{
				GD.PushWarning("Could not open file for writing percentile data at: " + path);
				return;
			}
			file.StoreString(data_string);
			file.Close();
			GD.Print("Saving percentile data to: " + path);
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

		public void Calculate_SSIM()
		{
			foreach (Texture2D after in _images_after_linear)
			{
				SSIM ssim = Get_SSIM(_image_before_linear, after);
				GD.Print("luminance: " + ssim.luminance);
				GD.Print("contrast: " + ssim.contrast);
				GD.Print("structure: " + ssim.structure);
				GD.Print("SSIM: " + ssim.ssim);
			}
		}

		private SSIM Get_SSIM(Texture2D beforeTex, Texture2D afterTex)
		{
			Image before = beforeTex.GetImage();
			Image after = afterTex.GetImage();
			before.Decompress();
			after.Decompress();


			double C1 = Math.Pow(0.01 * 255.0, 2.0);
			double C2 = Math.Pow(0.03 * 255.0, 2.0);
			double C3 = C2 / 2.0;


			int M = before.GetWidth();
			int N = before.GetHeight();
			int O = 3;

			double mue_i = 0.0;
			double mue_I = 0.0;
			for (int x = 0; x < M; x++)
			{
				for (int y = 0; y < N; y++)
				{
					for (int z = 0; z < O; z++)
					{
						mue_i += before.GetPixel(x, y)[z] * 255.0;
						mue_I += after.GetPixel(x, y)[z] * 255.0;
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
						sig_i2 += Math.Pow(before.GetPixel(x, y)[z] * 255.0 - mue_i, 2.0);
						sig_I2 += Math.Pow(after.GetPixel(x, y)[z] * 255.0 - mue_I, 2.0);

						sig_iI += (before.GetPixel(x, y)[z] * 255.0 - mue_i) * (after.GetPixel(x, y)[z] * 255.0 - mue_I);
					}
				}
			}
			sig_i2 /= M * N * O;
			sig_I2 /= M * N * O;
			sig_iI /= M * N * O;

			double l = (2.0 * mue_i * mue_I + C1) / (Math.Pow(mue_i, 2.0) + Math.Pow(mue_I, 2.0) + C1);
			double c = (2.0 * Math.Sqrt(sig_i2) * Math.Sqrt(sig_I2) + C2) / (sig_i2 + sig_I2 + C2);
			double s = (sig_iI + C3) / (Math.Sqrt(sig_i2) * Math.Sqrt(sig_I2) + C3);

			double ssim = l * c * s;

			return new SSIM { luminance = l, contrast = c, structure = s, ssim = ssim };
		}
	}
}