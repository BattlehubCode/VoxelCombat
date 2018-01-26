using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public interface IVoxelFactory
    {
        void SetPoolSize(int type, int poolSize);

        void RegisterPrefab(Voxel prefab);

        Voxel[] GetPrefabs();

        Voxel GetPrefab(int type);

        Voxel Acquire(int type);

        void Release(Voxel voxel);

        VoxelData InstantiateData(int type);
    }

    
    public class VoxelFactory : MonoBehaviour, IVoxelFactory
    {
        private Dictionary<int, Voxel> m_prefabs = new Dictionary<int, Voxel>();
        private Dictionary<int, VoxelPool> m_pools = new Dictionary<int, VoxelPool>();

        [SerializeField]
        private Voxel[] m_regsteredPrefabs;

        private void Awake()
        {
            for(int i = 0; i < m_regsteredPrefabs.Length; ++i)
            {
                Voxel prefab = m_regsteredPrefabs[i];
                m_prefabs.Add(prefab.Type, prefab);
                m_pools.Add(prefab.Type, new VoxelPool(prefab, transform, 10));
            }
        }

        private void OnDestroy()
        {
            foreach(VoxelPool pool in m_pools.Values)
            {
                pool.SetPoolSize(0);
            }
        }

        public void SetPoolSize(int type, int poolSize)
        {
            if(m_prefabs.ContainsKey(type))
            {
                throw new System.ArgumentException(string.Format("type {0} is not registered", type));
            }

            m_pools[type].SetPoolSize(poolSize);
        }

        public void RegisterPrefab(Voxel prefab)
        {
            m_prefabs.Add(prefab.Type, prefab);
        }

        public Voxel[] GetPrefabs()
        {
            return m_prefabs.Values.ToArray();
        }

        public Voxel GetPrefab(int type)
        {
            return m_prefabs[type];
        }

        public Voxel Acquire(int type)
        {
            if(!m_pools.ContainsKey(type))
            {
                return null;
            }

            return m_pools[type].Acquire();
        }

        public void Release(Voxel voxel)
        {
            if(voxel == null)
            {
                return;
            }

            m_pools[voxel.Type].Release(voxel);
        }

        public VoxelData InstantiateData(int type)
        {
            VoxelData data = new VoxelData();
            data.Type = type;
            return data;
        }
    }



    public class VoxelPool : Pool<Voxel>
    {
        private Voxel m_prefab;

        private Transform m_root;

        public VoxelPool(Voxel prefab, Transform root, int size) 
        {
            m_root = root;
            m_prefab = prefab;
            Initialize(size);
        }

        public override Voxel Acquire()
        {
            Voxel voxel = base.Acquire();
            voxel.GoToAcquiredState();
            return voxel;
        }

        public override void Release(Voxel voxel)
        {
            voxel.GoToReleasedState();
            voxel.transform.SetParent(m_root);
            base.Release(voxel);
        }

        public override Voxel Instantiate(int index)
        {
            GameObject voxelGO = Object.Instantiate(m_prefab.Root.gameObject, m_root);
            voxelGO.name = m_prefab.name + " " + index;
            voxelGO.SetActive(false);
            return voxelGO.GetComponentInChildren<Voxel>();
        }

        public override void Destroy(Voxel obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
    }
}
