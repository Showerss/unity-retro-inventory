using System;
using System.Collections.Generic;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// What a shop sells and how it prices things. One per shopkeeper type; the asset is never changed at runtime.
    /// </summary>
    [CreateAssetMenu(fileName = "SO_Shop_New", menuName = "Retro/Inventory/Shop Catalog")]
    public class ShopCatalog : ScriptableObject
    {
        [Serializable]
        public struct Listing
        {
            [Tooltip("Item for sale.")]
            public ItemDefinition Item;

            [Tooltip("How many the shop starts with.")]
            [Min(1)]
            public int Stock;
        }

        [Tooltip("Shop name shown at the top of the trade window.")]
        [SerializeField] private string _shopName = "Shop";

        [Tooltip("Starting stock.")]
        [SerializeField] private List<Listing> _listings = new List<Listing>();

        [Tooltip("Multiplier on an item's Value when the player buys.")]
        [Min(0f)]
        [SerializeField] private float _buyMultiplier = 1f;

        [Tooltip("Multiplier on an item's Value when the player sells. Never below 1 currency.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sellMultiplier = 0.5f;

        public string ShopName => _shopName;
        public IReadOnlyList<Listing> Listings => _listings;

        public int BuyPrice(ItemDefinition item) => Mathf.Max(1, Mathf.RoundToInt(item.Value * _buyMultiplier));
        public int SellPrice(ItemDefinition item) => Mathf.Max(1, Mathf.FloorToInt(item.Value * _sellMultiplier));
    }
}
