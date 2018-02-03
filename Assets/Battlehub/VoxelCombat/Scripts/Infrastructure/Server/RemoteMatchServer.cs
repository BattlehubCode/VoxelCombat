using System;

namespace Battlehub.VoxelCombat
{
    public class RemoteMatchServer : RemoteServer, IMatchServer
    {
        public event ServerEventHandler<bool> Paused;
        public event ServerEventHandler<RTTInfo> Ping;
        public event ServerEventHandler<Player[], Guid[], VoxelAbilitiesArray[], Room> ReadyToPlayAll;
        public event ServerEventHandler<CommandsBundle> Tick;

        protected override string ServerUrl
        {
            get { return m_settings.MatchServerUrl; }
        }

        protected override void Awake()
        {
            if (!Dependencies.RemoteGameServer.IsConnected)
            {
                gameObject.SetActive(false);
                return;
            }

            base.Awake();
        }

        protected override void OnMessage(ILowProtocol sender, byte[] args)
        {
            RemoteEvent evt = ProtobufSerializer.Deserialize<RemoteEvent>(args);

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
                        ReadyToPlayAll(evt.Error, evt.Get<Player[]>(0), evt.Get<Guid[]>(1), evt.Get<VoxelAbilitiesArray[]>(2), evt.Get<Room>(3));
                    }
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
                    mapData = ProtobufSerializer.Deserialize<MapData>(mapDataBin);
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

        private RemoteArg<Guid> m_playerIdArg = new RemoteArg<Guid>();
        private RemoteArg<Cmd> m_cmdArg = new RemoteArg<Cmd>();
        public void Submit(Guid clientId, Guid playerId, Cmd cmd, ServerEventHandler callback)
        {
            m_playerIdArg.Value = playerId;
            m_cmdArg.Value = cmd;

            RemoteCall rpc = new RemoteCall(
                RemoteCall.Proc.Submit,
                clientId,
                m_playerIdArg,
                m_cmdArg);

            Call(rpc, (error, result) => callback(error));
        }
    }
}

