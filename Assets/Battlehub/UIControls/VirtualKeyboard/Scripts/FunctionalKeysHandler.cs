using UnityEngine;

namespace Battlehub.UIControls
{
    public class FunctionalKeysHandler : MonoBehaviour
    {
        [SerializeField]
        private VirtualKeyboard m_virtualKeyboard;

        private void Awake()
        {
            if(m_virtualKeyboard == null)
            {
                m_virtualKeyboard = GetComponent<VirtualKeyboard>();
            }
        }

        private void Start()
        {
            m_virtualKeyboard.KeyDown += OnKeyDown;
        }

        private void OnDestroy()
        {
            m_virtualKeyboard.KeyDown -= OnKeyDown;
        }

        private void OnKeyDown(VirtualKeyboard keyboard, VirtualKey downKey)
        {
            if(!downKey.IsFunctional)
            {
                return;
            }

            switch(downKey.KeyCode) 
            {
                case KeyCode.CapsLock: //upper case toggle

                    VirtualKey[] keys = m_virtualKeyboard.Keys;
                    for(int i = 0; i < keys.Length; ++i)
                    {
                        VirtualKey key = keys[i];
                        if(key.KeyCode >= KeyCode.A && key.KeyCode <= KeyCode.Z)
                        {
                            char c = key.Char[0];
                            if(char.IsUpper(c))
                            {
                                key.Char = key.Char.ToLower();
                            }
                            else
                            {
                                key.Char = key.Char.ToUpper();
                            }
                        }
                    }
                    break;   
            }
            
        }
    }
}

