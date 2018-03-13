using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public static class DontDestroyOnLoadManager
    {
        static List<GameObject> _ddolObjects = new List<GameObject>();

        public static void DontDestroyOnLoad(this GameObject go)
        {
            Object.DontDestroyOnLoad(go);
            _ddolObjects.Add(go);
        }

        public static void DestroyAll()
        {
            foreach (var go in _ddolObjects)
                if (go != null)
                    Object.Destroy(go);

            _ddolObjects.Clear();
        }
    }
}