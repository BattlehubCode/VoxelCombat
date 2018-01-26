using System;

namespace Battlehub.VoxelCombat
{
    public class ChatServerImpl : IChatServer
    {
        public event ServerEventHandler<ChatMessage> MessageReceived;

        public void Send(Guid clientId, ChatMessage message)
        {
            MessageReceived(new Error(), message);
        }
    }

}
