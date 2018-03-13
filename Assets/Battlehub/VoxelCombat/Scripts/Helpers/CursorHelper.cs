using UnityEngine;

namespace Battlehub.VoxelCombat
{

    public static class CursorHelper
    {
        private static bool m_isVisible;
        public static bool visible
        {
            get { return m_isVisible; }
            set
            {
                m_isVisible = value;
                Cursor.visible = value;
            }
        }

        private static CursorLockMode m_lockState;
        public static CursorLockMode lockState
        {
            get { return m_lockState; }
            set
            {
                m_lockState = value;
                Cursor.lockState = value;
            }
        }

        private static object m_locker;

        public static void SetCursor(object locker, Texture2D texture, Vector2 hotspot, CursorMode mode)
        {
            if (m_locker != null && m_locker != locker)
            {
                return;
            }
            m_locker = locker;
            Cursor.SetCursor(texture, hotspot, mode);
        }

        public static void ResetCursor(object locker)
        {            
            if (m_locker != locker)
            {
                return;
            }

            m_locker = null;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

    }

}
