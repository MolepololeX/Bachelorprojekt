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
		[Export] private float inventoryScale = 1.0f;
		[Export] private bool cauldronRandomRotate = true;
		[Export] private bool randomRotateOnSpawn = true;
		[Export] private Vector3 spawnRotation;

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

			if (randomRotateOnSpawn)
				spawnRotation = new Vector3(
								GD.Randf() * (float)Math.PI * 2.0f,
								GD.Randf() * (float)Math.PI * 2.0f,
								GD.Randf() * (float)Math.PI * 2.0f
							);

			Rotation = spawnRotation;
		}

		public void OnBodyEntered(Node3D node)
		{
			Player player = node as Player;
			if (player.Inventory.AddItem(itemType))
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
			rb.SetPhysicsProcess(false);
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

		public void SpawnAsInventoryItem(Vector3 rotation = default)
		{
			if(rotation == default)
            {
				float x = (float)Math.PI / 180.0f;
				rotation = new Vector3(45.0f * x, 45.0f * x, 0.0f * x);
            }
			area.Monitoring = false;
			rb.CollisionLayer = 0;
			rb.CollisionMask = 0;
			rb.GravityScale = 0.0f;
			randomRotateOnSpawn = false;
			spawnRotation = rotation;
			Scale *= inventoryScale;
		}

		private async void Destroy(double delay)
		{
			await Task.Delay((int)(delay * 1000.0));
			QueueFree();
		}

		//Gives both angular and linear velocity
		public void GiveRandomVelocity(Vector3 vel)
		{
			rb.LinearVelocity = vel;
			rb.AngularVelocity = vel;
		}

		public static WorldItem CreateInstanceFromType(ItemType itemType)
		{
			if (!GameManager.Instance.WorldItems.ContainsKey(itemType))
			{
				GD.Print("...No spawnable WorldItem Found");
				itemType = ItemType.None;
			}

			PackedScene item = GameManager.Instance.WorldItems[itemType];
			WorldItem i = item.Instantiate() as WorldItem;

			if (i == null)
			{
				GD.Print("...Cast to WorldItem failed");
				item = GameManager.Instance.WorldItems[ItemType.None];
				i = item.Instantiate() as WorldItem;
			}

			return i;
		}
	}
}