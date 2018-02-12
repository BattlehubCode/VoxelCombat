using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public delegate void VirtualKeyboardEventHandler(VirtualKeyboard keyboard, VirtualKey key);
    
    public class VirtualKeyboard : MonoBehaviour, IPointerExitHandler
    {
        [SerializeField]
        private float m_repeatDelay = 0.05f;
        private float m_repeatT;

        public event VirtualKeyboardEventHandler KeyDown;
        public event VirtualKeyboardEventHandler KeyPressed;
        public event VirtualKeyboardEventHandler KeyUp;

        private VirtualKey m_pressedKey;
        public VirtualKey PressedKey
        {
            get { return m_pressedKey; }
        }

        private VirtualKey[] m_keys;
        public VirtualKey[] Keys
        {
            get
            {
                if(m_keys == null)
                {
                    m_keys = GetComponentsInChildren<VirtualKey>(true);
                }
                return m_keys;
            }
        }

        private bool m_isOn;

        public bool IsOn
        {
            get { return m_isOn; }
            set
            {
                m_isOn = value;
                m_pressedKey = null;
                gameObject.SetActive(m_isOn);
            }
        }

        private InputField m_target;
        public InputField Target
        {
            get { return m_target; }
            set { m_target = value; }
        }

        private void Awake()
        {
            m_keys = GetComponentsInChildren<VirtualKey>(true);
            for(int i = 0; i < m_keys.Length; ++i)
            {
                VirtualKey key = m_keys[i];

                key.KeyDown += OnKeyDown;
                key.KeyUp += OnKeyUp;
            }

        }

        private void OnDestroy()
        {
            for (int i = 0; i < m_keys.Length; ++i)
            {
                VirtualKey key = m_keys[i];
                if(key)
                {
                    key.KeyDown -= OnKeyDown;
                    key.KeyUp -= OnKeyUp;
                }   
            }
        }

        private void Update()
        {
            if(m_pressedKey != null)
            {
                if(m_repeatT <= Time.time)
                {
                    if (KeyPressed != null)
                    {
                        KeyPressed(this, m_pressedKey);
                    }

                    m_repeatT = Time.time + m_repeatDelay;
                }
            }
        }

        private void OnKeyDown(VirtualKey sender)
        {
            m_pressedKey = sender;
            m_repeatT = Time.time + m_repeatDelay * 4;
            if(KeyDown != null)
            {
                KeyDown(this, sender);
            }
            if (KeyPressed != null)
            {
                KeyPressed(this, m_pressedKey);
            }
        }

        private void OnKeyUp(VirtualKey sender)
        {
            ReleaseKey();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ReleaseKey();
        }

        public void ReleaseKey()
        {
            if (m_pressedKey != null)
            {
                m_pressedKey = null;
                if (KeyUp != null)
                {
                    KeyUp(this, m_pressedKey);
                }
            }
        }
    }
}
