using System;
using System.Threading.Tasks;
using Godot;
using static GenerateColorGrid;

[Tool]
public partial class DataManager : Node
{
	internal enum PalettePlotType
	{
		oklab_L,
		oklab_C,
		oklab_h,
		oklab_E,
		cie_L,
		cie_C,
		cie_H,
		cie_E
	}


	[ExportToolButton("Calculate SSIM")] public Callable CalcSSIM => Callable.From(Calculate_SSIM);
	[ExportToolButton("Calculate Average")] public Callable CalcAverage => Callable.From(Calculate_Average);
	[ExportToolButton("Calculate Percentiles")] public Callable CalcPercentile => Callable.From(Calculate_All_Percentile);


	[ExportCategory("Graphs")]
	[Export] private Color _backGroundColor = Colors.Black;
	[Export] private int _graphRes = 2048;
	[Export] private int _pointRadius = 9;
	[Export] private WorldEnvironment _env = null;
	[Export] private string _fileNameAtt = "";
	[ExportToolButton("Create Chart Delta By Hue")] public Callable CreateDeltaHGraph => Callable.From(Create_Chart_Delta_By_Hue);
	[ExportToolButton("Create Chart Chroma By Brightness")] public Callable CreateChromaBrightness => Callable.From(Create_Chart_Chroma_Brightness);
	[ExportToolButton("Create Chart Hue By Brightness")] public Callable CreateHueBrightness => Callable.From(Create_Chart_Hue_Brightness);
	// [ExportToolButton("Create All Graphs")] public Callable CreateGraphs => Callable.From(CreateAllGraphs);


	[ExportCategory("Images")]
	[ExportToolButton("CapturePrePost")] public Callable CaptureBoth => Callable.From(CapturePrePost);
	[ExportToolButton("CaptureBaseImage")] public Callable CaptureIngameImage => Callable.From(CaptureViewportPre);
	[ExportToolButton("CaptureComparisonImage")] public Callable CaptureIngameImageComparison => Callable.From(CaptureViewportPost);
	[ExportCategory("Test")]
	[ExportToolButton("Test_Calc_D65")] public Callable CalcD65 => Callable.From(Test_CalcD65);
	[Export] private bool _autoCapturePrePost = true;
	[Export] private bool _plotByOKLCh = true;
	// [Export] public Texture2D image_base;
	// [Export] public Texture2D image_compare;

	[ExportCategory("Palettes")]
	[ExportToolButton("Plot Palette Delta")] public Callable PlotPaletteDelta => Callable.From(GeneratePaletteDelta);
	[Export] private PalettePlotType palettePlotType = PalettePlotType.oklab_L;
	[Export] private Texture2D palette;


	Image baseImage;
	Image comparisonImage;


	public void GeneratePaletteDelta()
	{
		if (palette == null)
		{
			GD.PushWarning("Missing Palette...");
		}
		var img = palette.GetImage();
		img.Decompress();
		int M = img.GetWidth();
		int N = img.GetHeight();

		Image chart = Image.CreateEmpty(_graphRes, _graphRes, false, Image.Format.Rgb8);
		chart.Fill(_backGroundColor);

		//TODO: Draw Grid
		for (int x = 0; x < _graphRes; x++)
		{
			for (int y = -(_pointRadius / 3); y <= (_pointRadius / 3); y++)
			{
				chart.SetPixel(x, y + (_graphRes - 1) / 2, Colors.DarkGray);
			}
		}

		for (int x = 0; x < M; x++)
		{
			for (int y = 0; y < N; y++)
			{
				Color c = img.GetPixel(x, y);
				c = c.SrgbToLinear(); //highly necessary

				float delta = 0.0f;

				Lab c_lab;
				switch (palettePlotType)
				{
					case PalettePlotType.oklab_L:

						c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
						delta = c_lab.L;

						break;
					case PalettePlotType.oklab_C:

						c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
						double C = new Vector2(c_lab.a, c_lab.b).Length();
						delta = (float)C;

						break;
					case PalettePlotType.oklab_h:

						c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
						double hue = Math.Atan2(c_lab.b, c_lab.a) / (Math.PI * 2.0);
						delta = (float)hue;

						break;
					case PalettePlotType.oklab_E:

						c_lab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
						double e = new Vector3(c_lab.L, c_lab.a, c_lab.b).Length();
						delta = (float)e;

						break;

				}

				int ix = (int)(((float)x / (float)M) * _graphRes);
				int iy = 0;

				if (delta <= 0.0)
				{
					iy = _graphRes - ((int)(delta * ((_graphRes - 1) / 2)) + ((_graphRes - 1) / 2));
					// c = Colors.Blue;
				}
				else
				{
					iy = _graphRes - (int)(delta * ((_graphRes - 1) / 2)) - ((_graphRes - 1) / 2);
					// c = Colors.Red;
				}

				if (ix >= _graphRes || ix < 0)
				{
					ix = 0;
					GD.PushWarning("Hue was greater than 1");
				}
				if (iy >= _graphRes || iy < 0)
				{
					iy = 0;
					GD.PushWarning("Delta value was greater than 1: " + delta);
				}

				for (int rx = -_pointRadius; rx <= _pointRadius; rx++)
				{
					for (int ry = -_pointRadius; ry <= _pointRadius; ry++)
					{
						if (new Vector2(rx, ry).Length() >= _pointRadius) continue;
						if (rx + ix >= _graphRes || rx + ix < 0) continue;
						if (ry + iy >= _graphRes || ry + iy < 0) continue;
						chart.SetPixel(ix + rx, iy + ry, c);
					}
				}
			}
		}

		// chart.FlipY();

		string palette_name = "";
		palette_name = palette.ResourcePath.Substring(palette.ResourcePath.LastIndexOf("/") + 1);
		palette_name = palette_name.Remove(palette_name.LastIndexOf("."));
		palette_name = "Palette_" + palette_name;

		string imagePath = "res://_Messdaten/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "__" + palette_name + "_" + "_plot_" + palettePlotType.ToString() + ".png";
		GD.Print("Saved chart to: " + imagePath);
		GD.Print(chart.SavePng(imagePath));
	}


	public override void _Process(double delta)
	{
	}

	public async Task CapturePrePost()
	{
		if (_env == null)
		{
			GD.PushError("No World Environment");
			return;
		}
		Compositor comp = _env.Compositor;
		if (comp == null)
		{
			GD.PushError("No Compositor in World Environment");
			return;
		}

		comp.CompositorEffects[0].Enabled = false;
		await Task.Delay(100);

		CaptureViewportPre();
		await Task.Delay(100);

		comp.CompositorEffects[0].Enabled = true;
		await Task.Delay(100);

		CaptureViewportPost();
		await Task.Delay(100);
	}

	public async void SetEffectDrawType()
	{

	}

	public async void SetEffectColorSpace()
	{

	}

	public void Calculate_All_Percentile()
	{
		Calculate_Percentile(0.01f);
		Calculate_Percentile(0.1f);
		Calculate_Percentile(0.5f);
		Calculate_Percentile(0.9f);
		Calculate_Percentile(0.99f);
		Calculate_Percentile(0.999f);
	}

	public async Task CreateAllGraphs()
	{
		// await Create_Histogram_Delta_Hue();
	}

	public void Create_Chart_Hue_Brightness()
	{
		_ = Create_Chart_Hue_Brightness_Task();
	}

	public async Task Create_Chart_Hue_Brightness_Task()
	{
		if (_autoCapturePrePost)
		{
			await CapturePrePost();
		}

		int M = comparisonImage.GetWidth();
		int N = comparisonImage.GetHeight();

		Image chart = Image.CreateEmpty(_graphRes, _graphRes, false, Image.Format.Rgb8);
		chart.Fill(_backGroundColor);

		//TODO: Draw Grid
		for (int x = 0; x < _graphRes; x++)
		{
			for (int y = -(_pointRadius / 3); y <= (_pointRadius / 3); y++)
			{
				chart.SetPixel(x, y + (_graphRes - 1) / 2, Colors.DarkGray);
			}
		}

		(float, float)[] pairs = new (float, float)[M * N];


		for (int x = 0; x < M; x++)
		{
			for (int y = 0; y < N; y++)
			{
				Color c = comparisonImage.GetPixel(x, y);
				Lab c_oklab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });

				double hue = Math.Atan2(c_oklab.b, c_oklab.a) / (Math.PI * 2.0);
				float delta = (float)hue;

				int ix = 0;
				int iy = 0;

				ix = (int)(c_oklab.L * (_graphRes - 1));

				if (delta <= 0.0)
				{
					iy = _graphRes - ((int)(delta * ((_graphRes - 1) / 2)) + ((_graphRes - 1) / 2));
					// c = Colors.Blue;
				}
				else
				{
					iy = _graphRes - (int)(delta * ((_graphRes - 1) / 2)) - ((_graphRes - 1) / 2);
					// c = Colors.Red;
				}

				if (ix >= _graphRes || ix < 0)
				{
					ix = 0;
					// GD.PushWarning("Hue was greater than 1");
				}
				if (iy >= _graphRes || iy < 0)
				{
					iy = 0;
					// GD.PushWarning("Delta value was greater than 1: " + delta);
				}

				for (int rx = -_pointRadius; rx <= _pointRadius; rx++)
				{
					for (int ry = -_pointRadius; ry <= _pointRadius; ry++)
					{
						if (new Vector2(rx, ry).Length() >= _pointRadius) continue;
						if (rx + ix >= _graphRes || rx + ix < 0) continue;
						if (ry + iy >= _graphRes || ry + iy < 0) continue;
						chart.SetPixel(ix + rx, iy + ry, c);
					}
				}

				pairs[x * N + y] = (c.H, delta);
			}
		}

		// chart.FlipY();

		string imagePath = "res://_Messdaten/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "_" + _fileNameAtt + "_" + ".png";
		GD.Print("Saved chart to: " + imagePath);
		GD.Print(chart.SavePng(imagePath));
	}


	public void Create_Chart_Chroma_Brightness()
	{
		_ = Create_Chart_Chroma_Brightness_Task();
	}

	public async Task Create_Chart_Chroma_Brightness_Task()
	{
		if (_autoCapturePrePost)
		{
			await CapturePrePost();
		}

		int M = comparisonImage.GetWidth();
		int N = comparisonImage.GetHeight();

		Image chart = Image.CreateEmpty(_graphRes, _graphRes, false, Image.Format.Rgb8);
		chart.Fill(_backGroundColor);

		//TODO: Draw Grid
		for (int x = 0; x < _graphRes; x++)
		{
			for (int y = -(_pointRadius / 3); y <= (_pointRadius / 3); y++)
			{
				chart.SetPixel(x, y + (_graphRes - 1) / 2, Colors.DarkGray);
			}
		}

		(float, float)[] pairs = new (float, float)[M * N];


		for (int x = 0; x < M; x++)
		{
			for (int y = 0; y < N; y++)
			{
				Color c = comparisonImage.GetPixel(x, y);
				Lab c_oklab = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });

				float delta = new Vector2(c_oklab.a, c_oklab.b).Length();

				int ix = 0;
				int iy = 0;

				ix = (int)(c_oklab.L * (_graphRes - 1));

				if (delta <= 0.0)
				{
					iy = _graphRes - ((int)(delta * ((_graphRes - 1) / 2)) + ((_graphRes - 1) / 2));
					// c = Colors.Blue;
				}
				else
				{
					iy = _graphRes - (int)(delta * ((_graphRes - 1) / 2)) - ((_graphRes - 1) / 2);
					// c = Colors.Red;
				}

				if (ix >= _graphRes || ix < 0)
				{
					ix = 0;
					// GD.PushWarning("Hue was greater than 1");
				}
				if (iy >= _graphRes || iy < 0)
				{
					iy = 0;
					// GD.PushWarning("Delta value was greater than 1: " + delta);
				}

				for (int rx = -_pointRadius; rx <= _pointRadius; rx++)
				{
					for (int ry = -_pointRadius; ry <= _pointRadius; ry++)
					{
						if (new Vector2(rx, ry).Length() >= _pointRadius) continue;
						if (rx + ix >= _graphRes || rx + ix < 0) continue;
						if (ry + iy >= _graphRes || ry + iy < 0) continue;
						chart.SetPixel(ix + rx, iy + ry, c);
					}
				}

				pairs[x * N + y] = (c.H, delta);
			}
		}

		// chart.FlipY();

		string imagePath = "res://_Messdaten/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "_" + _fileNameAtt + "_" + ".png";
		GD.Print("Saved chart to: " + imagePath);
		GD.Print(chart.SavePng(imagePath));
	}

	public void Create_Chart_Delta_By_Hue()
	{
		_ = Create_Histogram_Delta_Hue_Task();
	}

	public async Task Create_Histogram_Delta_Hue_Task()
	{
		if (_autoCapturePrePost)
		{
			await CapturePrePost();
		}

		int M = baseImage.GetWidth();
		int N = baseImage.GetHeight();

		Image chart = Image.CreateEmpty(_graphRes, _graphRes, false, Image.Format.Rgb8);
		chart.Fill(_backGroundColor);

		//TODO: Draw Grid
		for (int x = 0; x < _graphRes; x++)
		{
			for (int y = -(_pointRadius / 3); y <= (_pointRadius / 3); y++)
			{
				chart.SetPixel(x, y + (_graphRes - 1) / 2, Colors.DarkGray);
			}
		}

		(float, float)[] pairs = new (float, float)[M * N];


		for (int x = 0; x < M; x++)
		{
			for (int y = 0; y < N; y++)
			{
				Color c = baseImage.GetPixel(x, y);

				float delta = comparisonImage.GetPixel(x, y).R - comparisonImage.GetPixel(x, y).B;

				int ix = 0;
				int iy = 0;

				Lab cl = linear_srgb_to_oklab(new RGB { r = c.R, g = c.G, b = c.B });
				double hue_l = (Math.Atan2(cl.a, cl.b) + Math.PI) / (Math.PI * 2.0);

				ix = (int)(c.H * (_graphRes - 1));
				if (_plotByOKLCh)
					ix = (int)(hue_l * (_graphRes - 1));

				if (delta <= 0.0)
				{
					iy = _graphRes - ((int)(delta * ((_graphRes - 1) / 2)) + ((_graphRes - 1) / 2));
					// c = Colors.Blue;
				}
				else
				{
					iy = _graphRes - (int)(delta * ((_graphRes - 1) / 2)) - ((_graphRes - 1) / 2);
					// c = Colors.Red;
				}

				if (ix >= _graphRes || ix < 0)
				{
					ix = 0;
					// GD.PushWarning("Hue was greater than 1");
				}
				if (iy >= _graphRes || iy < 0)
				{
					iy = 0;
					// GD.PushWarning("Delta value was greater than 1: " + delta);
				}

				for (int rx = -_pointRadius; rx <= _pointRadius; rx++)
				{
					for (int ry = -_pointRadius; ry <= _pointRadius; ry++)
					{
						if (new Vector2(rx, ry).Length() >= _pointRadius) continue;
						if (rx + ix >= _graphRes || rx + ix < 0) continue;
						if (ry + iy >= _graphRes || ry + iy < 0) continue;
						chart.SetPixel(ix + rx, iy + ry, c);
					}
				}

				pairs[x * N + y] = (c.H, delta);
			}
		}

		// chart.FlipY();

		string imagePath = "res://_Messdaten/" + Time.GetDatetimeStringFromSystem().Replace(":", "_").Replace("T", "__") + "_" + _fileNameAtt + "_" + ".png";
		GD.Print("Saved chart to: " + imagePath);
		GD.Print(chart.SavePng(imagePath));
	}

	public void Calculate_Percentile(float p)
	{
		var img = GetViewport().GetTexture().GetImage();
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

	public void CaptureViewportPre()
	{
		var img = GetViewport().GetTexture().GetImage();
		string imagePath = "res://screenshot_base.png";
		img.SavePng(imagePath);
		baseImage = img;
	}
	public void CaptureViewportPost()
	{
		var img = GetViewport().GetTexture().GetImage();
		string imagePath = "res://screenshot_comp.png";
		img.SavePng(imagePath);
		comparisonImage = img;
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
