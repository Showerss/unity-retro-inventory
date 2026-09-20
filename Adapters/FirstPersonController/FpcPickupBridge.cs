using UnityEngine;

namespace Retro.Inventory.Adapters
{
    /// <summary>
    /// Connects IInventoryInteractable (WorldPickup, LockableTarget)
    /// to FirstPersonController global IInteractable raycast system.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class FpcPickupBridge : MonoBehaviour, global::IInteractable
    {
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
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (s, m) => HookAll();
            WorldPickup.OnPickupSpawned += p => { if (p != null) Hook(p.gameObject); };
            LockableTarget.OnLockableSpawned += l => { if (l != null) Hook(l.gameObject); };
        }

        private static void HookAll()
        {
            foreach (var p in Object.FindObjectsByType<WorldPickup>(FindObjectsSortMode.None))
            {
                if (p != null) Hook(p.gameObject);
            }

            foreach (var l in Object.FindObjectsByType<LockableTarget>(FindObjectsSortMode.None))
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
