using log4net;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Web;
using System.Web.WebSockets;

namespace Battlehub.VoxelCombat
{
    /// <summary>
    /// Summary description for GameServer
    /// </summary>
    public class GameServer : WebSocketHandler, IHttpHandler
    {
        protected override void RegisterConnection(ILowProtocol protocol)
        {
            GameServerContainer.Instance.RegisterConnection(protocol);   
        }

        protected override void UnregisterConnection(ILowProtocol socket)
        {
            GameServerContainer.Instance.UnregisterConnection(socket);
        }

        public void ProcessRequest(HttpContext context)
        {
            //Checks if the query is WebSocket request. 
            if (context.IsWebSocketRequest)
            {
                //If yes, we attach the asynchronous handler.
                context.AcceptWebSocketRequest(WebSocketRequestHandler);
            }
        }

        public bool IsReusable { get { return false; } }

    }
}