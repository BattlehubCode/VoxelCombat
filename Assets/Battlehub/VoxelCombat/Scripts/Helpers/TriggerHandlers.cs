using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void TriggerEventHander(Voxel target);

    public class TriggerHandlers : MonoBehaviour
    {
        public event TriggerEventHander TriggerEnter;
        public event TriggerEventHander TriggerExit;

        [SerializeField]
        private Voxel m_voxel;

        private void Awake()
        {
            if(m_voxel == null)
            {
                m_voxel = GetComponentInParent<Voxel>();
            }

            if(m_voxel == null)
            {
                Debug.LogError("Unable to find associated voxel");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if(TriggerEnter == null)
            {
                return;
            }

            Voxel voxel = other.GetComponent<Voxel>();
            if(voxel == null)
            {
                TriggerHandlers evtHandlers = other.GetComponent<TriggerHandlers>();
                if(evtHandlers != null)
                {
                    voxel = evtHandlers.m_voxel;
                }
            }
            if(voxel != null)
            {
                TriggerEnter(voxel);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (TriggerExit == null)
            {
                return;
            }

            Voxel voxel = other.GetComponent<Voxel>();
            if (voxel == null)
            {
                TriggerHandlers evtHandlers = other.GetComponent<TriggerHandlers>();
                if (evtHandlers != null)
                {
                    voxel = evtHandlers.m_voxel;
                }
            }

            if (voxel != null)
            {
                TriggerExit(voxel);
            }
        }
    }
}

