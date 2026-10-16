using System;
using System.Collections.Generic;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Core inventory component attached to the Player GameObject.
    /// Manages slots, item stacking, selection, use, and dropping.
    /// A player may carry several, each filtered to different categories (e.g. a weapon hotbar and a key ring).
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Total number of inventory slots. Applied on Awake; changing at runtime has no effect.")]
        [Range(1, 24)]
        [SerializeField] private int _capacity = 8;

        [Tooltip("Currently initialized slots.")]
        [SerializeField] private List<ItemStack> _slots = new List<ItemStack>();

        [Tooltip("Item categories this inventory holds. Leave empty to accept every item.")]
        [SerializeField] private List<ItemCategory> _acceptedCategories = new List<ItemCategory>();

        [Header("Selection")]
        [Tooltip("Start with nothing in hand instead of selecting slot 1.")]
        [SerializeField] private bool _startEmptyHanded = false;

        [Header("Pickup Behaviour")]
        [Tooltip("Automatically equip a newly picked-up item if it landed in an empty slot.")]
        [SerializeField] private bool _autoSelectNewItems = false;

        [Header("Dropping")]
        [Tooltip("Allow the player to drop items back into the world.")]
        [SerializeField] private bool _allowDropping = true;

        [Tooltip("Distance in front of the player that dropped items spawn.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _dropDistance = 1.2f;

        [Tooltip("Height above the player's feet that dropped items spawn.")]
        [Range(0f, 2f)]
        [SerializeField] private float _dropHeight = 0.5f;

        [Tooltip("Speed dropped items are tossed forward. 0 makes them drop straight down.")]
        [Range(0f, 15f)]
        [SerializeField] private float _dropForce = 4f;

        [Tooltip("How much upward arc is added to the toss.")]
        [Range(0f, 1f)]
        [SerializeField] private float _dropUpwardArc = 0.4f;

        [Header("Starting Items (Optional)")]
        [Tooltip("Optional items granted to the player on Awake.")]
        [SerializeField] private List<ItemDefinition> _startingItems = new List<ItemDefinition>();

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _defaultPickupSound;
        [SerializeField] private AudioClip _defaultUseSound;
        [SerializeField] private AudioClip _inventoryFullSound;

        // Current equipped / selected slot index (-1 for none)
        private int _selectedSlotIndex = 0;

        // Events for UI and external systems
        public event Action OnInventoryChanged;
        public event Action<int, ItemStack> OnSlotChanged;
        public event Action<int, ItemStack> OnSlotSelected;
        public event Action<string> OnNotification;
        public event Action<ItemDefinition, bool> OnItemUsed;

        public int Capacity => _capacity;
        public IReadOnlyList<ItemStack> Slots => _slots;
        public int SelectedSlotIndex => _selectedSlotIndex;

        public ItemStack SelectedStack
        {
            get
            {
                if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slots.Count)
                {
                    return _slots[_selectedSlotIndex];
                }
                return null;
            }
        }

        private void Awake()
        {
            EnsureAudioSource();
            InitializeSlots();
            GrantStartingItems();
        }

        private void Start()
        {
            // Initial selection notification
            if (_startEmptyHanded) Holster();
            else SelectSlot(_selectedSlotIndex);
        }

        /// <summary>
        /// True if this inventory holds items of the given item's category.
        /// </summary>
        public bool Accepts(ItemDefinition item)
        {
            if (item == null) return false;
            return _acceptedCategories == null || _acceptedCategories.Count == 0 || _acceptedCategories.Contains(item.Category);
        }

        private void EnsureAudioSource()
        {
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
                if (_audioSource == null)
                {
                    _audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            _audioSource.playOnAwake = false;
        }

        private void InitializeSlots()
        {
            if (_slots == null) _slots = new List<ItemStack>();

            while (_slots.Count < _capacity)
            {
                _slots.Add(new ItemStack());
            }

            if (_slots.Count > _capacity)
            {
                _slots.RemoveRange(_capacity, _slots.Count - _capacity);
            }
        }

        private void GrantStartingItems()
        {
            if (_startingItems == null) return;
            foreach (var item in _startingItems)
            {
                if (item != null)
                {
                    AddItem(item, 1, false);
                }
            }
        }

        /// <summary>
        /// Adds a quantity of the given item to the inventory.
        /// Stacks with existing items if allowed, or fills the first empty slot.
        /// </summary>
        public bool AddItem(ItemDefinition item, int quantity = 1, bool playFeedback = true)
        {
            if (item == null || quantity <= 0) return false;
            if (!Accepts(item)) return false;

            int remaining = quantity;
            int firstNewSlot = -1;

            // 1. Try stacking into existing non-full slots
            if (item.MaxStack > 1)
            {
                for (int i = 0; i < _slots.Count && remaining > 0; i++)
                {
                    var slot = _slots[i];
                    if (!slot.IsEmpty && slot.Item.Id == item.Id && slot.Quantity < item.MaxStack)
                    {
                        int space = item.MaxStack - slot.Quantity;
                        int addAmount = Mathf.Min(space, remaining);
                        slot.Quantity += addAmount;
                        remaining -= addAmount;
                        OnSlotChanged?.Invoke(i, slot);
                    }
                }
            }

            // 2. Place remaining into empty slots
            while (remaining > 0)
            {
                int emptyIndex = GetFirstEmptySlotIndex();
                if (emptyIndex == -1)
                {
                    // No space left!
                    if (playFeedback)
                    {
                        PlaySound(_inventoryFullSound);
                        Notify("<color=#FF5555>Inventory Full!</color>");
                    }
                    OnInventoryChanged?.Invoke();
                    return false;
                }

                int putAmount = Mathf.Min(remaining, item.MaxStack);
                _slots[emptyIndex].Item = item;
                _slots[emptyIndex].Quantity = putAmount;
                remaining -= putAmount;
                if (firstNewSlot == -1) firstNewSlot = emptyIndex;
                OnSlotChanged?.Invoke(emptyIndex, _slots[emptyIndex]);
            }

            if (playFeedback)
            {
                AudioClip clip = item.PickupSound != null ? item.PickupSound : _defaultPickupSound;
                PlaySound(clip);
                Notify($"<color=#55FFFF>+ {item.DisplayName}</color>");
            }

            OnInventoryChanged?.Invoke();

            // Equip the new item if requested (real pickups only, not starting inventory)
            if (_autoSelectNewItems && playFeedback && firstNewSlot >= 0)
            {
                SelectSlot(firstNewSlot);
            }
            // Otherwise just refresh the selected slot's visual if it was modified
            else if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slots.Count)
            {
                OnSlotSelected?.Invoke(_selectedSlotIndex, _slots[_selectedSlotIndex]);
            }

            return true;
        }

        /// <summary>
        /// Checks if the inventory contains at least the required quantity of an item by ID.
        /// </summary>
        public bool HasItem(string itemId, int requiredCount = 1)
        {
            if (string.IsNullOrEmpty(itemId)) return false;
            return GetItemCount(itemId) >= requiredCount;
        }

        /// <summary>
        /// Gets the total quantity of an item across all slots.
        /// </summary>
        public int GetItemCount(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsEmpty && _slots[i].Item.Id == itemId)
                {
                    total += _slots[i].Quantity;
                }
            }
            return total;
        }

        /// <summary>
        /// Removes a quantity of an item by ID.
        /// </summary>
        public bool RemoveItemById(string itemId, int quantity = 1)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            if (!HasItem(itemId, quantity)) return false;

            int remaining = quantity;
            for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var slot = _slots[i];
                if (!slot.IsEmpty && slot.Item.Id == itemId)
                {
                    int take = Mathf.Min(remaining, slot.Quantity);
                    slot.Quantity -= take;
                    remaining -= take;

                    if (slot.Quantity <= 0)
                    {
                        slot.Clear();
                    }
                    OnSlotChanged?.Invoke(i, slot);
                }
            }

            OnInventoryChanged?.Invoke();
            if (_selectedSlotIndex >= 0 && _selectedSlotIndex < _slots.Count)
            {
                OnSlotSelected?.Invoke(_selectedSlotIndex, _slots[_selectedSlotIndex]);
            }
            return true;
        }

        /// <summary>
        /// Selects an active hotbar / inventory slot.
        /// </summary>
        public void SelectSlot(int index)
        {
            if (index < 0 || index >= _capacity) return;

            _selectedSlotIndex = index;
            ItemStack stack = _slots[_selectedSlotIndex];
            OnSlotSelected?.Invoke(_selectedSlotIndex, stack);
        }

        /// <summary>
        /// Puts the held item away, leaving empty hands.
        /// </summary>
        public void Holster()
        {
            _selectedSlotIndex = -1;
            OnSlotSelected?.Invoke(-1, null);
        }

        /// <summary>
        /// Selects the slot, or holsters it if it is already selected.
        /// </summary>
        public void ToggleSlot(int index)
        {
            if (index == _selectedSlotIndex) Holster();
            else SelectSlot(index);
        }

        /// <summary>
        /// Cycles selected slot forwards or backwards.
        /// </summary>
        public void CycleSlot(int direction)
        {
            if (_capacity <= 0) return;
            if (_selectedSlotIndex < 0)
            {
                SelectSlot(direction > 0 ? 0 : _capacity - 1);
                return;
            }
            int next = (_selectedSlotIndex + direction) % _capacity;
            if (next < 0) next += _capacity;
            SelectSlot(next);
        }

        /// <summary>
        /// Uses the item in the currently selected slot.
        /// </summary>
        public bool UseSelectedItem()
        {
            return UseSlot(_selectedSlotIndex);
        }

        /// <summary>
        /// Uses the item in the specified slot index.
        /// </summary>
        public bool UseSlot(int index)
        {
            if (index < 0 || index >= _slots.Count) return false;

            ItemStack stack = _slots[index];
            if (stack.IsEmpty) return false;

            ItemDefinition item = stack.Item;
            bool success = item.TryUse(gameObject, out string message);

            if (!string.IsNullOrEmpty(message))
            {
                Notify(message);
            }

            if (success)
            {
                AudioClip clip = item.UseSound != null ? item.UseSound : _defaultUseSound;
                PlaySound(clip);

                if (item.IsConsumable)
                {
                    stack.Quantity--;
                    if (stack.Quantity <= 0)
                    {
                        stack.Clear();
                    }
                    OnSlotChanged?.Invoke(index, stack);
                    OnInventoryChanged?.Invoke();
                }

                OnItemUsed?.Invoke(item, true);
                return true;
            }

            OnItemUsed?.Invoke(item, false);
            return false;
        }

        /// <summary>
        /// Drops the item from the specified slot into the 3D world in front of the player.
        /// </summary>
        public GameObject DropSlot(int index)
        {
            if (!_allowDropping) return null;
            if (index < 0 || index >= _slots.Count) return null;

            ItemStack stack = _slots[index];
            if (stack.IsEmpty) return null;

            ItemDefinition item = stack.Item;
            GameObject worldObj = null;

            if (item.WorldPrefab != null)
            {
                Vector3 spawnPos = transform.position + transform.forward * _dropDistance + Vector3.up * _dropHeight;
                worldObj = Instantiate(item.WorldPrefab, spawnPos, transform.rotation);

                // Configure WorldPickup if present
                WorldPickup pickup = worldObj.GetComponent<WorldPickup>();
                if (pickup != null)
                {
                    pickup.SetItem(item, 1);
                }

                // Add physics toss if Rigidbody exists
                Rigidbody rb = worldObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                    rb.linearVelocity = (transform.forward + Vector3.up * _dropUpwardArc).normalized * _dropForce;
                }
            }

            stack.Quantity--;
            if (stack.Quantity <= 0)
            {
                stack.Clear();
            }

            Notify($"Dropped {item.DisplayName}");
            OnSlotChanged?.Invoke(index, stack);
            OnInventoryChanged?.Invoke();
            if (index == _selectedSlotIndex && stack.IsEmpty) Holster();
            else OnSlotSelected?.Invoke(_selectedSlotIndex, SelectedStack);

            return worldObj;
        }

        public void Notify(string message)
        {
            if (!string.IsNullOrEmpty(message))
            {
                OnNotification?.Invoke(message);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }

        private int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty) return i;
            }
            return -1;
        }
    }
}
