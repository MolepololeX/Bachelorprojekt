using System;
using Game.Managers;
using Godot;

namespace Game.Interactables
{
	public partial class ChoppableTree : Node3D
	{
		[Export] private AnimationPlayer animPlayer;
		[Export] private Node3D parent;
		[Export] private Node3D animRoot;
		[Export] private Node3D[] poofEffectSockets;
		[Export] private GpuParticles3D emitter;
		[Export] private PackedScene poofEffectScene;
		[Export] private float delayScale = 25.0f;
		[Export] private PackedScene spawnedItem;
		[Export] private Vector3 spawnOffset = new Vector3(0.0f, 10.0f, 0.0f);

		private int health = 5;
		private bool dead = false;

		public override void _Ready()
		{
			animPlayer.AnimationFinished += OnAnimationFinished;
		}

		public override void _Process(double delta)
		{
		}

		public void OnHit()
		{
			if (!dead)
			{
				health--;
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
					animPlayer.Play("Shake");
					break;
				case 1:
					animPlayer.Play("Shake2");
					break;
				case 2:
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
			dead = true;
		}

		private void OnAnimationFinished(StringName animName)
		{
			emitter.Emitting = false;

			if (animName == "Chop")
			{
				animPlayer.SpeedScale = delayScale;
				animPlayer.Play("Delay");
			}
			else if (animName == "Delay")
			{
				foreach (Node3D socket in poofEffectSockets)
				{
					SpawnPoof(socket);
				}
				SpawnItem();
				parent.QueueFree();
			}
			else if (health <= 0)
			{
				Die();
			}
		}

		private void SpawnItem()
		{
			Node3D item = spawnedItem.Instantiate() as Node3D;
			item.Position = parent.GlobalPosition + spawnOffset;
			item.Rotate(new Vector3(GD.Randf(), GD.Randf(), GD.Randf()).Normalized(), (float)GD.RandRange(0.0f, Mathf.Pi * 2.0));
			GameManager.Instance.TempSceneRoot.AddChild(item);
		}

		private void SpawnPoof(Node3D socket)
		{
			Node3D poofEffect = poofEffectScene.Instantiate() as Node3D;
			poofEffect.Position = socket.GlobalPosition;
			GameManager.Instance.TempSceneRoot.AddChild(poofEffect);
		}
	}
}