using System;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Represents an item and its current stacked quantity in a slot.
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField] private int _quantity;

        public ItemDefinition Item
        {
            get => _item;
            set => _item = value;
        }

        public int Quantity
        {
            get => _quantity;
            set => _quantity = Mathf.Max(0, value);
        }

        public bool IsEmpty => _item == null || _quantity <= 0;

        public ItemStack()
        {
            _item = null;
            _quantity = 0;
        }

        public ItemStack(ItemDefinition item, int quantity = 1)
        {
            _item = item;
            _quantity = Mathf.Max(0, quantity);
        }

        public void Clear()
        {
            _item = null;
            _quantity = 0;
        }
    }
}
