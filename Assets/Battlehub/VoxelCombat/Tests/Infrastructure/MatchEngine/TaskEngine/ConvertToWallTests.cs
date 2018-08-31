using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Battlehub.VoxelCombat.Tests
{
    public class ConvertToWallTests : LocalMatchServerTestBase
    {
        private const string TestEnv5 = "test_env_5";

        public static TaskInfo SearchAndMoveAnyway(Coordinate[] waypoints, TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput)
        {
            TaskInfo defineGoalTask = TaskInfo.EvalExpression(ExpressionInfo.Val(waypoints));
            TaskInfo findPathTask = TaskInfo.FindPath(unitIndexInput, new TaskInputInfo(defineGoalTask, 0));
            TaskInfo taskInfo = TaskInfo.Procedure
                (
                    defineGoalTask,
                    findPathTask,
                    TaskInfo.Move(unitIndexInput, new TaskInputInfo(findPathTask, 0)),
                    TaskInfo.Return(ExpressionInfo.PrimitiveVal(TaskInfo.TaskSucceded))
                );

            taskInfo.Inputs = new TaskInputInfo[] { unitIndexInput, playerIndexInput };
            return taskInfo;
        }

        [UnityTest]
        public IEnumerator MoveAnywayTest()
        {
            const int playerId = 1;
            const int unitNumber = 0;

            Coordinate[] waypoints = null;
            Coordinate initialCoord = new Coordinate();
            VoxelData voxel = null;
            MapRoot mapRoot = null;
            yield return TaskDefaultTest(playerId, (unitIndexInput, playerIndexInput) => SearchAndMoveAnyway(waypoints, unitIndexInput, playerIndexInput), false,
                map =>
                {
                    mapRoot = map;
                
                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                  
                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);

                    initialCoord = coords[unitNumber];
                    voxel = map.Get(initialCoord);
                    waypoints = new[] { initialCoord, map.FindDataOfType((int)KnownVoxelTypes.Ground, playerId)[0] };
                },
                rootTaskInfo =>
                {
                    VoxelData prev = voxel.Prev;

                    Coordinate[] coords = mapRoot.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                    Coordinate coord = coords[unitNumber];

                    Assert.IsNotNull(prev);
                    Assert.AreEqual((int)KnownVoxelTypes.Ground, prev.Type);

                    Assert.AreNotEqual(waypoints[1], coord);
                    Assert.AreEqual(1, initialCoord.MapPos.SqDistanceTo(coord.MapPos));
                },
                childTaskCompleted => { },
                unitNumber,
                TestEnv5);
        }

        [UnityTest]
        public IEnumerator MoveAndConvertTo()
        {
            const int playerId = 2;
            const int unitNumber = 0;

            Coordinate[] waypoints = null;
            Coordinate initialCoord = new Coordinate();
            VoxelData voxel = null;
            MapRoot mapRoot = null;
            yield return TaskDefaultTest(playerId, (unitIndexInput, playerIndexInput) => TaskInfo.MoveAndConvertTo(unitIndexInput, playerIndexInput, waypoints[1], (int)KnownVoxelTypes.Ground), false,
                map =>
                {
                    mapRoot = map;

                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);

                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);

                    initialCoord = coords[unitNumber];
                    voxel = map.Get(initialCoord);
                    waypoints = new[] { initialCoord, map.FindDataOfType((int)KnownVoxelTypes.Ground, playerId)[0] };
                },
                rootTaskInfo =>
                {
                    Assert.IsFalse(voxel.IsAlive);

                    Coordinate[] coords = mapRoot.FindDataOfType((int)KnownVoxelTypes.Ground, playerId);

                    Assert.AreEqual(2, coords.Length);

                    VoxelData zero = mapRoot.Get(coords[0]);
                    VoxelData one = mapRoot.Get(coords[1]);

                    Assert.IsTrue(one.Prev == zero || zero.Prev == one);
                    if(one.Prev == zero)
                    {
                        Assert.AreEqual(4, one.Height);
                        Assert.AreEqual(zero.Altitude + zero.Height, one.Altitude);
                    }
                    else
                    {
                        Assert.AreEqual(4, zero.Height);
                        Assert.AreEqual(one.Altitude + one.Height, zero.Altitude);
                    }
                },
                childTaskCompleted => { },
                unitNumber,
                TestEnv5);
        }

        /*
        [UnityTest]
        public IEnumerator MoveAndConvertToSuccess()
        {
            const int playerId = 3;
            const int unitNumber = 0;

            Coordinate[] waypoints = null;
            Coordinate initialCoord = new Coordinate();
            VoxelData voxel = null;
            MapRoot mapRoot = null;

            int childTasksCounter = 0;
            yield return TaskDefaultTest(playerId, (unitIndexInput, playerIndexInput) => TaskInfo.MoveAndConvertToUntilSucces(unitIndexInput, playerIndexInput, waypoints[1], (int)KnownVoxelTypes.Ground), false,
                map =>
                {
                    mapRoot = map;

                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);

                    Debug.LogFormat("Unit {0} starting at {1}", unitNumber, coords[unitNumber]);

                    initialCoord = coords[unitNumber];
                    voxel = map.Get(initialCoord);
                    waypoints = new[] { initialCoord, map.FindDataOfType((int)KnownVoxelTypes.Ground, playerId)[0] };
                },
                rootTaskInfo =>
                {
                    Assert.IsFalse(voxel.IsAlive);

                    Coordinate[] coords = mapRoot.FindDataOfType((int)KnownVoxelTypes.Ground, playerId);

                    Assert.AreEqual(2, coords.Length);

                    VoxelData zero = mapRoot.Get(coords[0]);
                    VoxelData one = mapRoot.Get(coords[1]);

                    Assert.IsTrue(one.Prev == zero || zero.Prev == one);
                    if (one.Prev == zero)
                    {
                        Assert.AreEqual(4, one.Height);
                        Assert.AreEqual(zero.Altitude + zero.Height, one.Altitude);
                    }
                    else
                    {
                        Assert.AreEqual(4, zero.Height);
                        Assert.AreEqual(one.Altitude + one.Height, zero.Altitude);
                    }
                },
                childTaskCompleted => 
                {
                    childTasksCounter++;
                    if (childTasksCounter == 50)
                    {
                        Coordinate[] coords = mapRoot.FindDataOfType((int)KnownVoxelTypes.Eater, 4);

                        Debug.LogFormat("Now Destroying unit {0} at {1}", 0, coords[0]);

                        IMatchEngineCli matchEngine = Dependencies.MatchEngine;
                        matchEngine.Submit(4, new ChangeParamsCmd { Code = CmdCode.SetHealth, IntParams = new[] { 0 }, UnitIndex = mapRoot.Get(coords[0]).UnitOrAssetIndex });
                    }
                },
                unitNumber,
                TestEnv5,
                4,
                0);
        }
        */

    }
}
