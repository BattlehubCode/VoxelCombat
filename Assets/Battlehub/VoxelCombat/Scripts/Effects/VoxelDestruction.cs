using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class VoxelDestruction : MonoBehaviour
    {
        [SerializeField]
        private ParticleSystem m_rootPS;

        private IMaterialsCache m_materialCache;

        private ParticleEffect m_particleEffect;

        private void Start()
        {
            m_particleEffect = GetComponent<ParticleEffect>();
            m_materialCache = Dependencies.MaterialsCache;

            m_particleEffect.Acquired += OnAcquired;
            m_particleEffect.Released += OnReleased;
        }

        private void OnDestroy()
        {
            if(m_particleEffect != null)
            {
                m_particleEffect.Acquired -= OnAcquired;
                m_particleEffect.Released -= OnReleased;
            }
        }

        private void OnAcquired()
        {
            enabled = true;
        }

        private void OnReleased()
        {
            
        }

        private void LateUpdate()
        {
            if(m_rootPS.isPlaying)
            {
                Color primaryColor = m_materialCache.GetPrimaryColor(m_particleEffect.Data.Owner);
                Color secondaryColor = m_materialCache.GetSecondaryColor(m_particleEffect.Data.Owner);

                ParticleSystem.Particle[] particles = new ParticleSystem.Particle[m_rootPS.main.maxParticles];
                int numParticlesAlive = m_rootPS.GetParticles(particles);

                int primaryColorParticles;
                if (m_particleEffect.Data.Type == (int)KnownVoxelTypes.Eater)
                {
                    primaryColorParticles = Mathf.Min(m_particleEffect.Health, numParticlesAlive);
                }
                else if (m_particleEffect.Data.Type == (int)KnownVoxelTypes.Bomb)
                {
                    primaryColorParticles = numParticlesAlive / 2;
                }
                else
                {
                    primaryColorParticles = numParticlesAlive;
                }

                if (m_particleEffect.Data.Type == (int)KnownVoxelTypes.Eater ||
                   m_particleEffect.Data.Type == (int)KnownVoxelTypes.Ground ||
                   m_particleEffect.Data.Type == (int)KnownVoxelTypes.Bomb)
                {
                    //float scale = Mathf.Pow(2, m_particleEffect.Data.Weight - GameConstants.MinVoxelActorWeight);
                    //float height = Mathf.Pow(2, m_particleEffect.Data.Weight - GameConstants.MinVoxelActorWeight) * GameConstants.UnitSize;
                    for (int i = 0; i < numParticlesAlive; ++i)
                    {
                        int index = VoxelEater.FillOrder[i];
                        IntVec intVec = VoxelEater.ToIntVec(index);
                        if (i < primaryColorParticles)
                        {
                            particles[i].startColor = primaryColor;
                        }
                        else
                        {
                            particles[i].startColor = secondaryColor;
                        }

                        particles[i].position = VoxelEater.GetPositionLocal(intVec) / 2 + Vector3.up / 2;
                    }
                }
                else if (m_particleEffect.Data.Type == (int)KnownVoxelTypes.Eatable)
                {
                    int particlesCount = Mathf.Min(8, numParticlesAlive);
                    for (int i = 0; i < particlesCount; ++i)
                    {
                        float voxelSideSize = 1;// Mathf.Pow(2, m_particleEffect.Data.Weight) * GameConstants.UnitSize;

                        Vector3 partPostion = Vector3.zero;

                        int imod4 = i % 4;
                        int imod2 = i % 2;
                        float vssDiv4 = voxelSideSize / 4.0f;
                        float vssDiv2 = voxelSideSize / 2.0f;
                        bool bx = (imod4 == 0 || imod4 == 1);
                        bool bz = (imod2 == 1);
                        partPostion.x += bx ? -vssDiv4 : vssDiv4;
                        partPostion.z += bz ? -vssDiv4 : vssDiv4;
                        partPostion.y += i < 4 ? 0 : vssDiv2;

                        particles[i].position = partPostion + Vector3.up / 4;
                        particles[i].startColor = primaryColor;
                    }
                }
                else
                {
                    for (int i = 0; i < numParticlesAlive; ++i)
                    {
                        particles[i].startColor = primaryColor;
                    }
                }
              
                m_rootPS.SetParticles(particles, numParticlesAlive);
                //var main = m_rootPS.main;
                //main.simulationSpeed = 0.01f;
                enabled = false;
            }   
        }
    }
}


