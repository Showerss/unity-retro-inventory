using FirstPerson.Interaction;
using UnityEngine;

namespace Retro.Inventory.Adapters
{
    /// <summary>
    /// Lets the first-person controller's PlayerInteraction reach an inventory object.
    /// Add it next to the WorldPickup or LockableTarget on the prefab (including item WorldPrefabs, so dropped items can be picked up again).
    /// When a LockableTarget is unlocked and its target is a controller interactable (e.g. a Door), interaction passes through to it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FpcPickupBridge : MonoBehaviour, IInteractable
    {
        private IInventoryInteractable _target;
        private LockableTarget _lock;

        private void Awake()
        {
            _target = GetComponent<IInventoryInteractable>();
            _lock = _target as LockableTarget;

            if (_target == null)
            {
                Debug.LogError($"[FpcPickupBridge] '{name}' has no WorldPickup, LockableTarget or other IInventoryInteractable.", this);
            }
        }

        public InteractionPrompt GetPrompt(PlayerInteraction interactor)
        {
            if (_target == null) return InteractionPrompt.None;
            if (TryGetUnlockedTarget(out IInteractable passThrough)) return passThrough.GetPrompt(interactor);

            string text = _target.GetPromptText(interactor.gameObject, out bool isAction);
            return new InteractionPrompt(text, isAction);
        }

        public void Interact(PlayerInteraction interactor)
        {
            if (_target == null) return;

            bool wasLocked = _lock != null && _lock.IsLocked;
            if (!wasLocked && TryGetUnlockedTarget(out IInteractable passThrough))
            {
                passThrough.Interact(interactor);
                return;
            }

            _target.Interact(interactor.gameObject);

            // Unlocking with the key opens the door in the same press.
            if (wasLocked && TryGetUnlockedTarget(out passThrough))
            {
                passThrough.Interact(interactor);
            }
        }

        private bool TryGetUnlockedTarget(out IInteractable target)
        {
            target = _lock != null && !_lock.IsLocked ? _lock.TargetToTrigger as IInteractable : null;
            return target != null;
        }
    }
}
