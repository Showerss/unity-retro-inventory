using UnityEngine;

namespace Retro.Inventory
{
    public enum LightTypeToToggle
    {
        Flashlight,
        Lantern
    }

    /// <summary>
    /// Item use action that toggles a light source (flashlight or lantern) on the player.
    /// </summary>
    [CreateAssetMenu(fileName = "Action_ToggleLight", menuName = "Retro/Inventory/Actions/Toggle Light Action")]
    public class ToggleLightAction : ItemUseAction
    {
        [Tooltip("Which light source type to toggle on the user.")]
        [SerializeField] private LightTypeToToggle _lightType = LightTypeToToggle.Flashlight;

        [Tooltip("Sound played when toggled.")]
        [SerializeField] private AudioClip _toggleSound;

        public LightTypeToToggle LightType => _lightType;

        public override bool Execute(GameObject user, ItemDefinition item)
        {
            if (user == null) return false;

            HeldItemManager heldManager = user.GetComponentInChildren<HeldItemManager>();
            if (heldManager != null)
            {
                bool isOn = heldManager.ToggleLight(_lightType, _toggleSound);
                string lightName = _lightType == LightTypeToToggle.Flashlight ? "Flashlight" : "Lantern";
                string status = isOn ? "ON" : "OFF";
                
                InventorySystem inv = user.GetComponent<InventorySystem>();
                if (inv != null)
                {
                    inv.Notify($"{lightName}: {status}");
                }
                return true;
            }

            return false;
        }

        public override string GetFailureReason() => "No light fixture found on player.";
    }
}
