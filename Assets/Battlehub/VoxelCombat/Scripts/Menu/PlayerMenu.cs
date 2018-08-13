using Battlehub.UIControls;
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
        private ButtonsPanel m_consolePanel;

        [SerializeField]
        private ButtonsPanel m_settingsPanel;

        private IVoxelGame m_gameState;
        private IVoxelInputManager m_inputManager;
        private INavigation m_navigation;
        private IConsole m_console;
        private INotification m_notification;
        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private IndependentEventSystem m_eventSystem;
        private IVirtualMouse m_virtualMouse;

        private int m_localPlayerIndex;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                m_localPlayerIndex = value;
                m_menuPanel.LocalPlayerIndex = value;
                m_consolePanel.LocalPlayerIndex = value;
                m_settingsPanel.LocalPlayerIndex = value;
                m_resultsPanel.LocalPlayerIndex = value;

                m_eventSystem = Dependencies.EventSystemManager.GetEventSystem(m_localPlayerIndex);
            }
        }

        private void Awake()
        {
            m_gSettings = Dependencies.Settings;
            m_gameState = Dependencies.GameState;
            m_inputManager = Dependencies.InputManager;
            m_navigation = Dependencies.Navigation;
            m_console = Dependencies.Console;
            m_notification = Dependencies.Notification;
            m_gameServer = Dependencies.GameServer;
            m_eventSystem = Dependencies.EventSystemManager.GetEventSystem(m_localPlayerIndex);
            
            m_gameState.Completed += OnGameCompleted;
            m_gameState.IsPausedChanged += OnIsPausedChanged;
            m_gameState.PlayerDefeated += OnPlayerDefeated;

            m_menuPanel.IsOpenedChanged += OnMenuPanelIsOpenedChanged;
            m_menuPanel.Action += OnMenuPanelAction;

            m_settingsPanel.IsOpenedChanged += OnSettingsIsOpenedChanged;
            m_settingsPanel.Action += OnSettingsPanelAction;

            m_consolePanel.IsOpenedChanged += OnConsoleIsOpenedChanged;
            m_consolePanel.Action += OnConsoleAction;

            m_resultsPanel.IsOpenedChanged += OnResultsIsOpenedChanged;
            m_resultsPanel.Action += OnResultsPanelAction;
        }

        private void Start()
        {
            m_virtualMouse = Dependencies.GameView.GetVirtualMouse(m_localPlayerIndex);
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

            if(m_settingsPanel != null)
            {
                m_settingsPanel.IsOpenedChanged -= OnSettingsIsOpenedChanged;
                m_settingsPanel.Action -= OnSettingsPanelAction;
            }

            if(m_consolePanel != null)
            {
                m_consolePanel.IsOpenedChanged -= OnConsoleIsOpenedChanged;
                m_consolePanel.Action -= OnConsoleAction;
            }

            if (m_resultsPanel != null)
            {
                m_resultsPanel.IsOpenedChanged -= OnResultsIsOpenedChanged;
                m_resultsPanel.Action -= OnResultsPanelAction;
            }
        }

        private void Update()
        {
            //if (m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            //{
            //    return;
            //}

            if (m_gameState.IsPauseStateChanging)
            {
                return;
            }

            
            if (m_inputManager.GetButtonDown(InputAction.Back, LocalPlayerIndex, false, false))
            {
                if (!m_gameState.IsContextActionInProgress(LocalPlayerIndex))
                {
                    m_gameState.IsMenuOpened(LocalPlayerIndex, !m_gameState.IsMenuOpened(LocalPlayerIndex));
                    m_menuPanel.SetIsOpened(!m_menuPanel.IsOpened);
                }
            }

            if (m_gameState.IsMenuOpened(LocalPlayerIndex))
            {
                if (m_inputManager.GetButtonDown(InputAction.B, LocalPlayerIndex, true, false))
                {
                    if (m_menuPanel.IsOpened)
                    {
                        m_gameState.IsMenuOpened(LocalPlayerIndex, false);
                    }
                    m_menuPanel.SetIsOpened(false);
                    m_consolePanel.SetIsOpened(false);
                    m_settingsPanel.SetIsOpened(false);
                    m_resultsPanel.SetIsOpened(false);
                }
            }  
        }

        private void OnGameCompleted()
        {
            m_resultsPanel.SetIsOpened(true);

            bool draw = true;
            for (int i = 0; i < m_gameState.PlayersCount; ++i)
            {
                PlayerStats stats = m_gameState.GetStats(i);
                if(stats.ControllableUnitsCount > 0 /*&& stats.IsInRoom*/)
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
                m_resultsPanel.SetIsOpened(true);
                m_resultsPanel.ResultText = "You Lose. Congratulations!";
            }
        }

        private void OnIsPausedChanged()
        {
            m_menuPanel.SetText(2, m_gameState.IsPaused ? "Resume" : "Pause");
        }

        private void OnMenuPanelIsOpenedChanged(ButtonsPanel sender)
        {
            m_notification.Close();
            m_resultsPanel.SetIsOpened(false, false);
            m_settingsPanel.SetIsOpened(false, false);
            m_consolePanel.SetIsOpened(false, false);
            UpdateIsOpenedState();
        }

        private void OnMenuPanelAction(ButtonsPanel sender, int code)
        {
            switch(code)
            {
                case 0: //console
                    m_menuPanel.SetIsOpened(false);
                    m_consolePanel.SetIsOpened(true);
                  
                    break;
                case 1: //close menu 
                    m_menuPanel.SetIsOpened(false);
                    

                    break;
                case 2: //pause
                    m_gameState.IsPaused = !m_gameState.IsPaused;
                   

                    break;
                case 3: //help
                    m_menuPanel.SetIsOpened(false);
                    m_settingsPanel.SetIsOpened(true);
                    
                    break;
                case 4: //back to menu
                    if(m_gameServer.IsConnected)
                    {
                        m_gameServer.LeaveRoom(m_gSettings.ClientId, error =>
                        {
                            if (m_gameServer.HasError(error))
                            {
                                m_notification.ShowErrorWithAction(error, () =>
                                {
                                    m_navigation.Navigate("Menu", "MainMenu", null);
                                });
                            }
                            else
                            {
                                m_navigation.Navigate("Menu", "MainMenu", null);
                            }
                        });
                    }
                    else
                    {
                        m_navigation.Navigate("Menu", "LoginMenu4Players", null);
                    }
                    break;
                case 5: //quit
                    m_console.GetChild(LocalPlayerIndex).Write("quit");
                    break;
                case 6: //map editor
                    m_console.GetChild(LocalPlayerIndex).Write("mapeditor");
                    break;
            }
        }

        private void OnResultsIsOpenedChanged(ButtonsPanel sender)
        {
            if(sender.IsOpened)
            {
                m_settingsPanel.SetIsOpened(false, false);
                m_menuPanel.SetIsOpened(false, false);
                m_consolePanel.SetIsOpened(false, false);
            }
            else
            {
                m_menuPanel.SetIsOpened(true);
            }

            UpdateIsOpenedState();
        }

        private void OnResultsPanelAction(ButtonsPanel sender, int code)
        {
            if(code == 0)
            {
                m_resultsPanel.SetIsOpened(false);
            }
        }

        private void OnConsoleIsOpenedChanged(ButtonsPanel sender)
        {
            if (sender.IsOpened)
            {
                m_resultsPanel.SetIsOpened(false, false);
                m_menuPanel.SetIsOpened(false, false);
                m_settingsPanel.SetIsOpened(false, false);
            }
            else
            {
                m_menuPanel.SetIsOpened(true);
            }

            UpdateIsOpenedState();
        }

        private void OnConsoleAction(ButtonsPanel sender, int code)
        {
            m_consolePanel.SetIsOpened(false);
        }

        private void OnSettingsIsOpenedChanged(ButtonsPanel sender)
        {
            if(sender.IsOpened)
            {
                m_resultsPanel.SetIsOpened(false, false);
                m_menuPanel.SetIsOpened(false, false);
                m_consolePanel.SetIsOpened(false, false);
            }
            else
            {
                m_menuPanel.SetIsOpened(true);
            }

            UpdateIsOpenedState();
        }

        private void OnSettingsPanelAction(ButtonsPanel sender, int code)
        {
            m_settingsPanel.SetIsOpened(false);
        }

        private void UpdateIsOpenedState()
        {
            bool isOpened = m_settingsPanel.IsOpened || m_resultsPanel.IsOpened || m_menuPanel.IsOpened || m_consolePanel.IsOpened;
            if(!isOpened)
            {
                m_eventSystem.SetSelectedGameObjectOnLateUpdate(null);
                m_virtualMouse.RestoreVirtualMouse();
            }
            else
            {
                m_virtualMouse.BackupVirtualMouse();
                m_virtualMouse.IsVirtualMouseEnabled = m_inputManager.IsKeyboardAndMouse(m_localPlayerIndex);
                m_virtualMouse.IsVirtualMouseCursorVisible = m_inputManager.IsKeyboardAndMouse(m_localPlayerIndex);
            }

            m_gameState.IsMenuOpened(LocalPlayerIndex, isOpened);
        }

    }
}

