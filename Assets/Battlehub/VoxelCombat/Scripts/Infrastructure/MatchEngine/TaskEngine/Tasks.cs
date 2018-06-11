using System;
using System.Diagnostics;

namespace Battlehub.VoxelCombat
{

    public delegate void TaskEvent(TaskInfo taskInfo);
    public abstract class TaskBase
    {
        public event TaskEvent ChildTaskActivated;

        private TaskState m_prevState;

        protected readonly IExpression m_expression;
        protected readonly ITaskEngine m_taskEngine;
        protected readonly TaskInfo m_taskInfo;
        public TaskInfo TaskInfo
        {
            get { return m_taskInfo; }
        }

        protected int InputsCount
        {
            get { return m_taskInfo.Inputs != null ? m_taskInfo.Inputs.Length : 0; }
        }

        public TaskBase(TaskInfo taskInfo, ITaskEngine taskEngine)
        {
            m_taskInfo = taskInfo;
            m_prevState = m_taskInfo.State;
            m_taskEngine = taskEngine;
            if (m_taskInfo.Expression != null)
            {
                m_expression = m_taskEngine.GetExpression(m_taskInfo.Expression.Code);
            }
        }

        public void Run()
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
            OnRun(); 
        }

        protected virtual void OnRun()
        {

        }

        public bool Tick()
        {
            OnTick();
            bool isStateChanged = m_prevState != m_taskInfo.State;
            if(isStateChanged && m_prevState == TaskState.Active)
            {
                m_taskEngine.Memory.DestroyScope(m_taskInfo.TaskId);
            }
            m_prevState = m_taskInfo.State;
            return isStateChanged;
        }

        public virtual void Destroy()
        {

        }

        protected virtual void OnTick() { }

        protected void RaiseChildTaskActivated(TaskInfo taskInfo)
        {
            if (ChildTaskActivated != null)
            {
                ChildTaskActivated(taskInfo);
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
    }

    public class SequentialTask : TaskBase
    {
        private int m_activeChildIndex;

        public SequentialTask(TaskInfo taskInfo, ITaskEngine taskEngine)
            : base(taskInfo, taskEngine)
        {
        }
        protected override void OnTick()
        {
            TaskInfo childTask = m_taskInfo.Children[m_activeChildIndex];
            if (childTask.State != TaskState.Active)
            {
                if (childTask.State == TaskState.Idle)
                {
                    childTask.State = TaskState.Active;
                    RaiseChildTaskActivated(childTask);
                }
                else
                {
                    m_activeChildIndex++;
                    if (m_activeChildIndex >= m_taskInfo.Children.Length || childTask.State == TaskState.Terminated)
                    {
                        m_taskInfo.State = childTask.State;
                        m_activeChildIndex = 0;
                    }
                    else
                    {
                        childTask = m_taskInfo.Children[m_activeChildIndex];
                        childTask.State = TaskState.Active;
                        RaiseChildTaskActivated(childTask);
                    }
                }
            }
        }
    }

    public class BranchTask : TaskBase
    {
        private bool m_evalExpression = true;

        private TaskInfo m_childTask;

        public BranchTask(TaskInfo taskInfo, ITaskEngine taskEngine)
            : base(taskInfo, taskEngine)
        {
        }
        protected override void OnTick()
        {
            if (m_evalExpression)
            {
                if (!m_taskInfo.Expression.IsEvaluating)
                {
                    m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
                    {
                        m_childTask = m_taskInfo.Children[(bool)value ? 0 : 1];
                        if(m_childTask == null)
                        {
                            //empty else or if block 
                            m_taskInfo.State = TaskState.Completed;
                        }
                        else
                        {
                            m_evalExpression = false;
                            m_childTask.State = TaskState.Active;
                            RaiseChildTaskActivated(m_childTask);
                        }       
                    });
                }
            }
            else if (m_childTask.State != TaskState.Active)
            {
                m_taskInfo.State = m_childTask.State;
                m_childTask = null;
                m_evalExpression = true;
            }
        }
    }

    public class RepeatTask : SequentialTask
    {
        private bool m_evalExpression = true;

        public RepeatTask(TaskInfo taskInfo, ITaskEngine taskEngine)
            : base(taskInfo, taskEngine)
        {
        }
        protected override void OnTick()
        {
            if (m_evalExpression)
            {
                if (!m_taskInfo.Expression.IsEvaluating)
                {
                    m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
                    {
                        if ((bool)value)
                        {
                            m_evalExpression = false;
                            TaskInfo childTask = m_taskInfo.Children[0];
                            childTask.State = TaskState.Active;
                            RaiseChildTaskActivated(childTask);
                        }
                        else
                        {
                            m_taskInfo.State = TaskState.Completed;
                        }
                    });
                }
            }
            else
            {
                base.OnTick();
                if (m_taskInfo.State == TaskState.Idle)
                {
                    m_evalExpression = true;
                }
            }
        }
    }

    public class ExecuteCmdTaskWithExpression : TaskBase
    {
        private bool m_running;
        public ExecuteCmdTaskWithExpression(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        { 
        }

        protected override void OnRun()
        {
            if(m_running)
            {
                return;
            }

            m_running = true;
            m_taskEngine.MatchEngine.Submit(m_taskInfo.PlayerIndex, m_taskInfo.Cmd);
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

        public ExecuteCmdTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
        }

        protected override void OnRun()
        {
            if(m_unit != null)
            {
                return;
            }

            IMatchPlayerView playerView = m_taskEngine.MatchEngine.GetPlayerView(m_taskInfo.PlayerIndex);
            if(playerView == null)
            {
                m_taskInfo.State = TaskState.Failed;
            }

            m_unit = playerView.GetUnit(m_taskInfo.Cmd.UnitIndex);
            if (m_unit == null)
            {
                m_taskInfo.State = TaskState.Failed;
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
            m_taskInfo.State = TaskState.Failed;
        }

        public override void Destroy()
        {
            base.Destroy();
            m_unit.CmdExecuted -= OnCmdExecuted;
        }
    }

    public class EvaluateExpressionTask : TaskBase
    {
        public EvaluateExpressionTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
            if(taskInfo.Parent == null)
            {
                throw new ArgumentException("tasInfo.Parent == null", "taskInfo");
            }
            if(taskInfo.OutputsCount != 1)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 1", "taskInfo");
            }
        }

        protected override void OnRun()
        {
            base.OnRun();
            m_expression.Evaluate(m_taskInfo.Expression, m_taskEngine, value =>
            {
                m_taskEngine.Memory.WriteOutput(m_taskInfo.Parent.PlayerIndex, m_taskInfo.TaskId, 0, value);
                m_taskInfo.State = TaskState.Completed;
            });
        }
            
    }

    public class FindPathTask : TaskBase
    {
        public FindPathTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
            if (taskInfo.OutputsCount != 1)
            {
                throw new ArgumentException("taskInfo.OutputsCount != 1", "taskInfo");
            }
        }

        protected override void OnRun()
        {
            //m_taskEngine.
        }
    }

    public class ExecuteMoveTask : ExecuteCmdTask
    {
        public ExecuteMoveTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
        }

        protected override void OnRun()
        {
            if(InputsCount > 1)
            {
                TaskInputInfo i0 = m_taskInfo.Inputs[0];
                long unitIndex = (long)m_taskEngine.Memory.ReadOutput(i0.ScopeId, i0.ConnectedTaskId, i0.OuputIndex);

                TaskInputInfo i1 = m_taskInfo.Inputs[1];
                Coordinate[] path = (Coordinate[])m_taskEngine.Memory.ReadOutput(i1.ScopeId, i1.ConnectedTaskId, i1.OuputIndex);

                m_taskInfo.Cmd = new MovementCmd(CmdCode.Move)
                {
                    UnitIndex = unitIndex,
                    Coordinates = path,
                };
            }

            base.OnRun();
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
                m_taskInfo.State = TaskState.Failed;
            }
        }
    }


}
