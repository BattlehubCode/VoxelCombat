using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class LoginMenu4Players : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button m_connectButton;

        [SerializeField]
        private GameObject m_p01Panel;

        [SerializeField]
        private GameObject m_p23Panel;

        [SerializeField]
        private GameObject m_p0Panel;

        [SerializeField]
        private GameObject m_p1Panel;

        [SerializeField]
        private GameObject m_p2Panel;

        [SerializeField]
        private GameObject m_p3Panel;


        [SerializeField]
        private LoginMenu[] m_playerMenu;
        private Transform[] m_slots;

        private IVoxelInputManager m_inputManager;
        private INotification m_notification;
        private INavigation m_navigation;
        private IGameServer m_remoteGameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;

        private int m_readyToGo;

        private void Awake()
        {
            m_inputManager = Dependencies.InputManager;
            m_notification = Dependencies.Notification;
            m_navigation = Dependencies.Navigation;
            m_gSettings = Dependencies.Settings;
            m_progress = Dependencies.Progress;
            m_remoteGameServer = Dependencies.RemoteGameServer;
          
            m_slots = new[]{ m_p0Panel.transform, m_p1Panel.transform, m_p2Panel.transform, m_p3Panel.transform };

            m_connectButton.onClick.AddListener(OnConnectButtonClick);
        }


        private void Start()
        {
            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].Go += OnGo;
                m_playerMenu[i].CancelGo += OnCancelGo;
                m_playerMenu[i].LoggedIn += OnLoggedIn;
                m_playerMenu[i].LoggedOff += OnLoggedOff;
                m_playerMenu[i].Disabled += OnPlayerMenuDisabled;
            }
        }

        private void OnEnable()
        {
            m_remoteGameServer.ConnectionStateChanging += OnConnectionStateChanging;
            m_remoteGameServer.ConnectionStateChanged += OnConnectionStateChanged;
            m_remoteGameServer.LoggedOff += OnPlayersLoggedOff;

            m_inputManager.IsInInitializationState = true;

            m_root.SetActive(true);

            HandleDevicesChange();

            if (m_remoteGameServer.IsConnectionStateChanging)
            {
                m_progress.IsVisible = true;
            }
            else
            {
                GetPlayers();
            }

            if (m_inputManager != null)
            {
                m_inputManager.DeviceEnabled += OnDeviceEnabled;
                m_inputManager.DeviceDisabled += OnDeviceDisabled;
            }

            m_connectButton.gameObject.SetActive(m_inputManager.DeviceCount > 0);
        }

        private void OnDisable()
        {
            if(m_notification != null)
            {
                m_notification.Close();
            }
            if(m_connectButton != null)
            {
                m_connectButton.gameObject.SetActive(false);
            }
            if (m_remoteGameServer != null)
            {
                m_remoteGameServer.ConnectionStateChanging -= OnConnectionStateChanging;
                m_remoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
                m_remoteGameServer.LoggedOff -= OnPlayersLoggedOff;
            }

            if (m_root != null)
            {
                m_root.SetActive(false);
            }

            if (m_inputManager != null)
            {
                m_inputManager.IsInInitializationState = false;
                m_inputManager.DeviceEnabled -= OnDeviceEnabled;
                m_inputManager.DeviceDisabled -= OnDeviceDisabled;
            }
   
        }

        private void OnDestroy()
        {
            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                if (m_playerMenu[i] != null)
                {
                    m_playerMenu[i].Go -= OnGo;
                    m_playerMenu[i].CancelGo -= OnCancelGo;
                    m_playerMenu[i].LoggedIn -= OnLoggedIn;
                    m_playerMenu[i].LoggedOff -= OnLoggedOff;
                    m_playerMenu[i].Disabled -= OnPlayerMenuDisabled;
                }
            }

            if (m_connectButton != null)
            {
                m_connectButton.onClick.RemoveListener(OnConnectButtonClick);
            }
        }

        private void OnConnectButtonClick()
        {
            if(m_remoteGameServer.IsConnected)
            {
                m_remoteGameServer.Disconnect();
            }
            else
            {
                m_remoteGameServer.Connect();
            }
        }

        private void OnConnectionStateChanging(Error error)
        {
            m_progress.IsVisible = true;
        }

        private void OnConnectionStateChanged(Error error, ValueChangedArgs<bool> args)
        {
            m_progress.IsVisible = false;

            for(int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].Player = null;
            }

            if (args.NewValue)
            {
                if (m_remoteGameServer.HasError(error))
                {
                    m_notification.ShowError(error);
                }
                else
                {
                    GetPlayers();
                }
            }
            else
            {
                if (m_remoteGameServer.HasError(error))
                {
                    m_notification.ShowError(error);
                }
                else
                {
                    m_notification.ShowError("You are offline. Server is not connected.", m_connectButton.gameObject);

                    GetPlayers();
                }
            }
        }

        private void OnPlayersLoggedOff(Error error, Guid[] playerIds)
        {
            for(int i = 0; i < playerIds.Length; ++i)
            {
                for(int j = 0; j  < m_playerMenu.Length; ++j)
                {
                    LoginMenu loginMenu = m_playerMenu[j];
                    if(loginMenu.Player != null && loginMenu.Player.Id == playerIds[i])
                    {
                        loginMenu.Player = null;
                    }
                }
            }
        }

        private void GetPlayers()
        {
            m_progress.IsVisible = true;

            m_readyToGo = 0;

            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].LocalPlayerIndex = i;
            }

            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].Player = null;
            }

            IGameServer gameServer = Dependencies.GameServer;
            gameServer.GetPlayers(m_gSettings.ClientId, (error, players) =>
            {
                if (gameServer.HasError(error))
                {
                    m_progress.IsVisible = false;
                    m_notification.ShowError(error);
                    return;
                }

                HandleDevicesChange();

                int playersCount = Mathf.Min(m_inputManager.DeviceCount, players.Length);
                for (int i = 0; i < playersCount; ++i)
                {
                    m_playerMenu[i].Player = players[i];
                }

                for(int i = 0; i < m_inputManager.DeviceCount; ++i)
                {
                    m_playerMenu[i].IsVirtualKeyboardEnabled = !m_inputManager.IsKeyboardAndMouse(i);
                }

                if (m_inputManager.DeviceCount < players.Length)
                {
                    List<Guid> logoffPlayers = new List<Guid>();
                    for (int i = m_inputManager.DeviceCount; i < players.Length; ++i)
                    {
                        logoffPlayers.Add(players[i].Id);
                    }

                    gameServer.Logoff(m_gSettings.ClientId, logoffPlayers.ToArray(), (error2, playerIds) =>
                    {
                        m_progress.IsVisible = false;
                        if (gameServer.HasError(error2))
                        {
                            m_notification.ShowError(error2);
                            return;
                        }
                    });
                }
                else
                {
                    m_progress.IsVisible = false;
                }

                for (int i = players.Length; i < m_inputManager.DeviceCount; ++i)
                {
                    m_playerMenu[i].TryAutoLogin();
                }
            });
        }

        private void OnDeviceEnabled(int index)
        {
            m_connectButton.gameObject.SetActive(m_inputManager.DeviceCount > 0);

            HandleDevicesChange();
            m_playerMenu[index].LocalPlayerIndex = index; 
            m_playerMenu[index].IsVirtualKeyboardEnabled = !m_inputManager.IsKeyboardAndMouse(index);

            if(!m_remoteGameServer.IsConnectionStateChanging && !m_progress.IsVisible)
            {
                m_playerMenu[index].TryAutoLogin();
            }
        }

        private void OnDeviceDisabled(int index)
        {
            m_connectButton.gameObject.SetActive(m_inputManager.DeviceCount > 0);

            Player player = m_playerMenu[index].Player;
            m_playerMenu[index].gameObject.SetActive(false);

            if (player != null)
            {
                IProgressIndicator progress = m_progress.GetChild(index);
                progress.IsVisible = true;

                IGameServer gameServer = Dependencies.GameServer;
                gameServer.Logoff(m_gSettings.ClientId, player.Id, (error, playerId) =>
                {
                    progress.IsVisible = false;
                    if (gameServer.HasError(error))
                    {
                        m_notification.ShowError(error);
                        return;
                    }

                    SetDisabledAsLast(index);
                    TryNavigateToMainMenu();
                });
            }
            else
            {
                SetDisabledAsLast(index);
                TryNavigateToMainMenu();
            }

        }

        private void SetDisabledAsLast(int index)
        {
            for (int i = index + 1; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].Root.SetParent(m_slots[i - 1], false);
            }

            m_playerMenu[index].Root.SetParent(m_slots[m_slots.Length - 1], false);
            m_playerMenu = m_playerMenu.Where(pm => pm != m_playerMenu[index]).Union(new[] { m_playerMenu[index] }).ToArray();

            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].LocalPlayerIndex = i;
            }

            HandleDevicesChange();
        }

        private void OnLoggedIn(LoginMenu sender, Player player)
        {
            TryNavigateToMainMenu();
        }

        private void OnLoggedOff(LoginMenu sender, Player loggedOffPlayer)
        {
            LoginMenu menu = m_playerMenu.Where(m => m.Player != null && m.Player.Id == loggedOffPlayer.Id).FirstOrDefault();
            Debug.Assert(menu != null);
            menu.Player = null;
        }

        private void OnPlayerMenuDisabled(LoginMenu sender)
        {
            m_inputManager.DeactivateDevice(sender.LocalPlayerIndex);
        }

        private void OnGo(LoginMenu sender)
        {
            m_readyToGo++;
            TryNavigateToMainMenu();
        }

        private void OnCancelGo(LoginMenu sender)
        {
            m_readyToGo--;
        }

        private void TryNavigateToMainMenu()
        {
            if (m_playerMenu.All(p => !p.IsInProgress))
            {
                if (m_playerMenu.Where(pm => pm.isActiveAndEnabled).Count() == m_readyToGo && m_readyToGo > 0)
                {
                    m_navigation.Navigate("MainMenu");
                }
            }
        }


        private void HandleDevicesChange()
        {
            m_p01Panel.SetActive(m_inputManager.DeviceCount > 0);
            m_p23Panel.SetActive(m_inputManager.DeviceCount > 2);

            m_p0Panel.SetActive (m_inputManager.DeviceCount > 0);
            m_p1Panel.SetActive(m_inputManager.DeviceCount > 1);
            m_p2Panel.SetActive(m_inputManager.DeviceCount > 2);
            m_p3Panel.SetActive(m_inputManager.DeviceCount > 3);

            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].gameObject.SetActive(i < m_inputManager.DeviceCount);
            }
        }


    }

}
