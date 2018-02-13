using System;
using UnityEngine;
namespace Battlehub.VoxelCombat
{
    public class BaseMenuBehaviour : MonoBehaviour
    {
        private IGlobalSettings m_gSettings;
        private IGameServer m_remoteGameServer;
        private IProgressIndicator m_progress;
        private INotification m_notification;
        private INavigation m_navigation;

        [SerializeField]
        private ConnectionButton m_connectionButton;

        protected virtual IGameServer GameServer
        {
            get { return Dependencies.GameServer; }
        }

        protected virtual void Awake()
        {
            if (m_connectionButton == null)
            {
                m_connectionButton = FindObjectOfType<ConnectionButton>();
            }

            m_gSettings = Dependencies.Settings;
            m_remoteGameServer = Dependencies.GameServer;
            m_progress = Dependencies.Progress;
            m_notification = Dependencies.Notification;
            m_navigation = Dependencies.Navigation;
        }

        protected virtual void OnEnable()
        {
            m_remoteGameServer.ConnectionStateChanging += OnConnectionStateChanging;
            m_remoteGameServer.ConnectionStateChanged += OnConnectionStateChanged;
            m_remoteGameServer.LoggedOff += OnPlayersLoggedOff;
        }


        protected virtual void OnDisable()
        {
            if (m_remoteGameServer != null)
            {
                m_remoteGameServer.ConnectionStateChanging -= OnConnectionStateChanging;
                m_remoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
                m_remoteGameServer.LoggedOff -= OnPlayersLoggedOff;
            }
        }

        protected virtual void OnDestroy()
        {

        }

        private void OnConnectionStateChanging(Error error)
        {
            m_progress.IsVisible = true;
        }


        private void OnPlayersLoggedOff(Error error, Guid[] players)
        {
            if (m_remoteGameServer.IsConnected)
            {
                foreach (Guid player in players)
                {
                    if(m_remoteGameServer.IsLocal(m_gSettings.ClientId, player))
                    {
                        m_notification.ShowError("Player was logged off");

                        Dependencies.RemoteGameServer.CancelRequests();
                        Dependencies.LocalGameServer.CancelRequests();

                        m_navigation.ClearHistory();
                        m_navigation.Navigate("LoginMenu4Players");

                        break;
                    }
                }
            }
        }

        protected virtual void OnConnectionStateChanged(Error error, ValueChangedArgs<bool> args)
        {
            m_progress.IsVisible = false;
            if (args.NewValue)
            {
                if (m_remoteGameServer.HasError(error))
                {
                    m_notification.ShowError(error);
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
                    m_notification.ShowError("You are offline. Server is not connected.", m_connectionButton.gameObject);
                }
            }

            if(args.NewValue != args.OldValue)
            {
                Dependencies.RemoteGameServer.CancelRequests();
                Dependencies.LocalGameServer.CancelRequests();

                m_navigation.ClearHistory();
                m_navigation.Navigate("LoginMenu4Players");
            }
        }
    }
}

