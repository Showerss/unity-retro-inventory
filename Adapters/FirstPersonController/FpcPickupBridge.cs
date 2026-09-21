using UnityEngine;

namespace Retro.Inventory.Adapters
{
    /// <summary>
    /// Connects IInventoryInteractable (WorldPickup, LockableTarget)
    /// to FirstPersonController global IInteractable raycast system,
    /// and bridges FirstPersonController PlayerInteraction prompts to InventoryHUD.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class FpcPickupBridge : MonoBehaviour, global::IInteractable
    {
        private static PlayerInteraction _cachedPlayerInteraction;
        private IInventoryInteractable _target;

        private void Awake()
        {
            _target = GetComponent<IInventoryInteractable>();
        }

        public void Interact(GameObject player)
        {
            if (_target == null) _target = GetComponent<IInventoryInteractable>();
            _target?.Interact(player);
        }

        public string GetPromptText()
        {
            if (_target == null) _target = GetComponent<IInventoryInteractable>();
            return _target?.GetPromptText() ?? string.Empty;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoHookSceneObjects()
        {
            HookAll();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) =>
            {
                _cachedPlayerInteraction = null;
                HookAll();
            };
            WorldPickup.OnPickupSpawned += p => { if (p != null) Hook(p.gameObject); };
            LockableTarget.OnLockableSpawned += l => { if (l != null) Hook(l.gameObject); };

            // Bridge PlayerInteraction prompt into InventoryHUD without coupling core assembly
            InventoryHUD.PromptProvider = GetFpcPrompt;
        }

        private static string GetFpcPrompt()
        {
            if (_cachedPlayerInteraction == null)
            {
                _cachedPlayerInteraction = Object.FindAnyObjectByType<PlayerInteraction>();
            }
            return _cachedPlayerInteraction != null ? _cachedPlayerInteraction.CurrentPrompt : string.Empty;
        }

        private static void HookAll()
        {
            foreach (var p in Object.FindObjectsByType<WorldPickup>())
            {
                if (p != null) Hook(p.gameObject);
            }

            foreach (var l in Object.FindObjectsByType<LockableTarget>())
            {
                if (l != null) Hook(l.gameObject);
            }
        }

        private static void Hook(GameObject go)
        {
            if (go != null && go.GetComponent<FpcPickupBridge>() == null)
            {
                go.AddComponent<FpcPickupBridge>();
            }
        }
    }
}
