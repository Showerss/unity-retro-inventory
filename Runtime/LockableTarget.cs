using UnityEngine;
using UnityEngine.Events;

namespace Retro.Inventory
{
    /// <summary>
    /// Interactive lock that requires a specific item (Key, Keycard, Passcode card) to unlock.
    /// Implements IInventoryInteractable and seamlessly delegates to Doors, Gates, or transitions.
    /// </summary>
    [SelectionBase]
    public class LockableTarget : MonoBehaviour, IInventoryInteractable
    {
        [Header("Lock Settings")]
        [Tooltip("The ID of the ItemDefinition required to unlock this object (e.g. 'key_brass', 'keycard_admin').")]
        [SerializeField] private string _requiredKeyId = "key_brass";

        [Tooltip("Friendly name displayed in the interaction prompt and notifications.")]
        [SerializeField] private string _requiredKeyName = "Brass Key";

        [Tooltip("Should the key/keycard be consumed upon unlocking?")]
        [SerializeField] private bool _consumeKey = false;

        [Tooltip("Current lock state.")]
        [SerializeField] private bool _isLocked = true;

        [Header("Target to Trigger (Optional)")]
        [Tooltip("Door or other interactable to pass interaction to once unlocked. Inventory interactables are driven here; " +
                 "controller interactables (e.g. a first-person Door) are driven by that controller's adapter.")]
        [SerializeField] private Component _targetToTrigger;

        [Header("Audio")]
        [Tooltip("Source for lock sounds. Created on this object if empty.")]
        [SerializeField] private AudioSource _audioSource;

        [Tooltip("Played when the lock opens.")]
        [SerializeField] private AudioClip _unlockSound;

        [Tooltip("Played when the player tries the lock without the key.")]
        [SerializeField] private AudioClip _lockedRattleSound;

        [Header("Events")]
        [Tooltip("Raised once, when the player unlocks this with the key.")]
        [SerializeField] private UnityEvent _onUnlocked;

        private string _unlockPrompt;
        private string _lockedPrompt;

        public bool IsLocked => _isLocked;
        public string RequiredKeyId => _requiredKeyId;
        public string RequiredKeyName => _requiredKeyName;
        public UnityEvent OnUnlocked => _onUnlocked;
        public Component TargetToTrigger => _targetToTrigger;

        private void Awake()
        {
            EnsureAudioSource();
            EnsureRaycastCollider();

            _unlockPrompt = $"Unlock with {_requiredKeyName}";
            _lockedPrompt = $"Locked. Requires {_requiredKeyName}.";
        }

        private InventorySystem FindInventory(GameObject interactor)
        {
            if (interactor == null) return null;

            // The player may carry several inventories (throwables, keys, lights); prefer the one holding the key.
            InventorySystem[] inventories = interactor.GetComponentsInChildren<InventorySystem>();
            foreach (InventorySystem inv in inventories)
            {
                if (inv.HasItem(_requiredKeyId)) return inv;
            }

            return inventories.Length > 0 ? inventories[0] : null;
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

        private void EnsureRaycastCollider()
        {
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                col = GetComponentInChildren<Collider>();
            }

            if (col != null)
            {
                col.isTrigger = false;
            }
            else
            {
                BoxCollider box = gameObject.AddComponent<BoxCollider>();
                box.size = Vector3.one;
                box.isTrigger = false;
            }
        }

        // --- IInventoryInteractable Implementation ---

        public void Interact(GameObject player)
        {
            InventorySystem inv = FindInventory(player);

            if (_isLocked)
            {
                if (inv != null && inv.HasItem(_requiredKeyId))
                {
                    // Player has the key! Unlock!
                    _isLocked = false;

                    if (_consumeKey)
                    {
                        inv.RemoveItemById(_requiredKeyId, 1);
                    }

                    PlaySound(_unlockSound);
                    inv.Notify($"<color=#55FF55>Unlocked with {_requiredKeyName}!</color>");
                    _onUnlocked?.Invoke();

                    // Automatically trigger target door/switch if assigned
                    TriggerTarget(player);
                }
                else
                {
                    // Locked! Player doesn't have the key
                    PlaySound(_lockedRattleSound);
                    if (inv != null)
                    {
                        inv.Notify($"<color=#FF5555>Locked! Requires {_requiredKeyName}.</color>");
                    }
                }
            }
            else
            {
                // Already unlocked: pass interaction directly to target
                TriggerTarget(player);
            }
        }

        public string GetPromptText(GameObject interactor, out bool isAction)
        {
            if (_isLocked)
            {
                InventorySystem inv = FindInventory(interactor);
                isAction = inv != null && inv.HasItem(_requiredKeyId);
                return isAction ? _unlockPrompt : _lockedPrompt;
            }

            // Unlocked: show the target's prompt, or nothing if there's nothing left to do here.
            if (_targetToTrigger is IInventoryInteractable interactable)
            {
                return interactable.GetPromptText(interactor, out isAction);
            }

            isAction = false;
            return string.Empty;
        }

        private void TriggerTarget(GameObject player)
        {
            if (_targetToTrigger is IInventoryInteractable interactable)
            {
                interactable.Interact(player);
            }
        }

        private void PlaySound(AudioClip clip)
        {
            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
