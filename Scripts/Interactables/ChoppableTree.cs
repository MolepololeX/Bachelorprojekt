using Godot;
using System;

public partial class ChoppableTree : Node3D
{
	[Export]
	public AnimationPlayer animPlayer;

	private int health = 5;

	public override void _Ready()
	{
	}

	public override void _Process(double delta)
	{
	}

	public void OnHit()
	{
		health--;
		HitEffect();
		if (health <= 0)
		{
			Die();
		}
	}

	private void HitEffect()
	{

	}

	private void Die()
	{
		QueueFree();
	}
}
