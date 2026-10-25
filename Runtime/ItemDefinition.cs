using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// ScriptableObject defining an item in the game.
    /// New items (keys, books, flashlights, lamps, keycards, etc.) are created as asset instances of this class.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "Retro/Inventory/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique programmatic identifier (e.g. 'key_brass', 'flashlight', 'keycard_admin').")]
        [SerializeField] private string _id = "item_id";

        [Tooltip("Player-facing name displayed in UI and prompts.")]
        [SerializeField] private string _displayName = "New Item";

        [Tooltip("Category of the item.")]
        [SerializeField] private ItemCategory _category = ItemCategory.Misc;

        [TextArea(3, 6)]
        [Tooltip("Detailed lore or utility description displayed when inspecting the item.")]
        [SerializeField] private string _description = "A curious object.";

        [Tooltip("Optional 2D icon for the UI slot. If null, a retro textual abbreviation or icon is used.")]
        [SerializeField] private Sprite _icon;

        [Header("Stacking & Usage")]
        [Tooltip("Maximum quantity allowed per inventory slot.")]
        [SerializeField] private int _maxStack = 1;

        [Tooltip("Can the player use or trigger this item from inventory?")]
        [SerializeField] private bool _isUsable = true;

        [Tooltip("Is the item consumed (removed/decremented) when used?")]
        [SerializeField] private bool _isConsumable = false;

        [Tooltip("Modular action executed when the item is used (e.g. ToggleLightAction, ReadDocumentAction, UnlockTargetAction).")]
        [SerializeField] private ItemUseAction _useAction;

        [Header("Trade")]
        [Tooltip("Shop price in currency. Shops sell at this and buy back at a fraction of it (see ShopCatalog).")]
        [Min(0)]
        [SerializeField] private int _value = 1;

        [Tooltip("Needed for a quest: shops will not buy it.")]
        [SerializeField] private bool _isQuestItem = false;

        [Header("Prefabs & Visuals")]
        [Tooltip("Prefab spawned in the 3D world when the item is dropped or placed.")]
        [SerializeField] private GameObject _worldPrefab;

        [Tooltip("Optional 3D visual or tool displayed in the player's hand when selected.")]
        [SerializeField] private GameObject _heldPrefab;

        [Header("Audio")]
        [Tooltip("Sound played when this item is picked up.")]
        [SerializeField] private AudioClip _pickupSound;

        [Tooltip("Sound played when this item is used.")]
        [SerializeField] private AudioClip _useSound;

        // Public accessors
        public string Id => _id;
        public string DisplayName => _displayName;
        public ItemCategory Category => _category;
        public string Description => _description;
        public Sprite Icon => _icon;
        public int MaxStack => Mathf.Max(1, _maxStack);
        public bool IsUsable => _isUsable;
        public bool IsConsumable => _isConsumable;
        public ItemUseAction UseAction => _useAction;
        public int Value => _value;
        public bool IsQuestItem => _isQuestItem;
        public GameObject WorldPrefab => _worldPrefab;
        public GameObject HeldPrefab => _heldPrefab;
        public AudioClip PickupSound => _pickupSound;
        public AudioClip UseSound => _useSound;

        /// <summary>
        /// Attempts to use the item with the provided user GameObject.
        /// </summary>
        public bool TryUse(GameObject user, out string message)
        {
            if (!_isUsable)
            {
                message = "This item cannot be used directly.";
                return false;
            }

            if (_useAction != null)
            {
                bool success = _useAction.Execute(user, this);
                message = success ? _useAction.SuccessMessage : _useAction.GetFailureReason();
                return success;
            }

            // Default generic use if no special action is attached
            message = $"Used {DisplayName}.";
            return true;
        }
    }
}
