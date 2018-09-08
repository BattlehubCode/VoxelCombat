using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace Battlehub.VoxelCombat
{
    public interface IBackgroundWorker
    {
        void Submit(Func<object> job, Action<object> completed);

        void CancelAll();
    }

    public class BackgroundWorker : MonoBehaviour, IBackgroundWorker
    {
        public class WorkItem
        {
            public object Lock = new object();

            public bool IsCompleted;

            private Func<object> m_job;

            private Action<object> m_completed;

            private object m_result;

            public WorkItem(Func<object> job, Action<object> completed)
            {
                m_job = job;
                m_completed = completed;
            }

            private void ThreadFunc(object arg)
            {
                m_result = m_job();   

                lock(Lock)
                {
                    IsCompleted = true;
                }
            }

            public void Run()
            {
                ThreadPool.QueueUserWorkItem(ThreadFunc);
            }

            public void RaiseCompleted()
            {
                m_completed(m_result);
                m_result = null;
            }
        }

        private List<WorkItem> m_toDoList = new List<WorkItem>();
        
        public void Submit(Func<object> job, Action<object> completed)
        {
            WorkItem work = new WorkItem(job, completed);
            m_toDoList.Add(work);
            work.Run();
        }

        public void CancelAll()
        {
            m_toDoList.Clear();
        }

        private void Update()
        {
            for(int i = m_toDoList.Count - 1; i >= 0; --i)
            {
                WorkItem work = m_toDoList[i];
                lock(work.Lock)
                {
                    if(work.IsCompleted)
                    {
                        try
                        {
                            work.RaiseCompleted();
                        }
                        finally
                        {
                            m_toDoList.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }

}

