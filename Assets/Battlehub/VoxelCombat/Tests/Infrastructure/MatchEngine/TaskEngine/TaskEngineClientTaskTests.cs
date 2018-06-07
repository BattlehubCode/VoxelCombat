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

     

        protected void BeginTest(string mapName, int playersCount, int botsCount, Action callback, int lag = 0)
        {
            Assert.DoesNotThrow(() =>
            {
                string testGamePath = "TestGame";
                UnityEngine.Object.Instantiate(Resources.Load<GameObject>(testGamePath));

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
        public IEnumerator FindPathClientSideTest()
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
                matchEngineCli.Submit(playerId, new TaskCmd(taskInfo));
            });

            while (true)
            {
                yield return null;
            }
        }

       
    }
}
