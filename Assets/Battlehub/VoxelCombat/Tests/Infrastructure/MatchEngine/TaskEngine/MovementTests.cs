using Battlehub.VoxelCombat.Tests;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Battlehub.VoxelCombat
{
    public class MovementTests : LocalMatchServerTestBase
    {
        private const string TestEnv2 = "test_env_2";
        private const string TestEnv3 = "test_env_3";
        private const string TestEnv4 = "test_env_4";

        private TaskInfo MoveToCoordinate(Coordinate[] path, TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput)
        {
            TaskInfo pathVar = TaskInfo.EvalExpression(ExpressionInfo.Val(path));
            TaskInputInfo pathInput = new TaskInputInfo(pathVar, 0);
            TaskInfo moveTask = TaskInfo.Move(unitIndexInput, pathInput);

            return TaskInfo.Procedure(
                pathVar,
                moveTask,
                TaskInfo.Return(ExpressionInfo.TaskStatus(moveTask)));
        }

        private TaskInfo SearchForTestTargetAndMove(TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput)
        {
            TaskInfo pathVar = TaskInfo.Var();
            TaskInfo searhForPathTask = TaskInfo.SearchForPath(TaskType.TEST_SearchForWall, pathVar, unitIndexInput);
            ExpressionInfo pathFound = ExpressionInfo.TaskSucceded(searhForPathTask);
            TaskInputInfo pathInput = new TaskInputInfo(pathVar, 0);
            TaskInfo moveTask = TaskInfo.Move(unitIndexInput, pathInput);
            
            return TaskInfo.Procedure(
                pathVar,
                TaskInfo.Log("search for path started"),
                searhForPathTask,
                TaskInfo.Log(
                    ExpressionInfo.Add(
                        ExpressionInfo.PrimitiveVal("path found ? "),
                        pathFound)
                ),
                moveTask,
                TaskInfo.Return(ExpressionInfo.TaskStatus(moveTask))
                );
        }

        [UnityTest]
        public IEnumerator MovementTest([NUnit.Framework.Range(0, 5, 1)] int unitNumber)
        {
            const int playerId = 2;

            VoxelData voxel = null;
            yield return TaskTest(playerId, (unitIndexInput, playerIndexInput) => SearchForTestTargetAndMove(unitIndexInput, playerIndexInput), false,
                map =>
                {
                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);
                    voxel = map.Get(coords[unitNumber]);
                },
                rootTaskInfo =>
                {
                    VoxelData prev = voxel.Prev;
                    Assert.IsNotNull(prev);
                    Assert.AreEqual(playerId, prev.Owner);
                    Assert.AreEqual((int)KnownVoxelTypes.Ground, prev.Type);
                },
                childTaskCompleted => { },
                unitNumber,
                TestEnv2);
        }

        [UnityTest]
        public IEnumerator MovementTest3()
        {
            const int playerId = 2;
            const int unitNumber = 0;
            VoxelData voxel = null;
            Coordinate[] path = null;
            yield return TaskTest(playerId, (unitIndexInput, playerIndexInput) => MoveToCoordinate(path, unitIndexInput, playerIndexInput), false,
                map =>
                {
                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);
                    voxel = map.Get(coords[unitNumber]);

                    Coordinate[] targetCoord = map.FindDataOfType((int)KnownVoxelTypes.Ground, playerId);
                    Debug.LogFormat("and should move to {0}", targetCoord[0]);

                    path = new[] { coords[unitNumber], targetCoord[0] };
                },
                rootTaskInfo =>
                {
                    VoxelData prev = voxel.Prev;
                    Assert.IsNotNull(prev);
                    Assert.AreEqual(playerId, prev.Owner);
                    Assert.AreEqual((int)KnownVoxelTypes.Ground, prev.Type);
                    Assert.AreEqual(prev.Altitude + prev.Height, voxel.Altitude);
                },
                childTaskCompleted => { },
                unitNumber,
                TestEnv3);
        }

        [UnityTest]
        public IEnumerator MovementTest4()
        {
            const int playerId = 2;
            const int unitNumber = 0;
            VoxelData voxel = null;
            Coordinate[] path = null;
            yield return TaskTest(playerId, (unitIndexInput, playerIndexInput) => MoveToCoordinate(path, unitIndexInput, playerIndexInput), false,
                map =>
                {
                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);
                    voxel = map.Get(coords[unitNumber]);

                    Coordinate[] targetCoord = map.FindDataOfType((int)KnownVoxelTypes.Ground, playerId);
                    Debug.LogFormat("and should move to {0}", targetCoord[0]);

                    targetCoord[0].Altitude = coords[unitNumber].Altitude;
                    path = new[] { coords[unitNumber], targetCoord[0] };
                },
                rootTaskInfo =>
                {
                    VoxelData prev = voxel.Prev;
                    Assert.IsNotNull(prev);
                    Assert.AreEqual(playerId, prev.Owner);
                    Assert.AreEqual((int)KnownVoxelTypes.Ground, prev.Type);
                    Assert.AreEqual(prev.Altitude + prev.Height, voxel.Altitude);
                },
                childTaskCompleted => { },
                unitNumber,
                TestEnv4);
        }

        public IEnumerator TaskTest(
            int playerId,
            Func<TaskInputInfo, TaskInputInfo, TaskInfo> GetTestTaskInfo,
            bool shouldTaskBeFailed,
            Action<MapRoot> testStarted,
            Action<TaskInfo> rootTaskCompleted,
            Action<TaskInfo> childTaskCompleted = null,
            int unitNumber = 0,
            string testEnv = TestEnv2)
        {
            BeginTest(testEnv, 2, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                testStarted(map);

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


