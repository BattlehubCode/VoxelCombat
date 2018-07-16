using UnityEngine.TestTools;
using System.Collections;
using UnityEngine;
using NUnit.Framework;
using System;

namespace Battlehub.VoxelCombat.Tests
{
    public class LocalMatchServerTestBase
    {
        protected const int MAX_TICKS = 1000;
        private GameObject m_testGame;
     
        protected void BeginTest(string mapName, int playersCount, int botsCount, Action callback, int lag = 0)
        {
            Assert.DoesNotThrow(() =>
            {
                string testGamePath = "TestGame";
                m_testGame = UnityEngine.Object.Instantiate(Resources.Load<GameObject>(testGamePath));

                TestGameInitArgs gameInitArgs = new TestGameInitArgs
                {
                    MapName = mapName,
                    PlayersCount = playersCount,
                    BotsCount = botsCount
                };
                Dependencies.State.SetValue("Battlehub.VoxelGame.TestGameInitArgs", gameInitArgs);

                LocalMatchServer localMatchServer = (LocalMatchServer)Dependencies.LocalMatchServer;
                localMatchServer.Lag = lag;

                LocalGameServer localGameServer = (LocalGameServer)Dependencies.LocalGameServer;
                localGameServer.Lag = lag;

                IVoxelGame voxelGame = Dependencies.GameState;
                VoxelGameStateChangedHandler started = null;
                started = () =>
                {
                    voxelGame.Started -= started;
                    callback();
                };
                voxelGame.Started += started;
            });
        }

        [TearDown]
        public void TearDown()
        {
            if (m_testGame != null)
            {
                UnityEngine.Object.Destroy(m_testGame);
            }
        }

        protected IEnumerator Run(int MAX_TICKS = 100000)
        {
            for(int i = 0; i < MAX_TICKS; ++i)
            {
                yield return null;
            }
        }

        protected void EndTest()
        {
            Assert.Pass();
        }
    }

    
    public class TaskEngineClientTaskTests : LocalMatchServerTestBase
    {
        //4 Players, Depth 6, Flat square, Size 4x4, Cell weight 4 (Map name  test_env_0 4 players)
        // protected readonly string TestEnv0 = "021ef2f8-789c-44ff-b59b-0f43064c581b.data";
        protected readonly string TestEnv0 = "test_env_0";
        protected readonly string TestEnv1 = "test_env_1";

        [UnityTest]
        public IEnumerator InitTestLag()
        {
            BeginTest(TestEnv0, 4, 0, () =>
            {
                EndTest();
            }, 10);

            while (true)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator InitTest()
        {
            BeginTest(TestEnv0, 4, 0, () => {
                EndTest();
            });


            while (true)
            {
                yield return null;
            }   
        }

        [UnityTest]
        public IEnumerator EatGrowSplitTest()
        {
            yield return TaskTest(2, (unitIndexInput, playerIndexInput) => TaskInfo.EatGrowSplit4(unitIndexInput, playerIndexInput), false,
                rootTaskInfo =>
                {
                    MapRoot map = Dependencies.Map.Map;
                    Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, rootTaskInfo.PlayerIndex);
                    Assert.AreEqual(4, coords.Length);
                    Assert.AreEqual(1, coords[0].MapPos.SqDistanceTo(coords[1].MapPos));
                    Assert.AreEqual(1, coords[2].MapPos.SqDistanceTo(coords[3].MapPos));
                    Assert.AreEqual(1, coords[0].MapPos.SqDistanceTo(coords[2].MapPos));
                    Assert.AreEqual(1, coords[1].MapPos.SqDistanceTo(coords[3].MapPos));
                });
        }


        private TaskInfo PathToRandomLocation(TaskInputInfo unitIndexInput, TaskInputInfo playerInput)
        {
            int radius = 3;
            TaskInfo radiusVar = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVar(radius));
            TaskInputInfo radiusInput = new TaskInputInfo
            {
                OutputIndex = 0,
                OutputTask = radiusVar
            };

            TaskInfo pathToRandomLocation = TaskInfo.PathToRandomLocation(unitIndexInput, radiusInput);
            TaskInputInfo pathInput = new TaskInputInfo
            {
                OutputIndex = 0,
                OutputTask = pathToRandomLocation
            };

            TaskInfo assert = TaskInfo.Assert((taskBase, taskInfo) =>
            {
                Coordinate[] path = taskBase.ReadInput<Coordinate[]>(taskInfo.Inputs[0]);
                Assert.IsNotNull(path);
                Assert.IsTrue(path.Length > 1);
                Coordinate first = path[0];
                Coordinate last = path[path.Length - 1];
                Assert.LessOrEqual(Mathf.Abs(first.Row - last.Row), 3);
                Assert.LessOrEqual(Mathf.Abs(first.Col - last.Col), 3);
                return TaskState.Completed;

            });
            assert.Inputs = new[] { pathInput };

            return TaskInfo.Sequence(
                radiusVar,
                pathToRandomLocation,
                TaskInfo.Branch(
                    ExpressionInfo.TaskSucceded(pathToRandomLocation),
                    assert,
                    new TaskInfo(TaskType.TEST_Fail)
                )
             );
        }

        [UnityTest]
        public IEnumerator PathToRandomLocationTest()
        {
            yield return TaskTest(3, PathToRandomLocation, false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed); 
                });
        }

        [UnityTest]
        public IEnumerator MoveToRandomLocationTest()
        {
            yield return TaskTest(2, (unitIndexInput, playerId) => TaskInfo.MoveToRandomLocation(unitIndexInput, 3, 100), false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed);
                });
        }

        [UnityTest]
        public IEnumerator MoveToRandomLocationTestFail()
        {
            yield return TaskTest(4, (unitIndexInput, playerId) => TaskInfo.MoveToRandomLocation(unitIndexInput, 3, 2), true,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsTrue(rootTaskInfo.IsFailed);
                },
                null,
                3);
        }

        [UnityTest]
        public IEnumerator MoveToRandomLocationTest2()
        {
            yield return TaskTest(4, (unitIndexInput, playerId) => TaskInfo.MoveToRandomLocation(unitIndexInput, 3, 1000), false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed);
                }, 
                null,
                2);
        }

        [UnityTest]
        public IEnumerator MoveToRandomLocationTestFail2()
        {
            yield return TaskTest(4, (unitIndexInput, playerId) => TaskInfo.MoveToRandomLocation(unitIndexInput, 3, 100), true,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsTrue(rootTaskInfo.IsFailed);
                },
                null,
                1);
        }

        [UnityTest]
        public IEnumerator SearchMoveOrRandomMoveTest()
        {
            yield return TaskTest(2, (unitIndexInput, playerId) => TaskInfo.SearchMoveOrRandomMove(TaskType.SearchForFood, unitIndexInput), false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed);
                });
        }

        [UnityTest]
        public IEnumerator SearchMoveOrRandomMove2Test()
        {
            yield return TaskTest(2, (unitIndexInput, playerId) => TaskInfo.SearchMoveOrRandomMove(TaskType.SearchForGrowLocation, unitIndexInput), false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed);
                });
        }

        [UnityTest]
        public IEnumerator SearchMoveOrRandomMove3Test()
        {
            yield return TaskTest(2, (unitIndexInput, playerId) => TaskInfo.SearchMoveOrRandomMove(TaskType.SearchForSplit4Location, unitIndexInput), false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed);
                });
        }

        [UnityTest]
        public IEnumerator SearchMoveGrowTest()
        {
            yield return TaskTest(2, (unitIndexInput, playerId) =>
                TaskInfo.Sequence
                (
                    TaskInfo.SetHealth(unitIndexInput, 64),
                    TaskInfo.SearchMoveGrow(unitIndexInput, playerId)
                ), 
                false,
                rootTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, rootTaskInfo.State);
                    Assert.IsFalse(rootTaskInfo.IsFailed);
                });
        }

        [UnityTest]
        public IEnumerator SearchMoveSplit4Test()
        {
            yield return TaskTest(2, (unitIndexInput, playerId) =>
             TaskInfo.Sequence
                (
                    TaskInfo.SetHealth(unitIndexInput, 64),
                    TaskInfo.SearchMoveGrow(unitIndexInput, playerId),
                    TaskInfo.SearchMoveSplit4(unitIndexInput, playerId)
                )
                , false,
                rootTaskInfo =>
                {

                });
        }

        public IEnumerator TaskTest(int playerId, 
            Func<TaskInputInfo, TaskInputInfo, TaskInfo> GetTestTaskInfo,
            bool shouldTaskBeFailed,
            Action<TaskInfo> rootTaskCompleted,
            Action<TaskInfo> childTaskCompleted = null,
            int unitNumber = 0)
        {
            BeginTest(TestEnv1, 4, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                VoxelData voxel = map.Get(coords[unitNumber]);
                TaskInfo unitIndexTask = TaskInfo.UnitOrAssetIndex(voxel.UnitOrAssetIndex);
                TaskInfo playerIndexTask = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVar(playerId));
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
                TaskInfo rootTask = TaskInfo.Sequence(
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
                            if(childTaskCompleted != null)
                            {
                                childTaskCompleted(taskInfo);
                            }
                        }
                    }
                    else if(taskInfo.State != TaskState.Idle)
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

        [UnityTest]
        public IEnumerator SearchForFoodTaskTest()
        {
            return SearchForFoodTaskTest(2, (taskEngine, taskInfo, searchForFoodTask, coords) =>
            {
                Assert.IsFalse(taskInfo.IsFailed);

                ITaskMemory memory = taskEngine.Memory;
                Coordinate[] coordinate = (Coordinate[])memory.ReadOutput(searchForFoodTask.Parent.TaskId, searchForFoodTask.TaskId, 1);
                Assert.AreEqual(1, coordinate[1].MapPos.SqDistanceTo(coords[0].MapPos));
            });
        }

        [UnityTest]
        public IEnumerator SearchForFoodTaskTestFail()
        {
            return SearchForFoodTaskTest(1, (taskEngine, taskInfo, searchForFoodTask, coords) =>
            {
                Assert.IsTrue(taskInfo.IsFailed);
            });
        }


        public IEnumerator SearchForFoodTaskTest(int playerId, Action<ITaskEngine, TaskInfo, TaskInfo, Coordinate[]> callback)
        {
            BeginTest(TestEnv1, 2, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                VoxelData voxel = map.Get(coords[0]);

                TaskInfo searchForFoodTask = new TaskInfo(TaskType.SearchForFood)
                {
                    OutputsCount = 2
                };
                TaskInputInfo searchForFoodContext = new TaskInputInfo
                {
                    OutputIndex = 0,
                    OutputTask = searchForFoodTask,
                };

                TaskInfo getUnitIndexTask = new TaskInfo(TaskType.EvalExpression)
                {
                    Expression = ExpressionInfo.PrimitiveVar(voxel.UnitOrAssetIndex),
                    OutputsCount = 1
                };
                TaskInputInfo unitIndex = new TaskInputInfo
                {
                    OutputIndex = 0,
                    OutputTask = getUnitIndexTask
                };

                searchForFoodTask.Inputs = new[] { searchForFoodContext, unitIndex };
                TaskInfo rootTask = new TaskInfo(TaskType.Sequence)
                {
                    Children = new[] { getUnitIndexTask, searchForFoodTask }
                };

                rootTask.SetParents();
                rootTask.Initialize(playerId);

                ITaskEngine taskEngine = matchEngineCli.GetClientTaskEngine(playerId);
                TaskEngineEvent<TaskInfo> taskStateChanged = null;
                taskStateChanged = taskInfo =>
                {
                    if (taskInfo.State == TaskState.Completed)
                    {
                        if(taskInfo.TaskId == searchForFoodTask.TaskId)
                        {
                            taskEngine.TaskStateChanged -= taskStateChanged;

                            callback(taskEngine, taskInfo, searchForFoodTask, coords);

                            EndTest();
                        } 
                    }
                    else
                    {
                        Assert.AreEqual(TaskState.Active, taskInfo.State);
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

        [UnityTest]
        public IEnumerator FindPathClientSideTaskTest()
        {
            BeginTest(TestEnv1, 2, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                const int playerId = 1;
                Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                VoxelData data = map.Get(coords[0]);
                Coordinate targetCoordinate = coords[0].Add(1, -1);
                MovementCmd moveCmd = new MovementCmd(CmdCode.Move, data.UnitOrAssetIndex, 0);
                moveCmd.Coordinates = new[] { coords[0], targetCoordinate };

                ITaskEngine taskEngine = matchEngineCli.GetClientTaskEngine(playerId);
                TaskEngineEvent<TaskInfo> taskStateChanged = null;
                taskStateChanged = taskStateInfo =>
                {
                    if (taskStateInfo.State == TaskState.Completed)
                    {
                        taskEngine.TaskStateChanged -= taskStateChanged;

                        Coordinate[] newCoords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                        Assert.AreEqual(targetCoordinate, newCoords[0]);

                        EndTest();
                    }
                    else
                    {
                        Assert.AreEqual(TaskState.Active, taskStateInfo.State);
                    }  
                };
                taskEngine.TaskStateChanged += taskStateChanged;

                TaskInfo taskInfo = new TaskInfo(moveCmd, playerId);
                taskInfo.RequiresClientSidePreprocessing = true;

                taskEngine.SubmitTask(taskInfo);
            });
            while (true)
            {
                yield return null;
            }
        }


        [UnityTest]
        public IEnumerator FindPathClientSidePreprocessingTest()
        {
            BeginTest(TestEnv0, 4, 0, () => {

                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                const int playerId = 3;
                Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                VoxelData data = map.Get(coords[0]);
                Coordinate targetCoordinate = coords[0].Add(-1, -1);
                MovementCmd moveCmd = new MovementCmd(CmdCode.Move, data.UnitOrAssetIndex, 0);
                moveCmd.Coordinates = new[] { coords[0],  targetCoordinate };

                MatchEngineCliEvent<long, CommandsBundle> eventHandler = null;
                eventHandler = (e, tick, commandsBundle) =>
                {
                    if (commandsBundle.TasksStateInfo != null)
                    {
                        TaskStateInfo taskStateInfo = commandsBundle.TasksStateInfo[0];
                        Assert.AreEqual(taskStateInfo.PlayerId, playerId);

                        if(taskStateInfo.State == TaskState.Completed)
                        {
                            matchEngineCli.ExecuteCommands -= eventHandler;

                            Coordinate[] newCoords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                            Assert.AreEqual(targetCoordinate, newCoords[0]);

                            EndTest();
                        }
                        else
                        {
                            Assert.AreEqual(TaskState.Active, taskStateInfo.State);
                        }
                    }
                };
                matchEngineCli.ExecuteCommands += eventHandler;

                TaskInfo taskInfo = new TaskInfo(moveCmd);
                taskInfo.RequiresClientSidePreprocessing = true;
                matchEngineCli.GetClientTaskEngine(playerId).GenerateIdentitifers(taskInfo);
                matchEngineCli.Submit(playerId, new TaskCmd(taskInfo));
            });

            while (true)
            {
                yield return null;
            }
        }

        

       
    }
}
