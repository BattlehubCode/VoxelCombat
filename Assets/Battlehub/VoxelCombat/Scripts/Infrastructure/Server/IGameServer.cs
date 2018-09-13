using System;
using ProtoBuf;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
   
    [ProtoContract]
    public class Error
    {
        [ProtoMember(1)]
        public int Code;
        [ProtoMember(2)]
        public string Message;
        [ProtoMember(3)]
        public Error InnerError;

        public Error()
        {
            Code = StatusCode.OK;
        }

        public Error(int code)
        {
            Code = code;
            Message = null;
        }

        public override string ToString()
        {
            if(!string.IsNullOrEmpty(Message))
            {
                if(InnerError != null)
                {
                    return string.Format("Error({0}): {1}, InnerError = [{2}]", Code, Message, InnerError.ToString());
                }
                else
                {
                    return string.Format("Error({0}): {1}", Code, Message);
                }                
            }
            else
            {
                if(InnerError != null)
                {
                    return string.Format("Error({0}): {1}, InnerError = [{2}]", Code, StatusCode.ToString(Code), InnerError.ToString());
                }
                else
                {
                    return string.Format("Error({0}): {1}", Code, StatusCode.ToString(Code));
                }
            }
        }
    }

    public enum BotType
    {
        None,
        Replay,
        Neutral,
        Easy,
        Medium,
        Hard
    }

    [ProtoContract]
    public class ByteArray
    {
        [ProtoMember(1)]
        public byte[] Bytes;

        public ByteArray()
        {
        }

        public ByteArray(byte[] bytes)
        {
            Bytes = bytes;
        }

        public static  implicit  operator byte[](ByteArray byteArray)
        {
            return byteArray.Bytes;
        }

        public static implicit operator ByteArray(byte[] bytes)
        {
            return new ByteArray(bytes);
        }
    }

    [ProtoContract]
    public class VoxelAbilitiesArray
    {
        [ProtoMember(1)]
        public VoxelAbilities[] Abilities;

        public VoxelAbilitiesArray()
        {
        }

        public VoxelAbilitiesArray(VoxelAbilities[] abilities)
        {
            Abilities = abilities;
        }

        public static implicit operator VoxelAbilities[] (VoxelAbilitiesArray abilitiesArray)
        {
            return abilitiesArray.Abilities;
        }

        public static implicit operator VoxelAbilitiesArray(VoxelAbilities[] ablilities)
        {
            return new VoxelAbilitiesArray(ablilities);
        }
    }

    [ProtoContract]
    public class SerializedTaskArray
    {
        [ProtoMember(1)]
        public SerializedTask[] Tasks;

        public SerializedTaskArray()
        {
        }

        public SerializedTaskArray(SerializedTask[] tasks)
        {
            Tasks = tasks;
        }

        public static implicit operator SerializedTask[] (SerializedTaskArray taskArray)
        {
            return taskArray.Tasks;
        }

        public static implicit operator SerializedTaskArray(SerializedTask[] serializedTasks)
        {
            return new SerializedTaskArray(serializedTasks);
        }
    }


    [ProtoContract]
    public class SerializedTaskTemplatesArray
    {
        [ProtoMember(1)]
        public SerializedNamedTaskLaunchInfo[] Templates;

        public SerializedTaskTemplatesArray()
        {
        }

        public SerializedTaskTemplatesArray(SerializedNamedTaskLaunchInfo[] templates)
        {
            Templates = templates;
        }

        public static implicit operator SerializedNamedTaskLaunchInfo[] (SerializedTaskTemplatesArray templates)
        {
            return templates.Templates;
        }

        public static implicit operator SerializedTaskTemplatesArray(SerializedNamedTaskLaunchInfo[] templates)
        {
            return new SerializedTaskTemplatesArray(templates);
        }
    }

    [ProtoContract]
    [ProtoInclude(3, typeof(RemoteArg<string>))]
    [ProtoInclude(4, typeof(RemoteArg<string[]>))]
    [ProtoInclude(5, typeof(RemoteArg<int>))]
    [ProtoInclude(6, typeof(RemoteArg<int[]>))]
    [ProtoInclude(7, typeof(RemoteArg<Guid>))]
    [ProtoInclude(8, typeof(RemoteArg<Guid[]>))]
    [ProtoInclude(9, typeof(RemoteArg<byte[]>))]
    [ProtoInclude(10, typeof(RemoteArg<ByteArray[]>))]
    [ProtoInclude(11, typeof(RemoteArg<bool>))]
    [ProtoInclude(20, typeof(RemoteArg<Player>))]
    [ProtoInclude(21, typeof(RemoteArg<Player[]>))]
    [ProtoInclude(22, typeof(RemoteArg<Room>))]
    [ProtoInclude(23, typeof(RemoteArg<Room[]>))]
    [ProtoInclude(24, typeof(RemoteArg<MapInfo>))]
    [ProtoInclude(25, typeof(RemoteArg<MapInfo[]>))]
    [ProtoInclude(26, typeof(RemoteArg<ServerStats>))]
    [ProtoInclude(27, typeof(RemoteArg<VoxelAbilitiesArray[]>))]
    [ProtoInclude(28, typeof(RemoteArg<RTTInfo>))]
    [ProtoInclude(29, typeof(RemoteArg<Cmd>))]
    [ProtoInclude(30, typeof(RemoteArg<ReplayData>))]
    [ProtoInclude(31, typeof(RemoteArg<CommandsBundle>))]
    [ProtoInclude(32, typeof(RemoteArg<ChatMessage>))]
    [ProtoInclude(33, typeof(RemoteArg<ClientRequest>))]
    [ProtoInclude(34, typeof(RemoteArg<SerializedTaskArray[]>))]
    [ProtoInclude(35, typeof(RemoteArg<SerializedTaskTemplatesArray[]>))]
    [ProtoInclude(37, typeof(RemoteArg<SerializedNamedTaskLaunchInfo>))]
    public class RemoteArg
    {
        public virtual object Value
        {
            get;
            set;
        }

        public static RemoteArg Create<T>(T value)
        {
            return new RemoteArg<T>(value);
        }
    }

    [ProtoContract]
    public class RemoteArg<T> : RemoteArg
    {
        [ProtoMember(1, IsRequired = true)]
        public T m_value;

        public override object Value
        {
            get { return m_value; }
            set { m_value = (T)value; }
        }

        public RemoteArg()
        {
        }

        public RemoteArg(T v)
        {
            m_value = v;
        }

    }

    [ProtoContract]
    public class RemoteCall
    {
        public enum Proc
        {
            RegisterClient,
            GetPlayers,
            GetPlayersByRoomId,
            GetPlayer,
            Login,
            LoginHash,
            SignUp,
            Logoff,
            LogoffMultiple,

            JoinRoom,
            LeaveRoom,
            GetRooms,
            GetRoom,
            GetRoomById,
            CreateRoom,
            DestroyRoom,
            CreateBot,
            CreateBots,
            DestroyBot,

            UploadMapData,
            GetMaps,
            DownloadMapData,

            GetReplays,
            SetReplay,
            SaveReplay,
            
            GetStats,

            SetReadyToLaunch,
            Launch,

            CreateMatch,
            GetReplay,
            GetTaskTemplates,
            SaveTaskTemplate,
            ReadyToPlay,
            Submit,
            SubmitResponse,
            Pong,
            Pause,
            IsAliveCheck,
            GetState,

            SendChatMessage,
        }

        [ProtoMember(1)]
        public Proc Procedure;

        [ProtoMember(2)]
        public Guid ClientId;

        [ProtoMember(3)]
        public RemoteArg[] Args;

        public T Get<T>(int index)
        {
            if(Args == null)
            {
                return default(T);
            }

            if (index >= Args.Length)
            {
                return default(T);
            }

            RemoteArg arg = Args[index];
            T result = (T)arg.Value;
            if (result == null && Reflection.IsArray(typeof(T)))
            {
                return (T)Activator.CreateInstance(typeof(T), new object[] { 0 });
            }
            return result;
        }

        public RemoteCall()
        {

        }

        public RemoteCall(Proc code, Guid clientId, params RemoteArg[] args)
        {
            Procedure = code;
            ClientId = clientId;
            Args = args;
        }
    }

    [ProtoContract]
    public class RemoteEvent
    {
        public enum Evt
        {
            LoggedIn,
            LoggedOff,
            JoinedRoom,
            LeftRoom,
            RoomDestroyed,
            RoomsListChanged,
            ReadyToLaunch,
            Launched,

            Tick,
            ReadyToPlayAll,
            Pause,
            Ping,
            
            ChatMessage
        }

        [ProtoMember(1)]
        public Evt Event;

        [ProtoMember(2)]
        public Error Error;

        [ProtoMember(3)]
        public RemoteArg[] Args;

        public T Get<T>(int index)
        {
            if (index >= Args.Length)
            {
                return default(T);
            }

            RemoteArg arg = Args[index];
            T result = (T)arg.Value;
            if (result == null && Reflection.IsArray(typeof(T)))
            {
                return (T)Activator.CreateInstance(typeof(T), new object[] { 0 });
            }
            return result;
        }

        public RemoteEvent()
        {
            Args = new RemoteArg[0];
        }

        public RemoteEvent(Evt evt, Error error, params RemoteArg[] args)
        {
            Event = evt;
            Error = error;
            Args = args;
        }
    }

    [ProtoContract]
    public class RemoteResult
    {
        [ProtoMember(1)]
        public Error Error;

        [ProtoMember(3)]
        public RemoteArg[] Args;

        [ProtoMember(4)]
        public byte[] Binary;

        public T Get<T>(int index)
        {
            if (index >= Args.Length)
            {
                return default(T);
            }

            RemoteArg arg = Args[index];
            T result = (T)arg.Value;
            if (result == null && Reflection.IsArray(typeof(T)))
            {
                return (T)Activator.CreateInstance(typeof(T), new object[] { 0 });
            }
            return result;
        }

        public RemoteResult()
        {
            Args = new RemoteArg[0];
            Binary = new byte[0];
        }

        public RemoteResult(Error error, params RemoteArg[] args)
        {
            Error = error;
            Args = args;
        }

        public RemoteResult(Error error)
        {
            Error = error;
        }
    }

    public interface IPersistentObject
    {
        int Id
        {
            get;
            set;
        }
    }

    [ProtoContract]
    public class Player : IPersistentObject
    {
        [ProtoMember(1)]
        public Guid Id;
        [ProtoMember(2)]
        public string Name;
        [ProtoMember(3)]
        public BotType BotType;
        [ProtoMember(4)]
        public int Victories;

        public bool IsBot
        {
            get { return BotType != BotType.None; }
        }

        public bool IsActiveBot
        {
            get { return IsBot && BotType != BotType.Neutral && BotType != BotType.Replay; }
        }

        int IPersistentObject.Id //Not Used
        {
            get;
            set;
        }
    }    

    public enum KnownVoxelTypes
    {
        //VoxelTypes of lower levels should have smaller value !!!!!!!

        GroundPreview = Ground | Preview,
        Ground = 1,

        SpawnerPreview = Spawner | Preview,
        Spawner = 50,
        
        Eatable = 100,

        BombPreview = Bomb | Preview,
        Bomb = 900,
        
        Eater = 1000,

        Preview = 1 << 24
    }

    [ProtoContract]
    public class Room
    {
        [ProtoMember(1)]
        public Guid Id;
        [ProtoMember(2)]
        public List<Guid> Players;
        [ProtoMember(3)]
        public MapInfo MapInfo;
        [ProtoMember(4)]
        public GameMode Mode;
        [ProtoMember(5)]
        public List<Guid> ReadyToLaunchPlayers;
        public bool IsReadyToLauch
        {
            get
            {
                if(Players == null || ReadyToLaunchPlayers == null)
                {
                    return false;
                }

                if(Players.Count < 2)
                {
                    return false;
                }

                if(Players.Count != ReadyToLaunchPlayers.Count)
                {
                    return false;
                }

                for(int i = 0; i < Players.Count; ++i)
                {
                    if(!ReadyToLaunchPlayers.Contains(Players[i]))
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        [ProtoMember(6)]
        public bool IsLaunched;
        [ProtoMember(7)]
        public Guid CreatorPlayerId;

        [ProtoIgnore]
        public Guid CreatorClientId;

    }

    [Flags]
    public enum GameMode
    {
        FreeForAll = 1 << 0,
        TeamVsTeam = 1 << 1,
        Replay = 1 << 2,

        All = FreeForAll | TeamVsTeam | Replay
    }

    [ProtoContract]
    public class MapInfo
    {
        [ProtoMember(1)]
        public Guid Id;
        [ProtoMember(2)]
        public string Name;
        [ProtoMember(3)]
        public int MaxPlayers;
        [ProtoMember(4)]
        public GameMode SupportedModes = GameMode.All;
    }

    [ProtoContract]
    public class MapData
    {
        [ProtoMember(1)]
        public Guid Id;

        [ProtoMember(2)]
        public byte[] Bytes;
    }


    [ProtoContract]
    public class ServerStats
    {
        [ProtoMember(1)]
        public int PlayersCount;
        [ProtoMember(2)]
        public int RoomsCount;
        [ProtoMember(3)]
        public int MatchesCount;
    }

    public delegate void ServerEventHandler(Error error);
    public delegate void ServerEventHandler<TPayload>(Error error, TPayload payload);
    public delegate void ServerEventHandler<TSender, TPayload>(Error error, TSender sender, TPayload payload);
    public delegate void ServerEventHandler<TSender, TPayload, TExtra>(Error error, TSender sender, TPayload payload, TExtra extra);
    public delegate void ServerEventHandler<TSender, TPayload, TExtra, TExtra2>(Error error, TSender sender, TPayload payload, TExtra extra, TExtra2 extra2);
    public delegate void ServerEventHandler<TSender, TPayload, TExtra, TExtra2, TExtra3, TExtra4>(Error error, TSender sender, TPayload payload, TExtra extra, TExtra2 extra2, TExtra3 extra3, TExtra4 extra4);
    public delegate void ServerEventHandler<TSender, TPayload, TExtra, TExtra2, TExtra3, TExtra4, Textra5>(Error error, TSender sender, TPayload payload, TExtra extra, TExtra2 extra2, TExtra3 extra3, TExtra4 extra4, Textra5 extra5);


    public static class EnumExtensions
    {
        public static int[] ToIntArray<T>(this T[] e) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            int[] result = new int[e.Length];
            for(int i = 0; i < result.Length; ++i)
            {
                result[i] = (int)e.GetValue(i);
            }
            return result;
        }

        public static T[] ToEnum<T>(this int[] intArray) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            T[] result = new T[intArray.Length];
            for (int i = 0; i < result.Length; ++i)
            {
                result[i] = (T)Enum.ToObject(typeof(T), i);
            }
            return result;
        }
    }

    public class ServerEventArgs
    {
        public static readonly ServerEventArgs Empty = new ServerEventArgs();

#if SERVER
        public Guid[] Targets;
        public Guid Except;
#endif
    }

    public class ServerEventArgs<T> : ServerEventArgs
    {
        public T Arg;

        public ServerEventArgs()
        {
        }

        public ServerEventArgs(T arg)
        {
            Arg = arg;
        }
    }

    public class ServerEventArgs<T1, T2> : ServerEventArgs
    {
        public T1 Arg;
        public T2 Arg2;

        public ServerEventArgs()
        {
        }
        public ServerEventArgs(T1 arg, T2 arg2)
        {
            Arg = arg;
            Arg2 = arg2;
        }
    }

    public class ServerEventArgs<T1, T2, T3, T4> : ServerEventArgs
    {
        public T1 Arg;
        public T2 Arg2;
        public T3 Arg3;
        public T4 Arg4;

        public ServerEventArgs()
        {
        }

        public ServerEventArgs(T1 arg, T2 arg2, T3 arg3, T4 arg4)
        {
            Arg = arg;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
        }
    }

    public class ServerEventArgs<T1, T2, T3, T4, T5, T6> : ServerEventArgs
    {
        public T1 Arg;
        public T2 Arg2;
        public T3 Arg3;
        public T4 Arg4;
        public T5 Arg5;
        public T6 Arg6;

        public ServerEventArgs()
        {
        }

        public ServerEventArgs(T1 arg, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            Arg = arg;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
            Arg5 = arg5;
            Arg6 = arg6;
        }
    }


    public interface ITimeService
    {
        float Time
        {
            get;
        }
    }

    public struct ContainerDiagInfo
    {
        public int ConnectionsCount;
        public int RegisteredClientsCount;
        public bool IsMainThreadRunning;
        public bool IsSecondaryThreadRunning;
        public float IncomingMessagesFrequency;
        public float OutgoingMessagesFrequency;
    }

    public struct GameServerDiagInfo
    {
        public int ActiveReplaysCount;
        public int ClientsJoinedToRoomsCount;
        public int CreatedRoomsCount;
        public int ClinetsWithPlayersCount;
        public int LoggedInPlayersCount;
        public int LoggedInBotsCount;
        public int RunningMatchesCount;
    }

    public struct MatchServerDiagInfo
    {
        public bool IsInitializationStarted;
        public bool IsInitialized;
        public bool IsEnabled;
        public bool IsMatchEngineCreated;
        public bool IsReplay;
        public int ServerRegisteredClientsCount;
        public int ReadyToPlayClientsCount;
        public int ClientsWithPlayersCount;
        public int PlayersCount;
        public int BotsCount;
    }

    public interface IGameServerContainerDiagnostics
    {
        ContainerDiagInfo GetContainerDiagInfo();
        GameServerDiagInfo GetDiagInfo();
    }

    public interface IGameServerDiagnostics
    {
        GameServerDiagInfo GetDiagInfo();
    }

    public interface IMatchServerContainerDiagnostics
    {
        ContainerDiagInfo GetContainerDiagInfo();
        MatchServerDiagInfo GetDiagInfo();
    }

    public interface IMatchServerDiagnostics
    {
        MatchServerDiagInfo GetDiagInfo();
    }

    public interface ILoop
    {
        bool Start(ITimeService time);
        void Update();
        void Destroy();
    }

    public interface IGameServer : IServer
    { 
        /// <summary>
        /// Is guid is local (player)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        bool IsLocal(Guid clientId, Guid playerId);

        
        // void Connect(Guid clientId, ILowProtocol protocol, ServerEventHandler callback);

        //Raised for remote players only
        //==============================
#if SERVER
        /// <summary>
        /// Raised when remote player connects to server
        /// </summary>
        event ServerEventHandler<ServerEventArgs<Guid>> LoggedIn;

        /// <summary>
        /// Raised when remote player disconnect from server
        /// </summary>
        event ServerEventHandler<ServerEventArgs<Guid[]>> LoggedOff;

        /// <summary>
        /// Raised whenever new room created or destroyed
        /// </summary>
        event ServerEventHandler<ServerEventArgs> RoomsListChanged;

        /// <summary>
        /// Raised when remote player join room
        /// </summary>
        event ServerEventHandler<ServerEventArgs<Guid[], Room>> JoinedRoom;

        /// <summary>
        /// Raised when remote player left room (first arg is player id, second is roomid)
        /// </summary>
        event ServerEventHandler<ServerEventArgs<Guid[], Room>> LeftRoom;

        event ServerEventHandler<ServerEventArgs> RoomDestroyed;

        //(first arg is is roomid, second is traget client ids)
        event ServerEventHandler<ServerEventArgs<Room>> ReadyToLaunch;
        /// <summary>
        /// Raise when player is ready to launch  (first arg is is roomid)
        /// </summary>
        event ServerEventHandler<ServerEventArgs<string>> Launched;
#else
        event ServerEventHandler<Guid> LoggedIn;
        event ServerEventHandler<Guid[]> LoggedOff;
        event ServerEventHandler RoomsListChanged;
        event ServerEventHandler<Guid[], Room> JoinedRoom;
        event ServerEventHandler<Guid[], Room> LeftRoom;
        event ServerEventHandler RoomDestroyed;
        event ServerEventHandler<Room> ReadyToLaunch;
        event ServerEventHandler<string> Launched;
#endif

        //Methods called by localplayers
        //==============================

        /// <summary>
        /// Become admin
        /// </summary>
        /// <param name="playerId"></param>
        /// <param name="callback"></param>
        void BecomeAdmin(Guid playerId, ServerEventHandler callback);

        void Login(string name, byte[] pwdHash, Guid clientId, ServerEventHandler<Guid> callback);
        /// <summary>
        /// Possible errors TooMuchLocalPlayers, NotAuthenticated 
        /// </summary>
        void Login(string name, string password, Guid clientId, ServerEventHandler<Guid, byte[]> callback);
        /// <summary>
        /// Possible errors TooMuchLocalPlayers, NotAuthenticated, 
        /// </summary>
        void SignUp(string name, string password, Guid clientId, ServerEventHandler<Guid, byte[]> callback);
        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void Logoff(Guid clientId, Guid playerId, ServerEventHandler<Guid> callback);

        void Logoff(Guid clientId, Guid[] playerIds, ServerEventHandler<Guid[]> callback);

        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void GetPlayer(Guid clientId, Guid playerId, ServerEventHandler<Player> callback);

        void GetPlayers(Guid clientId, Guid roomId, ServerEventHandler<Player[]> callback);

        /// <summary>
        /// Get Logged in players by clientId
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="callback"></param>
        void GetPlayers(Guid clientId, ServerEventHandler<Player[]> callback);

        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void GetStats(Guid clientId, ServerEventHandler<ServerStats> callback);

        void GetMaps(Guid clientId, ServerEventHandler<ByteArray[]> callback);
        void GetMaps(Guid clientId, ServerEventHandler<MapInfo[]> callback);

        /// <summary>
        /// Only user who own admin rights could upload map to server
        /// </summary>
        void UploadMap(Guid clientId, MapInfo mapInfo, MapData mapData, ServerEventHandler callback);
        void UploadMap(Guid clientId, MapInfo mapInfo, byte[] mapData, ServerEventHandler callback);

        void DownloadMapData(Guid clientId, Guid mapId, ServerEventHandler<MapData> callback);
        void DownloadMapData(Guid cleintId, Guid mapId, ServerEventHandler<byte[]> callback);

        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void CreateRoom(Guid clientId, Guid mapId, GameMode gameMode, ServerEventHandler<Room> callback);

        /// <summary>
        /// Get room that client joined to
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="callback"></param>
        void GetRoom(Guid clientId, ServerEventHandler<Room> callback);
        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void GetRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback);
        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void GetRooms(Guid clientId, int page, int count, ServerEventHandler<Room[]> callback);
        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void DestroyRoom(Guid clientId, Guid roomId, ServerEventHandler<Guid> callback);
        /// <summary>
        /// Possible errors NotAuthenticated, TooMuchPlayersInRoom, RoomNotExist
        /// </summary>
        void JoinRoom(Guid clientId, Guid roomId, ServerEventHandler<Room> callback);

        /// <summary>
        /// Possible errors NotAuthenticated 
        /// </summary>
        void LeaveRoom(Guid clientId, ServerEventHandler callback);

        //void ReorderPlayers(Guid clientId, Guid roomId, Guid[] playerGuids, int[] order, ServerEventHandler<Room> callback); //Empty guid mean no player

        void CreateBot(Guid clientId, string botName, BotType botType, ServerEventHandler<Guid, Room> callback); //bot guid and room with bot

        void CreateBots(Guid clientId, string[] botNames, BotType[] botTypes, ServerEventHandler<Guid[], Room> callback); //bot guids and room with bots

        void DestroyBot(Guid clientId, Guid botId, ServerEventHandler<Guid, Room> callback); //bot guid and room without him


        /// <summary>
        /// This method should be called by each player when they are ready for game to be launched
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="callback"></param>
        void SetReadyToLaunch(Guid clientId, bool isReady, ServerEventHandler<Room> callback); //roomId

        /// <summary>
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="callback"></param>
        void Launch(Guid clientId, ServerEventHandler<string> callback);


        /// <summary>
        /// Returns ReplayInfo for all saved replys
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="callback"></param>
        void  GetReplays(Guid clientId, ServerEventHandler<ReplayInfo[]> callback);

        void GetReplays(Guid clientId, ServerEventHandler<ByteArray[]> callback);
        
        void SetReplay(Guid clientId, Guid id, ServerEventHandler callback);

        /// <summary>
        /// Save Current Replay
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        void SaveReplay(Guid clientId, string name, ServerEventHandler callback);

        void SavePlayersStats(ServerEventHandler callback);

        //void Update(float t);
    }

    public static class StatusCode
    {
        //Common codes
        public const int OK = 0; //OK
        public const int UnhandledException = 1;
        public const int NotAuthenticated = 2; //Invalid player name or password
        public const int AlreadyExists = 3; //already exists
        public const int NotAuthorized = 4; //Command is valid but player has insufficient rights to do so
        public const int NotAllowed = 5; //Command is invalid (breaking rules defined by server) 
        public const int OutOfSync = 6;  //Player submit command that can't be executed because server state changed (For example tried to move to occupied cell)
        public const int NotFound = 8; //Something was not found
        public const int TooMuchLocalPlayers = 9;
        public const int Outdated = 10; //Ping is too high;
        public const int Paused = 11; //Game was paused
        public const int NotRegistered = 12;// Client was not registered
        public const int AlreadyJoined = 13;
        public const int Failed = 14;
        public const int RequestTimeout = 15;
        public const int ConnectionClosed = 16;
        public const int ConnectionError = 17;
        public const int AlreadyLaunched = 18;
        public const int NotReady = 19;

        //Room management codes
        public const int TooMuchPlayersInRoom = 100;


        public static string ToString(int code)
        {
            switch(code)
            {
                case OK:
                    return "OK";
                case UnhandledException:
                    return "Unhandled Exception";
                case NotAuthenticated:
                    return "Not Authenticated";
                case AlreadyExists:
                    return "Already Exist";
                case NotAuthorized:
                    return "Not Authorized";
                case NotAllowed:
                    return "Not Allowed";
                case OutOfSync:
                    return "Out Of Sync";
                case NotFound:
                    return "Not Found";
                case TooMuchLocalPlayers:
                    return "Too Much Local Players";
                case TooMuchPlayersInRoom:
                    return "Too Much Players In Room";
                case Outdated:
                    return "High Ping";
                case Paused:
                    return "Paused";
                case NotRegistered:
                    return "Not Registered";
                case AlreadyJoined:
                    return "Alread Joined";
                case Failed:
                    return "Failed";
                case RequestTimeout:
                    return "Request Timeout";
                case ConnectionClosed:
                    return "Connection Closed";
                case ConnectionError:
                    return "Connection Error";
                case AlreadyLaunched:
                    return "Already Launched";
                case NotReady:
                    return "Not Ready";
                default:
                    return "Unknown status code";
                    
            }
        }
    }



    [ProtoContract]
    public class CommandsBundle
    {
        [ProtoMember(1)]
        public long Tick;

        //[ProtoMember(2)]
        //public Guid[] Players;

        [ProtoMember(3)]
        public CommandsArray[] Commands;

        [ProtoMember(4)]
        public List<ClientRequest> ClientRequests; //Заполнять как и TasksStateInfo

        [ProtoMember(5)]
        public List<TaskStateInfo> TasksStateInfo;
        
        [ProtoMember(6)]
        public bool IsGameCompleted;
    }

    [ProtoContract]
    public class CommandsArray
    {
        [ProtoMember(1)]
        public Cmd[] Commands;

        public CommandsArray()
        {
        }

        public CommandsArray(Cmd[] commands)
        {
            Commands = commands;
        }

        public CommandsArray(CommandsArray arr)
        {
            if(arr.Commands != null)
            {
                Commands = arr.Commands.ToArray();
            }
        }
    }

    [ProtoContract]
    public class RTTInfo
    {
        [ProtoMember(1)]
        public float RTT; //RTT of current player

        [ProtoMember(2)]
        public float RTTMax; //
    }

    public class ValueChangedArgs<T>
    {
        public T OldValue
        {
            get;
            private set;
        }

        public T NewValue
        {
            get;
            private set;
        }

        public ValueChangedArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }

    [ProtoContract]
    public class ChatMessage
    {
        [ProtoMember(1)]
        public Guid MessageId;

        [ProtoMember(2)]
        public Guid[] ReceiverIds; //Not equals to Guid.Empty -> send private message

        [ProtoMember(3)]
        public long DateTime; //Utc DateTime.Ticks 

        [ProtoMember(4)]
        public string Message;

        [ProtoMember(5)]
        public Guid SenderId;

        public ChatMessage()
        {

        }

        public ChatMessage(Guid senderId, string message, Guid[] receiverIds)
        {
            SenderId = senderId;
            MessageId = Guid.NewGuid();
            Message = message;
            ReceiverIds = receiverIds;
        }
    }

    public interface IServer
    {
        event ServerEventHandler ConnectionStateChanging;

        event ServerEventHandler<ValueChangedArgs<bool>> ConnectionStateChanged;

#if SERVER
        event ServerEventHandler<ServerEventArgs<ChatMessage>> ChatMessage;
#else
        event ServerEventHandler<ChatMessage> ChatMessage;
#endif

        bool IsConnectionStateChanging { get; }

        bool IsConnected { get; }

        bool HasError(Error error);

        void RegisterClient(Guid clientId, ServerEventHandler callback);

        void UnregisterClient(Guid clientId, ServerEventHandler callback);

        void CancelRequests();

        void SendMessage(Guid clientId, ChatMessage message, ServerEventHandler<Guid> callback);

#if !SERVER
        void Connect();

        void Disconnect();
#endif

    }

    public interface IMatchServer : IServer
    {
#if SERVER
        /// <summary>
        /// Raised when match started and all players called ReadyToPlay method 
        /// </summary>
        event ServerEventHandler<ServerEventArgs<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room>> ReadyToPlayAll;

        //event ServerEventHandler<Guid[], Room> LeftRoom;  This event will be raised using Tick command

        event ServerEventHandler<ServerEventArgs<CommandsBundle>> Tick;

        event ServerEventHandler<ServerEventArgs<RTTInfo>> Ping; // Raised by server. Pong method should be called in response to this event. RTTInfo contains information from previos Ping Pong cycle

        event ServerEventHandler<ServerEventArgs<bool>> Paused;

        /// <summary>
        /// Equivalent of ReadyToPlayAll event used during reconnect
        /// </summary>
        /// <param name="clientId"></param>
        /// <param name="callback"></param>
        void GetState(Guid clientId, ServerEventHandler<Player[], Dictionary<Guid, Dictionary<Guid, Player>>, VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room, MapRoot> callback);
#else
        /// <summary>
        /// Raised when match started and all players called ReadyToPlay method 
        /// </summary>
        event ServerEventHandler<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room> ReadyToPlayAll;

        //event ServerEventHandler<Guid[], Room> LeftRoom;  This event will be raised using Tick command

        event ServerEventHandler<CommandsBundle> Tick;

        event ServerEventHandler<RTTInfo> Ping; // Raised by server. Pong method should be called in response to this event. RTTInfo contains information from previos Ping Pong cycle

        event ServerEventHandler<bool> Paused;

        void GetState(Guid clientId, ServerEventHandler<Player[], Guid[], VoxelAbilitiesArray[], SerializedTaskArray[], SerializedTaskTemplatesArray[], Room, MapRoot> callback);
#endif
        void Activate();

        void Deactivate();
        /// <summary>
        /// Is guid is local (player)
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //bool IsLocal(Guid clientId, Guid playerId);

        void DownloadMapData(Guid clientId, ServerEventHandler<MapData> callback);

        void DownloadMapData(Guid clientId, ServerEventHandler<byte[]> callback);

        /// <summary>
        /// This metod must be called to notify other players that player's ui is loaded and he is ready for match
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="callbacK"></param>
        void ReadyToPlay(Guid clientId, ServerEventHandler callback);

        /// <summary>
        /// Return error if called before Launched event
        /// </summary>
        void Submit(Guid clientId, int playerIndex, Cmd cmd, ServerEventHandler<Cmd> callback);

        void SubmitResponse(Guid clientId, ClientRequest response, ServerEventHandler<ClientRequest> callback);

        void Pong(Guid clientId, ServerEventHandler callback);

        void Pause(Guid clientId, bool pause, ServerEventHandler callback);

        void GetReplay(Guid clientId, ServerEventHandler<ReplayData, Room> callback);

        void GetTaskTemplates(Guid clientId, Guid playerId, ServerEventHandler<SerializedTask[], SerializedNamedTaskLaunchInfo[]> callback);

        void SaveTaskTemplate(Guid clientId, Guid playerId, SerializedTask taskTemplate, SerializedNamedTaskLaunchInfo templateInfo, ServerEventHandler callback);

    }

}


