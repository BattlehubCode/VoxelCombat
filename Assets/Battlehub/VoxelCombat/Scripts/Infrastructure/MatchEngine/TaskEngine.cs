using System;

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
        int Submit(TaskInfo taskInfo);

        void Terminate(int taskId);

        void Tick();

        void Update();

        void Destroy();
    }

    public class TaskEngine : ITaskEngine
    {
        public event TaskEngineEvent TaskStateChanged;

        private IMatchView m_match;

        public TaskEngine(IMatchView match)
        {
            m_match = match;
        }

        public int Submit(TaskInfo taskInfo)
        {
            throw new NotImplementedException();
        }

    
        public void Terminate(int taskId)
        {

        }

        public void Tick()
        {

        }

        public void Update()
        {

        }

        public void Destroy()
        {

        }
    }
}
