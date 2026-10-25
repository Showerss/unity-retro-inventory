using TMPro;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Shows the wallet's amount on a HUD text, e.g. "Leaves: 12".
    /// </summary>
    public class WalletLabel : MonoBehaviour
    {
        [Tooltip("Wallet to show.")]
        [SerializeField] private Wallet _wallet;

        [Tooltip("Text to write the amount into.")]
        [SerializeField] private TMP_Text _label;

        private void OnEnable()
        {
            if (_wallet == null || _label == null) return;
            _wallet.OnChanged += Show;
        }

        // Not in OnEnable: the wallet sets its starting amount in its own Awake, which may run after ours.
        private void Start()
        {
            if (_wallet != null && _label != null) Show(_wallet.Amount);
        }

        private void OnDisable()
        {
            if (_wallet != null) _wallet.OnChanged -= Show;
        }

        private void Show(int amount) => _label.text = $"{_wallet.CurrencyName}: {amount}";
    }
}
