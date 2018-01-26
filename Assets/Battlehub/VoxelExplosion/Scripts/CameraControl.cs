using UnityEngine;

namespace Battlehub.VoxelExplosion
{
    public class CameraControl : MonoBehaviour
    {

        [SerializeField]
        private GameObject m_player;

        private Vector3 m_diff;

        private void Start()
        {
            m_diff = transform.position - m_player.transform.position;
        }

        private void LateUpdate()
        {
            if(m_player)
            {
                Vector3 pp = m_player.transform.position;
                transform.position = pp + m_diff;
            }
        }
    }
}


