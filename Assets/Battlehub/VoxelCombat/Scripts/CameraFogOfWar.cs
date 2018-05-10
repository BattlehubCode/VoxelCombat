using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Battlehub.VoxelCombat
{
    [DefaultExecutionOrder(-10)]
    public class CameraFogOfWar : MonoBehaviour
    {
        private int m_FogOfWarTexIndex;
        private int m_playerIndex;
        public int PlayerIndex
        {
            get { return m_playerIndex; }
            set { m_playerIndex = value; }
        }
#if UNITY_EDITOR
        public static void AttachToEditorCamera()
        {
            Camera[] cameras = SceneView.GetAllSceneCameras();
            for (int i = 0; i < cameras.Length; ++i)
            {
                if(cameras[i].gameObject.GetComponent<CameraFogOfWar>() == null)
                {
                    CameraFogOfWar fogOfWar = cameras[i].gameObject.AddComponent<CameraFogOfWar>();
                    fogOfWar.PlayerIndex = 0;
                }
            }
        }
#endif
        private void Start()
        {

#if UNITY_EDITOR
            AttachToEditorCamera();
#endif

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
