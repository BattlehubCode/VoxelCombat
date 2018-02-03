using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class ConnectionButton : MonoBehaviour
    {
        [SerializeField]
        private Sprite m_connectedStateSprite;

        [SerializeField]
        private Sprite m_disconnectedStateSprite;

        [SerializeField]
        private Button m_button;

        private IGameServer m_remoteGameServer;
        private INotification m_notification;

        private void Start()
        {
            m_notification = Dependencies.Notification;
            m_remoteGameServer = Dependencies.RemoteGameServer;
            UpdateButtonState();
            m_remoteGameServer.ConnectionStateChanging += OnConnectionStateChanging;
            m_remoteGameServer.ConnectionStateChanged += OnConnectionStateChanged;
        }

        private void OnDestroy()
        {
            if(m_remoteGameServer != null)
            {
                m_remoteGameServer.ConnectionStateChanging -= OnConnectionStateChanging;
                m_remoteGameServer.ConnectionStateChanged -= OnConnectionStateChanged;
            }
        }

        private void OnConnectionStateChanging(Error error)
        {
            m_notification.Close();
            UpdateButtonState();
        }

        private void OnConnectionStateChanged(Error error, ValueChangedArgs<bool> args)
        {
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if(m_remoteGameServer.IsConnectionStateChanging)
            {
                Text text = m_button.GetComponentInChildren<Text>();
                if (text != null)
                {
                    if (m_remoteGameServer.IsConnected)
                    {
                        text.text = "Disconnecting...";
                    }
                    else
                    {
                        text.text = "Connecting...";
                    }
                }
            }
            else if(m_remoteGameServer.IsConnected)
            {
                Image image = m_button.GetComponent<Image>();
                if(image != null)
                {
                    image.sprite = m_connectedStateSprite;
                }

                Text text = m_button.GetComponentInChildren<Text>();
                if(text != null)
                {
                    text.text = "Online";
                }
            }
            else
            {
                Image image = m_button.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = m_disconnectedStateSprite;
                }
                Text text = m_button.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = "Offline";
                }
            }
        }
       
    }

}
