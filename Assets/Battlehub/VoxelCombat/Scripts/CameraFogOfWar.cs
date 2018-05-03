using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class CameraFogOfWar : MonoBehaviour
    {
        private int m_FogOfWarTexIndex;
        private int m_playerIndex;
        public int PlayerIndex
        {
            get { return m_playerIndex; }
            set { m_playerIndex = value; }
        }

        private void Start()
        {
            m_FogOfWarTexIndex = Shader.PropertyToID("_FogOfWarTexIndex");
        }

        private void OnPreRender()
        {
            Shader.SetGlobalInt(m_FogOfWarTexIndex, m_playerIndex);
        }

        private void OnPostRender()
        {
            
        }
    }
}
