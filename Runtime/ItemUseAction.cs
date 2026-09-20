using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Abstract base class for extensible item use behaviors.
    /// Allows any future AI or developer to implement new item mechanics without editing core inventory code.
    /// </summary>
    public abstract class ItemUseAction : ScriptableObject
    {
        [Tooltip("Optional message to display when the item is used successfully.")]
        [SerializeField] private string _successMessage = string.Empty;

        public string SuccessMessage => _successMessage;

        /// <summary>
        /// Executes the item's unique behavior.
        /// </summary>
        /// <param name="user">The GameObject using the item (e.g. Player).</param>
        /// <param name="item">The ItemDefinition being used.</param>
        /// <returns>True if the item was successfully used; false otherwise.</returns>
        public abstract bool Execute(GameObject user, ItemDefinition item);

        /// <summary>
        /// Optional reason provided if the item use failed.
        /// </summary>
        public virtual string GetFailureReason() => "Cannot use this item right now.";
    }
}
