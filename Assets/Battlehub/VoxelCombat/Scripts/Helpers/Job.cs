using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;


namespace Battlehub.VoxelCombat
{
    public interface IJob
    {
        void Submit(Func<object> job, Action<object> completed);

        void CancelAll();
    }

    public class Job : MonoBehaviour, IJob
    {
        public class JobContainer
        {
            public object Lock = new object();

            public bool IsCompleted;

            private Func<object> m_job;

            private Action<object> m_completed;

            private object m_result;

            public JobContainer(Func<object> job, Action<object> completed)
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

                //return () =>
                //{
                //    m_result = null;
                //};
            }

            public void RaiseCompleted()
            {
                m_completed(m_result);
                m_result = null;
            }
        }

        private List<JobContainer> m_jobs = new List<JobContainer>();
        
        public void Submit(Func<object> job, Action<object> completed)
        {
            JobContainer jc = new JobContainer(job, completed);
            m_jobs.Add(jc);
            jc.Run();
        }

        public void CancelAll()
        {
            m_jobs.Clear();
        }

        private void Update()
        {
            for(int i = m_jobs.Count - 1; i >= 0; --i)
            {
                JobContainer jc = m_jobs[i];
                lock(jc.Lock)
                {
                    if(jc.IsCompleted)
                    {
                        try
                        {
                            jc.RaiseCompleted();
                        }
                        finally
                        {
                            m_jobs.RemoveAt(i);
                        }
                    }
                }
            }
        }
    }

}

