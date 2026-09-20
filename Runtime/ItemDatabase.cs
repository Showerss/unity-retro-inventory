using System.Collections.Generic;
using UnityEngine;

namespace Retro.Inventory
{
    /// <summary>
    /// Maps the stable string ids on <see cref="ItemDefinition"/> back to the assets
    /// themselves.
    ///
    /// Persistence can only store ids - Unity cannot serialise a reference to a
    /// ScriptableObject across sessions - so anything that reloads an inventory needs
    /// this to rehydrate. Create one via Assets > Create > Retro > Item Database and
    /// press Rebuild after adding new items.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "Retro/Item Database")]
    public class ItemDatabase : ScriptableObject
    {
        [SerializeField] private List<ItemDefinition> _items = new List<ItemDefinition>();

        Dictionary<string, ItemDefinition> _lookup;

        public IReadOnlyList<ItemDefinition> Items => _items;

        public ItemDefinition Resolve(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            if (_lookup == null)
            {
                _lookup = new Dictionary<string, ItemDefinition>();
                foreach (ItemDefinition item in _items)
                {
                    if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                    _lookup[item.Id] = item;
                }
            }

            return _lookup.TryGetValue(id, out ItemDefinition found) ? found : null;
        }

#if UNITY_EDITOR
        /// <summary>Editor-only: refills the list from every ItemDefinition in the project.</summary>
        [ContextMenu("Rebuild From Project")]
        public void RebuildFromProject()
        {
            _items.Clear();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:ItemDefinition"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (item != null) _items.Add(item);
            }

            _lookup = null;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
