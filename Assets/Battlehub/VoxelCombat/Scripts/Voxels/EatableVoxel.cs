using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class EatableVoxel : Voxel
    {
        public override int Type
        {
            get { return (int)KnownVoxelTypes.Eatable; }
        }

        public override int Weight
        {
            get
            {
                return m_weight;
            }

            set
            {
                m_weight = value;

                float scale = Mathf.Pow(2, m_weight);
                Vector3 localScale = Root.localScale;
                localScale.x = scale - 0.5f;
                localScale.y = scale - 0.5f;
                localScale.z = scale - 0.5f;
                Root.localScale = localScale;

            }
        }

        private MeshRenderer m_meshRenderer;
        
        private void Awake()
        {
            m_meshRenderer = GetComponent<MeshRenderer>();
        }

        protected override void SetMaterials(Material primary, Material secondary)
        {
            base.SetMaterials(primary, secondary);
            Material[] materials = m_meshRenderer.sharedMaterials;
            materials[1] = m_primaryMaterial;
            materials[0] = m_secondaryMaterial;
            m_meshRenderer.sharedMaterials = materials;
        }

        public override void BeginAssimilate(float delay)
        {
            base.BeginAssimilate(delay);
        }

        public override void Assimlate(float delay)
        {
            base.Assimlate(delay);
        }

        public override void Smash(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.EatableCollapse, delay, health);
            base.Smash(delay, health);
        }

        public override void Explode(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.EatableExplosion, delay, health);
            base.Explode(delay, health);
        }
    }
}

