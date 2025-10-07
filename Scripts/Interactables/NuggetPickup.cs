using Godot;
using System;

public partial class NuggetPickup : Node3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var area = GetNode<Area3D>("Area3D");
		area.BodyEntered += OnBodyEntered;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
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
