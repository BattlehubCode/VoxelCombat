using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void TaskEngineEvent(TaskInfo taskInfo);

    public interface ITaskEngine
    {
        event TaskEngineEvent TaskStateChanged;

        IMatchEngine MatchEngine
        {
            get;
        }

        ITaskRunner TaskRunner
        {
            get;
        }

        IExpression GetExpression(int expressionCode);

        /// <summary>
        /// Submit task to task engine. Side effect generates and assignes taskid
        /// </summary>
        /// <param name="taskInfo"></param>
        /// <returns>unique task id</returns>
        void Submit(TaskInfo taskInfo);

        void Terminate(int taskId);

        void Tick();

        void Destroy();
    }

    public class TaskEngine : ITaskEngine
    {
        public event TaskEngineEvent TaskStateChanged;

        private IMatchEngine m_match;
        private ITaskRunner m_taskRunner;
        private int m_identity;
        private List<TaskBase> m_activeTasks;
        private readonly Dictionary<int, IExpression> m_expressions;

        public IMatchEngine MatchEngine
        {
            get { return m_match; }
        }

        public ITaskRunner TaskRunner
        {
            get { return m_taskRunner; }
        }


        public IExpression GetExpression(int expressionCode)
        {
            return m_expressions[expressionCode];
        }

        public TaskEngine(IMatchEngine match, ITaskRunner taskRunner)
        {
            m_match = match;
            m_taskRunner = taskRunner;
            m_activeTasks = new List<TaskBase>();

            m_expressions = new Dictionary<int, IExpression>
            {
                {ExpressionCode.Var, new VarExpression() },
                { ExpressionCode.And, new AndExpression() },
                { ExpressionCode.Or, new OrExpression() },
                { ExpressionCode.Not, new NotExpression() },
                { ExpressionCode.Eq, new EqExpression() },
                { ExpressionCode.NotEq, new NotEqExpression() },
                { ExpressionCode.UnitExists, new UnitExistsExpression() },
                { ExpressionCode.UnitState, new UnitStateExpression() },
                { ExpressionCode.UnitCoordinate, new UnitCoordinateExpression() },
                { ExpressionCode.TaskStatus, new TaskStatusExpression() },
            };
        }

        public void Submit(TaskInfo taskInfo)
        {
            GenerateIdentitifers(taskInfo);

            taskInfo.State = TaskState.Active;

            TaskBase task = InstantiateTask(taskInfo);
            task.ChildTaskActivated += OnChildTaskActivated;
            m_activeTasks.Add(task);

            RaiseTaskStateChanged(taskInfo);
        }

        public void Terminate(int taskId)
        {
            for (int i = m_activeTasks.Count; i >= 0; --i)
            {
                TaskBase activeTask = m_activeTasks[i];
                if(activeTask.TaskInfo.TaskId == taskId)
                {
                    activeTask.TaskInfo.State = TaskState.Terminated;
                    m_activeTasks.RemoveAt(i);
                    break;
                }
            }
        }

        public void Tick()
        {
            for(int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                TaskBase activeTask = m_activeTasks[i];
                bool stateChanged = activeTask.Tick();
                if(stateChanged)
                {
                    Debug.Assert(activeTask.TaskInfo.State != TaskState.Active);
                    m_activeTasks.RemoveAt(i);
                    RaiseTaskStateChanged(activeTask.TaskInfo);
                }
            }
        }

        private void OnChildTaskActivated(TaskInfo taskInfo)
        {
            m_activeTasks.Add(InstantiateTask(taskInfo));
        }

        public void Destroy()
        {
            for (int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                TaskBase task = m_activeTasks[i];
                task.TaskInfo.State = TaskState.Terminated;
                RaiseTaskStateChanged(task.TaskInfo);
            }
            m_activeTasks.Clear();
        }

        private void RaiseTaskStateChanged(TaskInfo task)
        {
            if(TaskStateChanged != null)
            {
                TaskStateChanged(task);
            }
        }

        private void GenerateIdentitifers(TaskInfo taskInfo)
        {
            m_identity++;
            taskInfo.TaskId = m_identity;
            if(taskInfo.Children != null)
            {
                for(int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    GenerateIdentitifers(taskInfo.Children[i]);
                }
            }
        }

        public TaskBase InstantiateTask(TaskInfo taskInfo)
        {
            switch (taskInfo.TaskType)
            {
                case TaskType.Command:
                    {
                        Debug.Assert(taskInfo.Cmd != null && taskInfo.Cmd.Code != CmdCode.Nop);

                        if(taskInfo.Expression != null)
                        {
                            return new ExecuteCmdTaskWithExpression(taskInfo, this);
                        }
                        else
                        {
                            if(taskInfo.Cmd.Code == CmdCode.MoveSearch)
                            {
                                return new ExecuteMoveSearchCmdTask(taskInfo, this);
                            }
                            return new ExecuteCmdTask(taskInfo, this);
                        } 
                    }
                    
                case TaskType.Sequence:
                    return new SequentialTask(taskInfo, this);
                case TaskType.Branch:
                    return new BranchTask(taskInfo, this);
                case TaskType.Repeat:
                    return new RepeatTask(taskInfo, this);
            }

            throw new NotSupportedException();
        }
    }
}
