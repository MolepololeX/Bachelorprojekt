using Game.Managers;
using Godot;
using System;

public partial class Testcube : MeshInstance3D
{
	// needs to be either 1/int or int
	[Export] float _speedMult = 0.5f;
	private float _texelSizeInMeters;

	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		Camera3D cam = GameManager.Instance.Camera as Camera3D;
		Vector2 viewPortSize = (Vector2I)cam.GetViewport().GetVisibleRect().Size;
		float height = cam.Size;
		_texelSizeInMeters = 1.0f / (viewPortSize.Y / height);
		
		//moving left - right 	: cam is not angled so 1m -> 1m in camera width;
		//moving up -down		: cam is angled so 1m -> sin(cam angle) * 1m in cam height;
		//	so if i want to move something by one texel away from the cam it needs to move 1.0 / sin(cam angle) * texelSizeMeters
		//	luckily the sin(30degreesRad) is 0.5 so i can just make the smallest possible movement two == (1.0 / sin(30degRad)) texelSizesInMeters
		//	and it should work in any direction seamlessly

		Vector3 direction = Vector3.Zero;
		if (Input.IsActionPressed("arrow_left"))
		{
			direction.X += _texelSizeInMeters;
		}
		if (Input.IsActionPressed("arrow_right"))
		{
			direction.X -= _texelSizeInMeters;
		}
		if (Input.IsActionPressed("arrow_up"))
		{
			direction.Z += _texelSizeInMeters;
		}
		if (Input.IsActionPressed("arrow_down"))
		{
			direction.Z -= _texelSizeInMeters;
		}

		Position += direction * _speedMult;
	}
}
