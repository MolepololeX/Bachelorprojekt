using System;
using System.Collections.Generic;
using Game.Item;
using Godot;

namespace Game.InventoryStuff
{
    public class Inventory
    {
        public List<ItemType> Content = [ItemType.Wood, ItemType.Wood, ItemType.Wood];
        public int MaxSize = 5;
        public int SelectionIndex = 0;

        public Action ItemAdded;
        public Action ItemRemoved;

        public bool AddItem(ItemType type)
        {
            if (Content.Count >= MaxSize) return false;

            Content.Add(type);
            ItemAdded?.Invoke();
            return true;
        }

        public ItemType RequestCurrentSelectedItem()
        {
            if (Content.Count == 0) return ItemType.None;
            ItemType temp = Content[SelectionIndex];
            Content.RemoveAt(SelectionIndex);
            ItemRemoved?.Invoke();
            if (SelectionIndex >= Content.Count)
            {
                SelectionIndex--;
                if (SelectionIndex < 0) SelectionIndex = 0;
            }
            GD.Print(SelectionIndex);
            return temp;
        }
    }
}