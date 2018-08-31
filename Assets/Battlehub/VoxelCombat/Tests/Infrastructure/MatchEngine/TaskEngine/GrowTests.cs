using Battlehub.VoxelCombat.Tests;
using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Battlehub.VoxelCombat
{
    public class GrowTests : LocalMatchServerTestBase
    {
        private const string Bug3Repro = "bug3_repro";
        
        public static TaskInfo HealMoveToCoordGrow(Coordinate[] path, TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput)
        {
            TaskInfo taskInfo = TaskInfo.Procedure
                (
                    TaskInfo.SetHealth(unitIndexInput, 64),
                    MoveToCoordinate(path, unitIndexInput, playerIndexInput),
                    TaskInfo.Grow(unitIndexInput),
                    TaskInfo.Return(ExpressionInfo.PrimitiveVal(TaskInfo.TaskSucceded))
                );

            taskInfo.Inputs = new TaskInputInfo[] { unitIndexInput, playerIndexInput };
            return taskInfo;
        }

        private static TaskInfo MoveToCoordinate(Coordinate[] path, TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput)
        {
            TaskInfo pathVar = TaskInfo.EvalExpression(ExpressionInfo.Val(path));
            TaskInputInfo pathInput = new TaskInputInfo(pathVar, 0);
            TaskInfo moveTask = TaskInfo.Move(unitIndexInput, pathInput);

            return TaskInfo.Procedure(
                pathVar,
                moveTask,
                TaskInfo.Return(ExpressionInfo.TaskStatus(moveTask)));
        }

        [UnityTest]
        public IEnumerator GrowTest()
        {
            const int playerId = 1;
            const int unitNumber = 0;
            Coordinate[] coordinates = new[]
            {
                new Coordinate(27, 32, 1, 2),
                new Coordinate(27, 33, 1, 2),
            };
            MapCell cell = null;
            VoxelData voxel = null;
            MapRoot mapRoot = null;
            yield return TaskTest(playerId, (unitIndexInput, playerIndexInput) => HealMoveToCoordGrow(coordinates, unitIndexInput, playerIndexInput), false,
                map =>
                {
                    mapRoot = map;
                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);

                    Coordinate coord = coords[unitNumber];
                    voxel = map.Get(coord);
                   
                },
                rootTaskInfo =>
                {
                    VoxelData prev = voxel.Prev;

                    Coordinate[] coords = mapRoot.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                    Coordinate coord = coords[unitNumber];
                    cell = mapRoot.Get(coord.Row, coord.Col, coord.Weight);
                    Assert.IsNull(cell.GetDescendantsWithVoxelData(data => data != null));
                    Assert.IsNotNull(prev);
                    Assert.AreEqual((int)KnownVoxelTypes.Ground, prev.Type);
                },
                childTaskCompleted => { },
                unitNumber,
                Bug3Repro);
        }

        public IEnumerator TaskTest(
            int playerId,
            Func<TaskInputInfo, TaskInputInfo, TaskInfo> GetTestTaskInfo,
            bool shouldTaskBeFailed,
            Action<MapRoot> testStarted,
            Action<TaskInfo> rootTaskCompleted,
            Action<TaskInfo> childTaskCompleted,
            int unitNumber,
            string testEnv)
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

            yield return Run();
        }
    }
}


