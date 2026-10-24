using UnityEngine;
using UnityEngine.InputSystem;

namespace Retro.Inventory
{
    /// <summary>
    /// Turns the carried light source (flashlight, lantern) on and off with one key,
    /// without selecting it in a hotbar. Point it at a LightSource-only InventorySystem
    /// on the player; it uses the first item whose action is a <see cref="ToggleLightAction"/>.
    /// </summary>
    public class LightToggleInput : MonoBehaviour
    {
        [Tooltip("Inventory holding the light sources. If empty, uses the first InventorySystem on this object.")]
        [SerializeField] private InventorySystem _inventory;

        [Tooltip("Key that switches the carried light on and off.")]
        [SerializeField] private Key _toggleKey = Key.F;

        private void Awake()
        {
            if (_inventory == null)
            {
                _inventory = GetComponent<InventorySystem>();
            }
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null || _inventory == null || Time.timeScale <= 0f) return;
            if (!kb[_toggleKey].wasPressedThisFrame) return;

            for (int i = 0; i < _inventory.Slots.Count; i++)
            {
                ItemStack stack = _inventory.Slots[i];
                if (stack == null || stack.IsEmpty || !(stack.Item.UseAction is ToggleLightAction)) continue;

                _inventory.UseSlot(i);
                return;
            }
        }
    }
}
