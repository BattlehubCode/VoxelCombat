using System.Collections;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public enum ParticleEffectType
    {
        BombExplosion = 10,
        BombCollapse = 11,
        EaterExplosion = 20,
        EaterCollapse = 21,
        EatableExplosion = 30,
        EatableCollapse = 31,
        SpawnerExplosion = 40,
        SpawnerCollapse = 41,
        GroundExplosion = 50,
        GroundCollapse = 51
    }

    public delegate void ParticleEffectEventHandler();      

    public class ParticleEffect : MonoBehaviour
    {
        public event ParticleEffectEventHandler Acquired;
        public event ParticleEffectEventHandler Released;

        [SerializeField]
        private float m_scaleMultiplier = 0.5f;

        [SerializeField]
        private ParticleSystem m_rootPs;
        private VoxelData m_data;
        public VoxelData Data
        {
            get { return m_data; }
            set
            {
                m_data = value;
                if(m_data != null)
                {
                    ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>();
                    for (int i = 0; i < particleSystems.Length; ++i)
                    {
                        particleSystems[i].transform.localScale =
                            Vector3.one * m_scaleMultiplier * Mathf.Pow(2, m_data.Weight);
                    }
                }
            }
            
        }

        public float StartDelay
        {
            get;
            set;
        }

        public int Health
        {
            get;
            set;
        }

        
        [SerializeField]
        private ParticleEffectType m_type;

        public ParticleEffectType Type
        {
            get { return m_type; }
        }

        private float m_timeBeforeStart;
        private float m_timeBeforeStop;

        private IParticleEffectFactory m_factory;

        private void Start()
        {
            m_factory = Dependencies.EffectFactory;
        }

        private void Update()
        {
            if(m_timeBeforeStart > 0)
            {
                m_timeBeforeStart -= Time.deltaTime;
                return;
            }

            if(!m_rootPs.isPlaying)
            {
                m_rootPs.Play();
            }
            
            if(m_timeBeforeStop > 0)
            {
                m_timeBeforeStop -= Time.deltaTime;
                return;
            }
        
            m_factory.Release(this);
        }

        public void GoToAcquiredState()
        {
            gameObject.SetActive(true);
            enabled = true;
            m_timeBeforeStart = StartDelay;
            m_timeBeforeStop = m_rootPs.main.duration;

            if(Acquired != null)
            {
                Acquired();
            }
        }

        public void GoToReleasedState()
        {
            m_rootPs.Stop();
            enabled = false;
            gameObject.SetActive(false);

            if(Released != null)
            {
                Released();
            }
        }

    }
}

