using Battlehub.UIControls;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface IEventSystemManager
    {
        void Apply(GameObject root, int eventSystemIndex);

        IndependentEventSystem GetEventSystem(int index);
    }

    public class EventSystemManager : MonoBehaviour, IEventSystemManager
    {
        [SerializeField]
        private IndependentEventSystem[] m_eventSystems;

        [SerializeField]
        private VirtualKeyboard[] m_virtualKeyboards;

        public IndependentEventSystem GetEventSystem(int index)
        {
            return m_eventSystems[index];
        }

        public void Apply(GameObject root, int eventSystemIndex)
        {

            IndependentSelectable[] selectables = root.GetComponentsInChildren<IndependentSelectable>(true);
            
            for(int i = 0; i < selectables.Length; ++i)
            {
                IndependentSelectable selectable = selectables[i];
                selectables[i].EventSystem = m_eventSystems[eventSystemIndex];

                InputFieldWithVirtualKeyboard ifwvk = selectable as InputFieldWithVirtualKeyboard;
                if(ifwvk != null)
                {
                    ifwvk.VirtualKeyboard = m_virtualKeyboards[eventSystemIndex];
                }
            }
        }
    }
}

