using UnityEngine;
namespace Battlehub.VoxelCombat
{
    public class BaseMenuBehaviour : MonoBehaviour
    {
        private IGameServer m_remoteGameServer;
        private IProgressIndicator m_progress;
        private INotification m_notification;
        private INavigation m_navigation;

        [SerializeField]
        private ConnectionButton m_connectionButton;

        protected virtual void Awake()
        {
            if (m_connectionButton == null)
            {
                m_connectionButton = FindObjectOfType<ConnectionButton>();
            }

            m_remoteGameServer = Dependencies.GameServer;
            m_progress = Dependencies.Progress;
            m_notification = Dependencies.Notification;
            m_navigation = Dependencies.Navigation;
        }

        protected virtual void OnEnable()
        {
            m_remoteGameServer.ConnectionStateChanging += OnConnectionStateChanging;
            m_remoteGameServer.ConnectionStateChanged += OnConnectionStateChanged;
        }

        protected virtual void OnDisable()
        {
            if (m_remoteGameServer != null)
            {
                m_remoteGameServer.ConnectionStateChanging -= OnConnectionStateChanging;
                m_remoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        protected virtual void OnDestroy()
        {

        }

        private void OnConnectionStateChanging(Error error)
        {
            m_progress.IsVisible = true;
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

