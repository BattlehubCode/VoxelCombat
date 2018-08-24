using System;
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

        public event Action<CmdResultCode> CmdExecuted;

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
                RaiseCmdFailed(null, CmdResultCode.Fail_InvalidOperation);
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
                    RaiseCmdFailed(null, CmdResultCode.Fail_NoUnit);
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

                    CmdResultCode noFail = m_dataController.SetVoxelDataState(State);
                    if(noFail != CmdResultCode.Success)
                    {
                        throw new InvalidOperationException("");
                    }
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
                CmdExecuted(CmdResultCode.Success);
            }
        }

        protected void RaiseCmdFailed(Cmd cmd, CmdResultCode errorCode)
        {
            if(cmd != null)
            {
                cmd.ErrorCode = errorCode;
            }

            GoToIdleState();
            if(CmdExecuted != null)
            {
                CmdExecuted(errorCode);
            }
        }

        protected void OnInstantCmd(int beginCmdCode, Cmd cmd, int delay, int duration)
        {
            State = VoxelDataState.Busy;

            Cmd beginCmd = cmd.Clone();
            beginCmd.Code = beginCmdCode;

            beginCmd.Duration = delay;
            m_commandsQueue.Enqueue(beginCmd);

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
                RaiseCmdFailed(null, CmdResultCode.Fail_InvalidOperation);
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
            m_pathFinder = m_engine.GetPathFinder(dataController.PlayerIndex);
        }

        protected override void OnSetCommand(Cmd cmd)
        {
            switch (cmd.Code)
            {
                case CmdCode.Move:
                    OnMoveCmd(cmd);
                    break;
                case CmdCode.Split:
                    OnInstantCmd(CmdCode.BeginSplit, cmd, m_dataController.Abilities.SplitDelay, m_dataController.Abilities.SplitDuration);
                    break;
                case CmdCode.Split4:
                    OnInstantCmd(CmdCode.BeginSplit4, cmd, m_dataController.Abilities.SplitDelay, m_dataController.Abilities.SplitDuration);
                    break;
                case CmdCode.Grow:
                    OnInstantCmd(CmdCode.BeginGrow, cmd, m_dataController.Abilities.GrowDelay,  m_dataController.Abilities.GrowDuration);
                    break;
                case CmdCode.Diminish:
                    OnInstantCmd(CmdCode.Diminish, cmd, m_dataController.Abilities.DiminishDelay, m_dataController.Abilities.DiminishDuration);
                    break;
                case CmdCode.Convert:
                    OnInstantCmd(CmdCode.BeginConvert, cmd, m_dataController.Abilities.ConvertDelay, m_dataController.Abilities.ConvertDuration);
                    break;
                case CmdCode.SetHealth:
                    OnInstantCmd(CmdCode.BeginSetHealth, cmd, 0, 0);
                    break;
            }
        }

        protected override void OnGoToIdleState()
        {
            if(State == VoxelDataState.SearchingPath)
            {
                m_pathFinder.Terminate(Id);
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
                        if(Coordinate.CanMergePath(foundPath, path))
                        {
                            State = VoxelDataState.Moving;
                            PopulateCommandsQueue(Id, Coordinate.MergePath(foundPath, path), false, CmdCode.Move);
                        }
                        else
                        {
                            RaiseCmdFailed(cmd, CmdResultCode.Fail_NotFound);
                        }
                    },
                    null);
            }
            else
            {
                RaiseCmdFailed(cmd, CmdResultCode.Fail_NotFound);
            }
        }

        protected void OnMoveSearchPath(Cmd cmd)
        {
            //m_ticksBeforeNextCommand = 0;// <-- this will enable immediate commands

            MovementCmd coordinateCmd = (MovementCmd)cmd;
            Coordinate[] cmdCoordinates = coordinateCmd.Coordinates;

            if (cmdCoordinates.Length != 1)
            {
                RaiseCmdFailed(cmd, CmdResultCode.Fail_InvalidArguments);
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
                        case CmdCode.BeginSplit:
                        case CmdCode.BeginSplit4:
                        case CmdCode.BeginGrow:
                        case CmdCode.BeginDiminish:
                        case CmdCode.BeginConvert:
                        case CmdCode.BeginSetHealth:
                            return cmd;
                        case CmdCode.Split:
                        {
                            CoordinateCmd coordinateCmd = new CoordinateCmd(cmd.Code, cmd.UnitIndex, cmd.Duration);
                            Coordinate[] coordinates;
                            CmdResultCode result = m_dataController.Split(out coordinates, EatOrDestroyCallback);
                            if (result == CmdResultCode.Success)
                            {
                                coordinateCmd.Coordinates = coordinates;
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(coordinateCmd, result);
                            }
                          
                            return coordinateCmd;
                        }

                        case CmdCode.Split4:
                        {
                            CoordinateCmd coordinateCmd = new CoordinateCmd(cmd.Code, cmd.UnitIndex, cmd.Duration);
                            Coordinate[] coordinates;
                            CmdResultCode result = m_dataController.Split4(out coordinates);
                            if (result == CmdResultCode.Success)
                            {
                                coordinateCmd.Coordinates = coordinates;
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(coordinateCmd, result);
                            }
                            
                            return coordinateCmd;
                        }

                        case CmdCode.Grow:
                        {
                            CmdResultCode result = m_dataController.Grow(EatOrDestroyCallback);
                            if (result == CmdResultCode.Success)
                            {
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(cmd, result);
                            }
                            
                            return cmd;
                        }

                        case CmdCode.Diminish:
                        {
                            CmdResultCode result = m_dataController.Diminish();
                            if (result == CmdResultCode.Success)
                            {
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(cmd, result);
                            }
                            
                            return cmd;
                        }
                           
                        case CmdCode.Convert:
                        {
                            ChangeParamsCmd convertCmd = (ChangeParamsCmd)cmd;

                            int type = convertCmd.IntParams[0];

                            CmdResultCode result = m_dataController.Convert(type);
                            if (result == CmdResultCode.Success)
                            {
                                RaiseCmdExecuted();
                            }
                            else
                            {
                                RaiseCmdFailed(cmd, result);
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
            CmdResultCode result = m_dataController.Move(to, isLastCmdInSequence, EatOrDestroyCallback);
            if (result != CmdResultCode.Success)
            {
                RaiseCmdFailed(cmd, result);
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

                if (i == path.Length - 2)
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
            CmdResultCode result = m_dataController.PerformSpawnAction(out coordinates);
            if (result == CmdResultCode.Success)
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

