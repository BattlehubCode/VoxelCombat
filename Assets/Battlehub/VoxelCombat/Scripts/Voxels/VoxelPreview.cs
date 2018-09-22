using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class VoxelPreview : MonoBehaviour
    {
        [SerializeField]
        private Material m_allowedMaterial;

        [SerializeField]
        private Material m_disallowedMaterial;

        [SerializeField]
        private Renderer m_renderer;

        [SerializeField]
        private Voxel m_voxel;

        private bool m_isAllowedLocation;
        private bool IsAllowedLocation
        {
            get { return m_isAllowedLocation; }
            set
            {
                if(m_isAllowedLocation != value)
                {
                    m_isAllowedLocation = value;
                    SetMaterials();
                }
            }
        }

        private void SetMaterials()
        {
            Material[] materials = m_renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; ++i)
            {
                materials[i] = m_isAllowedLocation ? m_allowedMaterial : m_disallowedMaterial;
            }
            m_renderer.sharedMaterials = materials;
        }

        private void Awake()
        {
            m_voxel.Acquired += OnAquired;
            SetMaterials();
        }

        private void OnDestroy()
        {
            if(m_voxel != null)
            {
                m_voxel.Acquired -= OnAquired;
            }
        }

        private void OnAquired(Voxel sender)
        {
            m_voxel.enabled = false;
            SetMaterials();
        }        
    }
}
