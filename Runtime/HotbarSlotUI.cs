using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Retro.Inventory
{
    /// <summary>
    /// UI representation of a single hotbar/inventory slot.
    /// Displays hotkey number, item icon/name, count, and selection frame.
    /// </summary>
    public class HotbarSlotUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Image _selectionBorder;
        [SerializeField] private Image _iconImage;
        [SerializeField] private TextMeshProUGUI _hotkeyLabel;
        [SerializeField] private TextMeshProUGUI _nameLabel;
        [SerializeField] private TextMeshProUGUI _quantityLabel;
        [SerializeField] private Button _button;

        [Header("Styling")]
        [SerializeField] private Color _normalBorderColor = new Color(0.2f, 0.4f, 0.5f, 0.5f);
        [SerializeField] private Color _selectedBorderColor = new Color(0.0f, 1.0f, 0.85f, 1.0f); // Bright retro cyan

        private int _slotIndex = -1;
        private InventorySystem _inventory;

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
            }
        }

        public void Initialize(int slotIndex, InventorySystem inventory)
        {
            _slotIndex = slotIndex;
            _inventory = inventory;

            if (_hotkeyLabel != null)
            {
                _hotkeyLabel.text = $"[{slotIndex + 1}]";
            }
        }

        public void Bind(int slotIndex, ItemStack stack)
        {
            _slotIndex = slotIndex;

            if (_hotkeyLabel != null)
            {
                _hotkeyLabel.text = $"[{slotIndex + 1}]";
            }

            if (stack == null || stack.IsEmpty || stack.Item == null)
            {
                Clear();
                return;
            }

            ItemDefinition item = stack.Item;

            // Icon
            if (_iconImage != null)
            {
                if (item.Icon != null)
                {
                    _iconImage.sprite = item.Icon;
                    _iconImage.enabled = true;
                }
                else
                {
                    _iconImage.enabled = false;
                }
            }

            // Name label
            if (_nameLabel != null)
            {
                _nameLabel.text = item.DisplayName;
                _nameLabel.enabled = true;
            }

            // Quantity label
            if (_quantityLabel != null)
            {
                if (stack.Quantity > 1)
                {
                    _quantityLabel.text = $"x{stack.Quantity}";
                    _quantityLabel.enabled = true;
                }
                else
                {
                    _quantityLabel.enabled = false;
                }
            }
        }

        public void Clear()
        {
            if (_iconImage != null) _iconImage.enabled = false;
            if (_nameLabel != null) _nameLabel.text = "- Empty -";
            if (_quantityLabel != null) _quantityLabel.enabled = false;
        }

        public void SetSelected(bool isSelected)
        {
            if (_selectionBorder != null)
            {
                _selectionBorder.color = isSelected ? _selectedBorderColor : _normalBorderColor;
                _selectionBorder.enabled = isSelected;
            }

            if (_backgroundImage != null)
            {
                _backgroundImage.color = isSelected
                    ? new Color(0.05f, 0.2f, 0.25f, 0.85f)
                    : new Color(0.05f, 0.08f, 0.12f, 0.75f);
            }
        }

        private void OnClick()
        {
            if (_inventory == null)
            {
                _inventory = Object.FindAnyObjectByType<InventorySystem>();
            }

            if (_inventory != null && _slotIndex >= 0)
            {
                if (_inventory.SelectedSlotIndex == _slotIndex)
                {
                    // Double click/tapping active slot uses it
                    _inventory.UseSlot(_slotIndex);
                }
                else
                {
                    _inventory.SelectSlot(_slotIndex);
                }
            }
        }

        public void SetReferences(Image bg, Image border, Image icon, TextMeshProUGUI hotkey, TextMeshProUGUI nameText, TextMeshProUGUI qty, Button btn)
        {
            _backgroundImage = bg;
            _selectionBorder = border;
            _iconImage = icon;
            _hotkeyLabel = hotkey;
            _nameLabel = nameText;
            _quantityLabel = qty;
            _button = btn;
        }
    }
}
