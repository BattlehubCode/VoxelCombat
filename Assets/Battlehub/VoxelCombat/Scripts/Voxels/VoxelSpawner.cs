using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class VoxelSpawner : Voxel
    {
        public override int Weight
        {
            get { return m_weight; }
            set
            {
                m_weight = value;
                float scale = Mathf.Pow(2, m_weight - GameConstants.MinVoxelActorWeight);
                
                Vector3 localScale = Root.localScale;
                localScale.x = scale - 0.125f;
                localScale.z = scale - 0.125f;
                Root.localScale = localScale;

              
            }
        }
        public override int Height
        {
            get { return m_height; }
            set
            {
                m_height = value;

                Vector3 scale = transform.localScale;
                scale.y = m_height / 4.0f;
                transform.localScale = scale;
            }
        }

        protected override float EvalHeight(int height)
        {
            float result = height / 4.0f;
            result = Mathf.Max(0.01f, result);
            return result;
        }

        public override int Type
        {
            get { return (int)KnownVoxelTypes.Spawner; }
        }

        protected override void SetMaterials(Material primary, Material secondary)
        {
            base.SetMaterials(primary, secondary);
            m_renderer.sharedMaterial = primary;
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
            InstantiateParticleEffect(ParticleEffectType.SpawnerCollapse, delay, health);
            base.Smash(delay, health);
        }

        public override void Explode(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.SpawnerExplosion, delay, health);
            base.Explode(delay, health);
        }
    }

}
