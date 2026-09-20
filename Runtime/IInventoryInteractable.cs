using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Interaction contract for objects that can be interacted with or picked up.
    /// Can be triggered directly by any player controller or through an adapter.
    /// </summary>
    public interface IInventoryInteractable
    {
        void Interact(GameObject interactor);
        string GetPromptText();
    }
}
