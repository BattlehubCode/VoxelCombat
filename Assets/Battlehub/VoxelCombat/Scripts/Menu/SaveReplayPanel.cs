using Battlehub.UIControls;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class SaveReplayPanel : ButtonsPanel
    {
        private const int SaveIndex = 0;
        private const int CancelIndex = 1;

        [SerializeField]
        private InputField m_inputField;

        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private INotification m_notification;
        private IProgressIndicator m_progress;
        private IVoxelInputManager m_inputManager;

        private void Awake()
        {
            m_inputManager = Dependencies.InputManager;
            m_gSettings = Dependencies.Settings;
            m_gameServer = Dependencies.GameServer;
            m_notification = Dependencies.Notification;
            m_progress = Dependencies.Progress;

            m_inputField.onValueChanged.AddListener(OnInputValueChanged);
            m_inputField.onEndEdit.AddListener(OnInputEndEdit);
  
            Sequence[SaveIndex].interactable = false;
        }

        protected override void Start()
        {
            base.Start();

            GameViewport parentViewport = GetComponentInParent<GameViewport>();

            InputFieldWithVirtualKeyboard ifwk = m_inputField.GetComponent<InputFieldWithVirtualKeyboard>();
            ifwk.VirtualKeyboardEnabled = !m_inputManager.IsKeyboardAndMouse(parentViewport.LocalPlayerIndex);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(m_inputField != null)
            {
                m_inputField.onValueChanged.RemoveListener(OnInputValueChanged);
                m_inputField.onEndEdit.RemoveListener(OnInputEndEdit);
            }    
        }

        private IEnumerator m_coSelect;
        protected override void OnEnable()
        {
            base.OnEnable();

            m_coSelect = CoSelect();
            StartCoroutine(m_coSelect);
        }

#warning This is real PORN. do something with it or at least document everything 
        private IEnumerator CoSelect()
        {
            yield return new WaitForEndOfFrame();

            IndependentSelectable.Select(m_inputField.gameObject);
            InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);

            m_coSelect = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_coSelect != null)
            {
                StopCoroutine(m_coSelect);
                m_coSelect = null;
            }
        }

        protected override void SelectDefault()
        {
         //   InputFieldWithVirtualKeyboard.Select(m_inputField);
        }

        private void OnInputEndEdit(string value)
        {
            IndependentSelectable.Select(m_sequence[0]);
        }
        private void OnInputValueChanged(string value)
        {
            Sequence[SaveIndex].interactable = !string.IsNullOrEmpty(value);
        }

        protected override void OnAction(int index)
        {
            if(index == SaveIndex)
            {
                m_progress.IsVisible = true;
                m_gameServer.SaveReplay(m_gSettings.ClientId, m_inputField.text, error =>
                {
                    m_progress.IsVisible = false;
                    if(m_gameServer.HasError(error))
                    {
                        m_notification.ShowError(error);
                        return;
                    }

                    RaiseAction(SaveIndex);
                });
            }
            else
            {
                base.OnAction(index);
            }
        }
    }
}
