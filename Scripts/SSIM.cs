using Godot;
using System;

[Tool]
public partial class SSIM : Node
{
	[ExportToolButton("Calculate SSIM")] public Callable CalcSSIM => Callable.From(Calculate_SSIM);
	[ExportToolButton("Calculate Average")] public Callable CalcAverage => Callable.From(Calculate_Average);
	[ExportToolButton("Calculate Percentiles")] public Callable CalcPercentile => Callable.From(Calculate_All_Percentile);
	[ExportToolButton("CaptureBaseImage")] public Callable CaptureIngameImage => Callable.From(CaptureViewport);
	[ExportToolButton("CaptureComparisonImage")] public Callable CaptureIngameImageComparison => Callable.From(CaptureViewportComparison);
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

	public double Calculate_Percentile(float p)
	{
		var img = GetViewport().GetTexture().GetImage();
		int M = img.GetWidth();
		int N = img.GetHeight();

		float[] data = new float[M * N * 2]; //will include r and b channel, b are neagtive values

		for (int x = 0; x < M; x++)
		{
			for (int y = 0; y < N; y++)
			{
				data[x * N + y] = img.GetPixel(x, y).R;
				data[x * N + y + (M * N)] = -img.GetPixel(x, y).B;
				// data[x + y + (M * N)] = -img.GetPixel(x, y).B;
				//g channel is unused
			}
		}

		Array.Sort(data);
		double percentile = data[(int)Math.Round(data.Length * p)];

		GD.Print("Image " + (int)Math.Round(p * 100.0) + "th Percentile: " + percentile);
		return percentile;
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
		// string imagePath = "res://screenshot_base.png";
		// img.SavePng(imagePath);
		i = img;
	}
	public void CaptureViewportComparison()
	{
		var img = GetViewport().GetTexture().GetImage();
		// string imagePath = "res://screenshot_comp.png";
		// img.SavePng(imagePath);
		I = img;
	}

	public double Calculate_Average()
	{
		var img = GetViewport().GetTexture().GetImage();
		int M = img.GetWidth();
		int N = img.GetHeight();
		int O = 3;

		double total = 0.0;
		for (int x = 0; x < M; x++)
		{
			for (int y = 0; y < N; y++)
			{
				for (int z = 0; z < O; z++)
				{
					// if((x + y + z) % 10000 == 0) GD.Print(x + "/" + M + ", " + y + "/" + N + ", " + z + "/" + O);
					total += img.GetPixel(x, y)[z]; //check if format correct
				}
			}
		}
		double average = total / (M * N); //O component is ignored since there can only be values in either the r xor b channel
		GD.Print("Image Average: " + average);
		return average;
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
