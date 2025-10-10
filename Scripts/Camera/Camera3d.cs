using Godot;
using System;

public partial class Camera3d : Camera3D
{
	[Export]
	public Node3D playerNode;
	[Export]
	public ColorRect rect;
	[Export]
	public double zoom = 1.0;
	private ShaderMaterial mat;

	private float delay;

	public override void _Ready()
	{
		mat = rect.Material as ShaderMaterial;
		GD.Print(mat);
	}

	public override void _Process(double delta)
	{
		Vector2 pos = UnprojectPosition(playerNode.Position);
		mat.SetShaderParameter("offsetX", pos.X);
		mat.SetShaderParameter("offsetY", pos.Y);
		mat.SetShaderParameter("zoom", zoom);

		if (delay > 1.0)
		{
			delay = 0.0f;
			GD.Print(pos);
		}
		delay += (float)delta;
	}
}
