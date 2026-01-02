using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Homebound.Features.Visuals
{
    [CreateAssetMenu(fileName = "ItemVisualDB", menuName = "Homebound/Visuals/Item Visual Database")]
    public class ItemVisualDatabase : ScriptableObject
    {
        [System.Serializable]
        public class ItemEntry
        {
            public string LogicID; // Ej: "Job_Mine", "Tool_Axe"
            public GameObject VisualPrefab;
            public SocketType TargetSocket = SocketType.MainHand;
        }

        [SerializeField] private List<ItemEntry> _entries;

        private Dictionary<string, ItemEntry> _lookup;

        public void Initialize()
        {
            if (_entries == null) return;
            _lookup = _entries.ToDictionary(x => x.LogicID, x => x);
        }

        public bool TryGetVisual(string id, out GameObject prefab, out SocketType socket)
        {
            if (_lookup == null) Initialize();

            if (_lookup.TryGetValue(id, out var entry))
            {
                prefab = entry.VisualPrefab;
                socket = entry.TargetSocket;
                return true;
            }

            prefab = null;
            socket = SocketType.MainHand;
            return false;
        }
    }
}