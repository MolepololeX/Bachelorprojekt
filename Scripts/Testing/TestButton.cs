using Game.Managers;
using Game.PlayerStuff;
using Godot;
using System;

public partial class TestButton : Button
{
    public override void _Ready()
    {
        MouseEntered += OnEntered;
        MouseExited += OnExited;
        Pressed += OnPressed;
    }

    private void OnPressed()
    {
        GD.Print("Pressed");
    }

    private void OnExited()
    {
        GD.Print("Focus Exit");
    }

    private void OnEntered()
    {
        (GameManager.Instance.Player as Player).Inventory.SelectionIndex = 0;
        GD.Print("Focus Entered");
    }
}
