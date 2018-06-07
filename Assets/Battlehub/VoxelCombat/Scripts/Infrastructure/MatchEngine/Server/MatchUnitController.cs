using System;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public interface IMatchUnitController : IMatchUnitAssetView
    {
        event Action<int> CmdExecuted;

        int Type
        {
            get;
        }

        void SetCommand(Cmd cmd);

        //Tick side effect
        IList<VoxelDataCellPair> CreatedVoxels
        {
            get;
        }

        //Tick side effect
        IList<VoxelData> EatenOrDestroyedVoxels
        {
            get;
        }

        void Tick(out Cmd cmd);

        void Destroy();
    }

    public abstract class MatchUnitControllerBase : IMatchUnitController
    {
        protected VoxelDataState m_prevState; //last tick state
        private VoxelDataState m_state;

        protected int m_ticksBeforeNextCommand;
        protected readonly Queue<Cmd> m_commandsQueue;
        protected readonly IVoxelDataController m_dataController;
        protected readonly List<VoxelDataCellPair> m_createdVoxels;
        protected readonly List<VoxelData> m_eatenOrDestroyedVoxels;

        public event Action<int> CmdExecuted;

        protected VoxelDataState State
        {
            get { return m_state; }
            set { m_state = value; }
        }

        public IList<VoxelDataCellPair> CreatedVoxels
        {
            get { return m_createdVoxels; }
        }

        public IList<VoxelData> EatenOrDestroyedVoxels
        {
            get { return m_eatenOrDestroyedVoxels; }
        }

        public long Id
        {
            get { return m_dataController.ControlledData.UnitOrAssetIndex; }
        }

        public int Type
        {
            get { return m_dataController.ControlledData.Type; }
        }

        public bool IsAlive
        {
            get { return m_dataController.IsAlive; }
        }

        public MapPos Position
        {
            get { return m_dataController.Coordinate.MapPos; }
        }

        public VoxelData Data
        {
            get { return m_dataController.ControlledData; }
        }

        public IVoxelDataController DataController
        {
            get { return m_dataController; }
        }

        public MatchUnitControllerBase(IVoxelDataController dataController)
        {
            m_dataController = dataController;
            m_commandsQueue = new Queue<Cmd>();
            m_createdVoxels = new List<VoxelDataCellPair>();
            m_eatenOrDestroyedVoxels = new List<VoxelData>();
        }

        public void SetCommand(Cmd cmd)
        {
            if(!m_dataController.IsAlive)
            {
                return;
            }

            if(cmd.Code != CmdCode.Cancel && State != VoxelDataState.Idle)
            {
                RaiseCmdFailed(null);
            }
            else
            {
                GoToIdleState();
            }
           
            if (cmd.Code != CmdCode.Cancel && cmd.Code != CmdCode.LeaveRoom)
            {
                if (State == VoxelDataState.Busy)
                {
                    return;
                }
                OnSetCommand(cmd);
            }
        }

        public void Tick(out Cmd cmd)
        {
            if(m_createdVoxels.Count != 0)
            {
                m_createdVoxels.Clear();
            }

            if(m_eatenOrDestroyedVoxels.Count != 0)
            {
                m_eatenOrDestroyedVoxels.Clear();
            }

            if (!m_dataController.IsAlive)
            {
                if(State != VoxelDataState.Idle)
                {
                    RaiseCmdFailed(null);
                }
                else
                {
                    GoToIdleState();
                }
                cmd = null;
                return;
            }

            if (m_ticksBeforeNextCommand == 0)
            {
                cmd = OnTick();

                if (State != m_prevState)
                {
                    if (cmd != null)
                    {
                        cmd = new CompositeCmd
                        {
                            UnitIndex = cmd.UnitIndex,
                            Duration = cmd.Duration,
                            Commands = new[]
                            {
                                cmd,
                                new ChangeParamsCmd(CmdCode.StateChanged)
                                {
                                    UnitIndex = cmd.UnitIndex,
                                    Duration = cmd.Duration,
                                    IntParams = new[]
                                    {
                                        (int)m_prevState,
                                        (int)State
                                    }
                                }
                            }
                        };
                    }
                    else
                    {
                        cmd = new ChangeParamsCmd(CmdCode.StateChanged)
                        {
                            UnitIndex = Id,
                            IntParams = new[]
                            {
                                (int)m_prevState,
                                (int)State
                            }
                        };
                    }

                    bool noFail = m_dataController.SetVoxelDataState(State);
                    System.Diagnostics.Debug.Assert(noFail);
                    m_prevState = State;
                }

                return;
            }
            else
            {
                m_ticksBeforeNextCommand--;
            }

            cmd = null;
            return;
        }

        protected void GoToIdleState()
        {
            OnGoToIdleState();
            m_commandsQueue.Clear();
            State = VoxelDataState.Idle;
        }

        protected void RaiseCmdExecuted()
        {
            GoToIdleState();
            if(CmdExecuted != null)
            {
                CmdExecuted(CmdErrorCode.Success);
            }
        }

        protected void RaiseCmdFailed(Cmd cmd)
        {
            if(cmd != null)
            {
                cmd.ErrorCode = CmdErrorCode.Failed;
            }

            GoToIdleState();
            if(CmdExecuted != null)
            {
                CmdExecuted(CmdErrorCode.Failed);
            }
        }

        protected void OnInstantCmd(Cmd cmd, int duration)
        {
            State = VoxelDataState.Busy;
            cmd.Duration = duration;
            m_commandsQueue.Enqueue(cmd);
        }

        protected virtual void OnGoToIdleState() { }
        protected abstract void OnSetCommand(Cmd cmd);
        protected abstract Cmd OnTick();

        public void Destroy()
        {
            if(State != VoxelDataState.Idle)
            {
                RaiseCmdFailed(null);
            }
        }
    }

    public class VoxelActorUnitController : MatchUnitControllerBase
    {
        protected readonly IPathFinder m_pathFinder;
        private readonly IMatchEngine m_engine;

        private int m_failedMoveAttempts;
        private int m_maxFailedMoveAttempts = 3;

        public VoxelActorUnitController(IVoxelDataController dataController, IMatchEngine engine)
            : base(dataController)
        {
            m_engine = engine;
            m_pathFinder = m_engine.PathFinder;
        }

        protected override void OnSetCommand(Cmd cmd)
        {
            switch (cmd.Code)
            {
                case CmdCode.Move:
                    OnMoveCmd(cmd);
                    break;
                case CmdCode.Split:
                    OnInstantCmd(cmd, m_dataController.Abilities.SplitDuration);
                    break;
                case CmdCode.Split4:
                    OnInstantCmd(cmd, m_dataController.Abilities.SplitDuration);
                    break;
                case CmdCode.Grow:
                    OnInstantCmd(cmd, m_dataController.Abilities.GrowDuration);
                    break;
                case CmdCode.Diminish:
                    OnInstantCmd(cmd, m_dataController.Abilities.DiminishDuration);
                    break;
                case CmdCode.Convert:
                    OnInstantCmd(cmd, m_dataController.Abilities.ConvertDuration);
                    break;
                case CmdCode.SetHealth:
                    OnInstantCmd(cmd, 0);
                    break;
            }
        }

        protected override void OnGoToIdleState()
        {
            if(State == VoxelDataState.SearchingPath)
            {
                m_pathFinder.Terminate(Id, m_dataController.PlayerIndex);
            } 
        }
  
        protected void OnMoveCmd(Cmd cmd)
        {
            CoordinateCmd coordinateCmd = (CoordinateCmd)cmd;
            Coordinate[] path = coordinateCmd.Coordinates;
            if (!ValidatePath(path))
            {
                OnMoveSearchPath(cmd);
                return;
            }

            Coordinate closestCoordinate;
            int coordIndex = Array.IndexOf(path, m_dataController.Coordinate);
            if (coordIndex > -1)  //data control is on path
            {
                path = path.Skip(coordIndex).ToArray();
                if (path.Length > 1)
                {
                    State = VoxelDataState.Moving;
                    PopulateCommandsQueue(Id, path, false, CmdCode.Move);
                }
            }
            else if (DataController.Coordinate.FindClosestTo(path, out closestCoordinate))
            {
                State = VoxelDataState.SearchingPath;
                //find path segment connection current unity coordinate with path found on client
                m_pathFinder.Find(Id, -1, m_dataController.Clone(),
                    new[] { m_dataController.Coordinate, closestCoordinate }, (unitIndex, foundPath) =>
                    {
                        State = VoxelDataState.Moving;
                        PopulateCommandsQueue(Id, Coordinate.MergePath(foundPath, path), false, CmdCode.Move);
                    },
                    null);
            }
            else
            {
                RaiseCmdFailed(cmd);
            }
        }

        protected void OnMoveSearchPath(Cmd cmd)
        {
            //m_ticksBeforeNextCommand = 0;// <-- this will enable immediate commands

            MovementCmd coordinateCmd = (MovementCmd)cmd;
            Coordinate[] cmdCoordinates = coordinateCmd.Coordinates;

            if (cmdCoordinates.Length != 1)
            {
                RaiseCmdFailed(cmd);
                return;
            }

            cmdCoordinates = ToWaypoints(cmdCoordinates);

            State = VoxelDataState.SearchingPath;
            m_pathFinder.Find(Id, -1, m_dataController.Clone(), cmdCoordinates, (unitIndex, path) =>
            {
                State = VoxelDataState.Moving;
                PopulateCommandsQueue(unitIndex, path, false, CmdCode.Move);
            }, null);
        }

   
        protected override Cmd OnTick() //Tick should be able return several commands
        {
            if (State == VoxelDataState.Moving)
            {
                if (m_commandsQueue.Count > 0 && !m_dataController.IsCollapsedOrBlocked)
                {
                    Cmd cmd = m_commandsQueue.Peek();
                    m_ticksBeforeNextCommand = cmd.Duration;

                    bool dequeue = true;
                    switch(cmd.Code)
                    {
                        case CmdCode.Move:
                        {
                            cmd = HandleNextMoveCmd(cmd);
                            if (cmd == null)
                            {
                                m_failedMoveAttempts++;
                                m_failedMoveAttempts %= (m_maxFailedMoveAttempts + 1);
                            }
                            else
                            {
                                m_failedMoveAttempts = 0;
                            }
                            dequeue = cmd != null; //if null then wait a little bit and try again
                            break;
                        }
                            
                        case CmdCode.RotateLeft:
                        {
                            m_dataController.RotateLeft();
                            break;
                        }
                          
                        case CmdCode.RotateRight:
                        {
                            m_dataController.RotateRight();
                            break;
                        }
                           
                        default:
                        {
                            cmd = HandleNextCmd(cmd);
                            dequeue = cmd != null; //if null then wait al little bit and try again
                            break;
                        }      
                    }

                    if (dequeue && m_commandsQueue.Count > 0)
                    {
                        m_commandsQueue.Dequeue();
                    }

                    if (m_commandsQueue.Count == 0)
                    {
                        RaiseCmdExecuted();
                    }

                    return cmd;
                }

                if (m_commandsQueue.Count == 0)
                {
                    RaiseCmdExecuted();
                }

                return null;
            }
            else if (State == VoxelDataState.Busy)
            {
                if (m_commandsQueue.Count > 0)
                {
                    Cmd cmd = m_commandsQueue.Dequeue();
                    m_ticksBeforeNextCommand = cmd.Duration;

                    switch (cmd.Code)
                    {
                        case CmdCode.Split:
                        {
                            CoordinateCmd coordinateCmd = new CoordinateCmd(cmd.Code, cmd.UnitIndex, cmd.Duration);
                            Coordinate[] coordinates;
                            if (m_dataController.Split(out coordinates, EatOrDestroyCallback))
                            {
                                coordinateCmd.Coordinates = coordinates;
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(coordinateCmd);
                            }
                          
                            return coordinateCmd;
                        }

                        case CmdCode.Split4:
                        {
                            CoordinateCmd coordinateCmd = new CoordinateCmd(cmd.Code, cmd.UnitIndex, cmd.Duration);
                            Coordinate[] coordinates;
                            if (m_dataController.Split4(out coordinates))
                            {
                                coordinateCmd.Coordinates = coordinates;
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(coordinateCmd);
                            }
                            
                            return coordinateCmd;
                        }

                        case CmdCode.Grow:
                        {
                            if (m_dataController.Grow(EatOrDestroyCallback))
                            {
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(cmd);
                            }
                            
                            return cmd;
                        }

                        case CmdCode.Diminish:
                        {
                            if (m_dataController.Diminish())
                            {
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(cmd);
                            }
                            
                            return cmd;
                        }
                           
                        case CmdCode.Convert:
                        {
                            ChangeParamsCmd convertCmd = (ChangeParamsCmd)cmd;

                            int type = convertCmd.IntParams[0];

                            if (m_dataController.Convert(type))
                            {
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(cmd);
                            }
                            
                            return cmd;
                        }
                           
                        case CmdCode.SetHealth:
                        {
                            ChangeParamsCmd changeCmd = (ChangeParamsCmd)cmd;
                            int health = changeCmd.IntParams[0];
                            m_dataController.SetHealth(health);
                            RaiseCmdExecuted();
                            return changeCmd;
                        }
                    }
                }
            }

            return null;
        }

        protected virtual Cmd HandleNextMoveCmd(Cmd cmd)
        {
            MovementCmd movementCmd = (MovementCmd)cmd;
            Coordinate to = movementCmd.Coordinates[1];

            bool isLastCmdInSequence = movementCmd.IsLastCmdInSequence;

            const bool considerIdleStateAsValid = true;
            if (!m_dataController.IsValidAndEmpty(to, considerIdleStateAsValid))
            {
                if (m_failedMoveAttempts < m_maxFailedMoveAttempts)
                {
                    //Do not move if there is voxel actor or voxel bomb in one of active states. just wait a little bit
                    return null;
                }
            }

            if (!m_dataController.Move(to, isLastCmdInSequence, EatOrDestroyCallback))
            {
                RaiseCmdFailed(cmd);
            }

            return cmd;
        }

        protected virtual Cmd HandleNextCmd(Cmd cmd)
        {
            return cmd;
        }

        protected void EatOrDestroyCallback(VoxelData eater, VoxelData voxelData, int deltaHealth, int voxelDataHealth)
        {
            m_eatenOrDestroyedVoxels.Add(voxelData);
        }

        protected bool ValidatePath(Coordinate[] path)
        {
            if (path.Length < 2)
            {
                return false;
            }

            for (int i = 1; i < path.Length; ++i)
            {
                int diff = Math.Abs(path[i - 1].Row - path[i].Row) + Math.Abs(path[i - 1].Col - path[i].Col);
                if (diff != 1)
                {
                    return false;
                }
            }
            return true;
        }

        protected Coordinate[] ToWaypoints(Coordinate[] cmdCoordinates)
        {
            Coordinate[] waypoints = new Coordinate[cmdCoordinates.Length + 1];

            waypoints[0] = m_dataController.Coordinate;
            for (int i = 1; i < waypoints.Length; ++i)
            {
                waypoints[i] = cmdCoordinates[i - 1];
            }

            return waypoints;
        }

        protected virtual void PopulateCommandsQueue(long unitIndex, Coordinate[] path, bool isAutomatedAction, int moveCode)
        {
            int dir = m_dataController.ControlledData.Dir;

            for (int i = 0; i < path.Length - 1; ++i)
            {
                Coordinate from = path[i];
                Coordinate to = path[i + 1];

                int rotateLeft = VoxelData.ShouldRotateLeft(dir, from, to);
                int rotateRight = VoxelData.ShouldRotateRight(dir, from, to);
                if (rotateLeft > 0)
                {
                    for (int r = 0; r < rotateLeft; ++r)
                    {
                        dir = VoxelData.RotateLeft(dir);

                        Cmd rotateCmd = new Cmd(CmdCode.RotateLeft, unitIndex);
                        rotateCmd.Duration = m_dataController.Abilities.RotationDuration;
                        m_commandsQueue.Enqueue(rotateCmd);
                    }
                }
                else if (rotateRight > 0)
                {
                    for (int r = 0; r < rotateRight; ++r)
                    {
                        dir = VoxelData.RotateRight(dir);

                        Cmd rotateCmd = new Cmd(CmdCode.RotateRight, unitIndex);
                        rotateCmd.Duration = m_dataController.Abilities.RotationDuration;

                        m_commandsQueue.Enqueue(rotateCmd);
                    }
                }

                MovementCmd moveCmd = new MovementCmd
                {
                    Code = moveCode,
                    Duration = m_dataController.Abilities.MovementDuration,
                    Coordinates = new[] { from, to },
                    UnitIndex = unitIndex
                };

                if (i == path.Length - 1)
                {
                    moveCmd.IsLastCmdInSequence = true && !isAutomatedAction;
                }

                m_commandsQueue.Enqueue(moveCmd);
            }
        }
    }

    public class SpawnerUnitController : MatchUnitControllerBase
    {
        public SpawnerUnitController(IVoxelDataController dataController) 
            : base(dataController)
        {
            m_ticksBeforeNextCommand = dataController.Abilities.ActionInterval;
        }

        protected override void OnSetCommand(Cmd cmd)
        {

        }

        protected override Cmd OnTick()
        {
            m_ticksBeforeNextCommand = DataController.Abilities.ActionInterval;

            Coordinate[] coordinates;
            if (m_dataController.PerformSpawnAction(out coordinates))
            {
                for (int i = 0; i < coordinates.Length; ++i)
                {
                    Coordinate coordinate = coordinates[i];
                    MapCell cell = m_dataController.Map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
                    VoxelData data = cell.GetVoxelDataAt(coordinate.Altitude);
                    m_createdVoxels.Add(new VoxelDataCellPair(data, cell));
                }

                Cmd cmd = new Cmd(CmdCode.Spawn, Id);
                cmd.Duration = m_ticksBeforeNextCommand;
                return cmd;
            }
            return null;
        }
    }

    public class VoxelBombUnitController : VoxelActorUnitController
    {
         public VoxelBombUnitController(IVoxelDataController dataController, IMatchEngine engine) 
            : base(dataController, engine)
        {
        }


        //protected override void OnSetCommand(Cmd cmd)
        //{
        //    if (cmd.Code == CmdCode.MoveSearch)
        //    {
        //        OnMoveWithSearchCmd(cmd);
        //    }
        //    else if(cmd.Code == CmdCode.Move)
        //    {
        //        OnMoveCmd(cmd);
        //    }
        //}

       
        //protected override Cmd HandleNextMoveCmd(Cmd cmd)
        //{
        //    MovementCmd movementCmd = (MovementCmd)cmd;
        //    Coordinate to = movementCmd.Coordinates[1];

        //    if (HasTarget)
        //    {
        //        Coordinate explodeCoordinate;
        //        VoxelData target = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out explodeCoordinate);

        //        if (target == null || explodeCoordinate != to.ToWeight(target.Weight))
        //        {
        //            return base.HandleNextMoveCmd(cmd);
        //        }

        //        if (!m_dataController.Explode(to, target, EatOrDestroyCallback))
        //        {
        //            return base.HandleNextMoveCmd(cmd);
        //        }
        //    }
        //    else
        //    {
        //        return base.HandleNextMoveCmd(cmd);
        //    }

        //    if (!m_dataController.IsAlive)
        //    {
        //        m_commandsQueue.Clear();
        //        //State = VoxelDataState.Dead;
        //    }

        //    to.Altitude = m_dataController.ControlledData.Altitude;
        //    movementCmd.Coordinates[1] = to;
        //    movementCmd.Code = CmdCode.Explode;

        //    return cmd;
        //}

//        protected override Cmd HandleNextCmd(Cmd cmd)
//        {
//            if(cmd.Code == CmdCode.Explode)
//            {
//                Coordinate coordinate = m_dataController.Coordinate;
//                if (HasTarget)
//                {
//                    Coordinate explodeCoordinate;
//                    VoxelData target = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out explodeCoordinate);

//                    Coordinate weightedCoordinate = coordinate.ToWeight(target.Weight);

//#warning Following lines could cause errors 
//                    if (target == null || explodeCoordinate.MapPos != weightedCoordinate.MapPos ||
//                        explodeCoordinate.Altitude != weightedCoordinate.Altitude &&
//                        explodeCoordinate.Altitude + TargetData.Height != weightedCoordinate.Altitude)
//                    {
//                        if (!m_dataController.IsValidAndEmpty(coordinate, false))
//                        {
//                            return TryToFinishMovementInValidCell(cmd);
//                        }

//                        cmd.ErrorCode = CmdErrorCode.Failed;
//                        return cmd;
//                    }
//                    else
//                    {
//                        if (!m_dataController.Explode(coordinate, target, EatOrDestroyCallback))
//                        {
//                            if (!m_dataController.IsValidAndEmpty(coordinate, false))
//                            {
//                                return TryToFinishMovementInValidCell(cmd);
//                            }

//                            cmd.ErrorCode = CmdErrorCode.Failed;
//                            return cmd;
//                        }
//                    }
//                }
//                else
//                {
//                    if (!m_dataController.IsValidAndEmpty(coordinate, false))
//                    {
//                        return TryToFinishMovementInValidCell(cmd);
//                    }

//                    cmd.ErrorCode = CmdErrorCode.Failed;
//                    return cmd;
//                }
//            }

//            return cmd;
//        }
    }
}


/*
 * 
 * using System;
using System.Collections.Generic;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public interface IMatchUnitController : IMatchUnitAssetView
    {
        int Type
        {
            get;
        }

        void SetCommand(Cmd cmd);

        //Tick side effect
        IList<VoxelDataCellPair> CreatedVoxels
        {
            get;
        }

        //Tick side effect
        IList<VoxelData> EatenOrDestroyedVoxels
        {
            get;
        }

        Cmd Tick();

    }

    public abstract class MatchUnitControllerBase : IMatchUnitController
    {
        protected VoxelDataState m_prevState; //last tick state
        private VoxelDataState m_state;

        protected int m_ticksBeforeNextCommand;
        protected readonly Queue<Cmd> m_commandsQueue;
        protected readonly IVoxelDataController m_dataController;
        protected readonly List<VoxelDataCellPair> m_createdVoxels;
        protected readonly List<VoxelData> m_eatenOrDestroyedVoxels;

        protected VoxelDataState State
        {
            get { return m_state; }
            set { m_state = value; }
        }

        public IList<VoxelDataCellPair> CreatedVoxels
        {
            get { return m_createdVoxels; }
        }

        public IList<VoxelData> EatenOrDestroyedVoxels
        {
            get { return m_eatenOrDestroyedVoxels; }
        }

        public long Id
        {
            get { return m_dataController.ControlledData.UnitOrAssetIndex; }
        }

        public int Type
        {
            get { return m_dataController.ControlledData.Type; }
        }

        public MapPos Position
        {
            get { return m_dataController.Coordinate.MapPos; }
        }

        public VoxelData Data
        {
            get { return m_dataController.ControlledData; }
        }

        public IVoxelDataController DataController
        {
            get { return m_dataController; }
        }

        public virtual bool IsDead
        {
            get { return !m_dataController.IsAlive; }
        }

        public MatchUnitControllerBase(IVoxelDataController dataController)
        {
            m_dataController = dataController;
            m_commandsQueue = new Queue<Cmd>();
            m_createdVoxels = new List<VoxelDataCellPair>();
            m_eatenOrDestroyedVoxels = new List<VoxelData>();
        }

        public void SetCommand(Cmd cmd)
        {
            if(!m_dataController.IsAlive)
            {
                return;
            }

            if (cmd.Code == CmdCode.LeaveRoom)
            {
                OnLeaveRoom(cmd);
            }
            else if(cmd.Code == CmdCode.Cancel)
            {
                m_commandsQueue.Clear();
                State = VoxelDataState.Idle;
                OnStop();
            }
            else
            {
                if (State == VoxelDataState.Busy)
                {
                    return;
                }
                OnBeforeSetCommand(cmd);
                OnSetCommand(cmd);
            }
        }

        public Cmd Tick()
        {
            if(m_createdVoxels.Count != 0)
            {
                m_createdVoxels.Clear();
            }

            if(m_eatenOrDestroyedVoxels.Count != 0)
            {
                m_eatenOrDestroyedVoxels.Clear();
            }

            if (!m_dataController.IsAlive)
            {
                m_commandsQueue.Clear();
                return null;
            }

            if (m_ticksBeforeNextCommand == 0)
            {
                Cmd result = OnTick();

                if (State != m_prevState)
                {
                    if (result != null)
                    {
                        result = new CompositeCmd
                        {
                            UnitIndex = result.UnitIndex,
                            Duration = result.Duration,
                            Commands = new[]
                            {
                                result,
                                new ChangeParamsCmd(CmdCode.StateChanged)
                                {
                                    UnitIndex = result.UnitIndex,
                                    Duration = result.Duration,
                                    IntParams = new[]
                                    {
                                        (int)m_prevState,
                                        (int)State
                                    }
                                }
                            }
                        };
                    }
                    else
                    {
                        result = new ChangeParamsCmd(CmdCode.StateChanged)
                        {
                            UnitIndex = Id,
                            IntParams = new[]
                            {
                                (int)m_prevState,
                                (int)State
                            }
                        };
                    }

                    bool noFail = m_dataController.SetVoxelDataState(State);
                    System.Diagnostics.Debug.Assert(noFail);
                    m_prevState = State;
                }

                return result;
            }
            else
            {
                m_ticksBeforeNextCommand--;
            }

            return null;
        }

        protected virtual void OnAction(Cmd cmd, int duration)
        {
            State = VoxelDataState.Busy;
            m_commandsQueue.Clear();

            cmd.Duration = duration;
            m_commandsQueue.Enqueue(cmd);
        }

        protected virtual void OnLeaveRoom(Cmd cmd)
        {
            State = VoxelDataState.Idle;
            m_commandsQueue.Clear();
        }

        protected abstract void OnStop();
        protected abstract void OnBeforeSetCommand(Cmd cmd);
        protected abstract void OnSetCommand(Cmd cmd);
        protected abstract Cmd OnTick();
    }


    public class SpawnerUnitController : MatchUnitControllerBase
    {
        public SpawnerUnitController(IVoxelDataController dataController) 
            : base(dataController)
        {
            m_ticksBeforeNextCommand = dataController.Abilities.ActionInterval;
        }

        protected override void OnBeforeSetCommand(Cmd cmd)
        {
            
        }

        protected override void OnSetCommand(Cmd cmd)
        {

        }

        protected override void OnStop()
        {
            
        }

        protected override Cmd OnTick()
        {
            m_ticksBeforeNextCommand = DataController.Abilities.ActionInterval;

            Coordinate[] coordinates;
            if (m_dataController.PerformSpawnAction(out coordinates))
            {
                for (int i = 0; i < coordinates.Length; ++i)
                {
                    Coordinate coordinate = coordinates[i];
                    MapCell cell = m_dataController.Map.Get(coordinate.Row, coordinate.Col, coordinate.Weight);
                    VoxelData data = cell.GetVoxelDataAt(coordinate.Altitude);
                    m_createdVoxels.Add(new VoxelDataCellPair(data, cell));
                }

                Cmd cmd = new Cmd(CmdCode.Spawn, Id);
                cmd.Duration = m_ticksBeforeNextCommand;
                return cmd;
            }
            return null;
        }
    }

    public class VoxelBombUnitController : VoxelActorUnitController
    {
         public VoxelBombUnitController(IVoxelDataController dataController, IMatchEngine engine) 
            : base(dataController, engine)
        {
        }


        protected override void OnSetCommand(Cmd cmd)
        {
            if (cmd.Code == CmdCode.MoveConditional)
            {
                OnMoveConditional(cmd);
            }
            else if(cmd.Code == CmdCode.MoveUnconditional)
            {
                OnMoveUnconditinal(cmd);
            }
        }

        protected override Cmd OnMoveConditionalCmd(Cmd cmd)
        {
            MovementCmd movementCmd = (MovementCmd)cmd;
            Coordinate to = movementCmd.Coordinates[1];

            if (HasTarget)
            {
                Coordinate explodeCoordinate;
                VoxelData target = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out explodeCoordinate);

                if (target == null || explodeCoordinate != to.ToWeight(target.Weight))
                {
                    return base.OnMoveConditionalCmd(cmd);
                }

                if (!m_dataController.Explode(to, target, EatOrDestroyCallback))
                {
                    return base.OnMoveConditionalCmd(cmd);
                }
            }
            else
            {
                return base.OnMoveConditionalCmd(cmd);
            }

            if (!m_dataController.IsAlive)
            {
                m_commandsQueue.Clear();
                //State = VoxelDataState.Dead;
            }

            to.Altitude = m_dataController.ControlledData.Altitude;
            movementCmd.Coordinates[1] = to;
            movementCmd.Code = CmdCode.Explode;

            return cmd;
        }

        protected override Cmd OnMoveUnconditionalCmd(Cmd cmd)
        {
            MovementCmd movementCmd = (MovementCmd)cmd;
            Coordinate to = movementCmd.Coordinates[1];

            if (HasTarget)
            {
                Coordinate explodeCoordinate;
                VoxelData target = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out explodeCoordinate);

                if (target == null || explodeCoordinate != to.ToWeight(target.Weight))
                {
                    return base.OnMoveUnconditionalCmd(cmd);
                }

                if (!m_dataController.Explode(to, target, EatOrDestroyCallback))
                {
                    return base.OnMoveUnconditionalCmd(cmd);
                }
            }
            else
            {
                return base.OnMoveUnconditionalCmd(cmd);
            }

            if (!m_dataController.IsAlive)
            {
                m_commandsQueue.Clear();
                //State = VoxelDataState.Dead;
            }

            to.Altitude = m_dataController.ControlledData.Altitude;
            movementCmd.Coordinates[1] = to;
            movementCmd.Code = CmdCode.Explode;

            return cmd;
        }

        protected override Cmd OnCustomCommand(Cmd cmd)
        {
            if(cmd.Code == CmdCode.Explode)
            {
                Coordinate coordinate = m_dataController.Coordinate;
                if (HasTarget)
                {
                    Coordinate explodeCoordinate;
                    VoxelData target = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out explodeCoordinate);

                    Coordinate weightedCoordinate = coordinate.ToWeight(target.Weight);

#warning Following lines could cause errors 
                    if (target == null || explodeCoordinate.MapPos != weightedCoordinate.MapPos ||
                        explodeCoordinate.Altitude != weightedCoordinate.Altitude &&
                        explodeCoordinate.Altitude + TargetData.Height != weightedCoordinate.Altitude)
                    {
                        if (!m_dataController.IsValidAndEmpty(coordinate, false))
                        {
                            return TryToFinishMovementInValidCell(cmd);
                        }

                        cmd.ErrorCode = CmdErrorCode.Failed;
                        return cmd;
                    }
                    else
                    {
                        if (!m_dataController.Explode(coordinate, target, EatOrDestroyCallback))
                        {
                            if (!m_dataController.IsValidAndEmpty(coordinate, false))
                            {
                                return TryToFinishMovementInValidCell(cmd);
                            }

                            cmd.ErrorCode = CmdErrorCode.Failed;
                            return cmd;
                        }
                    }
                }
                else
                {
                    if (!m_dataController.IsValidAndEmpty(coordinate, false))
                    {
                        return TryToFinishMovementInValidCell(cmd);
                    }

                    cmd.ErrorCode = CmdErrorCode.Failed;
                    return cmd;
                }
            }

            return cmd;
        }

        protected override void PopulateCommandsQueue(long unitIndex, Coordinate[] path, bool isAutomatedAction, int cmdCode)
        {
            if (path.Length > 1)
            {
                base.PopulateCommandsQueue(unitIndex, path, isAutomatedAction, cmdCode);
            }
            else if (path.Length == 1)
            {
                if(TargetData != null)
                {
                    Coordinate targetCoordinate;
                    GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out targetCoordinate);

                    targetCoordinate.Altitude = m_dataController.Coordinate.Altitude;

                    if(Coordinate.EqualWithNormalizedWeight(targetCoordinate, m_dataController.Coordinate))
                    {
                        m_commandsQueue.Enqueue(new TargetCmd(CmdCode.Explode)
                        {
                            UnitIndex = unitIndex,
                            TargetIndex = TargetData.UnitOrAssetIndex,
                            TargetPlayerIndex = TargetData.Owner
                        });
                    }

                    
                }
            }   
        }

        protected override void HandleTargetCoordinateChange(Coordinate targetCoordinate)
        {
            if(InAutomaticMode)
            {
                m_pathFinder.Terminate(Id, m_dataController.PlayerIndex);
                m_commandsQueue.Clear(); 
                State = VoxelDataState.Idle;
            }
            else
            {
                base.HandleTargetCoordinateChange(targetCoordinate);
            }
            
        }

        protected override Cmd OnAutomaticAction()
        {
            if (State == VoxelDataState.Idle)
            {
                int visionRadius = m_dataController.Abilities.VisionRadius;
                FindAndMoveToClosest(GetExplodableAncestors, visionRadius, 1, -1, -1, (explodableAncestorFound, ancestorData, pathToAncestor) =>
                {
                    if(explodableAncestorFound)
                    {
                        TargetCoordinate = pathToAncestor.Last();
                        SetWaypoits(new[] { TargetCoordinate });

                        TargetCoordinate = TargetCoordinate.ToWeight(ancestorData.Weight);
                        TargetData = ancestorData;
                        TargetDir = TargetData.Dir;

                        UnityEngine.Debug.Assert(
                            TargetData.Owner != m_dataController.PlayerIndex ||
                            TargetData.UnitOrAssetIndex != m_dataController.ControlledData.UnitOrAssetIndex);
                    }
                    else
                    {
                        FindAndMoveToClosest(GetExplodableOfEqualWeight, visionRadius, 1, -1, -1, (explodableOfEqualWeightFound, data, pathToEqual) =>
                        {
                            if (explodableOfEqualWeightFound)
                            {
                                TargetCoordinate = pathToEqual.Last();
                                SetWaypoits(new[] { TargetCoordinate });

                                TargetCoordinate = TargetCoordinate.ToWeight(data.Weight);
                                TargetData = data;
                                TargetDir = TargetData.Dir;

                                UnityEngine.Debug.Assert(
                                    TargetData.Owner != m_dataController.PlayerIndex ||
                                    TargetData.UnitOrAssetIndex != m_dataController.ControlledData.UnitOrAssetIndex);
                            }
                            else
                            {
                                FindAndMoveToClosest(GetAttackableDescendants, visionRadius, 1, -1, -1, (attackableFound, attackableData, pathToAttackable) =>
                                {
                                    if(attackableFound)
                                    {
                                        TargetCoordinate = pathToAttackable.Last();
                                        SetWaypoits(new[] { TargetCoordinate });

                                        TargetCoordinate = TargetCoordinate.ToWeight(attackableData.Weight);
                                        TargetData = attackableData;
                                        TargetDir = TargetData.Dir;

                                        UnityEngine.Debug.Assert(
                                            TargetData.Owner != m_dataController.PlayerIndex ||
                                            TargetData.UnitOrAssetIndex != m_dataController.ControlledData.UnitOrAssetIndex);
                                    }
                                });
                            }
                        });
                    }
                });
            }
            return null;
        }

        private VoxelData GetExplodableAncestors(MapCell cell)
        {
            VoxelData destroyer = m_dataController.ControlledData;
            MapCell parent = cell.Parent;
            int delta = GameConstants.ExplodableWeightDelta - 1;
            while(parent != null && delta >= 0)
            {
                VoxelData result = parent.GetVoxelData(data => data.Owner != destroyer.Owner && data.IsExplodableBy(destroyer.Type, destroyer.Weight));
                if (result != null)
                {
                    return result;
                }

                parent = parent.Parent;
                delta--;
            }
            
            return null;
        }

        private VoxelData GetExplodableOfEqualWeight(MapCell cell)
        {
            VoxelData destroyer = m_dataController.ControlledData;
            return cell.GetVoxelData(data => data.Owner != destroyer.Owner && data.IsExplodableBy(destroyer.Type, destroyer.Weight));
        }

        private VoxelData GetAttackableDescendants(MapCell cell)
        {
            VoxelData destroyer = m_dataController.ControlledData;
            return cell.GetDescendantsWithVoxelData(data => data.IsAttackableBy(destroyer));
        }
    }

    public class VoxelActorUnitController : MatchUnitControllerBase
    {
        protected IPathFinder m_pathFinder;
        private ITaskRunner m_taskRunner;
        private IMatchEngine m_engine;
        private Coordinate[] m_waypoints;
        private VoxelData m_targetData;
        private Coordinate m_targetCoordinate;
        private int m_targetDir;
        private int m_ticksBeforeTargetCheck;
        private int m_ticksBeforeTargetCheckMultiplier = 1;
        private bool m_inAutomaticMode;

        private int m_failedMoveAttempts;
        private int m_maxFailedMoveAttempts = 3;

        protected bool InAutomaticMode
        {
            get { return m_inAutomaticMode; }
        }

        protected long TargetId
        {
            get { return TargetData != null ? TargetData.UnitOrAssetIndex : -1; }
        }

        protected VoxelData TargetData
        {
            get { return m_targetData; }
            set { m_targetData = value; }
        }

        protected Coordinate TargetCoordinate
        {
            get { return m_targetCoordinate; }
            set { m_targetCoordinate = value; }
        }

        protected int TargetDir
        {
            get { return m_targetDir; }
            set { m_targetDir = value; }
        }


        protected bool HasTarget
        {
            get { return m_targetData != null; }
        }

        public VoxelActorUnitController(IVoxelDataController dataController, IMatchEngine engine)
            : base(dataController)
        {
            m_engine = engine;
            m_pathFinder = m_engine.PathFinder;
            m_taskRunner = m_engine.TaskRunner;
        }

        protected override void OnStop()
        {
            m_pathFinder.Terminate(Id, m_dataController.PlayerIndex);
            m_taskRunner.Terminate(Id, m_dataController.PlayerIndex);
        }

        protected override void OnBeforeSetCommand(Cmd cmd)
        {
            m_failedMoveAttempts = 0;

            m_taskRunner.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);

            TargetData = null;

            if (cmd.Code == CmdCode.Automatic)
            {
                m_inAutomaticMode = !m_inAutomaticMode;
                if (State == VoxelDataState.SearchingPath)
                {
                    m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
                }


                State = VoxelDataState.Idle;
                m_commandsQueue.Clear();

            }
            else
            {
                m_inAutomaticMode = false;
            }
        }

        protected override void OnSetCommand(Cmd cmd)
        {
            if (cmd.Code == CmdCode.MoveUnconditional)
            {
                OnMoveUnconditinal(cmd);
            }
            else if (cmd.Code == CmdCode.MoveConditional)
            {
                OnMoveConditional(cmd);
            }
            else if (cmd.Code == CmdCode.Split)
            {
                OnAction(cmd, m_dataController.Abilities.SplitDuration);
            }
            else if (cmd.Code == CmdCode.Split4)
            {
                OnAction(cmd, m_dataController.Abilities.SplitDuration);
            }
            else if (cmd.Code == CmdCode.Grow)
            {
                OnAction(cmd, m_dataController.Abilities.GrowDuration);
            }
            else if (cmd.Code == CmdCode.Diminish)
            {
                OnAction(cmd, m_dataController.Abilities.DiminishDuration);
            }
            else if (cmd.Code == CmdCode.Convert)
            {
                OnAction(cmd, m_dataController.Abilities.ConvertDuration);
            }
            else if (cmd.Code == CmdCode.SetHealth)
            {
                OnAction(cmd, 0);
            }
        }


        protected override void OnLeaveRoom(Cmd cmd)
        {
            if (State == VoxelDataState.SearchingPath)
            {
                m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
            }

            State = VoxelDataState.Idle;
            m_commandsQueue.Clear();
        }

        private bool ValidatePath(Coordinate[] path)
        {
            if(path.Length < 2)
            {
                return false;
            }

            for(int i = 1; i < path.Length; ++i)
            {
                int diff = Math.Abs(path[i - 1].Row - path[i].Row) + Math.Abs(path[i - 1].Col - path[i].Col);
                if(diff != 1)
                {
                    return false;
                }
            }
            return true;
        }


        protected void OnMoveUnconditinal(Cmd cmd)
        {
            CoordinateCmd coordinateCmd = (CoordinateCmd)cmd;
            Coordinate[] path = coordinateCmd.Coordinates;
            if(!ValidatePath(path))
            {
                return;
            }

            if (State == VoxelDataState.SearchingPath)
            {
                m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
            }

            m_waypoints = null;
            m_commandsQueue.Clear();
            CancelTarget();

            Coordinate closestCoordinate;
            int coordIndex = Array.IndexOf(path, m_dataController.Coordinate);
            if (coordIndex > -1)  //data control is on path
            {
                path = path.Skip(coordIndex).ToArray();
                if(path.Length > 1)
                {
                    State = VoxelDataState.Moving;
                    PopulateCommandsQueue(Id, path, false, CmdCode.MoveUnconditional);
                }
            }
            else if(DataController.Coordinate.FindClosestTo(path, out closestCoordinate))
            {
                //find path segment connection current unity coordinate with path found on client
                m_pathFinder.Find(Id, -1, m_dataController.Clone(),
                    new[] { m_dataController.Coordinate, closestCoordinate }, (unitIndex, foundPath) =>
                    {
                        State = VoxelDataState.Moving;
                        PopulateCommandsQueue(Id, Coordinate.MergePath(foundPath, path), false, CmdCode.MoveUnconditional);
                    },
                    null);
            } 
            else
            {
                State = VoxelDataState.Idle;
            } 
        }

        protected void OnMoveConditional(Cmd cmd)
        {
            if (State == VoxelDataState.SearchingPath)
            {
                m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
            }

            State = VoxelDataState.SearchingPath;

            //m_ticksBeforeNextCommand = 0;// <-- this will enable immediate commands
          
            m_commandsQueue.Clear();

            MovementCmd coordinateCmd = (MovementCmd)cmd;

            if(coordinateCmd.HasTarget)
            {
                TargetData = GetTargetData(coordinateCmd.TargetIndex, coordinateCmd.TargetPlayerIndex, out m_targetCoordinate);

                if (TargetData == null || 
                    coordinateCmd.TargetIndex == coordinateCmd.UnitIndex &&
                    coordinateCmd.TargetPlayerIndex == m_dataController.PlayerIndex)
                {
                    CancelTarget();
                }
                else
                {
                    m_targetDir = TargetData.Dir;
                }
            }
            else
            {
                CancelTarget();
            }

            Coordinate[] cmdCoordinates = coordinateCmd.Coordinates;

            SetWaypoits(cmdCoordinates);

            m_pathFinder.Find(Id, -1, m_dataController.Clone(), m_waypoints, (unitIndex, path) =>
            {
                if (HasTarget && path[path.Length - 1] != m_waypoints[m_waypoints.Length - 1])
                {
                    UnityEngine.Debug.Log("Targeted Search");
                    m_pathFinder.Find(Id, TargetId, m_dataController.Clone(), m_waypoints, (altUnitIndex, altPath) =>
                    {
                        State = VoxelDataState.Moving;
                        PopulateCommandsQueue(altUnitIndex, altPath, false, CmdCode.MoveConditional);
                    }, null);
                }
                else
                {
                    State = VoxelDataState.Moving;
                    PopulateCommandsQueue(unitIndex, path, false, CmdCode.MoveConditional);
                }
                
            }, null);
        }

     
        protected void SetWaypoits(Coordinate[] cmdCoordinates)
        {
            m_waypoints = new Coordinate[cmdCoordinates.Length + 1];

            m_waypoints[0] = m_dataController.Coordinate;
            for (int i = 1; i < m_waypoints.Length; ++i)
            {
                m_waypoints[i] = cmdCoordinates[i - 1];
            }
        }

        protected virtual void PopulateCommandsQueue(long unitIndex, Coordinate[] path, bool isAutomatedAction, int moveCode)
        {
            int dir = m_dataController.ControlledData.Dir;

            for (int i = 0; i < path.Length - 1; ++i)
            {
                Coordinate from = path[i];
                Coordinate to = path[i + 1];

                int rotateLeft = VoxelData.ShouldRotateLeft(dir, from, to);
                int rotateRight = VoxelData.ShouldRotateRight(dir, from, to);
                if (rotateLeft > 0)
                {
                    for (int r = 0; r < rotateLeft; ++r)
                    {
                        dir = VoxelData.RotateLeft(dir);

                        Cmd rotateCmd = new Cmd(CmdCode.RotateLeft, unitIndex);
                        rotateCmd.Duration = m_dataController.Abilities.RotationDuration;
                        m_commandsQueue.Enqueue(rotateCmd);
                    }
                }
                else if (rotateRight > 0)
                {
                    for (int r = 0; r < rotateRight; ++r)
                    {
                        dir = VoxelData.RotateRight(dir);

                        Cmd rotateCmd = new Cmd(CmdCode.RotateRight, unitIndex);
                        rotateCmd.Duration = m_dataController.Abilities.RotationDuration;

                        m_commandsQueue.Enqueue(rotateCmd);
                    }
                }

                MovementCmd moveCmd = new MovementCmd
                {
                    Code = moveCode,
                    Duration = m_dataController.Abilities.MovementDuration,
                    Coordinates = new[] { from, to },
                    UnitIndex = unitIndex,
                    HasTarget = TargetData != null,
                    TargetIndex = TargetData != null ? TargetData.UnitOrAssetIndex : -1,
                    TargetPlayerIndex = TargetData != null ? TargetData.Owner : -1
                };

                if(i == path.Length - 1)
                {
                    moveCmd.IsLastCmdInSequence = true && !isAutomatedAction;    
                }

                m_commandsQueue.Enqueue(moveCmd);
            }
        }

        protected override void OnAction(Cmd cmd, int duration)
        {
            if (State == VoxelDataState.SearchingPath)
            {
                m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
            }

            State = VoxelDataState.Busy;
            m_commandsQueue.Clear();

            cmd.Duration = duration;
            m_commandsQueue.Enqueue(cmd);
        }


        //#warning DEBUG
        //private static long m_cmdNumber = 0;

        protected override Cmd OnTick() //Tick should be able return several commands
        {
            if (TargetData != null)
            {
                TargetCheck();
            }

            if (State == VoxelDataState.Moving)
            {
                if (m_commandsQueue.Count > 0 && !m_dataController.IsCollapsedOrBlocked)
                {
                    Cmd cmd = m_commandsQueue.Peek();
                    //cmd.Number = m_cmdNumber++;
                    m_ticksBeforeNextCommand = cmd.Duration;

                    bool dequeue = true;

                    if (cmd.Code == CmdCode.MoveUnconditional)
                    {
                        cmd = OnMoveUnconditionalCmd(cmd);
                        if (cmd == null)
                        {
                            if(State != VoxelDataState.Idle)
                            {
                                dequeue = false;
                            }
                        }
                    }
                    else if (cmd.Code == CmdCode.MoveConditional)
                    {
                        cmd = OnMoveConditionalCmd(cmd);
                        if(cmd == null)
                        {
                            m_failedMoveAttempts++;
                            m_failedMoveAttempts %= (m_maxFailedMoveAttempts + 1);
                        }
                        else
                        {
                            m_failedMoveAttempts = 0;
                        }
                        dequeue = cmd != null; //if null then wait a little bit and try again
                    }
                    else if (cmd.Code == CmdCode.RotateLeft)
                    {
                        m_dataController.RotateLeft();
                    }
                    else if (cmd.Code == CmdCode.RotateRight)
                    {
                        m_dataController.RotateRight();
                    }
                    else
                    {
                        cmd = OnCustomCommand(cmd);
                        dequeue = cmd != null; //if null then wait al little bit and try again
                    }

                    if (dequeue && m_commandsQueue.Count > 0)
                    {
                        m_commandsQueue.Dequeue();
                    }

                    return cmd;
                }

                if (m_commandsQueue.Count == 0)
                {
                    State = VoxelDataState.Idle;

                }

                return null;
            }
            else if (State == VoxelDataState.Busy)
            {
                if (m_commandsQueue.Count > 0)
                {
                    Cmd cmd = m_commandsQueue.Dequeue();
                    m_ticksBeforeNextCommand = cmd.Duration;

                    if (cmd.Code == CmdCode.Split)
                    {
                        CoordinateCmd coordinateCmd = new CoordinateCmd(cmd.Code, cmd.UnitIndex, cmd.Duration);
                        Coordinate[] coordinates;
                        if (m_dataController.Split(out coordinates, EatOrDestroyCallback))
                        {
                            coordinateCmd.Coordinates = coordinates;
                        }
                        else
                        {
                            coordinateCmd.ErrorCode = CmdErrorCode.Failed;
                        }
                        State = VoxelDataState.Idle;
                        return coordinateCmd;
                    }
                    else if (cmd.Code == CmdCode.Split4)
                    {
                        CoordinateCmd coordinateCmd = new CoordinateCmd(cmd.Code, cmd.UnitIndex, cmd.Duration);
                        Coordinate[] coordinates;
                        if (m_dataController.Split4(out coordinates))
                        {
                            coordinateCmd.Coordinates = coordinates;
                        }
                        else
                        {
                            coordinateCmd.ErrorCode = CmdErrorCode.Failed;
                        }
                        State = VoxelDataState.Idle;
                        return coordinateCmd;
                    }
                    else if (cmd.Code == CmdCode.Grow)
                    {
                        if (!m_dataController.Grow(EatOrDestroyCallback))
                        {
                            cmd.ErrorCode = CmdErrorCode.Failed;
                        }

                        State = VoxelDataState.Idle;
                        return cmd;
                    }
                    else if (cmd.Code == CmdCode.Diminish)
                    {
                        if (!m_dataController.Diminish())
                        {
                            cmd.ErrorCode = CmdErrorCode.Failed;
                        }

                        State = VoxelDataState.Idle;
                        return cmd;
                    }
                    else if (cmd.Code == CmdCode.Convert)
                    {
                        ChangeParamsCmd convertCmd = (ChangeParamsCmd)cmd;

                        int type = convertCmd.IntParams[0];

                        if (!m_dataController.Convert(type))
                        {
                            cmd.ErrorCode = CmdErrorCode.Failed;
                        }

                        State = VoxelDataState.Idle;
                        return cmd;

                    }
                    else if (cmd.Code == CmdCode.SetHealth)
                    {
                        ChangeParamsCmd changeCmd = (ChangeParamsCmd)cmd;
                        int health = changeCmd.IntParams[0];

                        m_dataController.SetHealth(health);

                        State = VoxelDataState.Idle;

                        return changeCmd;
                    }
                    else
                    {
                        throw new System.InvalidOperationException(string.Format("cmd.Code {0} is invalid in current state", cmd.Code));
                    }
                }
            }


            if (m_inAutomaticMode)
            {
                return OnAutomaticAction();
            }

            return null;
        }

        protected VoxelData GetTargetData(long targetIndex, int targetPlayerIndex, out Coordinate coordinate)
        {            
            IMatchUnitController controller = m_engine.GetUnitController(targetPlayerIndex, targetIndex);
            if(controller == null)
            {
                IMatchUnitAssetView asset = m_engine.GetAsset(targetPlayerIndex, targetIndex);
                if(asset == null)
                {
                    coordinate = new Coordinate();
                    return null;
                }

                if(!asset.Data.IsAlive)
                {
                    coordinate = new Coordinate();
                    return null;
                }

                coordinate = new Coordinate(asset.Position, asset.Data);
                return asset.Data;
            }

            if (!controller.DataController.IsAlive)
            {
                coordinate = new Coordinate();
                return null;
            }


            coordinate = controller.DataController.Coordinate;
            return controller.DataController.ControlledData;
        }

        protected void TargetCheck()
        {
            if (m_ticksBeforeTargetCheck == 0 || TargetData.Dir != m_targetDir)
            {
                Coordinate targetCoordinate;
                TargetData = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out targetCoordinate);
                if(TargetData == null)
                {
                    CancelTarget();
                    return;
                }
                else
                {
                    if(targetCoordinate != m_targetCoordinate)
                    {
                        UnityEngine.Debug.Log("TargetChange");
                        if (State == VoxelDataState.SearchingPath)
                        {
                            if(TargetData.Dir == m_targetDir)
                            {
                                m_ticksBeforeTargetCheckMultiplier *= 2;
                            }
                        }

                        HandleTargetCoordinateChange(targetCoordinate);
                    }
                }
                
                m_ticksBeforeTargetCheck = m_dataController.Abilities.TargetCheckInterval;
                m_ticksBeforeTargetCheck *= m_ticksBeforeTargetCheckMultiplier;

                m_targetDir = TargetData.Dir;
            }
            else
            {
                m_ticksBeforeTargetCheck--;
            }
        }
       

        protected void CancelTarget()
        {
            TargetData = null;
            m_targetDir = -1;
            m_ticksBeforeTargetCheck = 0;
            m_ticksBeforeTargetCheckMultiplier = 1;
        }

        protected virtual void HandleTargetCoordinateChange(Coordinate targetCoordinate)
        {
            m_targetCoordinate = targetCoordinate;

            m_pathFinder.Terminate(Id, m_dataController.PlayerIndex);
            m_commandsQueue.Clear(); //<-- continue to move instead

            State = VoxelDataState.SearchingPath;
            SetWaypoits(new[] { m_targetCoordinate.ToWeight(m_dataController.ControlledData.Weight) });
            m_pathFinder.Find(Id, TargetId, m_dataController.Clone(), m_waypoints, (unitIndex, path) =>
            {
                State = VoxelDataState.Moving;
                PopulateCommandsQueue(unitIndex, path, false, CmdCode.MoveConditional);
            }, 
            null);
        }

        
        protected virtual Cmd OnAutomaticAction()
        {
            if(State == VoxelDataState.Idle && m_dataController.ControlledData.Health < m_dataController.Abilities.MaxHealth)
            {
                int visionRadius = m_dataController.Abilities.VisionRadius;
                FindAndMoveToClosest(GetAttackableDescendants, visionRadius, 1, -1, -1, (done, data, path) => 
                {
                    if (done)
                    {
                        SetWaypoits(new[] { path.Last() });
                    }
                });
            }
            return null;
        }


        private class FindClosestContext
        {
            public VoxelData ControlledVoxel;

            public Func<MapCell, VoxelData> FilterCallback;
            public Action<bool, VoxelData, Coordinate[]> CompletedCallback;

            public int Radius;
            public int CurrentRadius;
            public int DeltaRow;
            public int DeltaCol;

            public bool Found;

            public FindClosestContext(VoxelData controlledVoxel, Func<MapCell, VoxelData> filterCallback, int radius, int currentRadius, int deltaRow, int deltaCol, Action<bool, VoxelData, Coordinate[]> completedCallback)
            {
                ControlledVoxel = controlledVoxel;
                FilterCallback = filterCallback;
                CompletedCallback = completedCallback;
                Radius = radius;
                CurrentRadius = currentRadius;
                DeltaRow = deltaRow;
                DeltaCol = deltaCol;
            }
        }
        protected void FindAndMoveToClosest(Func<MapCell, VoxelData> filterCallback, int radius, int currentRadius, int deltaRow, int deltaCol, Action<bool, VoxelData, Coordinate[]> completedCallback)
        {
            Coordinate to = m_dataController.Coordinate.Add(deltaRow ,  deltaCol );
        
            int mapSize = m_dataController.MapSize;

MapCell cell = null;
            if(to.Row >= 0 && to.Row<mapSize && to.Col >= 0 && to.Col<mapSize)
            {
                if(deltaRow != 0 || deltaCol != 0)
                {
                    cell = m_dataController.Map.Get(to.Row, to.Col, to.Weight);
                }
            }
            VoxelData data = filterCallback(cell);
            if (cell != null && data != null)
            {
                to.Altitude = data.Altitude;

                State = VoxelDataState.SearchingPath;

              
                m_pathFinder.Find(Id, -1, m_dataController.Clone(), new[] { m_dataController.Coordinate, to }, (unitIndex, path) =>
                {
                    if (to != path[path.Length - 1])
                    {
                        deltaCol++;

                        if (deltaRow == currentRadius && deltaCol == currentRadius + 1)
                        {
                            currentRadius++;
                            if (currentRadius > radius)
                            {
                                m_ticksBeforeNextCommand = 10;
                                State = VoxelDataState.Idle;
                                completedCallback(false, null, null);
                                return;
                            }

                            deltaRow = -currentRadius;
                            deltaCol = -currentRadius;
                        }

                        else if (deltaCol == currentRadius + 1)
                        {
                            deltaRow++;
                            deltaCol = -currentRadius;
                        }

                        FindAndMoveToClosest(filterCallback, radius, currentRadius, deltaRow, deltaCol, completedCallback);
                    }
                    else
                    {
                        State = VoxelDataState.Moving;
                        completedCallback(true, data, path);
                        PopulateCommandsQueue(unitIndex, path, false, CmdCode.MoveConditional);
                    }
                },
                null);
            }
            else
            {
                State = VoxelDataState.SearchingPath;

                m_taskRunner.Run(Id, m_dataController.PlayerIndex, 
                    new FindClosestContext(m_dataController.ControlledData, filterCallback, radius, currentRadius, deltaRow, deltaCol, completedCallback),
                    FindClosestMatchingCell,
                    FindClosestMatchingCellCompleted,
                    null);
            }
        }

        private VoxelData GetAttackableDescendants(MapCell cell)
{
    VoxelData destroyer = m_dataController.ControlledData;
    return cell.GetDescendantsWithVoxelData(data => data.IsAttackableBy(destroyer) && (data.IsNeutral || data.Owner == destroyer.Owner));
}

private void FindClosestMatchingCellCompleted(long unitIndex, object ctx, object result)
{
    FindClosestContext context = (FindClosestContext)ctx;
    if (context.Found)
    {
        FindAndMoveToClosest(context.FilterCallback, context.Radius, context.CurrentRadius, context.DeltaRow, context.DeltaCol, context.CompletedCallback);
    }
    else
    {
        State = VoxelDataState.Idle;
        context.CompletedCallback(false, null, null);
    }
}

private object FindClosestMatchingCell(long unitIndex, object ctx)
{
    FindClosestContext context = (FindClosestContext)ctx;

    context.DeltaCol++;

    if (context.DeltaRow == context.CurrentRadius && context.DeltaCol == context.CurrentRadius + 1)
    {
        context.CurrentRadius++;
        if (context.CurrentRadius > context.Radius)
        {
            context.Found = false;
            return context;
        }

        context.DeltaRow = -context.CurrentRadius;
        context.DeltaCol = -context.CurrentRadius;
    }

    else if (context.DeltaCol == context.CurrentRadius + 1)
    {
        context.DeltaRow++;
        context.DeltaCol = -context.CurrentRadius;
    }

    if (HasMatchingCell(context.FilterCallback, context.DeltaRow, context.DeltaCol, context.CurrentRadius))
    {
        context.Found = true;
        return context;
    }

    return null;
}

private bool HasMatchingCell(Func<MapCell, VoxelData> predicate, int deltaRow, int deltaCol, int currentRadius)
{
    if (deltaRow == 0 && deltaCol == 0)
    {
        return false;
    }

    Coordinate to = m_dataController.Coordinate.Add(deltaRow , deltaCol);

    int mapSize = m_dataController.MapSize;

    MapCell cell = null;
    if (to.Row >= 0 && to.Row < mapSize && to.Col >= 0 && to.Col < mapSize)
    {
        cell = m_dataController.Map.Get(to.Row, to.Col, to.Weight);
    }

    return cell != null && predicate(cell) != null;
}

protected virtual Cmd OnMoveUnconditionalCmd(Cmd cmd)
{
    MovementCmd movementCmd = (MovementCmd)cmd;
    Coordinate to = movementCmd.Coordinates[1];

    bool isLastCmdInSequence = movementCmd.IsLastCmdInSequence;
    //m_commandsQueue.Count > 1 and isLastCmdInSequence is not the same
    if (m_commandsQueue.Count > 1)
    {
        bool considerIdleStateAsValid = true;
        if (!m_dataController.IsValidAndEmpty(to, considerIdleStateAsValid))
        {
            if (m_failedMoveAttempts < m_maxFailedMoveAttempts)
            {
                m_failedMoveAttempts++;
                //Do not move if there is voxel actor or voxel bomb in one of active states. just wait a little bit
            }
            else
            {
                m_commandsQueue.Clear();
                State = VoxelDataState.Idle;
            }
            return null;
        }
    }
    else
    {
        if (!m_dataController.IsValidAndEmpty(to, false))
        {
            m_commandsQueue.Clear();
            State = VoxelDataState.Idle;
            return null;
            //return TryToFinishMovementInValidCell(cmd);
        }
    }

    if (!m_dataController.Move(to, isLastCmdInSequence, EatOrDestroyCallback))
    {
        m_commandsQueue.Clear();
        State = VoxelDataState.Idle;
        return null;
    }

    if (!m_dataController.IsAlive)
    {
        m_commandsQueue.Clear();
        //State = VoxelDataState.Dead;
    }

    return cmd;
}

protected virtual Cmd OnMoveConditionalCmd(Cmd cmd)
{
    MovementCmd movementCmd = (MovementCmd)cmd;

    Coordinate to = movementCmd.Coordinates[1];

    bool isLastCmdInSequence = movementCmd.IsLastCmdInSequence;
    //m_commandsQueue.Count > 1 and isLastCmdInSequence is not the same
    if (m_commandsQueue.Count > 1)
    {
        bool considerIdleStateAsValid = true;
        if (!m_dataController.IsValidAndEmpty(to, considerIdleStateAsValid))
        {
            if (m_failedMoveAttempts < m_maxFailedMoveAttempts)
            {
                //Do not move if there is voxel actor or voxel bomb in one of active states. just wait a little bit
                return null;
            }
        }
    }
    else
    {
        if (!m_dataController.IsValidAndEmpty(to, false))
        {
            return TryToFinishMovementInValidCell(cmd);
        }
    }

    if (!m_dataController.Move(to, isLastCmdInSequence, EatOrDestroyCallback))
    {
        m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
        m_commandsQueue.Clear();

        State = VoxelDataState.SearchingPath;

        if (HasTarget)
        {
            TargetData = GetTargetData(TargetData.UnitOrAssetIndex, TargetData.Owner, out m_targetCoordinate);
            if (HasTarget)
            {
                HandleTargetCoordinateChange(m_targetCoordinate);
            }
            else
            {
                m_pathFinder.Find(Id, -1, m_dataController.Clone(), m_waypoints, (unitIndex, path) =>
                {
                    State = VoxelDataState.Moving;

                    PopulateCommandsQueue(unitIndex, path, false, CmdCode.MoveConditional);
                }, null);
            }
        }
        else
        {
            m_pathFinder.Find(Id, -1, m_dataController.Clone(), m_waypoints, (unitIndex, path) =>
            {
                State = VoxelDataState.Moving;

                PopulateCommandsQueue(unitIndex, path, false, CmdCode.MoveConditional);
            }, null);
        }

        return null;
    }

    if (!m_dataController.IsAlive)
    {
        m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
        m_commandsQueue.Clear();
        // State = VoxelDataState.Dead;
        return cmd;
    }

    return cmd;
}

protected virtual Cmd OnCustomCommand(Cmd cmd)
{
    return cmd;
}

protected Cmd TryToFinishMovementInValidCell(Cmd cmd)
{
    m_ticksBeforeTargetCheck = 0;

    if (!m_dataController.IsValidAndEmpty(m_dataController.Coordinate, false))
    {
        return FinishMovementInValidCell(cmd);
    }

    State = VoxelDataState.Idle;
    cmd.ErrorCode = CmdErrorCode.Failed;
    return cmd;
}

private Cmd FinishMovementInValidCell(Cmd cmd)
{
    if (m_dataController.IsValidAndEmpty(m_dataController.Coordinate, false))
    {
        State = VoxelDataState.Idle;
        cmd.ErrorCode = CmdErrorCode.Failed;
        return cmd;
    }
    else
    {
        m_pathFinder.Terminate(cmd.UnitIndex, m_dataController.PlayerIndex);
        m_commandsQueue.Clear();

        State = VoxelDataState.SearchingPath;

        const int searchRadius = 10;

        m_pathFinder.FindEmptySpace(Id, m_dataController.Clone(), searchRadius, (unitIndex, path) =>
        {
            State = VoxelDataState.Moving;

            m_ticksBeforeNextCommand = 0;

            PopulateCommandsQueue(unitIndex, path, true, CmdCode.MoveConditional);
        }, null);
        return null;
    }
}

protected void EatOrDestroyCallback(VoxelData eater, VoxelData voxelData, int deltaHealth, int voxelDataHealth)
{
    m_eatenOrDestroyedVoxels.Add(voxelData);
}
    }
}
*/