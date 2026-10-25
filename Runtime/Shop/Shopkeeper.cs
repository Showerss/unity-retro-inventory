using System;
using System.Collections.Generic;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// A trader. Interacting opens the trade window (ShopUI) straight away.
    /// Owns this shop's live stock, copied from its ShopCatalog; items the player sells join the stock.
    /// Needs FpcPickupBridge (or another adapter) on the same object to be interactable.
    /// </summary>
    public class Shopkeeper : MonoBehaviour, IInventoryInteractable
    {
        public sealed class StockEntry
        {
            public ItemDefinition Item;
            public int Count;
        }

        [Tooltip("What this shop sells and its prices.")]
        [SerializeField] private ShopCatalog _catalog;

        [Tooltip("Prompt shown when looking at the shopkeeper, without the key.")]
        [SerializeField] private string _prompt = "Trade";

        [Tooltip("The trade window closes if the player walks further than this, in metres.")]
        [Min(1f)]
        [SerializeField] private float _tradeRange = 4f;

        [Header("Audio")]
        [Tooltip("Played on every buy or sell.")]
        [SerializeField] private AudioClip _tradeSound;

        private readonly List<StockEntry> _stock = new List<StockEntry>();

        /// <summary>Raised when a player asks to trade. ShopUI listens.</summary>
        public static event Action<Shopkeeper, GameObject> OnOpenRequested;

        /// <summary>Raised after any buy or sell.</summary>
        public event Action OnStockChanged;

        public string ShopName => _catalog != null ? _catalog.ShopName : name;
        public IReadOnlyList<StockEntry> Stock => _stock;
        public float TradeRange => _tradeRange;

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError($"[Shopkeeper] '{name}' has no ShopCatalog.", this);
                return;
            }

            foreach (ShopCatalog.Listing listing in _catalog.Listings)
            {
                if (listing.Item != null) _stock.Add(new StockEntry { Item = listing.Item, Count = listing.Stock });
            }
        }

        public int BuyPrice(ItemDefinition item) => _catalog.BuyPrice(item);
        public int SellPrice(ItemDefinition item) => _catalog.SellPrice(item);

        public bool CanBuyFromPlayer(ItemDefinition item) => item != null && !item.IsQuestItem;

        /// <summary>The player buys one of the stock entry at <paramref name="index"/>.</summary>
        public bool TryBuy(int index, GameObject customer, out string message)
        {
            message = null;
            if (index < 0 || index >= _stock.Count) return false;

            StockEntry entry = _stock[index];
            Wallet wallet = customer.GetComponentInChildren<Wallet>();
            int price = BuyPrice(entry.Item);

            if (wallet == null || !wallet.CanAfford(price))
            {
                message = "<color=#FF5555>Not enough to pay.</color>";
                return false;
            }

            InventorySystem inventory = FindAccepting(customer, entry.Item);
            if (inventory == null || !inventory.AddItem(entry.Item, 1, false))
            {
                message = "<color=#FF5555>No room to carry that.</color>";
                return false;
            }

            wallet.TrySpend(price);
            entry.Count--;
            if (entry.Count <= 0) _stock.RemoveAt(index);

            message = $"Bought {entry.Item.DisplayName} for {price} {wallet.CurrencyName}";
            Traded();
            return true;
        }

        /// <summary>The player sells one <paramref name="item"/> from their inventories.</summary>
        public bool TrySell(ItemDefinition item, GameObject customer, out string message)
        {
            message = null;
            if (!CanBuyFromPlayer(item))
            {
                message = "<color=#FF5555>I can't take that.</color>";
                return false;
            }

            Wallet wallet = customer.GetComponentInChildren<Wallet>();
            if (wallet == null) return false;

            InventorySystem[] inventories = customer.GetComponentsInChildren<InventorySystem>();
            bool removed = false;
            for (int i = 0; i < inventories.Length && !removed; i++)
            {
                removed = inventories[i].RemoveItemById(item.Id, 1);
            }
            if (!removed) return false;

            int price = SellPrice(item);
            wallet.Add(price);
            AddToStock(item);

            message = $"Sold {item.DisplayName} for {price} {wallet.CurrencyName}";
            Traded();
            return true;
        }

        // --- IInventoryInteractable ---

        public void Interact(GameObject interactor)
        {
            if (_catalog != null) OnOpenRequested?.Invoke(this, interactor);
        }

        public string GetPromptText(GameObject interactor, out bool isAction)
        {
            isAction = true;
            return _prompt;
        }

        private void AddToStock(ItemDefinition item)
        {
            for (int i = 0; i < _stock.Count; i++)
            {
                if (_stock[i].Item.Id == item.Id)
                {
                    _stock[i].Count++;
                    return;
                }
            }
            _stock.Add(new StockEntry { Item = item, Count = 1 });
        }

        private void Traded()
        {
            if (_tradeSound != null) AudioSource.PlayClipAtPoint(_tradeSound, transform.position);
            OnStockChanged?.Invoke();
        }

        private static InventorySystem FindAccepting(GameObject customer, ItemDefinition item)
        {
            foreach (InventorySystem inventory in customer.GetComponentsInChildren<InventorySystem>())
            {
                if (inventory.Accepts(item)) return inventory;
            }
            return null;
        }
    }
}
