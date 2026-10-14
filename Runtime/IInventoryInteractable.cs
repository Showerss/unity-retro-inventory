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

        /// <summary>
        /// Prompt for whoever is looking at this. Polled every frame: return cached strings.
        /// Never include the key; the HUD adds it when <paramref name="isAction"/> is true.
        /// </summary>
        /// <param name="isAction">True when interacting does what the text says ("Take Brass Key");
        /// false for a plain message ("Locked. Requires Brass Key.").</param>
        string GetPromptText(GameObject interactor, out bool isAction);
    }
}
