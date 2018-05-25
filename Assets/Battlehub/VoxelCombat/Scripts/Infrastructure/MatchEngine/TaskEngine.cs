using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public delegate void TaskEngineEvent(TaskInfo taskInfo);

    public interface ITaskEngine
    {
        event TaskEngineEvent TaskStateChanged;

        /// <summary>
        /// Submit task to task engine. Side effect generates and assignes taskid
        /// </summary>
        /// <param name="taskInfo"></param>
        /// <returns>unique task id</returns>
        void Submit(TaskInfo taskInfo);

        void Terminate(int taskId);

        void Tick();

        void Update();

        void Destroy();
    }

    public class TaskEngine : ITaskEngine
    {
        public event TaskEngineEvent TaskStateChanged;

        private IMatchView m_match;
        private int m_identity;
        private readonly Dictionary<int, TaskInfo> m_tasks = new Dictionary<int, TaskInfo>();
       
        public TaskEngine(IMatchView match)
        {
            m_match = match;
        }

        public void Submit(TaskInfo taskInfo)
        {
            AddTasks(taskInfo);

            taskInfo.State = TaskState.Active;
            RaiseTaskStateChanged(taskInfo);
        }

        public void Terminate(int taskId)
        {
            TaskInfo task;
            if (m_tasks.TryGetValue(taskId, out task))
            {
                do
                {
                    task.State = TaskState.Terminated;
                    RaiseTaskStateChanged(task);
                    m_tasks.Remove(taskId);

                    if(task.Parent == null)
                    {
                        RemoveTasks(task);
                        break;
                    }

                    task = task.Parent;
                }
                while (true);
            }
        }

  
        public void Tick()
        {
            //Check completion state

            //Submit new commands to engine
        }

        public void Update()
        {

        }

        public void Destroy()
        {

        }

        private void RaiseTaskStateChanged(TaskInfo task)
        {
            if(TaskStateChanged != null)
            {
                TaskStateChanged(task);
            }
        }

        private void AddTasks(TaskInfo task)
        {
            if (task.Children != null)
            {
                for (int i = 0; i < task.Children.Length; ++i)
                {
                    AddTasks(task.Children[i]);
                }
            }

            m_identity++;
            task.TaskId = m_identity;
            m_tasks.Add(task.TaskId, task);
        }

        private void RemoveTasks(TaskInfo task)
        {
            if (task.Children != null)
            {
                for (int i = 0; i < task.Children.Length; ++i)
                {
                    RemoveTasks(task.Children[i]);
                }
            }

            m_tasks.Remove(task.TaskId);
        }
    }
}
