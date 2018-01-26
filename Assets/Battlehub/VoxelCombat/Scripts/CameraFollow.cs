using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField]
        private float m_smooth = 1.0f;

        [SerializeField]
        private float m_lookAtSmooth = 0.5f;
        
        [SerializeField]
        private Transform m_target;

        [SerializeField]
        private Transform m_camera;

        private Vector3 m_relCameraPos;
        private float m_relCameraPosMag;
        private Vector3 m_newPos;
        private Vector3[] m_checkPoints;

        private void Awake()
        {
            m_camera = GetComponent<Transform>();
            m_relCameraPos = m_camera.position - m_target.position;
            m_relCameraPosMag = m_relCameraPos.magnitude - 0.5f;
            m_checkPoints = new Vector3[5];
        }

        private void FixedUpdate()
        {
            Vector3 standardPos = m_target.position + m_relCameraPos;
            Vector3 abovePos = m_target.position + Vector3.up * m_relCameraPosMag;
            m_checkPoints[0] = standardPos;
            m_checkPoints[1] = Vector3.Lerp(standardPos, abovePos, 0.25f);
            m_checkPoints[2] = Vector3.Lerp(standardPos, abovePos, 0.5f);
            m_checkPoints[3] = Vector3.Lerp(standardPos, abovePos, 0.75f);
            m_checkPoints[4] = abovePos;

            for(int i = 0; i < m_checkPoints.Length; ++i)
            {
                if (ViewingPosCheck(m_checkPoints[i]))
                {
                    break;
                }
            }

            m_camera.position = Vector3.Slerp(m_camera.position, m_newPos, m_smooth * Time.deltaTime);

            SmoothLookAt();
        }

        private bool ViewingPosCheck(Vector3 checkPos)
        {
            RaycastHit hit;
            if(Physics.Raycast(checkPos, m_target.position - checkPos, out hit, m_relCameraPosMag))
            {
                if(hit.transform.parent != m_target)
                {
                    return false;
                }
            }
            m_newPos = checkPos;
            return true;
        }

        private void SmoothLookAt()
        {
            Vector3 relTargetPosition = m_target.position - m_camera.position;
            Quaternion lookAtRotation = Quaternion.LookRotation(relTargetPosition, Vector3.up);
            m_camera.rotation = Quaternion.Lerp(m_camera.rotation, lookAtRotation, m_lookAtSmooth * Time.deltaTime);
        }
    }

}
