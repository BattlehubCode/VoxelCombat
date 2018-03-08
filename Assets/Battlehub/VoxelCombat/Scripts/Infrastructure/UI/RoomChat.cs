
using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class RoomChat : MonoBehaviour
    {
        [SerializeField]
        private ChatControl m_chatControl;

        private IGlobalSettings m_gSettings;
        private IGameServer m_gameServer;
        private INotification m_notification;
        private IVoxelGame m_gameState;

        private int m_localPlayerIndex = 0;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set { m_localPlayerIndex = value; }
        }

        private void Awake()
        {
            m_gSettings = Dependencies.Settings;
            m_gameServer = Dependencies.GameServer;
            m_notification = Dependencies.Notification;
            m_gameState = Dependencies.GameState;
        }

        private void OnEnable()
        {
            m_gameServer.ChatMessage += OnChatMessage;
            m_chatControl.Message += OnChatControlMessage;
        }

        private void OnDisable()
        {
            if(m_gameServer != null)
            {
                m_gameServer.ChatMessage -= OnChatMessage;
            }

            if(m_chatControl != null)
            {
                m_chatControl.Message -= OnChatControlMessage;
            }
        }

        private void OnChatControlMessage(string message)
        {
            Guid senderId = m_gameState.GetLocalPlayerId(LocalPlayerIndex);
      
            m_gameServer.SendMessage(m_gSettings.ClientId, new ChatMessage(senderId, message, null), (error, messageId) =>
            {
                int playerIndex = m_gameState.GetPlayerIndex(senderId);
                Player player = m_gameState.GetPlayer(playerIndex);
                m_chatControl.Echo(player.Name, message, true);
            });
        }

        private void OnChatMessage(Error error, ChatMessage payload)
        {
            if(m_gameServer.HasError(error))
            {
                m_notification.ShowError(error);
                return;
            }

            int playerIndex = m_gameState.GetPlayerIndex(payload.SenderId);
            Player player = m_gameState.GetPlayer(playerIndex);
            m_chatControl.Echo(player.Name, payload.Message, false);
        }
    }
}


