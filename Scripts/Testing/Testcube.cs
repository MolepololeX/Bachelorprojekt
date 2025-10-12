using Game.Managers;
using Godot;
using System;

public partial class Testcube : MeshInstance3D
{
	private float _texelSizeInMeters;
	[Export] private int maxSteps = 60;
	private bool moveRight = true;
	private int moveCounter = 0;

	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector2 viewPortSize = (Vector2I)GetViewport().GetVisibleRect().Size;
		_texelSizeInMeters = 1.0f / (viewPortSize.Y / (int)(GameManager.Instance.Camera as Camera3D).Size);
		if (moveCounter >= maxSteps)
		{
			moveRight = !moveRight;
			moveCounter = 0;
		}
		if (moveRight)
		{
			Position = new Vector3(Position.X + _texelSizeInMeters, Position.Y, Position.Z);
		}
		else
		{
			Position = new Vector3(Position.X - _texelSizeInMeters, Position.Y, Position.Z);
		}

		moveCounter++;
	}
}
