using Godot;
using Game.PlayerStuff;

namespace Game.Interactables
{
	public partial class NuggetPickup : Node3D
	{
		public override void _Ready()
		{
			var area = GetNode<Area3D>("Area3D");
			area.BodyEntered += OnBodyEntered;
		}

		public void OnBodyEntered(Node3D node)
        {
            Player player = node as Player;
			if (player.TryAddToInventory(Player.ItemType.Copper))
			{
				QueueFree();
			}
		}
	}
}