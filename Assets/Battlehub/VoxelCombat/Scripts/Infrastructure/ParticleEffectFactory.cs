using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public interface IParticleEffectFactory
    {
        void SetPoolSize(ParticleEffectType type, int poolSize);

        void RegisterPrefab(ParticleEffect prefab);

        ParticleEffect[] GetPrefabs();

        ParticleEffect GetPrefab(ParticleEffectType type);

        ParticleEffect Acquire(ParticleEffectType type);

        void Release(ParticleEffect voxel);
    }

    
    public class ParticleEffectFactory : MonoBehaviour, IParticleEffectFactory
    {
        private Dictionary<ParticleEffectType, ParticleEffect> m_prefabs = new Dictionary<ParticleEffectType, ParticleEffect>();
        private Dictionary<ParticleEffectType, ParticleEffectPool> m_pools = new Dictionary<ParticleEffectType, ParticleEffectPool>();

        [SerializeField]
        private ParticleEffect[] m_regsteredPrefabs;

        private void Awake()
        {
            for(int i = 0; i < m_regsteredPrefabs.Length; ++i)
            {
                ParticleEffect prefab = m_regsteredPrefabs[i];
                m_prefabs.Add(prefab.Type, prefab);
                m_pools.Add(prefab.Type, new ParticleEffectPool(prefab, transform, 10));
            }
        }

        private void OnDestroy()
        {
            foreach(ParticleEffectPool pool in m_pools.Values)
            {
                pool.SetPoolSize(0);
            }
        }

        public void SetPoolSize(ParticleEffectType type, int poolSize)
        {
            if(m_prefabs.ContainsKey(type))
            {
                throw new System.ArgumentException(string.Format("type {0} is not registered", type));
            }

            m_pools[type].SetPoolSize(poolSize);
        }

        public void RegisterPrefab(ParticleEffect prefab)
        {
            m_prefabs.Add(prefab.Type, prefab);
        }

        public ParticleEffect[] GetPrefabs()
        {
            return m_prefabs.Values.ToArray();
        }

        public ParticleEffect GetPrefab(ParticleEffectType type)
        {
            return m_prefabs[type];
        }

        public ParticleEffect Acquire(ParticleEffectType type)
        {
            if(!m_pools.ContainsKey(type))
            {
                return null;
            }

            return m_pools[type].Acquire();
        }

        public void Release(ParticleEffect voxel)
        {
            if(voxel == null)
            {
                return;
            }

            m_pools[voxel.Type].Release(voxel);
        }

    }



    public class ParticleEffectPool : Pool<ParticleEffect>
    {
        private ParticleEffect m_prefab;

        private Transform m_root;

        public ParticleEffectPool(ParticleEffect prefab, Transform root, int size) 
        {
            m_root = root;
            m_prefab = prefab;
            Initialize(size);
        }

        public override ParticleEffect Acquire()
        {
            ParticleEffect effect = base.Acquire();
            effect.GoToAcquiredState();
            return effect;
        }

        public override void Release(ParticleEffect effect)
        {
            effect.GoToReleasedState();
            effect.transform.SetParent(m_root);
            base.Release(effect);
        }

        protected override ParticleEffect Instantiate(int index)
        {
            GameObject particleEffectGo = Object.Instantiate(m_prefab.gameObject, m_root);
            particleEffectGo.name = m_prefab.name + " " + index;
            particleEffectGo.SetActive(false);
            return particleEffectGo.GetComponentInChildren<ParticleEffect>();
        }

        protected override void Destroy(ParticleEffect obj)
        {
            if (obj != null)
            {
                Object.Destroy(obj.gameObject);
            }
        }
    }
}
