using Battlehub.VoxelCombat.Tests;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine.TestTools;

namespace Battlehub.VoxelCombat
{
    public class MovementTests : LocalMatchServerTestBase
    {
        protected readonly string TestEnv2 = "test_env_2";

        private TaskInfo SearchForTestTargetAndMove(TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput)
        {
            return null;
        }

        [UnityTest]
        public IEnumerator MovementTest([Range(0, 9, 1)] int unitNumber)
        {
            const int playerId = 2;
            MapRoot map = Dependencies.Map.Map;
            Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            VoxelData voxel = map.Get(coords[unitNumber]);

            yield return TaskTest(playerId, (unitIndexInput, playerIndexInput) => SearchForTestTargetAndMove(unitIndexInput, playerIndexInput), false,
                rootTaskInfo =>
                {
                    VoxelData prev = voxel.Prev;
                    Assert.IsNotNull(prev);
                    Assert.AreEqual(playerId, prev.Owner);
                    Assert.AreEqual((int)KnownVoxelTypes.Ground, prev.Type);
                },
                childTaskCompleted => { },
                unitNumber);
        }

        public IEnumerator TaskTest(int playerId,
            Func<TaskInputInfo, TaskInputInfo, TaskInfo> GetTestTaskInfo,
            bool shouldTaskBeFailed,
            Action<TaskInfo> rootTaskCompleted,
            Action<TaskInfo> childTaskCompleted = null,
            int unitNumber = 0)
        {
            BeginTest(TestEnv2, 2, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                VoxelData voxel = map.Get(coords[unitNumber]);
                TaskInfo unitIndexTask = TaskInfo.UnitOrAssetIndex(voxel.UnitOrAssetIndex);
                TaskInfo playerIndexTask = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVal(playerId));
                TaskInputInfo unitIndexInput = new TaskInputInfo
                {
                    OutputIndex = 0,
                    OutputTask = unitIndexTask
                };
                TaskInputInfo playerIndexInput = new TaskInputInfo
                {
                    OutputIndex = 0,
                    OutputTask = playerIndexTask
                };

                TaskInfo testTaskInfo = GetTestTaskInfo(unitIndexInput, playerIndexInput);
                TaskInfo rootTask = TaskInfo.Procedure(
                    playerIndexTask,
                    unitIndexTask,
                    testTaskInfo,
                    TaskInfo.Return(ExpressionInfo.TaskStatus(testTaskInfo)));
                rootTask.SetParents();
                rootTask.Initialize(playerId);

                ITaskEngine taskEngine = matchEngineCli.GetClientTaskEngine(playerId);
                TaskEngineEvent<TaskInfo> taskStateChanged = null;
                taskStateChanged = taskInfo =>
                {
                    if (taskInfo.State == TaskState.Completed)
                    {
                        if (taskInfo.TaskId == rootTask.TaskId)
                        {
                            Assert.AreEqual(shouldTaskBeFailed, taskInfo.IsFailed, taskInfo.ToString());
                            taskEngine.TaskStateChanged -= taskStateChanged;
                            rootTaskCompleted(taskInfo);
                            EndTest();
                        }
                        else
                        {
                            if (childTaskCompleted != null)
                            {
                                childTaskCompleted(taskInfo);
                            }
                        }
                    }
                    else if (taskInfo.State != TaskState.Idle)
                    {
                        Assert.AreEqual(TaskState.Active, taskInfo.State, taskInfo.ToString());
                    }
                };
                taskEngine.TaskStateChanged += taskStateChanged;
                taskEngine.SubmitTask(rootTask);
            });


            while (true)
            {
                yield return null;
            }
        }
    }
}


