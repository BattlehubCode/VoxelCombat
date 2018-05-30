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

        public bool Tick()
        {
            OnTick();
            bool isStateChanged = m_prevState != m_taskInfo.State;
            m_prevState = m_taskInfo.State;
            return isStateChanged;
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

    public class ExecuteCmdTask : TaskBase
    {
        public ExecuteCmdTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
            taskEngine.MatchView.Submit(taskInfo.PlayerId, taskInfo.Cmd);
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

    public class ExecuteMoveCmdTask : TaskBase
    {
        private IVoxelDataController m_dataController;
        private Coordinate m_targetCoordinate;

        public ExecuteMoveCmdTask(TaskInfo taskInfo, ITaskEngine taskEngine) : base(taskInfo, taskEngine)
        {
            IMatchPlayerView player = taskEngine.MatchView.GetPlayerView(taskInfo.PlayerId);
            IMatchUnitAssetView unit = player.GetUnit(taskInfo.Cmd.UnitIndex);
            m_dataController = unit.DataController;

            CoordinateCmd cmd = (CoordinateCmd)taskInfo.Cmd;
            m_targetCoordinate = cmd.Coordinates[cmd.Coordinates.Length - 1];

            taskEngine.MatchView.Submit(taskInfo.PlayerId, taskInfo.Cmd);
        }

        protected override void OnTick()
        {
            if (m_taskInfo.State == TaskState.Active)
            {
                VoxelDataState state = m_dataController.GetVoxelDataState();
                switch(state)
                {
                    case VoxelDataState.Idle:
                        if(m_dataController.Coordinate == m_targetCoordinate)
                        {
                            m_taskInfo.State = TaskState.Completed;
                        }
                        else
                        {
                            m_taskInfo.State = TaskState.Failed;
                        }
                        break;
                    case VoxelDataState.Dead:
                        m_taskInfo.State = TaskState.Failed;
                        break;
                }                
            }
        }
    }
}
