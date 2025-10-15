using Game.Item;
using Game.Managers;
using Game.PlayerStuff;
using Godot;
using Godot.Collections;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace Game.Cauldron
{
    public partial class Cauldron : Node3D
    {
        [Export] private Area3D _interacable;
        [Export] private Node3D _itemSpawnPosition;
        [Export] private float _itemExtractOffsetHeight = 5.0f;
        [Export] private float _itemExplosionHeightOffset = 3.0f;
        [Export] private float _itemExplosionVelocity = 10.0f;

        private bool _isPlayerInRange;
        private Player _player;
        private List<ItemType> _content = new List<ItemType>();

        public override void _Ready()
        {
            _interacable.BodyEntered += OnBodyEntered;
            _interacable.BodyExited += OnBodyLeft;
        }

        public override void _PhysicsProcess(double delta)
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
                ItemType itemType = _player.Inventory.RequestCurrentSelectedItem();
                if (itemType == ItemType.None)
                {
                    GD.Print("...No Item Selected");
                    return;
                }

                WorldItem i = WorldItem.CreateInstanceFromType(itemType);
                i.Position = _itemSpawnPosition.GlobalPosition;
                GameManager.Instance.TempSceneRoot.AddChild(i);
                i.DropAsItemInCauldron(.7f);

                _content.Add(itemType);

                GD.Print(">Contents");
                foreach (var t in _content)
                {
                    GD.Print("..." + t.ToString());
                }
            }
            if (Input.IsActionJustPressed("cauldron_extract"))
            {
                GD.Print(">Extracting Result...");
                ExtractResult();
            }
        }

        private void ExtractResult()
        {
            if (_content.Count == 0) return;
            ItemType result = ItemType.None;

            var recipeResult = Recipes.SolveRecipe2(_content);
            result = recipeResult.Item1;

            if (result == ItemType.None)
            {
                EjectContents();
                _content.Clear();
            }
            else
            {
                DropResult(result);
                foreach (ItemType t in recipeResult.Item2)
                {
                    _content.Remove(t);
                }
            }
        }

        private void DropResult(ItemType result)
        {
            WorldItem item = WorldItem.CreateInstanceFromType(result);
            item.Position = GameManager.Instance.Player.GlobalPosition + new Vector3(0.0f, _itemExtractOffsetHeight, 0.0f);
            GameManager.Instance.TempSceneRoot.AddChild(item);
        }

        private void EjectContents()
        {
            foreach (var itemType in _content)
            {
                WorldItem item = WorldItem.CreateInstanceFromType(itemType);

                item.Position = GlobalPosition + new Vector3(
                    (float)GD.RandRange(-1.0, 1.0) * _itemExplosionVelocity * 0.1f,
                     _itemExplosionHeightOffset,
                    (float)GD.RandRange(-1.0, 1.0) * _itemExplosionVelocity * 0.1f
                    );

                GameManager.Instance.TempSceneRoot.AddChild(item);

                item.GiveRandomVelocity(new Vector3(
                    (float)GD.RandRange(-1.0, 1.0) * _itemExplosionVelocity,
                    _itemExplosionVelocity,
                    (float)GD.RandRange(-1.0, 1.0) * _itemExplosionVelocity
                ));
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

}