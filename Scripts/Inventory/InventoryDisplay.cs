using System;
using Game.Item;
using Game.Managers;
using Game.PlayerStuff;
using Godot;

namespace Game.InventoryStuff
{
    public partial class InventoryDisplay : Node3D
    {
        [Export] private float _spacingY;
        [Export] private Node3D _anchor;
        [Export] private Node3D _tempPointer;
        [Export] private float _tempPointerEasing = 10.0f;

        private Inventory _inv;
        private Vector3 _tempPointerNewPos;

        public override void _Ready()
        {
            _inv = (GameManager.Instance.Player as Player).Inventory;
            UpdateItems();
            _tempPointerNewPos = _tempPointer.Position;
        }

        public override void _EnterTree()
        {
            (GameManager.Instance.Player as Player).Inventory.ItemAdded += UpdateItems;
            (GameManager.Instance.Player as Player).Inventory.ItemRemoved += UpdateItems;
        }

        public override void _ExitTree()
        {
            (GameManager.Instance.Player as Player).Inventory.ItemAdded -= UpdateItems;
            (GameManager.Instance.Player as Player).Inventory.ItemRemoved -= UpdateItems;
        }

        public override void _Process(double delta)
        {
            _tempPointerNewPos = new Vector3(0.0f, _inv.SelectionIndex * _spacingY, 0.0f);
            _tempPointer.Position = _tempPointer.Position.Lerp(_tempPointerNewPos, _tempPointerEasing * (float)delta);
        }


        private void UpdateItems()
        {
            //TODO: temp spaeter besser machen danke
            foreach (var c in _anchor.GetChildren())
            {
                c.QueueFree();
            }

            for (int i = 0; i < _inv.Content.Count; i++)
            {
                WorldItem item = WorldItem.CreateInstanceFromType(_inv.Content[i]);
                item.SpawnAsInventoryItem();
                item.Position = new Vector3(0.0f, _spacingY * (float)i, 0.0f);
                _anchor.AddChild(item);
            }

            if (_inv.Content.Count == 0)
            {
                _tempPointer.Visible = false;
            }
            else
            {
                _tempPointer.Visible = true;
            }
        }
    }
}