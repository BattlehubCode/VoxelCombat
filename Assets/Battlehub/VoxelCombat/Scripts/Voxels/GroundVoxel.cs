using UnityEngine;

namespace Battlehub.VoxelCombat
{

    //One of scenarios is to allow player to capture GroundVoxels. Possible goal of the game is to capture all GroundVoxels?

    //It will be several colors for each team and neutral color for eatable voxels

    //Neutral voxels are not recoverable

    //Captured Ground voxels could produces eatables 

    public class GroundVoxel : Voxel
    {
        private bool m_updateMesh = true;
        [SerializeField]
        private Mesh m_wall3;
        [SerializeField]
        private Mesh m_wall2;
        [SerializeField]
        private Mesh m_wall1;

        private MeshFilter m_filter;
       // private MeshCollider m_collider;
  
        protected override void SetMaterials(Material primary, Material secondary)
        {
            base.SetMaterials(primary, secondary);
            m_renderer.sharedMaterial = primary;
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
                localScale.z = scale - 0.5f;
                Root.localScale = localScale;
            }
        }

        public override int Health
        {
            get { return base.Health; }
            set
            {
                base.Health = value;
                UpdateMesh();
            }
        }

        public override int Type
        {
            get { return (int)KnownVoxelTypes.Ground; }
        }

        protected override void AwakeOverride()
        {
            m_filter = GetComponent<MeshFilter>();
           // m_collider = GetComponent<MeshCollider>();
            UpdateMesh();
        }

        protected override void OnDestroyOveride()
        {
            base.OnDestroyOveride();
        }
        private void UpdateMesh()
        {
            if(!m_updateMesh)
            {
                return;
            }

            if(Health >= 3)
            {
                m_filter.sharedMesh = m_wall3;
               // m_collider.sharedMesh = m_wall3; 
            }
            else if(Health == 2)
            {
                m_filter.sharedMesh = m_wall2;
               // m_collider.sharedMesh = m_wall2;
            }
            else if (Health == 1)
            {
                m_filter.sharedMesh = m_wall1;
              //  m_collider.sharedMesh = m_wall1;
            }


            Material[] materials = m_renderer.sharedMaterials;
            materials[0] = m_primaryMaterial;
            m_renderer.sharedMaterials = materials;
        }

        protected override void ReadRotation(VoxelData data)
        {
            
        }

        public override void ReadFrom(VoxelData data)
        {
            m_updateMesh = false;
            base.ReadFrom(data);
            m_updateMesh = true;
            UpdateMesh();
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
            InstantiateParticleEffect(ParticleEffectType.GroundCollapse, delay, health);
            base.Smash(delay, health);
        }

        public override void Explode(float delay, int health)
        {
            InstantiateParticleEffect(ParticleEffectType.GroundExplosion, delay, health);
            base.Explode(delay, health);
        }
    }

}
