using Battlehub.UIControls;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class CommonUIZone : MonoBehaviour
    {
        private InputProvider m_inputProvider;

        public InputProvider InputProvider
        {
            get { return m_inputProvider; }
        }

        public void Set(VirtualKeyboard keyboard, IndependentEventSystem eventSystem, InputProvider inputProvider)
        {
            m_inputProvider = inputProvider;    

            IndependentSelectable[] selectables = GetComponentsInChildren<IndependentSelectable>(true);
            foreach(IndependentSelectable selectable in selectables)
            {
                selectable.EventSystem = eventSystem;

                if(selectable is InputFieldWithVirtualKeyboard)
                {
                    InputFieldWithVirtualKeyboard ifwvk = (InputFieldWithVirtualKeyboard)selectable;
                    ifwvk.VirtualKeyboard = keyboard;
                }
            }

            InputProviderAdapter[] inputAdapters = GetComponentsInChildren<InputProviderAdapter>(true);
            foreach(InputProviderAdapter adapter in inputAdapters)
            {
                adapter.InputProvider = inputProvider;
            }
        }
    }

}
