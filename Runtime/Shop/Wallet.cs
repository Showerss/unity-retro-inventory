using System;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// The player's money. Put it on the player next to the InventorySystem(s).
    /// </summary>
    [DisallowMultipleComponent]
    public class Wallet : MonoBehaviour
    {
        [Tooltip("Player-facing name of the currency (e.g. 'Leaves', 'Gold').")]
        [SerializeField] private string _currencyName = "Leaves";

        [Tooltip("Amount the player starts with.")]
        [Min(0)]
        [SerializeField] private int _startingAmount = 0;

        private int _amount;

        /// <summary>Raised with the new amount whenever it changes.</summary>
        public event Action<int> OnChanged;

        public string CurrencyName => _currencyName;
        public int Amount => _amount;

        private void Awake()
        {
            _amount = _startingAmount;
        }

        public bool CanAfford(int cost) => cost <= _amount;

        public void Add(int amount)
        {
            if (amount <= 0) return;
            Set(_amount + amount);
        }

        public bool TrySpend(int cost)
        {
            if (cost < 0 || !CanAfford(cost)) return false;
            Set(_amount - cost);
            return true;
        }

        /// <summary>Overwrites the amount (save restore).</summary>
        public void Set(int amount)
        {
            _amount = Mathf.Max(0, amount);
            OnChanged?.Invoke(_amount);
        }
    }
}
