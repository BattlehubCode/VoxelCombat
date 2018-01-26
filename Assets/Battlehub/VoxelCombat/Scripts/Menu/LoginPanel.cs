using Battlehub.UIControls;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public delegate void LoginEventHandler(string name, string password);
    public delegate void LoginCancelEventHandler();

    public class LoginPanel : MonoBehaviour
    {
        public event LoginEventHandler Login;
        public event LoginCancelEventHandler LoginCancel;

        [SerializeField]
        private InputField m_loginInput;

        [SerializeField]
        private InputField m_passwordInput;

        [SerializeField]
        private Button m_okButton;

        [SerializeField]
        private Button m_cancelButton;

        private IEnumerator m_coSelect;

        private void Awake()
        {
            m_okButton.interactable = false;
            m_cancelButton.interactable = true;

            m_okButton.onClick.AddListener(OnOkClick);
            m_cancelButton.onClick.AddListener(OnCancelClick);
            m_loginInput.onValueChanged.AddListener(OnLoginValueChanged);
            m_loginInput.onEndEdit.AddListener(OnLoginEndEdit);
            
            m_passwordInput.onValueChanged.AddListener(OnPasswordValueChanged);
        }

        private void OnEnable()
        {
            m_coSelect = CoSelect();
            StartCoroutine(m_coSelect);
        }

        private IEnumerator CoSelect()
        {
            yield return new WaitForEndOfFrame();

            IndependentSelectable.Select(m_loginInput.gameObject);
            InputFieldWithVirtualKeyboard.ActivateInputField(m_loginInput);

            m_coSelect = null;
        }

        private void OnDisable()
        {
            if(m_coSelect != null)
            {
                StopCoroutine(m_coSelect);
                m_coSelect = null;
            }
        }

        private void OnDestroy()
        {
            if(m_okButton != null)
            {
                m_okButton.onClick.RemoveListener(OnOkClick);
            }
            if(m_cancelButton != null)
            {
                m_cancelButton.onClick.RemoveListener(OnCancelClick);
            }
            if(m_loginInput != null)
            {
                m_loginInput.onValueChanged.RemoveListener(OnLoginValueChanged);
                m_loginInput.onEndEdit.RemoveListener(OnLoginEndEdit);
            }
            if(m_passwordInput != null)
            {
                m_passwordInput.onValueChanged.RemoveListener(OnPasswordValueChanged);
            }   
        }

        private void OnLoginValueChanged(string value)
        {
            m_okButton.interactable = !string.IsNullOrEmpty(value);
        }

        private void OnPasswordValueChanged(string value)
        {

        }

        private void OnOkClick()
        {
            if (Login != null)
            {
                Login(m_loginInput.text, m_passwordInput.text);
            }

            m_loginInput.text = string.Empty;
            m_passwordInput.text = string.Empty;
        }

        private void OnCancelClick()
        {
            m_loginInput.text = string.Empty;
            m_passwordInput.text = string.Empty;

            if(LoginCancel != null)
            {
                LoginCancel();
            }
        }

        private void OnLoginEndEdit(string value)
        {
            IndependentSelectable.Select(m_okButton.gameObject);
        }
    }
}


