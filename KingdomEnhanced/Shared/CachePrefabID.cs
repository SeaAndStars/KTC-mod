using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KingdomMod;

/// <summary>Caches prefab IDs mapped to their GameObjects for fast lookups without rescanning the scene.</summary>
public class CachePrefabID
{
    /// <summary>Maps a prefab ID to all GameObjects currently carrying that ID.</summary>
    private Dictionary<int, List<GameObject>> _prefabIdCache = new Dictionary<int, List<GameObject>>();

    /// <summary>Rescans the scene and rebuilds the prefab ID to GameObject cache.</summary>
    public void CachePrefabIDs()
    {
        _prefabIdCache.Clear();

        PrefabID[] allPrefabIDs = Object.FindObjectsByType<PrefabID>(FindObjectsSortMode.None);
        foreach (PrefabID prefab in allPrefabIDs)
        {
            int id = prefab.prefabID;
            if (!_prefabIdCache.ContainsKey(id))
            {
                _prefabIdCache[id] = new List<GameObject>();
            }

            _prefabIdCache[id].Add(prefab.gameObject);
        }
    }

    /// <summary>Returns all cached GameObjects carrying the given prefab ID, or null if none are cached.</summary>
    public List<GameObject> FindGameObjects(int targetPrefabID)
    {
        if (_prefabIdCache.ContainsKey(targetPrefabID))
        {
            return _prefabIdCache[targetPrefabID];
        }

        return null;
    }

    /// <summary>Returns the first cached GameObject carrying the given prefab ID, or null if none are cached.</summary>
    public GameObject FindGameObject(int targetPrefabID)
    {
        if (_prefabIdCache.ContainsKey(targetPrefabID))
        {
            return _prefabIdCache[targetPrefabID].FirstOrDefault();
        }

        return null;
    }

}