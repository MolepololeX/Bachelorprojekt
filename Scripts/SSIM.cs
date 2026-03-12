using Godot;
using System;

[Tool]
public partial class SSIM : Node
{
    [ExportToolButton("CalculateSSIMFromImages")] public Callable CalcSSIM => Callable.From(Calculate_SSIM);
	[Export] public Texture2D image_base;
	[Export] public Texture2D image_compare;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

//nutzt rgb 0...255
	public double Calculate_SSIM()
	{
		Image i = image_base.GetImage();
		Image I = image_compare.GetImage();
		i.Decompress();
		I.Decompress();

		double C1 = Math.Pow(0.01 * 255.0, 2.0);
		double C2 = Math.Pow(0.03 * 255.0, 2.0);
	 	double C3 = C2 / 2.0;

		int M = image_base.GetHeight();
		int N = image_base.GetWidth();
		int O = 3;

		double mue_i = 0.0;
		double mue_I = 0.0;
		for(int x = 0; x < M; x++)
		{
			for(int y = 0; y < N; y++)
			{
				for(int z = 0; z < O; z++)
				{
					// GD.Print(x + "/" + M + ", " + y + "/" + N + ", " + z + "/" + O);
					mue_i += i.GetPixel(x, y)[z] * 255.0; //check if format correct
					mue_I += I.GetPixel(x, y)[z] * 255.0; //check if format correct
				}
			}
		}
		mue_i /= M*N*O;
		mue_I /= M*N*O;

		double sig_i2 = 0.0;
		double sig_I2 = 0.0;
		double sig_iI = 0.0;
		for(int x = 0; x < M; x++)
		{
			for(int y = 0; y < N; y++)
			{
				for(int z = 0; z < O; z++)
				{
					sig_i2 += Math.Pow(i.GetPixel(x, y)[z] * 255.0 - mue_i, 2.0); //check if format correct
					sig_I2 += Math.Pow(I.GetPixel(x, y)[z] * 255.0 - mue_I, 2.0); //check if format correct
					
					sig_iI += (i.GetPixel(x, y)[z] * 255.0 - mue_i) * (I.GetPixel(x, y)[z] * 255.0 - mue_I); //check if format correct
				}
			}
		}
		sig_i2 /= M*N*O;
		sig_I2 /= M*N*O;
		sig_iI /= M*N*O;

		double l = (2.0 * mue_i * mue_I + C1) / (Math.Pow(mue_i, 2.0) + Math.Pow(mue_I, 2.0) + C1);
		double c = (2.0 * Math.Sqrt(sig_i2) * Math.Sqrt(sig_I2) + C2) / (sig_i2 + sig_I2 + C2);
		double s = (sig_iI + C3) / (Math.Sqrt(sig_i2) * Math.Sqrt(sig_I2) + C3);

		double SSIM = l * c * s;

		GD.Print("SSIM: " + SSIM);
		return SSIM;
	}
}
