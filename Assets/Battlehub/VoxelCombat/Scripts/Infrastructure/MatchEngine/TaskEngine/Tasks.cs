using System;

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
            OnRun();
        }

        public virtual void OnRun()
        {

        }

        public bool Tick()
        {
            OnTick();
            bool isStateChanged = m_prevState != m_taskInfo.State;
            m_prevState = m_taskInfo.State;
            return isStateChanged;
        }

        public virtual void Destroy()
        {

        }

        protected abstract void OnTick();

        protected void RaiseChildTaskActivated(TaskInfo taskInfo)
        {
            if (ChildTaskActivated != null)
            {
                ChildTaskActivated(taskInfo);
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
                        m_evalExpression = false;
                        m_childTask = m_taskInfo.Children[(bool)value ? 0 : 1];
                        m_childTask.State = TaskState.Active;
                        RaiseChildTaskActivated(m_childTask);
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

        public override void OnRun()
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
        protected IMatchUnitController m_unit;

        public ExecuteCmdTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
        }

        public override void OnRun()
        {
            if(m_unit != null)
            {
                return;
            }
         
            m_unit = m_taskEngine.MatchEngine.GetUnitController(m_taskInfo.PlayerIndex, m_taskInfo.Cmd.UnitIndex);
            if (m_unit == null)
            {
                m_taskInfo.State = TaskState.Failed;
            }
            else
            {
                m_unit.CmdExecuted += OnCmdExecuted;
                if(m_taskInfo.RequiresClientSidePreprocessing)
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
            m_taskInfo.State = TaskState.Failed;
        }

        public override void Destroy()
        {
            base.Destroy();
            m_unit.CmdExecuted -= OnCmdExecuted;
        }

        protected override void OnTick()
        {
        }
    }

    public class ExecuteMoveSearchCmdTask : ExecuteCmdTask
    {
        public ExecuteMoveSearchCmdTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
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
