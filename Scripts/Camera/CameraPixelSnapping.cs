using Godot;
using System;
using System.Runtime.CompilerServices;

public partial class CameraPixelSnapping : Camera3D
{
	[ExportCategory("Config")]
	[Export] private ColorRect rect;

	[ExportCategory("Debug")]
	[Export] private bool _debugInfo = true;
	[Export] private bool _debugMovement = true;
	[Export] private Label _debugLabel;
	[Export] private float _camSpeed = 1.0f;
	[Export] private float _testmult = 1.0f;

	private ShaderMaterial mat;

	public override void _Ready()
	{
		mat = rect.Material as ShaderMaterial;
	}

	public override void _Process(double delta)
	{
		if (_debugMovement)
		{
			DebugMoveCam(delta);
		}

		Vector2 viewPortSize = (Vector2I)GetViewport().GetVisibleRect().Size;

		float texelSizeInMeters = 1.0f / (viewPortSize.Y / (int)Size);

		Vector3 p1 = GlobalPosition * GlobalTransform.Basis; //TODO: find out why this is so ass

		Vector3 p2 = p1.Snapped(texelSizeInMeters);

		Vector3 snapError = p2 - p1;

		HOffset = snapError.X;
		VOffset = snapError.Y;

		Vector2 snapDelta;

		snapDelta.X = snapError.X / texelSizeInMeters;// * (1.0f / viewPortSize.X);
		snapDelta.Y = snapError.Y / texelSizeInMeters;// * (1.0f / viewPortSize.X);



		mat.SetShaderParameter("snapDeltaX", snapDelta.X);
		mat.SetShaderParameter("snapDeltaY", snapDelta.Y);
		mat.SetShaderParameter("width", viewPortSize.X);
		mat.SetShaderParameter("height", viewPortSize.Y);
		mat.SetShaderParameter("testMult", _testmult);

		if (_debugInfo)
		{
			_debugLabel.Text = "";
			_debugLabel.Text += viewPortSize.ToString() + "\n";
			_debugLabel.Text += "s " + texelSizeInMeters + "\n";
			// _debugLabel.Text += "p1 " + p1 + "\n";
			// _debugLabel.Text += "p2 " + p2 + "\n";
			_debugLabel.Text += "r " + snapError + "\n";
			_debugLabel.Text += "d " + snapDelta + "\n";
		}
	}

	private void DebugMoveCam(double delta)
	{
		Vector3 dir = Vector3.Zero;
		if (Input.IsActionPressed("move_up"))
		{
			dir.Z -= _camSpeed * (float)delta;
		}
		if (Input.IsActionPressed("move_down"))
		{
			dir.Z += _camSpeed * (float)delta;
		}
		if (Input.IsActionPressed("move_right"))
		{
			dir.X += _camSpeed * (float)delta;
		}
		if (Input.IsActionPressed("move_left"))
		{
			dir.X -= _camSpeed * (float)delta;
		}

		Position += dir;
	}
}
