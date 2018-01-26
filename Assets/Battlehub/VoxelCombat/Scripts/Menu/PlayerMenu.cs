using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class PlayerMenu : MonoBehaviour
    {
        [SerializeField]
        private ButtonsPanel m_menuPanel;

        [SerializeField]
        private GameResultsPanel m_resultsPanel;

        [SerializeField]
        private ButtonsPanel m_helpPanel;

        private IVoxelGame m_gameState;
        private IVoxelInputManager m_inputManager;
        private INavigation m_navigation;
        private IConsole m_console;
        private INotification m_notification;

        private int m_localPlayerIndex;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                m_localPlayerIndex = value;
                m_menuPanel.LocalPlayerIndex = value;
                m_helpPanel.LocalPlayerIndex = value;
                m_resultsPanel.LocalPlayerIndex = value;
            }
        }

        private void Awake()
        {
            m_gameState = Dependencies.GameState;
            m_inputManager = Dependencies.InputManager;
            m_navigation = Dependencies.Navigation;
            m_console = Dependencies.Console;
            m_notification = Dependencies.Notification;

            m_gameState.Completed += OnGameCompleted;
            m_gameState.IsPausedChanged += OnIsPausedChanged;
            m_gameState.PlayerDefeated += OnPlayerDefeated;

            m_menuPanel.IsOpenedChanged += OnMenuPanelIsOpenedChanged;
            m_menuPanel.Action += OnMenuPanelAction;

            m_helpPanel.IsOpenedChanged += OnHelpIsOpenedChanged;
            m_helpPanel.Action += OnHelpPanelAction;

            m_resultsPanel.IsOpenedChanged += OnResultsIsOpenedChanged;
            m_resultsPanel.Action += OnResultsPanelAction;
        }


        private void OnDestroy()
        {
            if(m_gameState != null)
            {
                m_gameState.Completed -= OnGameCompleted;
                m_gameState.IsPausedChanged -= OnIsPausedChanged;
                m_gameState.PlayerDefeated -= OnPlayerDefeated;
            }

            if(m_menuPanel != null)
            {
                m_menuPanel.IsOpenedChanged -= OnMenuPanelIsOpenedChanged;
                m_menuPanel.Action -= OnMenuPanelAction;
            }

            if(m_helpPanel != null)
            {
                m_helpPanel.IsOpenedChanged -= OnHelpIsOpenedChanged;
                m_helpPanel.Action -= OnHelpPanelAction;
            }

            if(m_resultsPanel != null)
            {
                m_resultsPanel.IsOpenedChanged -= OnResultsIsOpenedChanged;
                m_resultsPanel.Action -= OnResultsPanelAction;
            }
        }

        private void Update()
        {
            if (m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsPauseStateChanging)
            {
                return;
            }

            if /*(m_inputManager.GetButtonDown(InputAction.ToggleMenu, LocalPlayerIndex) ||*/
                (m_inputManager.GetButtonDown(InputAction.Action0, LocalPlayerIndex) ||
                 m_inputManager.GetButtonDown(InputAction.Action9, LocalPlayerIndex))
            {
                m_gameState.IsMenuOpened(LocalPlayerIndex, !m_gameState.IsMenuOpened(LocalPlayerIndex));

                m_menuPanel.IsOpened = !m_menuPanel.IsOpened;
            }
        }

        private void OnGameCompleted()
        {
            m_resultsPanel.IsOpened = true;

            bool draw = true;
            for (int i = 0; i < m_gameState.PlayersCount; ++i)
            {
                PlayerStats stats = m_gameState.GetStats(i);
                if(stats.ControllableUnitsCount > 0 /* && stats.IsInRoom*/)
                {
                    Player player = m_gameState.GetPlayer(i);
                    player.Victories++;

                    m_resultsPanel.ResultText = player.Name + " Win!";

                    draw = false;
                    break;
                }
            }

            if(draw)
            {
                m_resultsPanel.ResultText = "Nobody wins this game";
            }

        }

        private void OnPlayerDefeated(int arg)
        {
            if(m_gameState.IsCompleted)
            {
                return;
            }

            if(m_gameState.PlayerToLocalIndex(arg) == LocalPlayerIndex)
            {
                m_resultsPanel.IsOpened = true;
                m_resultsPanel.ResultText = "You Lose. Congratulations!";
            }
        }


        private void OnIsPausedChanged()
        {
            m_menuPanel.SetText(1, m_gameState.IsPaused ? "Resume" : "Pause");
        }

        private void OnMenuPanelIsOpenedChanged(ButtonsPanel sender)
        {
            m_notification.Close();
            m_resultsPanel.IsOpened = false;
            m_helpPanel.IsOpened = false;
            UpdateIsOpenedState();
        }

        private void OnMenuPanelAction(ButtonsPanel sender, int code)
        {
            switch(code)
            {
                case 0: //close menu 
                    m_menuPanel.IsOpened = false;
                    break;
                case 1: //pause
                    m_gameState.IsPaused = !m_gameState.IsPaused;
                    break;
                case 2: //help
                    m_menuPanel.IsOpened = false;
                    m_helpPanel.IsOpened = true;
                    break;
                case 3: //back to menu
                    m_navigation.Navigate("Menu", "MainMenu", null);
                    break;
                case 4: //quit
                    m_console.Write("quit");
                    break;
            }
        }


     
        private void OnResultsIsOpenedChanged(ButtonsPanel sender)
        {
            if(sender.IsOpened)
            {
                m_helpPanel.IsOpened = false;
                m_menuPanel.IsOpened = false;
            }
            else
            {
                m_menuPanel.IsOpened = true;
            }

            UpdateIsOpenedState();
        }

        private void OnResultsPanelAction(ButtonsPanel sender, int code)
        {
            if(code == 0)
            {
                m_resultsPanel.IsOpened = false;
            }
        }
     
        private void OnHelpIsOpenedChanged(ButtonsPanel sender)
        {
            if(sender.IsOpened)
            {
                m_resultsPanel.IsOpened = false;
                m_menuPanel.IsOpened = false;
            }
            else
            {
                m_menuPanel.IsOpened = true;
            }

            UpdateIsOpenedState();
        }

        private void OnHelpPanelAction(ButtonsPanel sender, int code)
        {
            m_helpPanel.IsOpened = false;
        }

        private void UpdateIsOpenedState()
        {
            m_gameState.IsMenuOpened(LocalPlayerIndex, m_helpPanel.IsOpened || m_resultsPanel.IsOpened || m_menuPanel.IsOpened);
        }

    }
}

