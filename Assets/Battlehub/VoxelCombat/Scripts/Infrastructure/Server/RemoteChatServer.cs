using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class RemoteChatServer : MonoBehaviour, IChatServer
    {
        public bool IsConnected
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsConnectionStateChanging
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public event ServerEventHandler<ValueChangedArgs<bool>> ConnectionStateChanged;
        public event ServerEventHandler ConnectionStateChanging;
        public event ServerEventHandler<ChatMessage> MessageReceived;

        public void CancelRequests()
        {
            throw new NotImplementedException();
        }

        public void Connect()
        {
            throw new NotImplementedException();
        }

        public void Disconnect()
        {
            throw new NotImplementedException();
        }

        public bool HasError(Error error)
        {
            throw new NotImplementedException();
        }

        public void RegisterClient(Guid clientId, ServerEventHandler callback)
        {
            throw new NotImplementedException();
        }

        public void Send(Guid clientId, ChatMessage message)
        {
            throw new NotImplementedException();
        }

        public void UnregisterClient(Guid clientId, ServerEventHandler callback)
        {
            throw new NotImplementedException();
        }
    }
}