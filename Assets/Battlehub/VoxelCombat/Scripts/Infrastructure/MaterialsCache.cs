using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IMaterialsCache
    {
        Color GetPrimaryColor(int player);

        Color GetSecondaryColor(int player);

        Material GetPrimaryMaterial(int player);

        Material GetSecondaryMaterial(int player);
    }

    public class MaterialsCache : MonoBehaviour, IMaterialsCache
    {
        private IVoxelGame m_gameState;
        private IVoxelMinimapRenderer m_minimap;

#warning Should be moved to another place
        [SerializeField]
        private Renderer m_ground;

        [SerializeField]
        private Color[] m_playerColors;
        [SerializeField]
        private Material m_primaryMaterial;
        [SerializeField]
        private Material m_secondaryMaterial;

        private Material[] m_primaryMaterials;
        private Material[] m_secondaryMaterials;

        private const float m_alpha = 0.4f;

        public Color GetPrimaryColor(int player)
        {
            return m_playerColors[player];
        }

        public Color GetSecondaryColor(int player)
        {
            Color secondaryColor = m_playerColors[player];
            secondaryColor.a = m_alpha;
            return secondaryColor;
        }

        public Material GetPrimaryMaterial(int player)
        {
            return m_primaryMaterials[player];
        }

        public Material GetSecondaryMaterial(int player)
        {
            return m_secondaryMaterials[player];
        }

        private void Awake()
        {
            m_minimap = Dependencies.Minimap;
            m_minimap.Loaded += OnMinimapLoaded;
            m_gameState = Dependencies.GameState;

            if(m_playerColors.Length != m_gameState.MaxPlayersCount)
            {
                Debug.LogError("not all m_playerColors defined");
                return;
            }

            m_primaryMaterials = new Material[m_gameState.MaxPlayersCount];
            CreateMaterials(m_primaryMaterial, m_primaryMaterials);

            m_secondaryMaterials = new Material[m_gameState.MaxPlayersCount];
            CreateMaterials(m_secondaryMaterial, m_secondaryMaterials, m_alpha);
        }

        private void OnDestroy()
        {
            m_minimap.Loaded -= OnMinimapLoaded;
        }

#warning individual set of materials should be create for each viewport...
#warning Probably should me moved to Viewport camera on prerernder...
        private void OnMinimapLoaded(object sender, EventArgs e)
        {
            for(int i = 0; i < m_primaryMaterials.Length; ++i)
            {
                m_primaryMaterials[i].SetTexture("_FogOfWarTex", m_minimap.FogOfWar[2]);
            }

            for (int i = 0; i < m_secondaryMaterials.Length; ++i)
            {
                m_secondaryMaterials[i].SetTexture("_FogOfWarTex", m_minimap.FogOfWar[2]);
            }

            m_ground.sharedMaterial.SetTexture("_FogOfWarTex", m_minimap.FogOfWar[2]);
        }

        private void CreateMaterials(Material material, Material[] materials, float alpha = 1)
        {
            for(int i = 0; i < materials.Length; ++i)
            {
                materials[i] = Instantiate(material);
                Color color = m_playerColors[i];
                color.a = alpha;
                materials[i].color = color;
                materials[i].name = material.name;
            }
        }


    }

}

