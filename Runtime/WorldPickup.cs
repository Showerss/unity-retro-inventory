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
        [Header("Item Configuration")]
        [Tooltip("The ScriptableObject definition of the item picked up.")]
        [SerializeField] private ItemDefinition _itemDefinition;

        [Tooltip("Number of items added to inventory upon pickup.")]
        [SerializeField] private int _quantity = 1;

        [Tooltip("Custom prompt text, without the key. If left empty, defaults to 'Take [Item Name]'.")]
        [SerializeField] private string _customPrompt = string.Empty;

        [Header("Retro Bob & Spin Effect")]
        [Tooltip("Should the pickup float and gently spin in place?")]
        [SerializeField] private bool _enableBobAndSpin = true;

        [SerializeField] private float _bobSpeed = 2f;
        [SerializeField] private float _bobHeight = 0.08f;
        [SerializeField] private float _rotationSpeed = 45f;

        private Vector3 _startPosition;
        private float _randomOffset;
        private string _prompt = string.Empty;

        public ItemDefinition Item => _itemDefinition;
        public int Quantity => _quantity;

        private void Awake()
        {
            _startPosition = transform.position;
            _randomOffset = Random.Range(0f, 10f);

            EnsureRaycastCollider();
            RebuildPrompt();
        }

        private void RebuildPrompt()
        {
            if (_itemDefinition == null)
            {
                _prompt = string.Empty;
            }
            else if (!string.IsNullOrEmpty(_customPrompt))
            {
                _prompt = _customPrompt;
            }
            else
            {
                string countSuffix = _quantity > 1 ? $" x{_quantity}" : string.Empty;
                _prompt = $"Take {_itemDefinition.DisplayName}{countSuffix}";
            }
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
            RebuildPrompt();
        }

        // --- IInventoryInteractable Implementation ---

        public void Interact(GameObject player)
        {
            if (_itemDefinition == null) return;

            // The player may carry several inventories (weapons, keys, ...); use the first that takes this item.
            InventorySystem[] inventories = player.GetComponentsInChildren<InventorySystem>();
            for (int i = 0; i < inventories.Length; i++)
            {
                if (!inventories[i].Accepts(_itemDefinition)) continue;

                if (inventories[i].AddItem(_itemDefinition, _quantity, true))
                {
                    Destroy(gameObject);
                }
                return;
            }

            if (inventories.Length > 0)
            {
                inventories[0].Notify("You can't carry that yet.");
            }
        }

        public string GetPromptText(GameObject interactor, out bool isAction)
        {
            isAction = true;
            return _prompt;
        }
    }
}
