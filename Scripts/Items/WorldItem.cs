using Godot;
using Game.PlayerStuff;
using Game.Managers;
using System;
using System.Threading.Tasks;

namespace Game.Item
{
	public partial class WorldItem : Node3D
	{
		[ExportCategory("Individual Settings")]
		[Export] private ItemType itemType;
		[Export] private float cauldronScale = 0.5f;
		[Export] private bool cauldronRandomRotate = true;

		[ExportCategory("Base Dependencies")]
		[Export] private Area3D area;
		[Export] private RigidBody3D rb;
		[Export] private Node3D model;
		[Export] private PackedScene pickupEffect;
		[Export] private PackedScene spawnEffect;
		[Export] private AudioStreamPlayer pickupSound;
		[Export] private GpuParticles3D _speedParticles;

		public override void _Ready()
		{
			area.BodyEntered += OnBodyEntered;
			SpawnVFX(spawnEffect);
			_speedParticles.Emitting = false;

			model.Rotation = new Vector3(
							GD.Randf() * (float)Math.PI * 2.0f,
							GD.Randf() * (float)Math.PI * 2.0f,
							GD.Randf() * (float)Math.PI * 2.0f
						);
		}

		public void OnBodyEntered(Node3D node)
		{
			Player player = node as Player;
			if (player.TryAddToInventory(itemType))
			{
				SpawnVFX(pickupEffect);
				pickupSound.Play();
				QueueFree();
			}
		}

		public override void _PhysicsProcess(double delta)
		{
			if (rb.LinearVelocity.Length() > 1.0f)
			{
				_speedParticles.Emitting = true;
			}
			else
			{
				_speedParticles.Emitting = false;
			}
		}


		private void SpawnVFX(PackedScene effect)
		{
			Node3D vfx = effect.Instantiate() as Node3D;
			vfx.Position = rb.GlobalPosition;
			GameManager.Instance.TempSceneRoot.AddChild(vfx);
		}

		public void DropAsItemInCauldron(float destroyAfter)
		{
			area.Monitoring = false;
			rb.CollisionLayer = 0;
			rb.CollisionMask = 0;
			model.Scale *= cauldronScale;
			if (cauldronRandomRotate)
			{
				model.Rotation = new Vector3(
					GD.Randf() * (float)Math.PI * 2.0f,
					GD.Randf() * (float)Math.PI * 2.0f,
					GD.Randf() * (float)Math.PI * 2.0f
				);
			}
			Destroy(destroyAfter);
		}

		private async void Destroy(double delay)
		{
			await Task.Delay((int)(delay * 1000.0));
			QueueFree();
		}

		public void GiveVelocity(Vector3 vel)
		{
			rb.LinearVelocity = vel;
			rb.AngularVelocity = vel;
		}
	}
}