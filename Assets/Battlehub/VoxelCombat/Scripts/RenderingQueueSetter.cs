using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class RenderingQueueSetter : MonoBehaviour
    {
        [SerializeField]
        private int Queue = 3000; // transparent
        [SerializeField]
        private Material m_material;

        private void Start()
        {
            m_material.renderQueue = Queue;    
        }

        
    }
}

