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
        //4 Players, Depth 6, Flat square, Size 4x4, Cell weight 4 (Map name test_env_0 4 players)
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
                m_engine.Update();
                
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
            Assert.AreEqual(coords.Length, 2);

            VoxelData unit = m_map.Get(coords[0]);
            Coordinate targetCoordinate = coords[0].Add(offsetX, offsetY);

            cmd = new MovementCmd(CmdCode.Move, unit.UnitOrAssetIndex, 0)
            {
                Coordinates = new[] { targetCoordinate },
            };
            expression = ExpressionInfo.MoveTaskExpression(unit.UnitOrAssetIndex, playerId, targetCoordinate);
        }

        private Cmd PrepareTestData2(int playerId, int cmdCode, int param)
        {
            Cmd cmd;
            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);

            if(playerId == 1)
            {
                Assert.AreEqual(coords.Length, 2);
            }
            else
            {
                Assert.AreEqual(coords.Length, 1);
            }
    

            VoxelData unit = m_map.Get(coords[0]);

            switch (cmdCode)
            {
                case CmdCode.Convert:
                {
                    cmd = new ChangeParamsCmd(CmdCode.Convert)
                    {
                        UnitIndex = unit.UnitOrAssetIndex,
                        IntParams = new int[] { param }
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
                case CmdCode.SetHealth:
                {
                    cmd = new ChangeParamsCmd(CmdCode.SetHealth)
                    {
                        UnitIndex = unit.UnitOrAssetIndex,
                        IntParams = new int[] { param }
                    };

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
                BeginTest(TestEnv0, 4);
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
                BeginTest(TestEnv0, 4);
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
                BeginTest(TestEnv0, 4);
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
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(setHealthTaskInfo.State, TaskState.Completed);
                ExecuteGenericTaskTest(CmdCode.Grow, GrowTaskCompleted, false);
            });

        }

        [Test]
        public void DiminishTaskTest()
        {
            const int playerId = 1;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(TaskState.Completed, setHealthTaskInfo.State);
                ExecuteGenericTaskTest(CmdCode.Grow, growTaskInfo =>
                {
                    Assert.AreEqual(TaskState.Completed, growTaskInfo.State);
                    ExecuteGenericTaskTest(CmdCode.Diminish, DiminishTaskCompleted, false);
                },
                false);
            });

        }

        [Test]
        public void SplitTaskTest()
        {
            const int playerId = 3;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(TaskState.Completed, setHealthTaskInfo.State);
                ExecuteGenericTaskTest(CmdCode.Split, SplitTaskCompleted, false, playerId);
            },
            true, playerId);
        }

        [Test]
        public void Split4TaskTest()
        {
            const int playerId = 3;
            ExecuteTaskTest(() => PrepareTestData2(playerId, CmdCode.SetHealth, 64), setHealthTaskInfo =>
            {
                Assert.AreEqual(TaskState.Completed, setHealthTaskInfo.State);
                ExecuteGenericTaskTest(CmdCode.Grow, growTaskInfo =>
                {
                    ExecuteGenericTaskTest(CmdCode.Split4, Split4TaskCompleted, false, playerId);
                }, 
                false, playerId);
            }, 
            true, playerId);
        }
        

        private void ExecuteGenericTaskTest(int cmdCode, TaskEngineEvent<TaskInfo> taskStateChangeEventHandler, bool begin = true, int playerId = 1)
        {
            ExecuteTaskTest(() =>
            {
                return PrepareTestData2(playerId, cmdCode, 0);
            }, taskStateChangeEventHandler, begin, playerId);
        }

        private void ExecuteTaskTest(Func<Cmd> runTestCallback,  TaskEngineEvent<TaskInfo> taskStateChangeEventHandler, bool begin = true, int playerId = 1)
        {
            if(begin)
            {
                Assert.DoesNotThrow(() =>
                {
                    BeginTest(TestEnv0, 4);
                });
            }
           
            Cmd cmd = runTestCallback();
            TaskInfo task = new TaskInfo(cmd);
            FinializeTest(playerId, task, taskStateChangeEventHandler);
        }

        protected void TaskMovementCompleted(TaskInfo taskInfo)
        {
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

        protected void DiminishTaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);

            IMatchUnitController controller = m_engine.GetUnitController(1, taskInfo.Cmd.UnitIndex);
            Assert.IsNotNull(controller);
            Assert.AreEqual(controller.Data.Weight, 2);

            Assert.Pass();
        }

        protected void Split4TaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);

            const int playerId = 3;
            IMatchUnitController controller = m_engine.GetUnitController(playerId, taskInfo.Cmd.UnitIndex);
            Assert.IsNull(controller);

            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(4, coords.Length);
            for(int i = 0; i < coords.Length; ++i)
            {
                Assert.AreEqual(2, coords[i].Weight);

                VoxelData data = m_map.Get(coords[i]);
                Assert.IsNotNull(data);
                Assert.AreEqual((int)KnownVoxelTypes.Eater, data.Type); 
            }

            Assert.AreEqual(1, coords[0].MapPos.SqDistanceTo(coords[1].MapPos));
            Assert.AreEqual(1, coords[2].MapPos.SqDistanceTo(coords[3].MapPos));
            Assert.AreEqual(2, coords[1].MapPos.SqDistanceTo(coords[2].MapPos));
            Assert.AreEqual(2, coords[0].MapPos.SqDistanceTo(coords[3].MapPos));

            Assert.Pass();
        }

        protected void SplitTaskCompleted(TaskInfo taskInfo)
        {
            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            Assert.AreEqual(TaskState.Completed, taskInfo.State);

            const int playerId = 3;
            IMatchUnitController controller = m_engine.GetUnitController(playerId, taskInfo.Cmd.UnitIndex);
            Assert.IsNull(controller);

            Coordinate[] coords = m_map.FindDataOfType((int)KnownVoxelTypes.Eater, playerId);
            Assert.AreEqual(2, coords.Length);
            for (int i = 0; i < coords.Length; ++i)
            {
                Assert.AreEqual(2, coords[i].Weight);

                VoxelData data = m_map.Get(coords[i]);
                Assert.IsNotNull(data);
                Assert.AreEqual((int)KnownVoxelTypes.Eater, data.Type);
            }

            Assert.AreEqual(1, coords[0].MapPos.SqDistanceTo(coords[1].MapPos));
            Assert.Pass();
        }

        protected void FinializeTest(int playerIndex, TaskInfo task, TaskEngineEvent<TaskInfo> callback)
        {
            m_engine.Submit(playerIndex, new TaskCmd(task));

            TaskEngineEvent<TaskInfo> taskStateChangedEventHandler = null;
            taskStateChangedEventHandler = taskInfo =>
            {
                m_engine.GetTaskEngine(playerIndex).TaskStateChanged -= taskStateChangedEventHandler;
                callback(task);
            };
            m_engine.GetTaskEngine(playerIndex).TaskStateChanged += taskStateChangedEventHandler;

            RunEngine();
            Assert.Fail();
        }

        
    }

}
