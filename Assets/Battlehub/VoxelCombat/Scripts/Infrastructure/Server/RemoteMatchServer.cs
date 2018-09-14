using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public class RemoteMatchServer : RemoteServer, IMatchServer
    {
        public event ServerEventHandler<bool> Paused;
        public event ServerEventHandler<RTTInfo> Ping;
        public event ServerEventHandler<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], AssignmentGroupArray[], Room> ReadyToPlayAll;
        public event ServerEventHandler<CommandsBundle> Tick;

        protected override string ServerUrl
        {
            get { return m_settings.MatchServerUrl; }
        }

        protected override void Awake()
        {
            //if (!Dependencies.RemoteGameServer.IsConnected)
            //{
            //    gameObject.SetActive(false);
            //    return;
            //}
            Deactivate();
            base.Awake();
        }

        public void Activate()
        {
            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        protected override void OnRemoteEvent(RemoteEvent evt)
        {
            switch (evt.Event)
            {
                case RemoteEvent.Evt.Tick:
                    if(Tick != null)
                    {
                        Tick(evt.Error, evt.Get<CommandsBundle>(0));
                    }
                    break;
                case RemoteEvent.Evt.Ping:
                    if(Ping != null)
                    {
                        Ping(evt.Error, evt.Get<RTTInfo>(0));
                    }
                    break;
                case RemoteEvent.Evt.Pause:
                    if (Paused != null)
                    {
                        Paused(evt.Error, evt.Get<bool>(0));
                    }
                    break;
                case RemoteEvent.Evt.ReadyToPlayAll:
                    if(ReadyToPlayAll != null)
                    {
                        ReadyToPlayAll(evt.Error, 
                            evt.Get<Player[]>(0), 
                            evt.Get<Guid[]>(1), 
                            evt.Get<VoxelAbilitiesArray[]>(2),
                            evt.Get<SerializedTaskArray[]>(3),
                            evt.Get<SerializedTaskTemplatesArray[]>(4),
                            evt.Get<AssignmentGroupArray[]>(5),
                            evt.Get<Room>(6));
                    }
                    break;
                default:
                    base.OnRemoteEvent(evt);
                    break;
            }
        }


        public void DownloadMapData(Guid clientId, ServerEventHandler<byte[]> callback)
        {
            throw new NotSupportedException();
        }

        public void DownloadMapData(Guid clientId, ServerEventHandler<MapData> callback)
        {
            RemoteCall rpc = new RemoteCall(
                 RemoteCall.Proc.DownloadMapData,
                 clientId);

            Call(rpc, (error, result) =>
            {
                byte[] mapDataBin = result.Get<byte[]>(0);
                MapData mapData = null;
                if(mapDataBin != null && !HasError(error))
                {
                    mapData = m_serializer.Deserialize<MapData>(mapDataBin);
                }

                callback(error, mapData);
            });
        }

        public void GetReplay(Guid clientId, ServerEventHandler<ReplayData, Room> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetReplay,
                clientId);

            Call(rpc, (error, result) => callback(error, result.Get<ReplayData>(0), result.Get<Room>(1)));
        }

        public void GetTaskTemplates(Guid clientId, Guid playerId, ServerEventHandler<SerializedTask[], SerializedNamedTaskLaunchInfo[]> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetTaskTemplates,
                clientId,
                RemoteArg.Create(playerId));

            Call(rpc, (error, result) => callback(error, result.Get<SerializedTask[]>(0), result.Get<SerializedNamedTaskLaunchInfo[]>(1)));
        }

        public void SaveTaskTemplate(Guid clientId, Guid playerId, SerializedTask taskTemplate, SerializedNamedTaskLaunchInfo TaskTemplateData, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.SaveTaskTemplate,
                clientId,
                RemoteArg.Create(playerId),
                RemoteArg.Create(taskTemplate),
                RemoteArg.Create(TaskTemplateData));

            Call(rpc, (error, result) => callback(error));
        }

        public void GetState(Guid clientId, ServerEventHandler<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], AssignmentGroupArray[], Room, MapRoot> callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.GetState,
                clientId);

            Call(rpc, (error, result) =>
            {
                callback(error, 
                    result.Get<Player[]>(0), 
                    result.Get<Guid[]>(1),
                    result.Get<VoxelAbilitiesArray[]>(2),
                    result.Get<SerializedTaskArray[]>(3),
                    result.Get<SerializedTaskTemplatesArray[]>(4),
                    result.Get<AssignmentGroupArray[]>(5),
                    result.Get<Room>(6),
                    result.Get<MapRoot>(7));
            });
        }

        public void Pause(Guid clientId, bool pause, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Pause,
                clientId,
                RemoteArg.Create(pause));

            Call(rpc, (error, result) => callback(error));
        }

        public void Pong(Guid clientId, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Pong,
                clientId);

            Call(rpc, (error, result) => callback(error));
        }

        public void ReadyToPlay(Guid clientId, ServerEventHandler callback)
        {
            RemoteCall rpc = new RemoteCall(
              RemoteCall.Proc.ReadyToPlay,
              clientId);

            Call(rpc, (error, result) => callback(error));
        }

        private RemoteArg<int> m_playerIdArg = new RemoteArg<int>();
        private RemoteArg<Cmd> m_cmdArg = new RemoteArg<Cmd>();
        
        public void Submit(Guid clientId, int playerIndex, Cmd cmd, ServerEventHandler<Cmd> callback)
        {
            m_playerIdArg.Value = playerIndex;
            m_cmdArg.Value = cmd;

            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Submit,
                clientId,
                m_playerIdArg,
                m_cmdArg);

            Call(rpc, (error, result) => callback(error, result.Get<Cmd>(0)));
        }

        private RemoteArg<ClientRequest> m_responseArg = new RemoteArg<ClientRequest>();
        public void SubmitResponse(Guid clientId, ClientRequest response, ServerEventHandler<ClientRequest> callback)
        {
            m_responseArg.Value = response;

            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.SubmitResponse,
                    clientId,
                    m_responseArg);

            Call(rpc, (error, result) => callback(error, result.Get<ClientRequest>(0)));
        }

    
    }
}

