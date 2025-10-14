using Game.Item;
using Godot;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;

namespace Game.Cauldron
{
    public partial class Recipes : Node
    {
        private static List<(ItemType, List<ItemType>)> Recipe =
        [
            new( ItemType.QuicksilverII,
                [
                    ItemType.Copper,
                    ItemType.Copper,
                ]),
            new( ItemType.Copper,
                [
                    ItemType.Plank,
                    ItemType.Wood,
                ]),
            new( ItemType.Wood,
                [
                    ItemType.Plank,
                ]),
            new( ItemType.Plank,
                [
                    ItemType.Wood,
                ]),
        ];

        //scuffed, 0 - result, 1 - list of used items
        public static (ItemType, List<ItemType>) SolveRecipe(List<ItemType> contents)
        {

            foreach ((ItemType, List<ItemType>) t in Recipe)
            {
                List<ItemType> recipe = t.Item2;

                if (contents.Count != recipe.Count)
                    continue;

                recipe.Sort();

                bool recipeMatches = true;
                for (int i = 0; i < recipe.Count; i++)
                {
                    if (recipe[i] != contents[i])
                    {
                        recipeMatches = false;
                        break;
                    }
                }
                if (recipeMatches) return t;
            }

            (ItemType, List<ItemType>) unsuccessfulRecipe = new(ItemType.None, new List<ItemType>());
            return unsuccessfulRecipe;
        }

        public static (ItemType, List<ItemType>) SolveRecipe2(List<ItemType> contents)
        {
            (ItemType, List<ItemType>) longestMatch = new(ItemType.None, new List<ItemType>());

            foreach ((ItemType, List<ItemType>) t in Recipe)
            {
                List<ItemType> tempContents = [.. contents];
                List<ItemType> recipe = t.Item2;

                bool recipeMatches = true;
                for (int i = 0; i < recipe.Count; i++)
                {
                    ItemType foundtItemType = tempContents.Find(n => n == recipe[i]);
                    if (foundtItemType == recipe[i])
                    {
                        tempContents.Remove(foundtItemType);
                    }
                    else
                    {
                        recipeMatches = false;
                        break;
                    }
                }
                if (recipeMatches && longestMatch.Item2.Count < t.Item2.Count)
                {
                    longestMatch = t;
                }
            }

            return longestMatch;
        }
    }
}
