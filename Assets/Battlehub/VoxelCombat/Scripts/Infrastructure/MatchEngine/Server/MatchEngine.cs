using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public static class CmdCode
    {
        public const int Nop = 0;
        public const int Spawn = 1;
        
        public const int Move = 2;
        //public const int MoveFindPath = 3;
        public const int RotateLeft = 4;
        public const int RotateRight = 5;
        public const int ExecuteTask = 8;
        public const int Cancel = 10;
      
        public const int Split = 20;
        public const int Split4 = 24;
        public const int Grow = 25;
        public const int Diminish = 26;
       // public const int Automatic = 40;
        public const int Convert = 50;
        public const int Explode = 75;
        public const int StateChanged = 90;

        //public const int Failed = 98; //Last command was failed -> disable animations return to idle state
        public const int LeaveRoom = 99;
        public const int Composite = 100;

        //Debug command
        public const int SetHealth = 200;
        public const int Kill = 201;

        //Commands requested by server
 
    }

    //public static class CmdErrorCode
    //{
    //    public const int Success = 0;
    //    public const int Fail = 1;
    //    public const int HardFail = (1 << 1) | Fail;
    //    public const int SoftFail = (1 << 2) | Fail;
    //}

    public struct MapRect
    {
        public int Row;
        public int Col;
        public int RowsCount;
        public int ColsCount;

        public MapPos P0
        {
            get { return new MapPos(Row, Col); }
        }

        public MapPos P1
        {
            get { return new MapPos(Row + RowsCount - 1, Col + ColsCount - 1); }
        }

        public MapPos P2
        {
            get { return new MapPos(Row + RowsCount - 1, Col); }
        }

        public MapPos P3
        {
            get { return new MapPos(Row, Col + ColsCount - 1); }
        }

        public MapRect(int row, int col, int rowsCount, int colsCount)
        {
            Row = row;
            Col = col;
            RowsCount = rowsCount;
            ColsCount = colsCount;
        }

        public MapRect(MapPos min, MapPos max)
        {
            Row = min.Row;
            Col = min.Col;

            RowsCount = (max.Row - Row) + 1;
            ColsCount = (max.Col - Col) + 1;
        }

        public bool Contains(MapPos pos)
        {
            return Row <= pos.Row && pos.Row <= Row + RowsCount &&
                   Col <= pos.Col && pos.Col <= Col + ColsCount;
        }
    }

    public struct MapPos
    {
        public enum Align
        {
            Minus,
            Center,
            Plus
        }

        public int Row; //Z
        public int Col; //X

        public MapPos(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public override string ToString()
        {
            return Row + "," + Col;
        }

        public void Add(int deltaRow, int deltaCol)
        {
            Row += deltaRow;
            Col += deltaCol;
        }

        public int SqDistanceTo(MapPos pos)
        {
            int deltaRow = pos.Row - Row;
            int deltaCol = pos.Col - Col;
            return deltaRow * deltaRow + deltaCol * deltaCol;
        }

        public static bool operator==(MapPos p1, MapPos p2)
        {
            return p1.Row == p2.Row && p1.Col == p2.Col;
        }

        public static bool operator !=(MapPos p1, MapPos p2)
        {
            return p1.Row != p2.Row || p1.Col != p2.Col;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is MapPos))
            {
                return false;
            }

            MapPos pos = (MapPos)obj;
            return pos.Row == Row && pos.Col == Col;
        }

        
        public override int GetHashCode()
        {
            return (Row << 16) | Col;
        }
    }


    [ProtoContract]
    public struct Coordinate
    {
        [ProtoMember(1)]
        public int Row;
        [ProtoMember(2)]
        public int Col;
        [ProtoMember(3)]
        public int Altitude;
        [ProtoMember(4)]
        public int Weight;

        public MapPos MapPos
        {
            get { return new MapPos(Row, Col); }
        }

        public Coordinate(int row, int col, int altitude, int weight)
        {
            Row = row;
            Col = col;
            Weight = weight;
            Altitude = altitude;
        }

        public Coordinate(MapCell cell, VoxelData voxelData)
        {
            MapPos pos = cell.GetPosition();
            Row = pos.Row;
            Col = pos.Col;
            Altitude = voxelData.Altitude;
            Weight = voxelData.Weight;
        }

        public Coordinate(MapPos pos, int weight, int altitude)
        {
            Row = pos.Row;
            Col = pos.Col;
            Weight = weight;
            Altitude = altitude;
        }

        public Coordinate(MapPos pos, VoxelData voxelData)
        {
            Row = pos.Row;
            Col = pos.Col;
            Altitude = voxelData.Altitude;
            Weight = voxelData.Weight;
        }

        public Coordinate(Coordinate coordinate)
        {
            Row = coordinate.Row;
            Col = coordinate.Col;
            Altitude = coordinate.Altitude;
            Weight = coordinate.Weight;
        }

        public Coordinate Add(int deltaRow, int deltaCol)
        {
            Coordinate coord = new Coordinate(this);
            coord.Row += deltaRow;
            coord.Col += deltaCol;
            return coord;
        }

        public override string ToString()
        {
            return "R: " + Row + ", C: " + Col + ", A: " + Altitude + ", W: " + Weight;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17 * Weight;
                hash = 17 * hash + Row;
                hash = 17 * hash + Col;
                hash = 17 * hash + Altitude;
                return hash;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Coordinate))
            {
                return false;
            }

            Coordinate other = (Coordinate)obj;
            return Weight == other.Weight &&
                   Row == other.Row &&
                   Col == other.Col &&
                   Altitude == other.Altitude;
        }

        public static bool operator ==(Coordinate a, Coordinate b)
        {
            return a.Weight == b.Weight && a.Row == b.Row && a.Col == b.Col && a.Altitude == b.Altitude;
        }

        public static bool operator !=(Coordinate a, Coordinate b)
        {
            return !(a == b);
        }

        public  static bool EqualWithNormalizedWeight(Coordinate coord1, Coordinate coord2)
        {
            if(coord1.Weight == coord2.Weight)
            {
                return coord1 == coord2;
            }

            if(coord1.Weight < coord2.Weight)
            {
                return coord1.ToWeight(coord2.Weight) == coord2;
            }

            return coord2.ToWeight(coord1.Weight) == coord1;
            
        }

        public Coordinate ToWeight(int weight)
        {
            if(weight < Weight)
            {
                Coordinate result = new Coordinate(this);
                while(weight != Weight)
                {
                    result.Row *= 2;
                    result.Col *= 2;
                    result.Weight--;
                    weight++;
                }
                return result;
            }
            else if(weight > Weight)
            {
                Coordinate result = new Coordinate(this);
                while (weight != Weight)
                {
                    result.Row /= 2;
                    result.Col /= 2;
                    result.Weight++;
                    weight--;
                }
                return result;
            }

            return this;
        }

        public bool FindClosestTo(Coordinate[] path, out Coordinate result)
        {
            int minSqDistance = int.MaxValue;
            int minIndex = -1;
            for (int i = 0; i < path.Length; ++i)
            {
                Coordinate pathCoord = path[i];
                int sqDistance = pathCoord.MapPos.SqDistanceTo(MapPos);
                if (sqDistance < minSqDistance)
                {
                    minIndex = i;
                    minSqDistance = sqDistance;
                }
            }

            if (minIndex != -1)
            {
                result = path[minIndex];
                return true;
            }
            result = new Coordinate();
            return false;
        }

        public static Coordinate[] MergePath(Coordinate[] start, Coordinate[] end)
        {
            int index = Array.IndexOf(end, start.Last());
            if(index < 0)
            {
                throw new ArgumentException("unable to merge", "end");
            }
            List<Coordinate> result = new List<Coordinate>(start);
            for(int i = index + 1; i < end.Length; ++i)
            {
                result.Add(end[i]);
            }
            return result.ToArray();
        }
    }


    public delegate void CmdEventHandler(Cmd cmd);
    [ProtoContract]
    [ProtoInclude(50, typeof(CoordinateCmd))]
    [ProtoInclude(51, typeof(ChangeParamsCmd))]
    [ProtoInclude(52, typeof(CompositeCmd))]
    [ProtoInclude(53, typeof(TargetCmd))]
    [ProtoInclude(54, typeof(TaskCmd))]
    public class Cmd
    {
        [ProtoMember(1)]
        public int Code;

        [ProtoMember(2)]
        public long UnitIndex;

        [ProtoMember(3)]
        public int Duration;

        [ProtoMember(4)]
        public CmdResultCode ErrorCode;

        public bool IsFailed
        {
            get { return ErrorCode != CmdResultCode.Success; }
        }

        public bool IsHardFailed
        {
            get { return (ErrorCode & CmdResultCode.HardFail) != 0; }
        }

        public Cmd()
        {
            Code = CmdCode.Nop;
        }

        public Cmd(int code)
        {
            Code = code;
        }

        public Cmd(int code, long unitIndex)
        {
            Code = code;
            UnitIndex = unitIndex;
        }

        public Cmd(Cmd cmd)
        {
            Code = cmd.Code;
            UnitIndex = cmd.UnitIndex;
            Duration = cmd.Duration;
        }
    }

    [ProtoContract]
    public class TaskCmd : Cmd
    {
        [ProtoMember(1)]
        public TaskInfo Task;

        public TaskCmd()
        {
            Code = CmdCode.ExecuteTask;
        }

        public TaskCmd(TaskInfo task)
        {
            Code = CmdCode.ExecuteTask;
            Task = task;
        }  
    }

    [ProtoContract]
    public class ClientRequest 
    {
        [ProtoMember(1)]
        public int TaskId;

        [ProtoMember(2)]
        public int PlayerIndex;

        [ProtoMember(3)]
        public Cmd Cmd;

        public ClientRequest()
        {

        }

        public ClientRequest(int taskId, int playerIndex, Cmd cmd)
        {
            PlayerIndex = playerIndex;
            TaskId = taskId;
            Cmd = cmd;
        }
    }


  
    /// <summary>
    /// Command that has one or more coordinates (for example move from coord 0 to coord 1, or attack from coord 0 coord 1, or spawn at coord 0, etc.
    /// </summary>
    [ProtoContract]
    [ProtoInclude(60, typeof(MovementCmd))]
    public class CoordinateCmd : Cmd
    {
        [ProtoMember(1)]
        public Coordinate[] Coordinates;

        public CoordinateCmd()
        {

        }

        public CoordinateCmd(int code)
        {
            Code = code;
        }
        public CoordinateCmd(int code, long unitIndex, int duration)
        {
            Code = code;
            UnitIndex = unitIndex;
            Duration = duration;
        }
    }

    [ProtoContract]
    public class TargetCmd : Cmd
    {
        [ProtoMember(1, IsRequired = true)]
        public int TargetPlayerIndex = -1;

        [ProtoMember(2, IsRequired = true)]
        public long TargetIndex = -1;

        public bool HasTarget
        {
            get { return TargetIndex > -1 && TargetPlayerIndex > -1; }
        }

        public TargetCmd()
        {

        }

        public TargetCmd(int code)
        {
            Code = code;
        }
        public TargetCmd(int code, long unitIndex, int duration)
        {
            Code = code;
            UnitIndex = unitIndex;
            Duration = duration;
        }
    }

    [ProtoContract]
    public class MovementCmd : CoordinateCmd
    {
        [Obsolete]
        [ProtoMember(1, IsRequired = true)]
        public int TargetPlayerIndex = -1;

        [Obsolete]
        [ProtoMember(2, IsRequired = true)]
        public long TargetIndex = -1;

        [Obsolete]
        [ProtoMember(3)]
        public bool HasTarget;

        [ProtoMember(4)]
        public bool IsLastCmdInSequence;
       
        public MovementCmd()
        {

        }

        public MovementCmd(int code)
        {
            Code = code;
        }
        public MovementCmd(int code, long unitIndex, int duration)
        {
            Code = code;
            UnitIndex = unitIndex;
            Duration = duration;
        }
    }

    /// <summary>
    /// Command that has coordinates and additional Int or Float Params (could be random power up or something like that)
    /// </summary>
    [ProtoContract]
    public class ChangeParamsCmd : Cmd
    {
        [ProtoMember(1)]
        public int[] IntParams;

        [ProtoMember(2)]
        public float[] FloatParams;


        public ChangeParamsCmd()
        {

        }

        public ChangeParamsCmd(int code)
        {
            Code = code;
        }
    }

    [ProtoContract]
    public class CompositeCmd : Cmd
    {
        [ProtoMember(1)]
        public Cmd[] Commands;

        public CompositeCmd()
        {
            Code = CmdCode.Composite;
        }
    }

    public interface IMatchEngine : IMatchView
    {
        event Action<int, Cmd> OnSubmitted;

        ITaskEngine GetTaskEngine(int playerIndex);

        IPathFinder GetPathFinder(int playerIndex);

        ITaskRunner GetTaskRunner(int playerIndex);

        IMatchUnitController GetUnitController(int playerIndex, long unitIndex);

        IMatchUnitAssetView GetAsset(int playerIndex, long unitIndex);

        void RegisterPlayer(Guid playerId, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities);

        void CompletePlayerRegistration();

        void Update();

        bool Tick(out CommandsBundle commands);

        void SubmitResponse(ClientRequest response);

        void Destroy();
    }

    public class MatchEngine : IMatchEngine
    {
        public event Action<int, Cmd> OnSubmitted;

        private bool m_hasNewCommands;
        //private const bool EnableLog = true;
        //public event Action<int, Cmd> OnSubmitted;
        private readonly Dictionary<Guid, IMatchPlayerController> m_idToPlayers = new Dictionary<Guid, IMatchPlayerController>();
        private readonly IMatchPlayerController[] m_players;
        //private readonly Guid[] m_playerGuids;
        //private readonly List<float> m_rtt = new List<float>();
        private readonly CommandsBundle m_serverCommands = new CommandsBundle()
        {
            ClientRequests = new List<ClientRequest>(),
            TasksStateInfo = new List<TaskStateInfo>()
        };

        private MapRoot m_map;

        public MapRoot Map
        {
            get { return m_map; }
        }

        int IMatchView.PlayersCount
        {
            get { return m_players.Length; }
        }

        private ITaskEngine[] m_taskEngines;
        private ITaskRunner[] m_taskRunners;
        private IPathFinder[] m_pathFinders;
    
        public MatchEngine(MapRoot map, int playersCount)
        {
            m_map = map;

            m_players = new IMatchPlayerController[playersCount];
            //m_playerGuids = new Guid[playersCount];

            m_map.SetPlayerCount(playersCount);

            m_taskEngines = new ITaskEngine[playersCount];
            m_pathFinders = new IPathFinder[playersCount];
            m_taskRunners = new ITaskRunner[playersCount];
            for(int i = 0; i < playersCount; ++i)
            {
                IPathFinder pathFinder = MatchFactory.CreatePathFinder(m_map);
                ITaskRunner taskRunner = MatchFactory.CreateTaskRunner();
                m_pathFinders[i] = pathFinder;
                m_taskRunners[i] = taskRunner;

                ITaskEngine taskEngine = MatchFactory.CreateTaskEngine(this, taskRunner, pathFinder);
                taskEngine.TaskStateChanged += OnTaskStateChanged;
                taskEngine.ClientRequest += OnClientRequest;
                m_taskEngines[i] = taskEngine;
            }
        }

        public void Destroy()
        {
            for (int i = 0; i < m_players.Length; ++i)
            {
                ITaskEngine taskEngine = m_taskEngines[i];
                taskEngine.TaskStateChanged -= OnTaskStateChanged;
                taskEngine.ClientRequest -= OnClientRequest;

                MatchFactory.DestroyTaskEngine(taskEngine);
                MatchFactory.DestroyPathFinder(m_pathFinders[i]);
                MatchFactory.DestroyTaskRunner(m_taskRunners[i]);
            }   
        }

        public ITaskEngine GetTaskEngine(int playerIndex)
        {
            return m_taskEngines[playerIndex];
        }

        public IPathFinder GetPathFinder(int playerIndex)
        {
            return m_pathFinders[playerIndex];
        }

        public ITaskRunner GetTaskRunner(int playerIndex)
        {
            return m_taskRunners[playerIndex];
        }

        public IMatchUnitController GetUnitController(int playerIndex, long unitIndex)
        {
            return m_players[playerIndex].GetUnitController(unitIndex);
        }

        public IMatchUnitAssetView GetAsset(int playerIndex, long assetIndex)
        {
            return m_players[playerIndex].GetAsset(assetIndex);
        }

        public void RegisterPlayer(Guid playerId, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities)
        {
            IMatchPlayerController player = MatchFactory.CreatePlayerController(this, playerIndex, allAbilities);

            m_idToPlayers.Add(playerId, player);
            m_players[playerIndex] = player;

            if (m_serverCommands.Commands == null)
            {
                m_serverCommands.Commands = new CommandsArray[1];
            }
            else
            {
                Array.Resize(ref m_serverCommands.Commands, m_serverCommands.Commands.Length + 1);
            }
        }

        public void CompletePlayerRegistration()
        {
            for (int i = 0; i < m_players.Length; ++i)
            {
                m_players[i].ConnectWith(m_players);
            }
        }

        public bool IsSuitableCmdFor(Guid playerId, long unitIndex, int cmdCode)
        {
            IMatchPlayerController playerController;
            if (m_idToPlayers.TryGetValue(playerId, out playerController))
            {
                IMatchUnitController unitController = playerController.GetUnitController(unitIndex);
                if(unitController.Data.Type == (int)KnownVoxelTypes.Spawner)
                {
                    return false;
                }
                switch(cmdCode)
                {
                    //case CmdCode.Automatic:
                    //    return true;
                    case CmdCode.Convert:
                        return unitController.DataController.CanConvertImmediate(-1) == CmdResultCode.Success;
                    case CmdCode.Grow:
                        return unitController.DataController.CanGrowImmediate() == CmdResultCode.Success;
                    case CmdCode.Split4:
                        return unitController.DataController.CanSplit4Immediate() == CmdResultCode.Success;
                    case CmdCode.Split:
                        return unitController.DataController.CanSplitImmediate() == CmdResultCode.Success;
                    case CmdCode.Move:
                        return !unitController.DataController.IsCollapsedOrBlocked;
                     
                    case CmdCode.Explode:
                        return unitController.Data.Type == (int)KnownVoxelTypes.Bomb;
                }
            }
            return false;
        }

        public void Submit(int playerIndex, Cmd cmd)
        {
            if(cmd.Code == CmdCode.ExecuteTask)
            {
                TaskCmd executeTaskCmd = (TaskCmd)cmd;
                TaskInfo task = executeTaskCmd.Task;
                task.PlayerIndex = playerIndex;
                m_taskEngines[playerIndex].SubmitTask(task);
            }
            else
            {
                if (playerIndex >= 0 && playerIndex < m_players.Length)
                {
                    IMatchPlayerController player = m_players[playerIndex];
                    if (player != null)
                    {
                        player.Submit(cmd);
                    }
                }
            }

            if(OnSubmitted != null)
            {
                OnSubmitted(playerIndex, cmd);
            }
        }

        public void SubmitResponse(ClientRequest response)
        {
            m_taskEngines[response.PlayerIndex].SubmitResponse(response);
        }

        private void OnTaskStateChanged(TaskInfo taskInfo)
        {
            m_serverCommands.TasksStateInfo.Add(new TaskStateInfo(taskInfo.TaskId, taskInfo.PlayerIndex, taskInfo.State, taskInfo.StatusCode));
            m_hasNewCommands = true;
        }

        private void OnClientRequest(ClientRequest request)
        {
            m_serverCommands.ClientRequests.Add(request);
            m_hasNewCommands = true;
        }

        public void Update()
        {
            for (int i = 0; i < m_players.Length; ++i)
            {
                m_pathFinders[i].Update();
                m_taskRunners[i].Update();
            }
        }

        public bool Tick(out CommandsBundle commands)
        {
            for(int i = 0; i < m_players.Length; ++i)
            {
                m_pathFinders[i].Tick();
                m_taskRunners[i].Tick();
            }
          
            List<IMatchPlayerController> defeatedPlayers = null;
            for (int i = 0; i < m_players.Length; ++i)
            {
                IMatchPlayerController playerController = m_players[i];
            
                bool wasInRoom = playerController.IsPlayerInRoom;

                CommandsArray playerCommands;
        
                if(playerController.Tick(out playerCommands))
                {
                    m_hasNewCommands = true;
                }

                if (wasInRoom && !playerController.IsPlayerInRoom)
                {
                    playerCommands = new CommandsArray(playerCommands);

                    Array.Resize(ref playerCommands.Commands, playerCommands.Commands.Length + 1);

                    playerCommands.Commands[playerCommands.Commands.Length - 1] = new Cmd(CmdCode.LeaveRoom, -1);

                    if (defeatedPlayers == null)
                    {
                        defeatedPlayers = new List<IMatchPlayerController>();
                    }

                    defeatedPlayers.Add(playerController);
                }
                else if (!playerController.HasControllableUnits)
                {
                    if (defeatedPlayers == null)
                    {
                        defeatedPlayers = new List<IMatchPlayerController>();
                    }

                    defeatedPlayers.Add(playerController);
                }

                m_serverCommands.Commands[i] = playerCommands;
            }

            if(defeatedPlayers != null)
            {
                for(int i = 0; i < defeatedPlayers.Count; ++i)
                {
                    IMatchPlayerController defeatedPlayer = defeatedPlayers[i];
                    defeatedPlayer.DestroyAllUnitsAndAssets();
                }
            }

            bool wasGameCompleted = m_serverCommands.IsGameCompleted;
            m_serverCommands.IsGameCompleted = IsCompleted();

            if(wasGameCompleted != m_serverCommands.IsGameCompleted)
            {
                m_hasNewCommands = true;
            }

            if (m_hasNewCommands)
            {
                commands = ProtobufSerializer.DeepClone(m_serverCommands);
                if (m_serverCommands.TasksStateInfo.Count > 0)
                {
                    m_serverCommands.TasksStateInfo.Clear();
                }
                if(m_serverCommands.ClientRequests.Count > 0)
                {
                    m_serverCommands.ClientRequests.Clear();
                }
                m_hasNewCommands = false;
                for(int i = 0; i < m_taskEngines.Length; ++i)
                {
                    m_taskEngines[i].Tick();
                }
                
                return true;
            }
            
            commands = null;
            for (int i = 0; i < m_taskEngines.Length; ++i)
            {
                m_taskEngines[i].Tick();
            }
            return false;
        }

        public bool IsCompleted()
        {
            int alivePlayersCount = 0;
            for (int i = 1; i < m_players.Length; ++i) //does not include neutral
            {
                IMatchPlayerController playerController = m_players[i];
                if (playerController.IsPlayerInRoom && playerController.HasControllableUnits)
                {
                    alivePlayersCount++;
                }
            }

            return alivePlayersCount <= 1;
        }

        IMatchPlayerView IMatchView.GetPlayerView(int index)
        {
            return m_players[index];
        }

        IMatchPlayerView IMatchView.GetPlayerView(Guid guid)
        {
            return m_idToPlayers[guid];
        }
    }
}

