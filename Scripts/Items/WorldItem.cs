using Godot;
using Game.PlayerStuff;
using Game.Managers;

namespace Game.Item
{
	public partial class WorldItem : Node3D
	{
		[Export] private ItemType itemType;
		[Export] private Area3D area;
		[Export] private PackedScene pickupEffect;
		[Export] private PackedScene spawnEffect;
		[Export] private AudioStreamPlayer pickupSound;

		public override void _Ready()
		{
			area.BodyEntered += OnBodyEntered;
			SpawnVFX(spawnEffect);
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

		private void SpawnVFX(PackedScene effect)
		{
			Node3D vfx = effect.Instantiate() as Node3D;
			vfx.Position = area.GlobalPosition;
			GameManager.Instance.TempSceneRoot.AddChild(vfx);
		}

		public void DisablePickup()
		{
			area.Monitoring = false;
		}
	}
}