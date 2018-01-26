using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void VoxelDigestingHandler(VoxelDigestingBehavior sender);

    [DisallowMultipleComponent]
    public class VoxelDigestingBehavior : MonoBehaviour
    {
        private Voxel m_victim;

        [SerializeField]
        private float m_smoothRate = 0.05f;// 5.0f; 
        [SerializeField]
        private float m_lifeSpan = 20.0f; //0.5f;
        private float m_delay = 1.0f;
        
        /// <summary>
        /// Position in local space of Target;
        /// </summary>
        private Vector3 m_targetPosition;
        private Vector3 m_upCorrection;
        private Voxel m_target;
        private Transform m_stomic;
        private bool m_isInProgress;

        public event VoxelDigestingHandler Completed;

        public Vector3 TargetPosition
        {
            get { return m_targetPosition; }
        }

        public Voxel Voxel
        {
            get { return m_victim; }
        }

        private void Awake()
        {
            m_victim = GetComponent<Voxel>();
            m_victim.Released += OnReleased;
            if (m_victim == null)
            {
                Debug.LogError("Unable to attach VoxelDigestingBehavior to non-voxel object");
            }
            enabled = false;
        }

        private void OnDestroy()
        {
            if (m_victim != null)
            {
                m_victim.Released -= OnReleased;
            }
        }

        public void Digest(Voxel target, Transform stomic, Vector3 targetPosition)
        {
            if(m_isInProgress)
            {
                throw new System.InvalidOperationException("In Progress");
            }

            if(!target.IsAcquired)
            {
                throw new System.InvalidOperationException("!target.IsAcquired");
            }

            if(m_target != null)
            {
                throw new System.InvalidOperationException("m_target != null");
            }

            

           // m_digest = true;

            m_targetPosition = targetPosition;
            m_target = target;
            m_target.Released += OnTargetReleased;
            m_stomic = stomic;
            m_delay = 0;

            Vector3 targetScale = target.transform.localScale;
            m_upCorrection = new Vector3(0, m_victim.Height * GameConstants.UnitSize / (2.0f * targetScale.y), 0);

            enabled = true;
            m_isInProgress = true;
        }


        private void Update()
        {
            if(m_delay >= 0)
            {
                m_delay -= Time.deltaTime;
                if (m_delay <= 0)
                {
                    m_victim.Freeze();


                    m_victim.transform.SetParent(m_stomic, true);
                    m_victim.transform.localRotation = Quaternion.identity;
                    // Fix wierd scale
                    //    m_victim.Weight = m_victim.Weight;
                    //    m_victim.Height = m_victim.Height;
                    //   m_victim.Altitude = m_victim.Altitude;

                }
                return;
            }


            if (!m_target.IsAcquired)
            {
                enabled = false;
                m_victim.Kill(); //OnRelease should be called immediately
                return;
            }

            m_lifeSpan -= Time.deltaTime;

            Vector3 targetPosition = m_targetPosition - m_upCorrection;

            // Fix wierd scale
           // m_victim.Weight = m_victim.Weight;
           // m_victim.Height = m_victim.Height;
           // m_victim.Altitude = m_victim.Altitude;

            m_victim.transform.localPosition = Vector3.Lerp(m_victim.transform.localPosition, targetPosition, Time.deltaTime * m_smoothRate);

            if (Vector3.Magnitude(targetPosition - m_victim.transform.localPosition) < 0.05f)
            {
                enabled = false;
                m_victim.Assimlate(0);
            }
            else if(m_lifeSpan <= 0)
            {
                enabled = false;
                m_victim.Assimlate(0);
            }
        }

        private void OnTargetReleased(Voxel sender)
        {
            m_victim.Kill();
        }

        private void OnReleased(Voxel sender)
        {
            if(m_target != null)
            {
                m_target.Released -= OnTargetReleased;
                m_target = null;
                m_stomic = null;
            }
            
            enabled = false;

            if(m_isInProgress)
            {
                m_isInProgress = false;

                if (Completed != null)
                {
                    Completed(this);
                }
            }
           
        }

    }

}
