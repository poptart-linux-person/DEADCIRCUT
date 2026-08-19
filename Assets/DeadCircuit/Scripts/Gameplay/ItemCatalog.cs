using System;
using UnityEngine;

namespace DeadCircuit.Gameplay
{
    public enum ExtendedItemType
    {
        Medkit, Bandage, ArmorPlate, AdrenalineShot, Battery, Flashlight,
        Flare, NoiseMaker, Radio, Fuse, Lockpick, DoorWedge, RepairTool,
        ThrowableTool, HeavyCrate, LightCrate, Bottle, FireExtinguisher,
        ShockDevice, Keycard, Map, Food, Water, Syringe, XCore
    }

    [Serializable]
    public struct ItemDefinition
    {
        public ExtendedItemType type;
        public float weight;
        public float noise;
        public float value;
        public bool throwable;
        public bool consumable;
        public bool questItem;
    }

    [CreateAssetMenu(menuName = "Dead Circuit/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        public ItemDefinition[] items;

        public ItemDefinition Get(ExtendedItemType type)
        {
            if (items != null)
                foreach (var item in items)
                    if (item.type == type) return item;
            return new ItemDefinition { type = type };
        }
    }
}
