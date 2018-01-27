using System;
using System.Collections.Generic;
using System.Threading;
using System.Web;

namespace Battlehub.VoxelCombat
{
    public class MatchServer : WebSocketHandler, IHttpHandler
    {
        private Guid m_roomId;

        private static readonly Dictionary<Guid, MatchServerContainer> m_containers = new Dictionary<Guid, MatchServerContainer>();
        private static readonly Dictionary<Guid, DateTime> m_lastCheckedTime = new Dictionary<Guid, DateTime>();

        static MatchServer()
        {
            Thread gcThread = new Thread(GCThread);
            gcThread.Start();
        }

        private static void GCThread()
        {
            const int timeout = 30;
            Thread.Sleep(timeout * 1000);

            lock(m_containers)
            {
                List<Guid> removeContainers = null;
                foreach(KeyValuePair<Guid, DateTime> kvp in m_lastCheckedTime)
                {
                    DateTime checkedTime = kvp.Value;
                    if(DateTime.UtcNow > checkedTime.AddSeconds(timeout))
                    {
                        if(removeContainers == null)
                        {
                            removeContainers = new List<Guid>();
                        }
                        removeContainers.Add(kvp.Key);
                    }
                }

                if(removeContainers != null)
                {
                    for(int i = 0; i < removeContainers.Count; ++i)
                    {
                        Guid roomId = removeContainers[i];

                        MatchServerContainer container = m_containers[roomId];
                        if(container.ConnectionsCount == 0)
                        {
                            container.Stop();
                            m_containers.Remove(roomId);
                            m_lastCheckedTime.Remove(roomId);
                        }
                        else
                        {
                            m_lastCheckedTime[roomId] = DateTime.UtcNow;
                        }
                    }
                }
            }
        }

        protected override void RegisterConnection(ILowProtocol protocol)
        {
            lock(m_containers)
            {
                MatchServerContainer container;
                if(m_containers.TryGetValue(m_roomId, out container))
                {
                    container.RegisterConnection(protocol);
                }
            }
        }

        protected override void UnregisterConnection(ILowProtocol protocol)
        {
            lock (m_containers)
            {
                MatchServerContainer container;
                if (m_containers.TryGetValue(m_roomId, out container))
                {
                    container.UnregisterConnection(protocol);
                }
            }
        }

        public void ProcessRequest(HttpContext context)
        {
            lock (m_containers)
            {
                string roomIdStr = context.Request.QueryString["roomId"];
                string identity = context.Request.QueryString["identity"];
                string cmd = context.Request.QueryString["cmd"];
                Guid roomId;
                if (Guid.TryParse(roomIdStr, out roomId))
                {
                    m_roomId = roomId;

                    if (!string.IsNullOrEmpty(cmd))
                    {
                        Guid id;
                        if (!Guid.TryParse(identity, out id) || id != ServerContainer.ServerIdentity)
                        {
                            context.Response.StatusCode = 404;
                            return;
                        }

                        if (cmd == "create")
                        {
                            if (!m_containers.ContainsKey(roomId))
                            {
                                MatchServerContainer container = new MatchServerContainer();
                                container.Run();

                                m_containers.Add(roomId, container);
                                m_lastCheckedTime.Add(roomId, DateTime.UtcNow);
                            }
                        }
                    }

                    if(!m_containers.ContainsKey(roomId))
                    {
                        context.Response.StatusCode = 404;
                        return;
                    }
                }
                else
                {
                    context.Response.StatusCode = 404;
                    return;
                }
            }

            if (context.IsWebSocketRequest)
            {
                context.AcceptWebSocketRequest(WebSocketRequestHandler);
            }
        }

        public bool IsReusable { get { return false; } }        
    }
}