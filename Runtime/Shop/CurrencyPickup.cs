using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Money lying in the world. Adds to the player's Wallet when taken.
    /// Needs FpcPickupBridge (or another adapter) on the same object to be interactable.
    /// </summary>
    [SelectionBase]
    public class CurrencyPickup : MonoBehaviour, IInventoryInteractable
    {
        [Tooltip("Amount added to the wallet.")]
        [Min(1)]
        [SerializeField] private int _amount = 5;

        [Tooltip("Sound played at the pickup's position when taken.")]
        [SerializeField] private AudioClip _pickupSound;

        // Needs the currency name from the player's wallet, so it is built on first look and cached.
        private string _prompt;

        public string GetPromptText(GameObject interactor, out bool isAction)
        {
            isAction = true;
            if (_prompt == null)
            {
                Wallet wallet = interactor.GetComponentInChildren<Wallet>();
                _prompt = wallet != null ? $"Take {_amount} {wallet.CurrencyName}" : "Take";
            }
            return _prompt;
        }

        public void Interact(GameObject interactor)
        {
            Wallet wallet = interactor.GetComponentInChildren<Wallet>();
            if (wallet == null) return;

            wallet.Add(_amount);

            InventorySystem inventory = interactor.GetComponentInChildren<InventorySystem>();
            if (inventory != null) inventory.Notify($"<color=#55FF55>+ {_amount} {wallet.CurrencyName}</color>");

            if (_pickupSound != null) AudioSource.PlayClipAtPoint(_pickupSound, transform.position);
            Destroy(gameObject);
        }
    }
}
