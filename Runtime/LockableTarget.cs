using UnityEngine;
using UnityEngine.Events;

namespace Retro.Inventory
{
    /// <summary>
    /// Interactive lock that requires a specific item (Key, Keycard, Passcode card) to unlock.
    /// Implements IInteractable and seamlessly delegates to Doors, Gates, or transitions.
    /// </summary>
    [SelectionBase]
    public class LockableTarget : MonoBehaviour, IInteractable
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
        [Tooltip("Target Door or component to interact with once unlocked.")]
        [SerializeField] private Component _targetToTrigger;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _unlockSound;
        [SerializeField] private AudioClip _lockedRattleSound;

        [Header("Events")]
        [SerializeField] private UnityEvent _onUnlocked;

        public bool IsLocked => _isLocked;
        public string RequiredKeyId => _requiredKeyId;
        public string RequiredKeyName => _requiredKeyName;
        public UnityEvent OnUnlocked => _onUnlocked;

        private void Awake()
        {
            EnsureAudioSource();
            EnsureRaycastCollider();
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

        // --- IInteractable Implementation ---

        public void Interact(GameObject player)
        {
            InventorySystem inv = player.GetComponent<InventorySystem>();
            if (inv == null)
            {
                inv = player.GetComponentInChildren<InventorySystem>();
            }

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

        public string GetPromptText()
        {
            if (_isLocked)
            {
                // Check if the player currently looking has the key
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    InventorySystem inv = player.GetComponent<InventorySystem>();
                    if (inv != null && inv.HasItem(_requiredKeyId))
                    {
                        return $"[E] Unlock with {_requiredKeyName}";
                    }
                }

                return $"[Locked] Requires {_requiredKeyName}";
            }

            // If already unlocked, show target's prompt if it is IInteractable
            if (_targetToTrigger is IInteractable interactable)
            {
                return interactable.GetPromptText();
            }

            return "Unlocked";
        }

        private void TriggerTarget(GameObject player)
        {
            if (_targetToTrigger is IInteractable interactable)
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
