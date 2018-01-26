using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class RemoteChatServer : MonoBehaviour, IChatServer
    {
        public event ServerEventHandler<ChatMessage> MessageReceived;

        public void Send(Guid clientId, ChatMessage message)
        {
            throw new NotImplementedException();
        }
    }
}