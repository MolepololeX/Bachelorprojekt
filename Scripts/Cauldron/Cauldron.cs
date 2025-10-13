using Game.Item;
using Game.Managers;
using Game.PlayerStuff;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;

public partial class Cauldron : Node3D
{
    [Export] private Area3D _interacable;
    [Export] private Node3D _itemSpawnPosition;

    private bool _isPlayerInRange;
    private Player _player;

    public override void _Ready()
    {
        _interacable.BodyEntered += OnBodyEntered;
        _interacable.BodyExited += OnBodyLeft;
    }

    public override void _Process(double delta)
    {
        if (_isPlayerInRange)
        {
            Interact();
        }
    }

    private void Interact()
    {
        if (Input.IsActionJustPressed("cauldron_add"))
        {
            ItemType itemType = _player.RequestCurrentSelectedItem();
            if (itemType == ItemType.None)
            {
                GD.Print("...No Item Selected");
                return;
            }

            if (!GameManager.Instance.WorldItems.ContainsKey(itemType))
            {
                GD.Print("...No spawnable WorldItem Found");
                return;
            }

            PackedScene item = GameManager.Instance.WorldItems[itemType];
            WorldItem i = item.Instantiate() as WorldItem;
            if (i == null)
            {
                GD.Print("...Cast to WorldItem failed");
                return;
            }

            i.DisablePickup();
            i.Scale *= 0.5f;
            i.Position = _itemSpawnPosition.GlobalPosition;
            GameManager.Instance.TempSceneRoot.AddChild(i);


            GD.Print(">Added Item");
        }
        if (Input.IsActionJustPressed("cauldron_extract"))
        {
            GD.Print(">Extracted Result...");
        }
    }

    private void OnBodyEntered(Node3D body)
    {
        _player = body as Player;
        if (_player != null)
        {
            _isPlayerInRange = true;
            GD.Print(">Player in Range!");
        }
    }

    private void OnBodyLeft(Node3D body)
    {
        if (body as Player == _player)
        {
            _isPlayerInRange = false;
            _player = null;
            GD.Print(">Player left...");
        }
    }
}
