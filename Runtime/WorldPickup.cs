using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// World pickup object implementing IInventoryInteractable for PlayerInteraction detection.
    /// Can represent keys, books, flashlights, lamps, keycards, etc. in 3D space.
    /// </summary>
    [SelectionBase]
    public class WorldPickup : MonoBehaviour, IInventoryInteractable
    {
        public static event System.Action<WorldPickup> OnPickupSpawned;

        [Header("Item Configuration")]
        [Tooltip("The ScriptableObject definition of the item picked up.")]
        [SerializeField] private ItemDefinition _itemDefinition;

        [Tooltip("Number of items added to inventory upon pickup.")]
        [SerializeField] private int _quantity = 1;

        [Tooltip("Custom prompt text. If left empty, defaults to '[E] Take [Item Name]'.")]
        [SerializeField] private string _customPrompt = string.Empty;

        [Header("Retro Bob & Spin Effect")]
        [Tooltip("Should the pickup float and gently spin in place?")]
        [SerializeField] private bool _enableBobAndSpin = true;

        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.08f;
        [SerializeField] private float _rotationSpeed = 45f;

        private Vector3 _startPosition;
        private float _randomOffset;

        public ItemDefinition Item => _itemDefinition;
        public int Quantity => _quantity;

        private void Awake()
        {
            _startPosition = transform.position;
            _randomOffset = Random.Range(0f, 10f);

            EnsureRaycastCollider();
            OnPickupSpawned?.Invoke(this);
        }

        private void Update()
        {
            if (_enableBobAndSpin)
            {
                // Gentle floating bob
                float newY = _startPosition.y + Mathf.Sin((Time.time + _randomOffset) * _bobSpeed) * _bobHeight;
                transform.position = new Vector3(transform.position.x, newY, transform.position.z);

                // Gentle spin
                transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);
            }
        }

        /// <summary>
        /// Ensures there is a non-trigger collider so interaction raycasts
        /// (which run with QueryTriggerInteraction.Ignore) can detect this object.
        /// </summary>
        private void EnsureRaycastCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                col = GetComponentInChildren<Collider>();
            }

            if (col == null)
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(0.5f, 0.5f, 0.5f);
                box.isTrigger = false;
            }
            else
            {
                col.isTrigger = false;
            }
        }

        public void SetItem(ItemDefinition item, int quantity = 1)
        {
            _itemDefinition = item;
            _quantity = Mathf.Max(1, quantity);
        }

        // --- IInventoryInteractable Implementation ---

        public void Interact(GameObject player)
        {
            if (_itemDefinition == null) return;

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            if (inventory == null)
            {
                inventory = player.GetComponentInChildren<InventorySystem>();
            }

            if (inventory != null)
            {
                bool added = inventory.AddItem(_itemDefinition, _quantity, true);
                if (added)
                {
                    Destroy(gameObject);
                }
            }
        }

        public string GetPromptText()
        {
            if (_itemDefinition == null) return string.Empty;

            if (!string.IsNullOrEmpty(_customPrompt))
            {
                return _customPrompt;
            }

            string countSuffix = _quantity > 1 ? $" x{_quantity}" : string.Empty;
            return $"[E] Take {_itemDefinition.DisplayName}{countSuffix}";
        }
    }
}
