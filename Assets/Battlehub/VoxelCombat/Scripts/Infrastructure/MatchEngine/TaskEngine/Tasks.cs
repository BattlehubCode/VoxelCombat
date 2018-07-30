//#define DEBUG_OUTPUT
using System;
using System.Diagnostics;

namespace Battlehub.VoxelCombat
{
    public delegate void TaskEvent(TaskBase sender, TaskInfo taskInfo);
    public abstract class TaskBase
    {
        protected static readonly Random m_random = new Random((int)(DateTime.Now.AddMinutes(-15).Ticks % short.MaxValue));
        private bool m_isAcquired;
        public bool IsAcquired
        {
            get { return m_isAcquired; }
            set
            {
                if(m_isAcquired != value)
                {
                    m_isAcquired = value;
                    if (m_isAcquired)
                    {
                        OnAcquired();
                    }
                    else
                    {
                        OnReleased();
                    }
                }
            }
        }

        protected virtual void OnAcquired()
        {

        }

        protected virtual void OnReleased()
        {

        }

        public event TaskEvent ChildTaskActivated;
     
        protected IExpression m_expression;
        protected ITaskEngine m_taskEngine;
        protected TaskInfo m_taskInfo;
        public TaskInfo TaskInfo
        {
            get { return m_taskInfo; }
            set
            {
                m_taskInfo = value;
                if (m_taskInfo.Expression != null)
                {
                    m_expression = m_taskEngine.GetExpression(m_taskInfo.Expression.Code);
                }
                else
                {
                    m_expression = null;
                }
            }
        }

        public TaskBase Parent
        {
            get;
            set;
        }

        public ITaskEngine TaskEngine
        {
            get { return m_taskEngine; }
            set { m_taskEngine = value; }
        }

        protected int InputsCount
        {
            get { return m_taskInfo.Inputs != null ? m_taskInfo.Inputs.Length : 0; }
        }

        public void Construct()
        {
            if (m_taskInfo.State != TaskState.Active)
            {
                throw new InvalidOperationException("taskInfo.State != TaskState.Active");
            }
          
            if(m_taskInfo.OutputsCount > 0 && m_taskInfo.Parent != null)
            {
                int scopeId = m_taskInfo.Parent.TaskId;
                m_taskEngine.Memory.CreateOutputs(scopeId, m_taskInfo.TaskId, m_taskInfo.OutputsCount);
            }
            #if DEBUG_OUTPUT
            UnityEngine.Debug.Log("Constructing " + m_taskInfo.ToString());
            #endif
            OnConstruct(); 
        }

        protected virtual void OnConstruct()
        {

        }

        public void Tick()
        {
            OnTick();            
        }

        protected virtual void Reset()
        {
            if(m_taskInfo.State == TaskState.Active)
            {
                throw new InvalidOperationException("unable to reset active task");
            }

            m_taskInfo.Reset();
        }

        public virtual void Destroy()
        {

        }

        protected void BreakParent()
        {
            if (Parent != null)
            {
                Parent.OnBreak();
            }
        }

        protected virtual void OnBreak()
        {
           
        }

        protected void ContinueParent()
        {
            if (Parent != null)
            {
                Parent.OnContinue();
            }
        }

        protected virtual void OnContinue()
        {
           
        }

        protected virtual void ReturnParent()
        {
            if(Parent != null)
            {
                Parent.TaskInfo.State = TaskInfo.State;
                Parent.TaskInfo.StatusCode = TaskInfo.StatusCode;
                Parent.ReturnParent();
            }
        }

        protected virtual void OnTick() { }

        protected void RaiseChildTaskActivated(TaskInfo taskInfo)
        {
            if (ChildTaskActivated != null)
            {
                ChildTaskActivated(this, taskInfo);
            }
        }

        protected void WriteOutput(int index, object value)
        {
            m_taskEngine.Memory.WriteOutput(m_taskInfo.Parent.TaskId, m_taskInfo.TaskId, index, value);
        }

        public T ReadInput<T>(TaskInputInfo i)
        {
           return (T)m_taskEngine.Memory.ReadOutput(i.Scope.TaskId, i.OutputTask.TaskId, i.OutputIndex);
        }

        public T ReadInput<T>(TaskInputInfo i, T defaultValue)
        {
            object value = m_taskEngine.Memory.ReadOutput(i.Scope.TaskId, i.OutputTask.TaskId, i.OutputIndex);
            if(value == null)
            {
                return defaultValue;
            }
            return (T)value;
        }

        public override string ToString()
        {
            return m_taskInfo.ToString();
        }
    }

    public class NopTask : TaskBase
    {
        protected override void OnTick()
        {
            m_taskInfo.State = TaskState.Completed;
        }
    }

    public class TestFailTask : TaskBase
    {
        protected override void OnTick()
        {
            base.OnTick();
#if !SERVER
            NUnit.Framework.Assert.Fail();
#endif
        }
    }

    public class TestPassTask : TaskBase
    {
        protected override void OnTick()
        {
            base.OnTick();
#if !SERVER
            NUnit.Framework.Assert.Pass();
#endif
        }
    }

    public class TestAssertTask : TaskBase
    {
        protected override void OnTick()
        {
            base.OnTick();
            Func<TaskBase, TaskInfo, TaskState> callback = (Func<TaskBase, TaskInfo, TaskState>)m_taskInfo.Expression.Value;
            m_taskInfo.State = callback(this, m_taskInfo);
        }
    }

    public class MockTask : TaskBase
    {
        private int m_counter = 0;
        protected override void OnTick()
        {
            m_counter++;
            if(m_counter > 10)
            {
                m_taskInfo.State = TaskState.Completed;
            }
        }
    }

    public class MockImmediateTask : TaskBase
    {
        protected override void OnTick()
        {
            m_taskInfo.State = TaskState.Completed;
        }
    }

 
    public class ExecuteCmdTaskWithExpression : TaskBase
    {
        private bool m_running;

        protected override void OnReleased()
        {
            base.OnReleased();
            m_running = false;
        }

        protected override void OnConstruct()
        {
            if(m_running)
            {
                return;
            }

            m_running = true;
            SubmitCommand();
        }

        protected void SubmitCommand()
        {
            if (m_taskEngine.IsClient)
            {
                TaskInfo taskInfo = new TaskInfo(m_taskInfo, true);
                TaskCmd taskCmd = new TaskCmd(SerializedTask.FromTaskInfo(taskInfo));
                taskCmd.UnitIndex = m_taskInfo.Cmd.UnitIndex;
                m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, taskCmd);
            }
            else
            {
                if (m_taskInfo.RequiresClientSidePreprocessing)
                {
                    m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, m_taskInfo.PreprocessedCmd);
                }
                else
                {
                    m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, m_taskInfo.Cmd);
                }
            }
        }

        protected override void OnTick()
        {
            if (m_taskInfo.State == TaskState.Active)
            {
                if (!m_taskInfo.Expression.IsEvaluating)
                {
                    m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, taskState =>
                    {
                        m_taskInfo.State = (TaskState)taskState;
                    });
                }
            }
        }
    }

    public class ExecuteCmdTask : TaskBase
    {
        protected IMatchUnitAssetView m_unit;
        protected override void OnReleased()
        {
            base.OnReleased();
            m_unit = null;
        }

        protected override void OnConstruct()
        {
            if (InputsCount > 0)
            {
                long unitIndex = ReadInput<long>(m_taskInfo.Inputs[0]);
                m_taskInfo.Cmd.UnitIndex = unitIndex;
            }

            if (m_unit != null)
            {
                return;
            }

            IMatchPlayerView playerView = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex);
            if(playerView == null)
            {
                m_taskInfo.State = TaskState.Completed;
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                return;
            }

            m_unit = playerView.GetUnit(m_taskInfo.Cmd.UnitIndex);
            if (m_unit == null)
            {
                m_taskInfo.State = TaskState.Completed;
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                return;
            }
            else
            {
                if(!m_taskEngine.IsClient)
                {
                    m_unit.CmdExecuted += OnCmdExecuted;
                }
                
                SubmitCommand();               
            }
        }

        protected void SubmitCommand()
        {
            if (m_taskEngine.IsClient)
            {
                TaskInfo taskInfo = new TaskInfo(m_taskInfo, true);
                TaskCmd taskCmd = new TaskCmd(SerializedTask.FromTaskInfo(taskInfo));
                taskCmd.UnitIndex = m_taskInfo.Cmd.UnitIndex;
                m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, taskCmd);
            }
            else
            {
                if (m_taskInfo.RequiresClientSidePreprocessing)
                {
                    m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, m_taskInfo.PreprocessedCmd);
                }
                else
                {
                    m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, m_taskInfo.Cmd);
                }
            }
        }

        private void OnCmdExecuted(CmdResultCode cmdErrorCode)
        {
            m_unit.CmdExecuted -= OnCmdExecuted;
            if (cmdErrorCode == CmdResultCode.Success)
            {
                OnCompleted();
            }
            else
            {
                OnFailed();
            }      
        }

        protected virtual void OnCompleted()
        {
            m_taskInfo.State = TaskState.Completed;
        }

        protected virtual void OnFailed()
        {
            m_taskInfo.StatusCode = TaskInfo.TaskFailed;
            m_taskInfo.State = TaskState.Completed;
        }

        public override void Destroy()
        {
            base.Destroy();
            m_unit.CmdExecuted -= OnCmdExecuted;
        }
    }

    public class EvaluateExpressionTask : TaskBase
    {
        protected override void OnAcquired()
        {
            base.OnAcquired();
            if(m_taskInfo.Parent == null)
            {
                throw new ArgumentException("taskInfo.Parent == null", "taskInfo");
            }
            if(m_taskInfo.OutputsCount != 1)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 1", "taskInfo");
            }
        }

        protected override void OnConstruct()
        {
            base.OnConstruct();
            m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
            {
                m_taskEngine.Memory.WriteOutput(m_taskInfo.Parent.TaskId, m_taskInfo.TaskId, 0, value);
                m_taskInfo.State = TaskState.Completed;
            });
        }       
    }

    public class FindPathTask : TaskBase
    {
        private IMatchUnitAssetView m_unit;

        protected override void OnAcquired()
        {
            base.OnAcquired();
            if (m_taskInfo.OutputsCount != 1)
            {
                throw new ArgumentException(string.Format("taskInfo.OutputsCount == {0}, Must be equal to 1", m_taskInfo.OutputsCount), "taskInfo");
            }

            if(InputsCount < 2)
            {
                throw new ArgumentException("InputsCount < 2", "taskInfo");
            }
        }

        protected override void OnReleased()
        {
            base.OnReleased();
            m_unit = null;
        }

        protected override void OnConstruct()
        {
            long unitIndex = ReadInput<long>(m_taskInfo.Inputs[0]);
            Coordinate[] waypoints = ReadInput<Coordinate[]>(m_taskInfo.Inputs[1]);

            m_unit = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex).GetUnit(unitIndex);
            if(m_unit == null)
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
            }
            else
            {
                m_taskEngine.PathFinder.Find(unitIndex, -1, m_unit.DataController.Clone(), waypoints, (id, path) =>
                {
                    if (path[path.Length - 1] == waypoints[waypoints.Length - 1])
                    {
                        WriteOutput(0, path);
                        if(m_taskInfo.State != TaskState.Active)
                        {
                            throw new InvalidOperationException("m_taskInfo.State shoud be equal to Active but its " + m_taskInfo.State);
                        }
                        m_taskInfo.State = TaskState.Completed;
                    }
                    else
                    {
                        if (m_taskInfo.State != TaskState.Active)
                        {
                            throw new InvalidOperationException("m_taskInfo.State shoud be equal to Active but its " + m_taskInfo.State);
                        }
                        m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                        m_taskInfo.State = TaskState.Completed;
                    }
                },
                id =>
                {
                    if (m_taskInfo.State == TaskState.Active)
                    {
                        UnityEngine.Debug.LogWarning("Path finding should not be termiated when task is in active state" + m_taskInfo.ToString());
                        //throw new InvalidOperationException();
                        m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                        m_taskInfo.State = TaskState.Completed;
                    }
                });
            }
        }

        protected override void OnTick()
        {
            base.OnTick();
            if(m_taskInfo.State != TaskState.Active)
            {
                if(m_unit != null && m_taskEngine.PathFinder.IsRunning(m_unit.Id))
                {
                    m_taskEngine.PathFinder.Terminate(m_unit.Id);
                }
            }
        }
    }

    public class FindPathToRandLocationPath : FindPathTask
    {
        private IMatchUnitAssetView m_unit;

        protected override void OnAcquired()
        {
            base.OnAcquired();
            if (m_taskInfo.OutputsCount != 1)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 1", "taskInfo");
            }

            if (InputsCount < 2)
            {
                throw new ArgumentException("InputsCount < 2", "taskInfo");
            }
        }

        protected override void OnReleased()
        {
            base.OnReleased();
            m_unit = null;
        }

        protected override void OnConstruct()
        {
            long unitIndex = ReadInput<long>(m_taskInfo.Inputs[0]);
            TaskInputInfo firstInput = m_taskInfo.Inputs[1];
            int radius = ReadInput(firstInput, 10);

            m_unit = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex).GetUnit(unitIndex);
            if (m_unit == null)
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
                return;
            }

            Coordinate coordinate = m_unit.DataController.Coordinate;
            int deltaCol;
            int deltaRow;
            do
            {
                deltaCol = m_random.Next(0, radius + 1);
                deltaRow = m_random.Next(0, radius + 1);
            }
            while (deltaCol == 0 && deltaRow == 0);

            coordinate.Col = (m_random.Next() % 2 == 0) ?
                coordinate.Col + deltaCol :
                coordinate.Col - deltaCol;

            coordinate.Row = (m_random.Next() % 2 == 0) ?
                coordinate.Row + deltaRow :
                coordinate.Row - deltaRow;

            m_taskEngine.Memory.WriteOutput(
                firstInput.Scope.TaskId, 
                firstInput.OutputTask.TaskId, 
                firstInput.OutputIndex, 
                new[] { m_unit.DataController.Coordinate, coordinate });

            base.OnConstruct();

            m_taskEngine.Memory.WriteOutput(
                firstInput.Scope.TaskId, 
                firstInput.OutputTask.TaskId, 
                firstInput.OutputIndex,
                radius);
        }
    }

    public abstract class SearchAroundTask : TaskBase
    {
        protected class SearchAroundContext
        {
            public MapPos m_position;
            public int m_weight;
            public int m_deltaRow;
            public int m_deltaCol;
            public int m_radius;
            public int m_maxRadius;
            private int m_startRadius;
            
            public SearchAroundContext(MapPos pos, int weight, int startRadius, int maxRadius)
            {
                m_position = pos;
                m_weight = weight;
                m_startRadius = startRadius;
                m_maxRadius = maxRadius;
                Reset();
            }

            public void Reset()
            {
                m_deltaCol = -(m_startRadius + 1);
                m_deltaRow = -m_startRadius;
                m_radius = m_startRadius;
            }
        }

        protected virtual int StartRadius
        {
            get { return 1; }
        }

        protected IMatchUnitAssetView m_unit;
        protected IVoxelDataController m_dataController;

        protected override void OnReleased()
        {
            base.OnReleased();
            m_unit = null;
            m_dataController = null;
        }

        protected override void OnConstruct()
        {
            SearchAroundContext ctx = ReadInput<SearchAroundContext>(m_taskInfo.Inputs[0]);
            long unitIndex = ReadInput<long>(m_taskInfo.Inputs[1]);
            
            m_unit = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex).GetUnit(unitIndex);
            if (m_unit == null)
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
            }
            else
            {
                m_dataController = m_unit.DataController.Clone(); 
                if (ctx == null)
                {
                    ctx = new SearchAroundContext(m_unit.Position, m_unit.Data.Weight, StartRadius, GetMaxRadius(m_unit));
                }
                m_taskEngine.TaskRunner.Run(unitIndex, -1, ctx, FindSuitableData, FindSuitableDataCompleted, (id, context) =>
                {
                    if(m_taskInfo.State == TaskState.Active)
                    {
                        Debug.Assert(false, "Task Runner should not be interrupted when task is in active state");
                        m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                        m_taskInfo.State = TaskState.Completed;
                    }  
                });
            }
        }

        protected override void OnTick()
        {
            base.OnTick();
            if (m_taskInfo.State != TaskState.Active)
            {
                if (m_unit != null && m_taskEngine.TaskRunner.IsRunning(m_unit.Id))
                {
                    m_taskEngine.TaskRunner.Terminate(m_unit.Id);
                }
            }
        }

        protected virtual bool NextIteration(object context)
        {
            SearchAroundContext ctx = (SearchAroundContext)context;
            ctx.m_deltaCol++;

            if (ctx.m_deltaRow == ctx.m_radius && ctx.m_deltaCol == ctx.m_radius + 1)
            {
                ctx.m_radius++;
                if (ctx.m_radius > ctx.m_maxRadius)
                {
                    return false;
                }

                ctx.m_deltaRow = -ctx.m_radius;
                ctx.m_deltaCol = -ctx.m_radius;
            }

            else if (ctx.m_deltaCol == ctx.m_radius + 1)
            {
                ctx.m_deltaRow++;
                ctx.m_deltaCol = -ctx.m_radius;
            }

            return true;
        }

        private object FindSuitableData(long unitIndex, object context)
        {
            if (!NextIteration(context))
            {
                return false;
            }

            SearchAroundContext ctx = (SearchAroundContext)context;
            if(!GetSuitableData(ctx))
            {
                return null;
            }
            return true;
        }

        private void FindSuitableDataCompleted(long unitIndex, object context, object result)
        {
            SearchAroundContext ctx = (SearchAroundContext)context;
            bool found = (bool)result;
            if (found)
            {
                HandleSuitableDataFound();
                WriteOutput(0, ctx);
            }
            else
            {
                HandleLastIteration();
                WriteOutput(0, null);
            }
        }

        protected virtual int GetMaxRadius(IMatchUnitAssetView unit)
        {
            return unit.DataController.Abilities.VisionRadius;
        }

        protected abstract bool GetSuitableData(SearchAroundContext ctx);

        protected virtual void HandleSuitableDataFound()
        {
        }

        protected virtual void HandleLastIteration()
        {
        }
    }

    public class SearchAroundForTask : SearchAroundTask
    {
        protected override void OnAcquired()
        {
            base.OnAcquired();
            if (InputsCount < 2)
            {
                throw new ArgumentException("InputsCount < 2", "taskInfo");
            }

            if (m_taskInfo.OutputsCount != 2)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 2", "taskInfo");
            }
        }

        protected override bool GetSuitableData(SearchAroundContext ctx)
        {
            VoxelData unitData = m_unit.Data;

            int row = ctx.m_position.Row + ctx.m_deltaRow;
            int col = ctx.m_position.Col + ctx.m_deltaCol;

            int altitude;
            if (GetSuitableData(ctx, unitData, row, col, out altitude))
            {
                WriteOutput(1, new[] { m_unit.DataController.Coordinate, new Coordinate(row, col, altitude, ctx.m_weight) });
                return true;
            }

            return false;
        }

        protected virtual bool GetSuitableData(SearchAroundContext ctx, VoxelData unitData, int row, int col, out int altitude)
        {
            altitude = int.MinValue;
            return false;
        }

        protected override void HandleSuitableDataFound()
        {
            base.HandleSuitableDataFound();
            m_taskInfo.State = TaskState.Completed;
        }

        protected override void HandleLastIteration()
        {
            base.HandleLastIteration();
            m_taskInfo.StatusCode = TaskInfo.TaskFailed;
            m_taskInfo.State = TaskState.Completed;
        }
    }


    public class SearchAroundForFood : SearchAroundForTask
    {
        protected override bool GetSuitableData(SearchAroundContext ctx, VoxelData unitData, int row, int col, out int altitude)
        {
            MapCell cell = m_taskEngine.MatchEngine.Map.Get(row, col, ctx.m_weight);
            VoxelData eatable = cell.GetDescendantsWithVoxelData(data => IsEatable(data, unitData));
            if(eatable != null)
            {
                altitude = eatable.Altitude;
                return true;
            }
            altitude = int.MinValue;
            return false;   
        }

        private static bool IsEatable(VoxelData data, VoxelData destroyer)
        {
            if (data.IsAttackableBy(destroyer))
            {
                if (data.Type == (int)KnownVoxelTypes.Eatable)
                {
                    if (data.IsNeutral)
                    {
                        return true;
                    }

                    if (data.Owner == destroyer.Owner)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }

    public class SearchAroundForGrowLocation : SearchAroundForTask
    {
        protected override int StartRadius
        {
            get { return 0; }
        }

        protected override bool GetSuitableData(SearchAroundContext ctx, VoxelData unitData, int row, int col, out int altitude)
        {
            altitude = int.MinValue;
            MapCell cell = m_taskEngine.MatchEngine.Map.Get(row, col, ctx.m_weight);
            if(cell.Last == null)
            {
                return false;
            }

            if(cell.Last == unitData)
            {
                altitude = cell.Last.Prev.Altitude;
            }
            else
            {
                altitude = cell.Last.Altitude;
            }

            Coordinate coordinate = new Coordinate(row, col, altitude, ctx.m_weight);
            CmdResultCode result = m_dataController.CanGrow(
                m_dataController.ControlledData.Type,
                m_dataController.ControlledData.Health,
                coordinate);

            return result == CmdResultCode.Success;
        } 
    }

    public class SearchAroundForSplit4Location : SearchAroundForTask
    {
        protected override int StartRadius
        {
            get { return 0; }
        }

        protected override bool GetSuitableData(SearchAroundContext ctx, VoxelData unitData, int row, int col, out int altitude)
        {
            altitude = int.MinValue;
            MapCell cell = m_taskEngine.MatchEngine.Map.Get(row, col, ctx.m_weight);
            if (cell.Last == null)
            {
                return false;
            }

            if (cell.Last == unitData)
            {
                altitude = cell.Last.Prev.Altitude;
            }
            else
            {
                altitude = cell.Last.Altitude;
            }

            Coordinate coordinate = new Coordinate(row, col, altitude, ctx.m_weight);
            CmdResultCode result = m_dataController.CanSplit4(
                m_dataController.ControlledData.Type,
                m_dataController.ControlledData.Health,
                coordinate);

            return result == CmdResultCode.Success;
        }
    }

    public class ExecuteMoveTask : ExecuteCmdTask
    {
        protected override void OnConstruct()
        {
            if(InputsCount > 1)
            {
                Coordinate[] path = ReadInput<Coordinate[]>(m_taskInfo.Inputs[1]);   
                m_taskInfo.Cmd = new MovementCmd(CmdCode.Move)
                {
                    Coordinates = path,
                };
            }

            base.OnConstruct();
        }

        protected override void OnCompleted()
        {
            MovementCmd cmd = (MovementCmd)m_taskInfo.Cmd;
            if(cmd.Coordinates[cmd.Coordinates.Length - 1].MapPos == m_unit.DataController.Coordinate.MapPos)
            {
                m_taskInfo.State = TaskState.Completed;
            }
            else
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
            }
        }
    }
}
