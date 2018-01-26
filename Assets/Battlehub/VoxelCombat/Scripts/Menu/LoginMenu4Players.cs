using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class LoginMenu4Players : MonoBehaviour
    {
        [SerializeField]
        private GameObject m_root;

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
        private IGameServer m_gameServer;
        private IGlobalSettings m_gSettings;
        private IProgressIndicator m_progress;

        private int m_readyToGo;

        private void Awake()
        {
            m_inputManager = Dependencies.InputManager;
            m_notification = Dependencies.Notification;
            m_navigation = Dependencies.Navigation;
            m_gameServer = Dependencies.GameServer;
            m_gSettings = Dependencies.Settings;
            m_progress = Dependencies.Progress;
            m_gameServer.ConnectionStateChanged += OnConnectionStateChanged;
            if(!m_gameServer.IsConnected)
            {
                m_progress.IsVisible = true;
            }
            else
            {
                GetPlayers();
            }
            m_slots = new[]{ m_p0Panel.transform, m_p1Panel.transform, m_p2Panel.transform, m_p3Panel.transform };
        }

        private void OnConnectionStateChanged(Error error, bool connected)
        {
            m_progress.IsVisible = false;

            if (connected)
            {
                if (m_gameServer.HasError(error))
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
                if(m_gameServer.HasError(error))
                {
                    m_notification.ShowError(error);
                }
                else
                {
                    m_notification.ShowError("Connection lost");
                }
            }
        }

        private void Start()
        {
            for(int i = 0; i < m_playerMenu.Length; ++i)
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
            m_root.SetActive(true);

            if(m_gameServer.IsConnected)
            {
                GetPlayers();
            }
        }

        private void GetPlayers()
        {
            m_progress.IsVisible = true;

            m_inputManager.IsInInitializationState = true;

            m_readyToGo = 0;

            for (int i = 0; i < m_playerMenu.Length; ++i)
            {
                m_playerMenu[i].LocalPlayerIndex = i;
            }

            m_gameServer.GetPlayers(m_gSettings.ClientId, (error, players) =>
            {
                if (m_gameServer.HasError(error))
                {
                    m_progress.IsVisible = false;
                    m_notification.ShowError(error);
                    return;
                }

                m_inputManager.DeviceEnabled += OnDeviceEnabled;
                m_inputManager.DeviceDisabled += OnDeviceDisabled;

                HandleDevicesChange();

                int playersCount = Mathf.Min(m_inputManager.DeviceCount, players.Length);
                for (int i = 0; i < playersCount; ++i)
                {
                    m_playerMenu[i].Player = players[i];
                }

                if (m_inputManager.DeviceCount < players.Length)
                {
                    List<Guid> logoffPlayers = new List<Guid>();
                    for (int i = m_inputManager.DeviceCount; i < players.Length; ++i)
                    {
                        logoffPlayers.Add(players[i].Id);
                    }

                    m_gameServer.Logoff(m_gSettings.ClientId, logoffPlayers.ToArray(), (error2, playerIds) =>
                    {
                        m_progress.IsVisible = false;
                        if (m_gameServer.HasError(error2))
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
            });
        }

        private void OnDisable()
        {
            if(m_root != null)
            {
                m_root.SetActive(false);
            }

            if(m_inputManager != null)
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
                if(m_playerMenu[i] != null)
                {
                    m_playerMenu[i].Go -= OnGo;
                    m_playerMenu[i].CancelGo -= OnCancelGo;
                    m_playerMenu[i].LoggedIn -= OnLoggedIn;
                    m_playerMenu[i].LoggedOff -= OnLoggedOff;
                    m_playerMenu[i].Disabled -= OnPlayerMenuDisabled;
                }
            }

            if(m_gameServer != null)
            {
                m_gameServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        private void OnDeviceEnabled(int index)
        {
            HandleDevicesChange();
            m_playerMenu[index].LocalPlayerIndex = index;
            m_playerMenu[index].IsVirtualKeyboardEnabled = !m_inputManager.IsKeyboardAndMouse(index);
        }

        private void OnDeviceDisabled(int index)
        {
            Player player = m_playerMenu[index].Player;
            m_playerMenu[index].gameObject.SetActive(false);

            if (player != null)
            {
                IProgressIndicator progress = m_progress.GetChild(index);
                progress.IsVisible = true;

                m_gameServer.Logoff(m_gSettings.ClientId, player.Id, (error, playerId) =>
                {
                    progress.IsVisible = false;
                    if (m_gameServer.HasError(error))
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

            m_p0Panel.SetActive(m_inputManager.DeviceCount > 0);
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
