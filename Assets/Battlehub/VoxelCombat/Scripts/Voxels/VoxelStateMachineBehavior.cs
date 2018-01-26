using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void VoxelStateMachineCallback(AnimatorStateInfo stateInfo);

    public class VoxelStateMachineBehavior : StateMachineBehaviour
    {
        private VoxelStateMachineCallback m_onStateEnter;
        public void SetStateEnterCallback(VoxelStateMachineCallback onStateEnter)
        {
            m_onStateEnter = onStateEnter;
        }

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if(m_onStateEnter != null)
            {
                m_onStateEnter(stateInfo);
            }

          
        }

        

    }
}
