using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Battlehub.VoxelCombat.Tests
{
    public class TaskEngineTests
    {
        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode

        private bool m_moveTaskCompleted;
        private TaskInfo m_moveTask;

        [UnityTest]
        public IEnumerator ExecuteMoveTask()
        {
            ITaskEngine taskEngine = MatchFactory.CreateTaskEngine();
            taskEngine.TaskStateChanged += OnTaskStateChanged;

            TaskInfo taskInfo = new TaskInfo(TaskType.Command, new Cmd(CmdCode.RotateRight));
            taskEngine.Submit(taskInfo);

            int iteration = 0;
            while(!m_moveTaskCompleted)
            {
                taskEngine.Update();
                taskEngine.Tick();

                yield return null;

                iteration++;
                Assert.LessOrEqual(iteration, 100);
            }

            taskEngine.TaskStateChanged -= OnTaskStateChanged;
            MatchFactory.DestroyTaskEngine(taskEngine);
        }

        private void OnTaskStateChanged(TaskInfo taskInfo)
        {
            if(taskInfo == m_moveTask && taskInfo.State == TaskState.Completed)
            {
                m_moveTaskCompleted = true;
            }
        }
    }

}
