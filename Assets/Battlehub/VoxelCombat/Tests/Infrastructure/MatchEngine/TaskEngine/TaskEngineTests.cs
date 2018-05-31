using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Battlehub.VoxelCombat.Tests
{
    public class MatchEngineTestsBase
    {
        protected IMatchEngine m_engine;
        protected IReplaySystem m_replay;
        protected MapRoot m_map;
        private float m_prevTickTime;
        private long m_tick;


        protected const int MAX_TICKS = 1000;
        //4 Players, Depth 6, Flat square, Size 4x4, Cell weight 4
        protected readonly string TestEnv0 = "021ef2f8-789c-44ff-b59b-0f43064c581b.data";

        protected Guid[] m_players;
        protected Dictionary<int, VoxelAbilities[]> m_abilities;

        private VoxelAbilities[] CreateTemporaryAbilies()
        {
            List<VoxelAbilities> abilities = new List<VoxelAbilities>();
            Array voxelTypes = Enum.GetValues(typeof(KnownVoxelTypes));
            for (int typeIndex = 0; typeIndex < voxelTypes.Length; ++typeIndex)
            {
                VoxelAbilities ability = new VoxelAbilities((int)voxelTypes.GetValue(typeIndex));
                abilities.Add(ability);
            }
            return abilities.ToArray();
        }

        protected void BeginTest(string mapName, int playersCount)
        {
            m_abilities = new Dictionary<int, VoxelAbilities[]>();
            m_players = new Guid[playersCount];
            for(int i = 0; i < m_players.Length; ++i)
            {
                m_players[i] = Guid.NewGuid();
                m_abilities.Add(i, CreateTemporaryAbilies());
            }

            string dataPath = Application.streamingAssetsPath + "/Maps/";
            string filePath = dataPath + mapName;

            m_replay = MatchFactory.CreateReplayRecorder();
           
            Dictionary<int, VoxelAbilities>[] allAbilities = new Dictionary<int, VoxelAbilities>[m_players.Length];
            for (int i = 0; i < m_players.Length; ++i)
            {
                allAbilities[i] = m_abilities[i].ToDictionary(a => a.Type);
            }

            MapData mapData = ProtobufSerializer.Deserialize<MapData>(File.ReadAllBytes(filePath));
            m_map = ProtobufSerializer.Deserialize<MapRoot>(mapData.Bytes);
            m_engine = MatchFactory.CreateMatchEngine(m_map, playersCount);
            for (int i = 0; i < m_players.Length; ++i)
            {
                m_engine.RegisterPlayer(m_players[i], i, allAbilities);
            }
            m_engine.CompletePlayerRegistration();
        }

    
        protected void RunEngine(int ticks = MAX_TICKS)
        {
            for (int i = 0; i < ticks; ++i)
            {
                m_engine.PathFinder.Update();
                m_engine.TaskRunner.Update();
                m_engine.BotPathFinder.Update();
                m_engine.BotTaskRunner.Update();
                
                m_replay.Tick(m_engine, m_tick);
                CommandsBundle commands;
                if (m_engine.Tick(out commands))
                {
                    commands.Tick = m_tick;
                }

                m_tick++;
                m_prevTickTime += GameConstants.MatchEngineTick;
            }
        }

        protected void EndTest()
        {
            MatchFactory.DestroyMatchEngine(m_engine);
        }

        protected virtual void OnTaskStateChanged(TaskInfo taskInfo)
        {

        }
    }

    public class TaskEngineTests : MatchEngineTestsBase
    {
        private void PrepareTestData1(int playerId, int offsetX, int offsetY, out Cmd cmd, out ExpressionInfo expression)
        {
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(coords.Length, 1);

            VoxelData unit = m_map.Get(coords[0]);
            Coordinate targetCoordinate = coords[0].Add(offsetX, offsetY);

            cmd = new MovementCmd(CmdCode.MoveSearch, unit.UnitOrAssetIndex, 0)
            {
                Coordinates = new[] { targetCoordinate },
            };
            expression = ExpressionInfo.MoveTaskExpression(unit.UnitOrAssetIndex, playerId, targetCoordinate);
        }

        private Cmd PrepareTestData2(int playerId, int cmdCode, int type)
        {
            Cmd cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(coords.Length, 1);

            VoxelData unit = m_map.Get(coords[0]);

            switch(cmdCode)
            {
                case CmdCode.Convert:
                {
                    cmd = new ChangeParamsCmd(CmdCode.Convert)
                    {
                        UnitIndex = unit.UnitOrAssetIndex,
                        IntParams = new int[] { type }
                    };
                    break;
                }
                case CmdCode.Diminish:
                case CmdCode.Grow:
                case CmdCode.Split:
                case CmdCode.Split4:
                {
                    cmd = new Cmd(cmdCode, unit.UnitOrAssetIndex);
                    break;
                }
                default:
                {
                    cmd = null;
                    break;
                }  
            }

            return cmd;  
        }

        [Test]
        public void SimpleMoveUsingExpression()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 2);
            });
            const int playerId = 1;
            Cmd cmd;
            ExpressionInfo expression;
            PrepareTestData1(playerId, -1, 1,
                out cmd, 
                out expression);
            TaskInfo task = new TaskInfo(cmd, expression);
            FinializeTest(playerId, task, TaskMovementCompleted);
        }

        [Test]
        public void SimpleMoveWithoutExpression()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 2);
            });
            const int playerId = 1;
            Cmd cmd;
            ExpressionInfo notUsed;
            PrepareTestData1(playerId, -1, 1,
                out cmd,
                out notUsed);
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, TaskMovementCompleted);
        }

        [Test]
        public void SimpleMoveFailWithoutExpression()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 2);
            });
            const int playerId = 1;
            Cmd cmd;
            ExpressionInfo notUsed;
            PrepareTestData1(playerId, 10, 1,
                out cmd,
                out notUsed);
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, TaskMovementFailed);
        }

        [Test]
        public void ConvertToBombTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.Convert, (int)KnownVoxelTypes.Bomb), ConvertTaskCompleted);
        }

        [Test]
        public void ConvertToWallTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.Convert, (int)KnownVoxelTypes.Ground), ConvertTaskCompleted);
        }

        [Test]
        public void ConvertToSpawnerTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.Convert, (int)KnownVoxelTypes.Spawner), ConvertTaskCompleted);
        }

        [Test]
        public void GrowTaskTest()
        {
            ExecuteGenericTaskTest(CmdCode.Grow, GrowTaskCompleted);
        }

        //[Test]
        //public void DiminishTaskTest()
        //{
        //    ExecuteGenericTaskTest(CmdCode.Diminish);
        //}

        //[Test]
        //public void SplitTaskTest()
        //{
        //    ExecuteGenericTaskTest(CmdCode.Split);
        //}

        //[Test]
        //public void Split4TaskTest()
        //{
        //    ExecuteGenericTaskTest(CmdCode.Split4);
        //}

        private void ExecuteGenericTaskTest(int cmdCode, TaskEngineEvent taskStateChangeEventHandler)
        {
            ExecuteTaskTest(() =>
            {
                const int playerId = 1;
                return PrepareTestData2(playerId, cmdCode, 0);
            }, taskStateChangeEventHandler);
        }

        private void ExecuteTaskTest(Func<Cmd> runTestCallback, TaskEngineEvent taskStateChangeEventHandler)
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 2);
            });
            Cmd cmd = runTestCallback();
            const int playerId = 1;
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, taskStateChangeEventHandler);
        }

        protected void TaskMovementCompleted(TaskInfo taskInfo)
        {
            m_engine.TaskEngine.TaskStateChanged -= TaskMovementCompleted;

            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);

            MovementCmd cmd = (MovementCmd)taskInfo.Cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, 1);
            Assert.AreEqual(cmd.Coordinates[0], coords[0]);

            Assert.Pass();
        }

        protected void TaskMovementFailed(TaskInfo taskInfo)
        {
            m_engine.TaskEngine.TaskStateChanged -= TaskMovementFailed;

            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Failed, taskInfo.State);

            MovementCmd cmd = (MovementCmd)taskInfo.Cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, 1);
            Assert.AreNotEqual(cmd.Coordinates[0], coords[0]);

            Assert.Pass();
        }

        protected void ConvertTaskCompleted(TaskInfo taskInfo)
        {
            m_engine.TaskEngine.TaskStateChanged -= ConvertTaskCompleted;

            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);

            ChangeParamsCmd cmd = (ChangeParamsCmd)taskInfo.Cmd;
            Coordinate[] coords = m_map.FindDataOfType(cmd.IntParams[0], 1);
            VoxelData data = m_map.Get(coords[0]);
            Assert.IsNotNull(data);
            Assert.AreEqual(data.Type, cmd.IntParams[0]);

            Assert.Pass();
        }

        protected void GrowTaskCompleted(TaskInfo taskInfo)
        {
            m_engine.TaskEngine.TaskStateChanged -= GrowTaskCompleted;

            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);
           
            IMatchUnitController controller = m_engine.GetUnitController(1, taskInfo.Cmd.UnitIndex); 
            Assert.IsNotNull(controller);
            Assert.AreEqual(controller.Data.Weight, 3);
            
            Assert.Pass();
        }

        protected void FinializeTest(int playerIndex, TaskInfo task, TaskEngineEvent eventHandler)
        {
            m_engine.Submit(playerIndex, new TaskCmd(task));
            m_engine.TaskEngine.TaskStateChanged += eventHandler;
            RunEngine();
            Assert.Fail();
        }
    }

}
