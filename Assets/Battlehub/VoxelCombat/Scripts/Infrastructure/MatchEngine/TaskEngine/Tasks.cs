using System;
using System.Collections;
using System.Diagnostics;

namespace Battlehub.VoxelCombat
{
    public delegate void TaskEvent(TaskBase sender, TaskInfo taskInfo);
    public abstract class TaskBase
    {
        public bool IsAcquired
        {
            get;
            set;
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
                OnInitialized();
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

        protected virtual void OnInitialized()
        {

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
            OnConstruct(); 
        }

        protected virtual void OnConstruct()
        {

        }

        public void Tick()
        {
            OnTick();            
            //if(m_taskInfo.State != TaskState.Active)
            //{
            //    m_taskEngine.Memory.DestroyScope(m_taskInfo.TaskId);
            //}
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

        protected T ReadInput<T>(TaskInputInfo i)
        {
           return (T)m_taskEngine.Memory.ReadOutput(i.Scope.TaskId, i.ConnectedTask.TaskId, i.OuputIndex);
        }

        protected T ReadInput<T>(TaskInputInfo i, T defaultValue)
        {
            object value = m_taskEngine.Memory.ReadOutput(i.Scope.TaskId, i.ConnectedTask.TaskId, i.OuputIndex);
            if(value == null)
            {
                return defaultValue;
            }
            return (T)value;
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
        protected override void OnConstruct()
        {
            m_taskInfo.State = TaskState.Completed;
        }
    }

    public class BasicFlowTask : TaskBase
    {
        protected override void OnConstruct()
        {
            base.OnConstruct();
            Reset();
        }

        protected override void Reset()
        {
            m_taskInfo.Reset();
            m_taskInfo.State = TaskState.Active;
            if(m_taskInfo.Children != null)
            {
                for (int i = 0; i < m_taskInfo.Children.Length; ++i)
                {
                    if(m_taskInfo.Children[i] != null && m_taskInfo.Children[i].State != TaskState.Idle)
                    {
                        m_taskInfo.Children[i].Reset();
                    } 
                }
            }           
        }

        protected override void OnBreak()
        {
            BreakParent();
        }

        protected override void OnContinue()
        {
            ContinueParent();
        }
    }

    public class SequentialTask : BasicFlowTask
    {
        protected int m_activeChildIndex;
 
        protected override void OnTick()
        {
            TaskInfo childTask = m_activeChildIndex >= 0 ? m_taskInfo.Children[m_activeChildIndex] : null;
            if(childTask == null || childTask.State != TaskState.Active)
            {
                ActivateNextTask();
            }
        }

        protected override void Reset()
        {
            base.Reset();
            m_activeChildIndex = -1;
            if(m_taskInfo.Children == null || m_taskInfo.Children.Length == 0)
            {
                m_taskInfo.State = TaskState.Completed;
            }
            else
            {
                ActivateNextTask();
            }
        }

        private void ActivateNextTask()
        {
            TaskInfo childTask;
            do
            {
                m_activeChildIndex++;
                if (m_activeChildIndex >= m_taskInfo.Children.Length)
                {
                    m_activeChildIndex = -1;
                    m_taskInfo.State = TaskState.Completed;
                    break;
                }
                childTask = m_taskInfo.Children[m_activeChildIndex];
                RaiseChildTaskActivated(childTask);
                if(childTask.State == TaskState.Active)
                {
                    break;
                }
                else if(childTask.State != TaskState.Completed)
                {
                    m_taskInfo.State = TaskState.Terminated;
                    break;
                }
            }
            while (m_taskInfo.State == TaskState.Active);
        }
    }

    public class BranchTask : BasicFlowTask
    {
        private TaskInfo m_childTask;

        protected override void OnTick()
        {
            if (!m_taskInfo.Expression.IsEvaluating)
            {
                WaitChildTaskDeactivation();
            }
        }

        protected override void Reset()
        {
            base.Reset();
            m_childTask = null;
            
            if(m_taskInfo.Expression.IsEvaluating)
            {
                throw new InvalidOperationException("Unable to reset while Expression is evaluating");
            }

            m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
            {
                int index = (bool)value ? 0 : 1;
                m_childTask = m_taskInfo.Children.Length > index ? m_taskInfo.Children[index] : null;
                if (m_childTask == null)
                {
                    //empty else or if block 
                    m_taskInfo.State = TaskState.Completed;
                }
                else
                {
                    m_childTask.State = TaskState.Active;
                    RaiseChildTaskActivated(m_childTask);
                    WaitChildTaskDeactivation();
                }
            });
        }

        private void WaitChildTaskDeactivation()
        {
            if (m_childTask.State != TaskState.Active)
            {
                if (m_childTask.State == TaskState.Completed)
                {
                    m_taskInfo.State = TaskState.Completed;
                }
                else
                {
                    m_taskInfo.State = TaskState.Terminated;
                }
            }
        }
    }

    public class RepeatTask : SequentialTask
    {
        private bool m_break;
        private bool m_reset;

        protected override void OnTick()
        {
            if(!m_taskInfo.Expression.IsEvaluating)
            {
                if(m_reset)
                {
                    m_reset = false;
                    Reset();
                }
                else
                {
                    base.OnTick();

                    if (m_taskInfo.State == TaskState.Completed)
                    {
                        if (!m_break)
                        {
                            Reset();
                        }
                    }
                }
            }
        }

        protected override void OnBreak()
        {
            m_break = true;
            m_taskInfo.State = TaskState.Completed;
        }

        protected override void OnContinue()
        {
            m_taskInfo.State = TaskState.Completed;
            // Reset();
        }

        protected virtual void EvaluateExpression(Action<bool> callback)
        {
            m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
            {
                callback((bool)value);
            });
        }

        protected override void Reset()
        {
            if (m_taskInfo.Expression.IsEvaluating)
            {
                throw new InvalidOperationException("Unable to reset while Expression is evaluating");
            }

            m_break = false;
            EvaluateExpression(value => 
            {
                if (value)
                {
                    base.Reset();
                    if (m_taskInfo.State == TaskState.Completed)
                    {
                        if(!m_break)
                        {
                            m_reset = true;
                            m_taskInfo.State = TaskState.Active;
                        }  
                    }
                }
                else
                {
                    m_taskInfo.State = TaskState.Completed;
                }
            });
        }
    }

    public class ForeachTask : RepeatTask
    {
        private int m_index = -1;
        private IList m_list;

        protected override void OnInitialized()
        {
            if (InputsCount != 1)
            {
                throw new ArgumentException("InputsCount != 1", "taskInfo");
            }

            if (m_taskInfo.OutputsCount < 1)
            {
                throw new ArgumentException("taskInfo.OutputsCount < 1", "taskInfo");
            }
        }

        protected override void OnConstruct()
        {
            m_list = ReadInput<IList>(m_taskInfo.Inputs[0]);
            if (m_list != null && m_list.Count > 0)
            {
                base.OnConstruct();
            }
            else
            {
                m_taskInfo.State = TaskState.Completed;
            }       
        }

        protected override void EvaluateExpression(Action<bool> callback)
        {
            m_index++;
            if(m_index < m_list.Count)
            {
                WriteOutput(0, m_list[m_index]);
                WriteOutput(1, m_index);
                callback(true);
            }
            else
            {
                callback(false);
            }
        }
    }

    public class BreakTask : TaskBase
    {
        protected override void OnConstruct()
        {
            base.OnConstruct();
            m_taskInfo.State = TaskState.Completed;
            BreakParent();
        }
    }

    public class ContinueTask : TaskBase
    {
        protected override void OnConstruct()
        {
            base.OnConstruct();
            m_taskInfo.State = TaskState.Completed;
            ContinueParent();
        }
    }

    public class ExecuteCmdTaskWithExpression : TaskBase
    {
        private bool m_running;
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
                m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, new TaskCmd(m_taskInfo));
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

        protected override void OnConstruct()
        {
            if(m_unit != null)
            {
                return;
            }

            IMatchPlayerView playerView = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex);
            if(playerView == null)
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
                
            }

            m_unit = playerView.GetUnit(m_taskInfo.Cmd.UnitIndex);
            if (m_unit == null)
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
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
                m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, new TaskCmd(m_taskInfo));
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

        private void OnCmdExecuted(int cmdErrorCode)
        {
            m_unit.CmdExecuted -= OnCmdExecuted;
            if (cmdErrorCode == CmdErrorCode.Success)
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
        protected override void OnInitialized()
        {
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

        protected override void OnInitialized()
        {
            if (m_taskInfo.OutputsCount != 1)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 1", "taskInfo");
            }

            if(InputsCount < 2)
            {
                throw new ArgumentException("InputsCount < 2", "taskInfo");
            }
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
                        m_taskInfo.State = TaskState.Completed;
                    }
                    else
                    {
                        m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                        m_taskInfo.State = TaskState.Completed;
                    }
                },
                id =>
                {
                    if (m_taskInfo.State == TaskState.Active)
                    {
                        Debug.Assert(false, "Path finding should not be termiated when task is in active state");
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
            
            public SearchAroundContext(MapPos pos, int weight, int maxRadius)
            {
                m_position = pos;
                m_weight = weight;
                m_maxRadius = maxRadius;
                Reset();
            }

            public void Reset()
            {
                m_deltaCol = -1;
                m_deltaRow = -1;
                m_radius = 1;
            }
        }

        protected IMatchUnitAssetView m_unit;

        protected override void OnConstruct()
        {
            SearchAroundContext ctx = ReadInput<SearchAroundContext>(m_taskInfo.Inputs[0]);
            long unitIndex = ReadInput<long>(m_taskInfo.Inputs[1]);

            Coordinate[] waypoints = ReadInput<Coordinate[]>(m_taskInfo.Inputs[1]);

            m_unit = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex).GetUnit(unitIndex);
            if (m_unit == null)
            {
                m_taskInfo.StatusCode = TaskInfo.TaskFailed;
                m_taskInfo.State = TaskState.Completed;
            }
            else
            {
                if(ctx == null)
                {
                    ctx = new SearchAroundContext(m_unit.Position, m_unit.Data.Weight, GetMaxRadius(m_unit));
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
                if (ctx.m_radius >= ctx.m_maxRadius)
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

    public class SearchAroundForFood : SearchAroundTask
    {
        protected override void OnInitialized()
        {
            if (InputsCount < 2)
            {
                throw new ArgumentException("InputsCount < 2", "taskInfo");
            }

            if (m_taskInfo.OutputsCount != 2)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 3", "taskInfo");
            }
        }

        protected override bool GetSuitableData(SearchAroundContext ctx)
        {
            VoxelData destroyer = m_unit.Data;

            int row = ctx.m_position.Row + ctx.m_deltaRow;
            int col = ctx.m_position.Col + ctx.m_deltaCol;

            MapCell cell = m_taskEngine.MatchEngine.Map.Get(row, col, ctx.m_weight);
            VoxelData eatable = cell.GetDescendantsWithVoxelData(data => IsEatable(data, destroyer));
            if (eatable != null)
            {
                WriteOutput(1, new Coordinate(row, col, eatable.Altitude, ctx.m_weight));
                return true;
            }

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

    public class ExecuteMoveTask : ExecuteCmdTask
    {
        protected override void OnConstruct()
        {
            if(InputsCount > 1)
            {
                long unitIndex = ReadInput<long>(m_taskInfo.Inputs[0]);
                Coordinate[] path = ReadInput<Coordinate[]>(m_taskInfo.Inputs[1]);   

                m_taskInfo.Cmd = new MovementCmd(CmdCode.Move)
                {
                    UnitIndex = unitIndex,
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
