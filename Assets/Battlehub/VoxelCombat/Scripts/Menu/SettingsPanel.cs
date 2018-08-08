using Battlehub.UIControls;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class SettingsPanel : ButtonsPanel
    {
        private IGlobalSettings m_settings;
        private const int DebugModeAction = 0;
        private const int SaveReplayAction = 1;
        [SerializeField]
        private ButtonsPanel m_saveReplayPanel;
        private IVoxelGame m_gameState;
        private IGameServer m_gameServer;


        public override void SetIsOpened(bool value, bool raiseEvent = true)
        {
            base.SetIsOpened(value, raiseEvent);

            m_gameState = Dependencies.GameState;
            m_gameServer = Dependencies.GameServer;
            Sequence[SaveReplayAction].interactable = !m_gameState.IsReplay && m_gameServer.IsConnected;
            Sequence[SaveReplayAction].gameObject.SetActive(!m_gameState.IsReplay);

            m_saveReplayPanel.SetIsOpened(false, raiseEvent);
        }

        protected override void Awake()
        {
            base.Awake();
            m_settings = Dependencies.Settings;
            m_gameServer = Dependencies.GameServer;
            m_gameState = Dependencies.GameState;
            m_saveReplayPanel.Action += OnSaveReplayAction;
            UpdateButtonState();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
   
            if (m_saveReplayPanel != null)
            {
                m_saveReplayPanel.Action -= OnSaveReplayAction;
            }
        }

        protected override void OnAction(int index)
        {
            if(index == DebugModeAction)
            {
                m_settings.DebugMode = !m_settings.DebugMode;
            }
            else if(index == SaveReplayAction)
            {
                m_saveReplayPanel.LocalPlayerIndex = LocalPlayerIndex;
                m_saveReplayPanel.SetIsOpened(true);
                gameObject.SetActive(false);
            }
            else
            {
                base.OnAction(index);
            }
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            Text text = m_sequence[0].GetComponentInChildren<Text>();
          
            if (m_settings.DebugMode)
            {
                text.text = "Debug Mode On";
            }
            else
            {
                text.text = "Debug Mode Off";
            }
        }

        private void OnSaveReplayAction(ButtonsPanel sender, int code)
        {
            m_saveReplayPanel.SetIsOpened(false);
            gameObject.SetActive(true);

            IndependentSelectable.Select(m_sequence[0]);
        }
    }
}
