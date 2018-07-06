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
        public IEnumerator EatGrowSplit()
        {
            BeginTest(TestEnv1, 2, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                const int playerId = 2;
                Coordinate[] coords = map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
                VoxelData voxel = map.Get(coords[0]);
                TaskInfo unitIndexTask = TaskInfo.UnitOrAssetIndex(voxel.UnitOrAssetIndex);
                TaskInputInfo unitIndexInput = new TaskInputInfo
                {
                    OutputIndex = 0,
                    OutputTask = unitIndexTask
                };

                TaskInfo checkCanGrow = TaskInfo.EvalExpression(
                    ExpressionInfo.UnitCanGrow(ExpressionInfo.Val(unitIndexInput), playerId));

                TaskInputInfo checkCanGrowInput = new TaskInputInfo
                {
                    OutputTask = checkCanGrow,
                    OutputIndex = 0,
                };
                
                ExpressionInfo notSupported = ExpressionInfo.Eq(
                    ExpressionInfo.Val(CmdResultCode.Fail_NotSupported),
                    ExpressionInfo.Val(checkCanGrowInput));

                ExpressionInfo somethingWrong = ExpressionInfo.Eq(
                    ExpressionInfo.Val(CmdResultCode.Fail_InvalidOperation),
                    ExpressionInfo.Val(checkCanGrowInput));

                ExpressionInfo maxWeight = ExpressionInfo.Eq(
                    ExpressionInfo.Val(CmdResultCode.Fail_MaxWeight),
                    ExpressionInfo.Val(checkCanGrowInput));

                ExpressionInfo needMoreFood = ExpressionInfo.Eq(
                    ExpressionInfo.Val(CmdResultCode.Fail_NeedMoreResources),
                    ExpressionInfo.Val(checkCanGrowInput));

                ExpressionInfo collapsedOrBlocked = ExpressionInfo.Eq(
                    ExpressionInfo.Val(CmdResultCode.Fail_CollapsedOrBlocked),
                    ExpressionInfo.Val(checkCanGrowInput));

                ExpressionInfo wrongLocation = ExpressionInfo.Eq(
                    ExpressionInfo.Val(CmdResultCode.Fail_InvalidLocation),
                    ExpressionInfo.Val(checkCanGrowInput));

                TaskInfo searchForFoodTask = TaskInfo.SearchForFood(unitIndexInput);
                ExpressionInfo searchForFoodSucceded = ExpressionInfo.TaskSucceded(searchForFoodTask);
                ExpressionInfo whileTrue = ExpressionInfo.PrimitiveVar(true);
                TaskInputInfo foodCoordinateInput = new TaskInputInfo(searchForFoodTask, 1);
               

                TaskInfo findPathToFoodTask = TaskInfo.FindPath(unitIndexInput, foodCoordinateInput);
                ExpressionInfo findPathSucceded = ExpressionInfo.TaskSucceded(findPathToFoodTask);
                TaskInfo elseIfNeedMoreFood = null;
                //TaskInfo elseIfNeedMoreFood = TaskInfo.Branch(needMoreFood,
                //    TaskInfo.Repeat(whileTrue,
                //        searchForFoodTask,
                //        TaskInfo.Branch(searchForFoodSucceded,
                //            TaskInfo.Sequence(
                //                findPathToFoodTask,//find path move to food
                //                TaskInfo.Branch(findPathSucceded,
                //                    null,
                //                    TaskInfo.Continue()), 
                //            null//move to random location
                //        ) 
                //    ),
                //    null
                //);

                TaskInfo elseIfMaximumWeight = TaskInfo.Branch(maxWeight,
                    TaskInfo.Return(),
                    elseIfNeedMoreFood);

                TaskInfo ifNotSupportedOrSomethingWrong = TaskInfo.Branch(
                    ExpressionInfo.Or(notSupported, somethingWrong),
                    TaskInfo.Return(ExpressionInfo.PrimitiveVar(TaskInfo.TaskFailed)),
                    elseIfMaximumWeight);

                TaskInfo rootTask = new TaskInfo(TaskType.Sequence)
                {
                    Children = new[]
                    {
                        unitIndexTask,
                        checkCanGrow,
                        TaskInfo.SearchForFood(unitIndexInput),
                    }
                };

                rootTask.SetParents();
                rootTask.Initialize(playerId);

                ITaskEngine taskEngine = matchEngineCli.GetClientTaskEngine(playerId);
                TaskEngineEvent<TaskInfo> taskStateChanged = null;
                taskStateChanged = taskInfo =>
                {
                    if (taskInfo.State == TaskState.Completed)
                    {
                        if(taskInfo.TaskId == rootTask.TaskId)
                        {
                            taskEngine.TaskStateChanged -= taskStateChanged;
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
        public IEnumerator SearchForFoodTaskTest()
        {
            BeginTest(TestEnv1, 2, 0, () =>
            {
                MapRoot map = Dependencies.Map.Map;
                IMatchEngineCli matchEngineCli = Dependencies.MatchEngine;

                const int playerId = 2;
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
                            Assert.IsFalse(taskInfo.IsFailed);

                            ITaskMemory memory = taskEngine.Memory;
                            Coordinate coordinate = (Coordinate)memory.ReadOutput(searchForFoodTask.Parent.TaskId, searchForFoodTask.TaskId, 1);
                            Assert.AreEqual(1, coordinate.MapPos.SqDistanceTo(coords[0].MapPos));

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
