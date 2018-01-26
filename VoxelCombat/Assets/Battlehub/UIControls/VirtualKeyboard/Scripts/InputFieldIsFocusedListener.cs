using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public class InputFieldIsFocusedListener : MonoBehaviour
    {
        public event System.EventHandler IsFocusedChanged;

        private InputField m_inputField;

        private bool m_isFocused;
        
        private void Start()
        {
            m_inputField = GetComponent<InputField>();
            m_isFocused = m_inputField.isFocused;
        }

        private void Update()
        {
            if(m_isFocused != m_inputField.isFocused)
            {
                if(IsFocusedChanged != null)
                {
                    IsFocusedChanged(this, System.EventArgs.Empty);
                }
                m_isFocused = m_inputField.isFocused;
            }
        }
    }

}
