using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public interface ITaskRunner
    {
        bool IsRunning(long unitId);

        void Run(long unitId, int playerIndex, object taskData, Func<long, object, object> updateCallback, Action<long, object, object> completeCallback, Action<long, object> terminateCallback);

        void Terminate(long unitId);

        void Tick();

        void Update();

        void Destroy();
    }

    public class TaskRunner : ITaskRunner
    {
        private Dictionary<long, Task> m_idToActiveTask;
        private readonly List<Task> m_activeTasks = new List<Task>();

        public TaskRunner()
        {
            m_idToActiveTask = new Dictionary<long, Task>();
        }

        public void Destroy()
        {
            for (int i = 0; i < m_activeTasks.Count; ++i)
            {
                m_activeTasks[i].Terminate();
            }

            m_idToActiveTask = null;
            m_activeTasks.Clear();
        }


        public bool IsRunning(long unitId)
        {
            return m_idToActiveTask.ContainsKey(unitId);
        }

        public void Run(long unitId, int playerIndex, object taskContext, Func<long, object, object> updateCallback, Action<long, object, object> completeCallback, Action<long, object> terminateCallback)
        {
            Task task;
            if (m_idToActiveTask.TryGetValue(unitId, out task))
            {
                task.Terminate();
                m_activeTasks.Remove(task);
            }

            task = new Task(playerIndex, unitId, taskContext, updateCallback, completeCallback, terminateCallback);
            m_idToActiveTask[unitId] = task;
            m_activeTasks.Add(task);
        }

        public void Terminate(long unitId)
        {
            Task task;
            if (m_idToActiveTask.TryGetValue(unitId, out task))
            {
                task.Terminate();
                m_activeTasks.Remove(task);
                m_idToActiveTask.Remove(unitId);
            }
        }

        public void Tick() //This method should be called by MatchEngine
        {
            for (int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                Task task = m_activeTasks[i];
                Debug.Assert(!task.IsTerminated);
                if (task.CallbackIfCompleted())
                {
                    if(m_activeTasks[i] == task)
                    {
                        m_activeTasks.RemoveAt(i);
                        m_idToActiveTask.Remove(task.UnitId);
                    }
                }
            }
        }

        public void Update()
        {
            int maxIterationsPerFrame = 100;

            while (m_activeTasks.Count > 0)
            {
                int completedTasksCount = 0;
                for (int i = 0; i < m_activeTasks.Count; ++i)
                {
                    Task task = m_activeTasks[i];
                    if (task.IsCompleted)
                    {
                        completedTasksCount++;
                        if (m_activeTasks.Count == completedTasksCount)
                        {
                            return;
                        }

                        continue;
                    }

                    if (maxIterationsPerFrame == 0)
                    {
                        return;
                    }

                    maxIterationsPerFrame--;

                    object result = task.Update();

                    if(result != null)
                    {
                        task.SetCompleted(result);
                    }  
                }
            }
        }

        private class Task
        {
            private Action<long, object, object> m_completeCallback;
            private Func<long, object, object> m_udateCallback;
            private Action<long, object> m_terminateCallback;
            private object m_result;
            private object m_context;

            private long m_unitId;
            public long UnitId
            {
                get { return m_unitId; }
            }

            private int m_playerIndex;
            public int PlayerIndex
            {
                get { return m_playerIndex; }
            }

            private bool m_isTerminated;
            public bool IsTerminated
            {
                get { return m_isTerminated; }
            }

            public bool IsCompleted
            {
                get { return m_result != null; }
            }


            public Task(int playerIndex, long unitId, object context, Func<long, object, object> updateCallback, Action<long, object, object> completeCallback, Action<long, object> terminateCallback)
            {
                m_udateCallback = updateCallback;
                m_completeCallback = completeCallback;
                m_terminateCallback = terminateCallback;
                m_context = context;

                m_playerIndex = playerIndex;
                m_unitId = unitId;
            }

            public object Update()
            {
                return m_udateCallback(m_unitId, m_context);
            }

            public void SetCompleted(object result)
            {
                m_result = result;
            }

            public void Terminate()
            {
                if (m_terminateCallback != null)
                {
                    m_terminateCallback(m_unitId, m_context);
                }

                m_isTerminated = true;
            }

            public bool CallbackIfCompleted()
            {
                if (m_isTerminated)
                {
                    return true;
                }

                if (m_result != null)
                {
                    if (m_completeCallback != null)
                    {
                        m_completeCallback(m_unitId, m_context, m_result);
                    }
                    return true;
                }
                return false;
            }
        }
    }
}
