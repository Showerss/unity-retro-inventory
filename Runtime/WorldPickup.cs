using System.Collections;
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

        [Header("Collect Animation")]
        [Tooltip("Seconds the item flies to the player's hand before it disappears. 0 removes it instantly.")]
        [SerializeField] private float _collectDuration = 0.28f;

        [Tooltip("Where the item flies to, relative to the player's camera (right, up, forward) in metres.")]
        [SerializeField] private Vector3 _collectTarget = new Vector3(0.18f, -0.28f, 0.45f);

        [Tooltip("Height of the arc above the straight line, in metres.")]
        [SerializeField] private float _collectArcHeight = 0.15f;

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
                    Collect(player);
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

        /// <summary>Already in the inventory: fly to the player's hand, shrinking, then remove.</summary>
        private void Collect(GameObject player)
        {
            Camera cam = player.GetComponentInChildren<Camera>();
            if (_collectDuration <= 0f || cam == null)
            {
                Destroy(gameObject);
                return;
            }

            // No second pickup, no marker, no physics while it flies.
            foreach (Collider col in GetComponentsInChildren<Collider>()) col.enabled = false;
            if (TryGetComponent(out Rigidbody body)) body.isKinematic = true;
            _enableBobAndSpin = false;

            StartCoroutine(FlyToHand(cam.transform));
        }

        private IEnumerator FlyToHand(Transform cam)
        {
            Vector3 from = transform.position;
            Vector3 startScale = transform.localScale;

            for (float t = 0f; t < 1f; t += Time.deltaTime / _collectDuration)
            {
                Vector3 to = cam.TransformPoint(_collectTarget); // follows the camera if the player moves
                float eased = t * t * (3f - 2f * t);
                transform.position = Vector3.Lerp(from, to, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * _collectArcHeight);
                transform.localScale = startScale * (1f - eased * eased);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
