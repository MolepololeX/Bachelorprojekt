using Godot;
using System;

[Tool]
public partial class SSIM : Node
{
	[ExportToolButton("Calculate SSIM")] public Callable CalcSSIM => Callable.From(Calculate_SSIM);
	[ExportToolButton("Calculate Average")] public Callable CalcAverage => Callable.From(Calculate_Average);
	[ExportToolButton("Calculate Percentiles")] public Callable CalcPercentile => Callable.From(Calculate_All_Percentile);
	[ExportCategory("Graphs")]
	[Export] private int _graphRes = 2048;
	[Export] private int _pointRadius = 9;
	[ExportToolButton("Create Histogramm deltaH")] public Callable CreateDeltaHGraph => Callable.From(Create_Histogram_Delta_Hue);
	[ExportCategory("Images")]
	[ExportToolButton("CaptureBaseImage")] public Callable CaptureIngameImage => Callable.From(CaptureViewport);
	[ExportToolButton("CaptureComparisonImage")] public Callable CaptureIngameImageComparison => Callable.From(CaptureViewportComparison);
	[ExportCategory("Test")]
	[ExportToolButton("Test_Calc_D65")] public Callable CalcD65 => Callable.From(Test_CalcD65);
	// [Export] public Texture2D image_base;
	// [Export] public Texture2D image_compare;

	Image i;
	Image I;

	public override void _Ready() { }
	public override void _Process(double delta) { }

	public void Calculate_All_Percentile()
	{
		Calculate_Percentile(0.01f);
		Calculate_Percentile(0.1f);
		Calculate_Percentile(0.5f);
		Calculate_Percentile(0.9f);
		Calculate_Percentile(0.99f);
		Calculate_Percentile(0.999f);
	}

	public void Create_Histogram_Delta_Hue_Chroma()
    {
        int M = i.GetWidth();
        int N = i.GetHeight();

        Image chart = Image.CreateEmpty(_graphRes, _graphRes, false, Image.Format.Rgb8);
        chart.Fill(Colors.White);

        //TODO: Draw Grid

        (float, float)[] pairs = new (float, float)[M * N];

        CreateGraph(M, N, chart, pairs);

        string imagePath = "res://graph_d.png";
        chart.SavePng(imagePath);
    }

    private void CreateGraph(int M, int N, Image chart, (float, float)[] pairs)
    {
        for (int x = 0; x < M; x++)
        {
            for (int y = 0; y < N; y++)
            {
                Color c = i.GetPixel(x, y);

                float delta = i.GetPixel(x, y).R - i.GetPixel(x, y).B;

                int ix = 0;
                int iy = 0;

                ix = (int)(c.H * (_graphRes - 1));

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

	public void CaptureViewport()
	{
		var img = GetViewport().GetTexture().GetImage();
		string imagePath = "res://screenshot_base.png";
		img.SavePng(imagePath);
		i = img;
	}
	public void CaptureViewportComparison()
	{
		var img = GetViewport().GetTexture().GetImage();
		string imagePath = "res://screenshot_comp.png";
		img.SavePng(imagePath);
		I = img;
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
		i.Decompress();
		I.Decompress();

		double C1 = Math.Pow(0.01 * 255.0, 2.0);
		double C2 = Math.Pow(0.03 * 255.0, 2.0);
		double C3 = C2 / 2.0;

		int M = i.GetWidth();
		int N = i.GetHeight();
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
					mue_i += i.GetPixel(x, y)[z] * 255.0; //check if format correct
					mue_I += I.GetPixel(x, y)[z] * 255.0; //check if format correct
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
					sig_i2 += Math.Pow(i.GetPixel(x, y)[z] * 255.0 - mue_i, 2.0); //check if format correct
					sig_I2 += Math.Pow(I.GetPixel(x, y)[z] * 255.0 - mue_I, 2.0); //check if format correct

					sig_iI += (i.GetPixel(x, y)[z] * 255.0 - mue_i) * (I.GetPixel(x, y)[z] * 255.0 - mue_I); //check if format correct
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
