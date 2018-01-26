using Battlehub.UIControls;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class PlayerUIZone : MonoBehaviour
    {
        [SerializeField]
        private VoxelUIInputProvider m_keyboard;

        [SerializeField]
        private VoxelUIInputProvider m_eventSystem;

        [SerializeField]
        private CommonUIZone[] m_commonUIZones;

        [SerializeField]
        private int m_localPlayerIndex;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                SetLocalPlayerIndex(value);
            }
        }


        private void SetLocalPlayerIndex(int value)
        {
            m_localPlayerIndex = value;

            if (m_keyboard != null)
            {
                m_keyboard.LocalPlayerIndex = m_localPlayerIndex;
            }

            if (m_eventSystem != null)
            {
                m_eventSystem.LocalPlayerIndex = m_localPlayerIndex;
            }

            if(m_localPlayerIndex == 0)
            {
                for(int i = 0; i < m_commonUIZones.Length; ++i)
                {
                    m_commonUIZones[i].Set(m_keyboard.GetComponent<VirtualKeyboard>(), m_eventSystem.GetComponent<IndependentEventSystem>(), m_eventSystem);
                }
            }
        }

        private void Awake()
        {
            SetLocalPlayerIndex(m_localPlayerIndex);
        }

    }

}

