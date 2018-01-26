using Battlehub.UIControls;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class CommonUIZoneSetter : MonoBehaviour
    {
        [SerializeField]
        private CommonUIZone[] m_commonZones;

        [SerializeField]
        private VirtualKeyboard m_keyboard;

        [SerializeField]
        private IndependentEventSystem m_eventSystem;

        [SerializeField]
        private VoxelUIInputProvider m_inputProvider;

        private void OnEnable()
        {
            for(int i = 0; i < m_commonZones.Length; ++i)
            {
                m_commonZones[i].Set(m_keyboard, m_eventSystem, m_inputProvider);
            }
        }
    }
}

