using UnityEngine;
using UnityEngine.InputSystem;

namespace Retro.Inventory
{
    /// <summary>
    /// Left-click uses (throws) the readied item. Lives on the held prefab, so it only listens while the item is in hand.
    /// The item's ThrowItemAction does the actual launch.
    /// </summary>
    public sealed class HeldThrowInput : MonoBehaviour
    {
        [Tooltip("Minimum seconds between throws.")]
        [SerializeField, Min(0f)] private float _cooldown = 0.4f;

        [Tooltip("Local position offset at the peak of the throw motion.")]
        [SerializeField] private Vector3 _throwOffset = new Vector3(0f, 0.05f, 0.15f);

        [Tooltip("Seconds for the hand to push forward and return.")]
        [SerializeField, Range(0.05f, 0.5f)] private float _motionDuration = 0.2f;

        private InventorySystem _inventory;
        private Vector3 _restPosition;
        private float _nextAllowed;
        private float _motionElapsed = -1f;

        // Start, not Awake: HeldItemManager positions the model right after Instantiate.
        private void Start()
        {
            _restPosition = transform.localPosition;
            HeldItemManager manager = GetComponentInParent<HeldItemManager>();
            _inventory = manager != null ? manager.Inventory : GetComponentInParent<InventorySystem>();
        }

        private void OnDisable()
        {
            if (_motionElapsed < 0f) return;
            _motionElapsed = -1f;
            transform.localPosition = _restPosition;
        }

        private void Update()
        {
            AnimateMotion();

            // Ignore clicks while a menu has freed the cursor.
            var mouse = Mouse.current;
            if (_inventory == null || mouse == null || !mouse.leftButton.wasPressedThisFrame || Cursor.lockState != CursorLockMode.Locked) return;
            if (Time.time < _nextAllowed) return;

            _nextAllowed = Time.time + _cooldown;
            _motionElapsed = 0f;
            _inventory.UseSelectedItem(); // may destroy this held model if the stack empties
        }

        private void AnimateMotion()
        {
            if (_motionElapsed < 0f) return;
            _motionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_motionElapsed / _motionDuration);
            transform.localPosition = _restPosition + _throwOffset * Mathf.Sin(t * Mathf.PI);
            if (t >= 1f) _motionElapsed = -1f;
        }
    }
}
