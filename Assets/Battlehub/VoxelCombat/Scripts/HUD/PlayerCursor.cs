using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class PlayerCursor : MonoBehaviour
    {
        [SerializeField]
        private Image m_graphics;

        public Vector3 Position
        {
            get { return m_graphics.transform.position; }
            set { m_graphics.transform.position = value; }
        }
    }

}

