using Godot;
using System;

namespace Game.Interactables
{
	public partial class ChoppableTree : Node3D
	{
		[Export]
		public AnimationPlayer animPlayer;
		[Export]
		public Node3D Parent;
		[Export]
		public Node3D AnimRoot;
		[Export]
		public GpuParticles3D emitter;

		private int health = 5;

		public override void _Ready()
		{
			animPlayer.AnimationFinished += OnAnimationFinished;
		}

		public override void _Process(double delta)
		{
		}

		public void OnHit()
		{
			health--;
			if (health <= 0)
			{
				Die();
			}
			else
			{
				HitEffect();
			}
		}

		private void HitEffect()
		{
			int rand = GD.RandRange(0, 2);
			animPlayer.SpeedScale = 3.0f;
			switch (rand)
			{
				case 0:
					GD.Print("1");
					animPlayer.Play("Shake");
					break;
				case 1:
					GD.Print("2");
					animPlayer.Play("Shake2");
					break;
				case 2:
					GD.Print("3");
					animPlayer.Play("Shake3");
					break;
			}
			emitter.Emitting = true;
		}

		private void Die()
		{
			emitter.Emitting = true;
			animPlayer.SpeedScale = 1.0f;
			animPlayer.Play("Chop");
		}

		private void OnAnimationFinished(StringName animName)
		{
			emitter.Emitting = false;

			if (animName == "Chop")
			{
				animPlayer.Play("Delay");
			}

			if (animName == "Delay")
			{
				Parent.QueueFree();
			}
		}

	}
}