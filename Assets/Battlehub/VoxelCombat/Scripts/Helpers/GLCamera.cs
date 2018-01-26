using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public enum RTLayer
    {
        None = 0,
        SceneView = 1 << 0,
        GameView = 1 << 1,
        Viewport0 = (1 << 2) | GameView,
        Viewport1 = (1 << 3) | GameView,
        Viewport2 = (1 << 4) | GameView,
        Viewport3 = (1 << 5) | GameView,
        Any = -1,
    }

    /// <summary>
    /// Camera behavior for GL. rendering
    /// </summary>
    [ExecuteInEditMode]
    public class GLCamera : MonoBehaviour
    {
        public int CullingMask = -1;

        private void OnPostRender()
        { 
            if(GLRenderer.Instance != null)
            {
                GLRenderer.Instance.Draw(CullingMask);
            }
        }
    }
}

