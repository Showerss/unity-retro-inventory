using System;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Item use action for reading books, notes, journals, logs, and keycard files.
    /// Opens the retro document reader modal.
    /// </summary>
    [CreateAssetMenu(fileName = "Action_ReadDocument", menuName = "Retro/Inventory/Actions/Read Document Action")]
    public class ReadDocumentAction : ItemUseAction
    {
        [Tooltip("The title shown at the top of the reading modal.")]
        [SerializeField] private string _documentTitle = "Document";

        [TextArea(6, 18)]
        [Tooltip("The readable text/lore content.")]
        [SerializeField] private string _documentContent = "The pages are filled with handwritten notes...";

        public static event Action<string, string> OnReadDocumentRequested;

        public string DocumentTitle => _documentTitle;
        public string DocumentContent => _documentContent;

        public override bool Execute(GameObject user, ItemDefinition item)
        {
            string title = !string.IsNullOrEmpty(_documentTitle) ? _documentTitle : (item != null ? item.DisplayName : "Document");
            OnReadDocumentRequested?.Invoke(title, _documentContent);
            return true;
        }
    }
}
