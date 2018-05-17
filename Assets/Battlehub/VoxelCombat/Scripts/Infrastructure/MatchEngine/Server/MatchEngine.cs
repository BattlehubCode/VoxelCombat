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
        
        public const int MoveUnconditional = 2;
        public const int MoveConditional = 3;
        public const int RotateLeft = 4;
        public const int RotateRight = 5;
        public const int Cancel = 10;
      
        public const int Split = 20;
        public const int Split4 = 24;
        public const int Grow = 25;
        public const int Diminish = 26;
        public const int Automatic = 40;
        public const int Convert = 50;
        public const int Explode = 75;
        public const int StateChanged = 90;

        public const int Failed = 98; //Last command was failed -> disable animations return to idle state
        public const int LeaveRoom = 99;
        public const int Composite = 100;

        //Debug command
        public const int SetHealth = 200;
        public const int Kill = 201;
    }

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
            int hash = 17 * Weight;
            hash = 17 * hash + Row;
            hash = 17 * hash + Col;
            hash = 17 * hash + Altitude;
            return hash;
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

    [ProtoContract]
    [ProtoInclude(50, typeof(CoordinateCmd))]
    [ProtoInclude(51, typeof(ChangeParamsCmd))]
    [ProtoInclude(52, typeof(CompositeCmd))]
    [ProtoInclude(53, typeof(TargetCmd))]
    public class Cmd
    {
        [ProtoMember(1)]
        public int Code;

        [ProtoMember(2)]
        public long UnitIndex;

        [ProtoMember(3)]
        public int Duration;

        //[ProtoMember(4)]
        //public long Tick; //Delivery tick

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
        [ProtoMember(1, IsRequired = true)]
        public int TargetPlayerIndex = -1;

        [ProtoMember(2, IsRequired = true)]
        public long TargetIndex = -1;

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

        IPathFinder PathFinder
        {
            get;
        }
        ITaskRunner TaskRunner
        {
            get;
        }

        IPathFinder BotPathFinder
        {
            get;
        }

        ITaskRunner BotTaskRunner
        {
            get;
        }


        IMatchUnitController GetUnitController(int playerIndex, long unitIndex);

        IMatchUnitAssetView GetAsset(int playerIndex, long unitIndex);

        void RegisterPlayer(Guid playerId, int playerIndex, Dictionary<int, VoxelAbilities>[] allAbilities);

        void CompletePlayerRegistration();

        bool Tick(out CommandsBundle commands);

        void Destroy();
    }

    public class MatchEngine : IMatchEngine
    {
        //private const bool EnableLog = true;
        public event Action<Guid, Cmd> OnSubmitted;
        private readonly Dictionary<Guid, IMatchPlayerController> m_idToPlayers = new Dictionary<Guid, IMatchPlayerController>();
        private readonly IMatchPlayerController[] m_players;
        private readonly Guid[] m_playerGuids;
        //private readonly List<float> m_rtt = new List<float>();
        private readonly CommandsBundle m_serverCommands = new CommandsBundle();

        private MapRoot m_map;

        public MapRoot Map
        {
            get { return m_map; }
        }


        private IPathFinder m_pathFinder;

        public IPathFinder PathFinder
        {
            get { return m_pathFinder; }
        }

        private ITaskRunner m_taskRunner;

        public ITaskRunner TaskRunner
        {
            get { return m_taskRunner; }
        }

        private IPathFinder m_botPathFinder;

        public IPathFinder BotPathFinder
        {
            get { return m_botPathFinder; }
        }

        private ITaskRunner m_botTaskRunner;

        public ITaskRunner BotTaskRunner
        {
            get { return m_botTaskRunner; }
        }

        int IMatchView.PlayersCount
        {
            get { return m_players.Length; }
        }

        public MatchEngine(MapRoot map, int playersCount)
        {
            m_map = map;

            m_players = new IMatchPlayerController[playersCount];
            m_playerGuids = new Guid[playersCount];

            m_pathFinder = MatchFactory.CreatePathFinder(m_map, playersCount);
            m_taskRunner = MatchFactory.CreateTaskRunner(playersCount);

            m_botPathFinder = MatchFactory.CreatePathFinder(m_map, playersCount);
            m_botTaskRunner = MatchFactory.CreateTaskRunner(playersCount);

            m_map.SetPlayerCount(playersCount);
        }

       

        public void Destroy()
        {
            MatchFactory.DestroyPathFinder(m_pathFinder);
            MatchFactory.DestroyTaskRunner(m_taskRunner);

            MatchFactory.DestroyPathFinder(m_botPathFinder);
            MatchFactory.DestroyTaskRunner(m_botTaskRunner);
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
            m_playerGuids[playerIndex] = playerId;

            if (m_serverCommands.Players == null)
            {
                m_serverCommands.Players = new Guid[1];
                m_serverCommands.Commands = new CommandsArray[1];
            }
            else
            {
                Array.Resize(ref m_serverCommands.Players, m_serverCommands.Players.Length + 1);
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
                    case CmdCode.Automatic:
                        return true;
                    case CmdCode.Convert:
                        return !unitController.DataController.IsCollapsedOrBlocked;
                    case CmdCode.Grow:
                        return unitController.DataController.CanGrow() == true;
                    case CmdCode.Split4:
                        return unitController.DataController.CanSplit4() == true;
                    case CmdCode.Split:
                        return unitController.DataController.CanSplit() == true;
                    case CmdCode.MoveConditional:
                        return !unitController.DataController.IsCollapsedOrBlocked;
                    case CmdCode.Explode:
                        return unitController.Data.Type == (int)KnownVoxelTypes.Bomb;
                }
            }
            return false;
        }

        public void Submit(Guid playerId, Cmd cmd)
        {
            IMatchPlayerController playerController;
            if (m_idToPlayers.TryGetValue(playerId, out playerController))
            {
                playerController.Submit(cmd);
            }
            if(OnSubmitted != null)
            {
                OnSubmitted(playerId, cmd);
            }
        }

        public bool Tick(out CommandsBundle commands)
        {
            m_pathFinder.Tick();
            m_taskRunner.Tick();
            m_botPathFinder.Tick();
            m_botTaskRunner.Tick();

            bool newCommands = false;
            List<IMatchPlayerController> defeatedPlayers = null;
            for (int i = 0; i < m_players.Length; ++i)
            {
                IMatchPlayerController playerController = m_players[i];
                m_serverCommands.Players[i] = m_playerGuids[i];

                bool wasInRoom = playerController.IsPlayerInRoom;

                CommandsArray playerCommands;

                if(playerController.Tick(out playerCommands))
                {
                    newCommands = true;
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
//#warning Temporary disabled due to strange bugs
                    defeatedPlayer.DestroyAllUnitsAndAssets();
                }
            }

            bool wasGameCompleted = m_serverCommands.IsGameCompleted;
            m_serverCommands.IsGameCompleted = IsCompleted();

            if(wasGameCompleted != m_serverCommands.IsGameCompleted)
            {
                newCommands = true;
            }

            commands = m_serverCommands;
            return newCommands;
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

