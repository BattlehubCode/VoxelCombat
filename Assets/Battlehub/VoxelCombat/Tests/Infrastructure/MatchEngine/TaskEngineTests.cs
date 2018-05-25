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
        private int m_ticksCount = 0;
        protected IMatchEngine m_engine;
        protected IReplaySystem m_replay;
        protected MapRoot m_map;
        private float m_prevTickTime;
        private long m_tick;


        protected const int MAX_TICKS = 1000;
        //4 Players, Depth 6, Flat square, Size 4x4, Cell weight 4
        protected readonly string TestEnv0 = "021ef2f8-789c-44ff-b59b-0f43064c581b.data";

        protected Guid[] m_players;
        protected Dictionary<int, VoxelAbilities[]> m_abilities = new Dictionary<int, VoxelAbilities[]>();


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
            m_players = new Guid[playersCount];
            for(int i = 0; i < m_players.Length; ++i)
            {
                m_players[i] = Guid.NewGuid();
                m_abilities.Add(i, CreateTemporaryAbilies());
            }

            string dataPath = Application.streamingAssetsPath + "/Maps/";

            string filePath = dataPath + mapName;

            if (m_replay == null)
            {
                m_replay = MatchFactory.CreateReplayRecorder();
            }

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
                m_replay.RegisterPlayer(m_players[i], i);
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
                m_engine.TaskEngine.Update();

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
        [UnityTest]
        public IEnumerator TestEnvInitTest()
        {
            Assert.DoesNotThrow(() =>
            {
                BeginTest(TestEnv0, 2);
            });

            Coordinate[] coords = m_map.FindUnits((int)KnownVoxelTypes.Eater, 1);
            Assert.AreEqual(coords.Length, 1);

            VoxelData unit = m_map.Get(coords[0]);
            TaskInfo task = new TaskInfo(
                TaskType.Command,
                new MovementCmd(CmdCode.MoveConditional, unit.UnitOrAssetIndex, 0)
                {
                    Coordinates = new[] { coords[0].Add(1, 1) },
                });

            m_engine.Submit(m_players[1], new TaskCmd(task));
            m_engine.TaskEngine.TaskStateChanged += TestEnvInitTest_OnTaskStateChanged;

            RunEngine();
            yield return null;

            Assert.Fail();
        }

        protected void TestEnvInitTest_OnTaskStateChanged(TaskInfo taskInfo)
        {
            m_engine.TaskEngine.TaskStateChanged -= TestEnvInitTest_OnTaskStateChanged;

            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });

            if(taskInfo.State == TaskState.Completed)
            {
                MovementCmd cmd = (MovementCmd)taskInfo.Cmd;
                Coordinate[] coords = m_map.FindUnits((int)KnownVoxelTypes.Eater, 1);
                Assert.AreEqual(coords[0], cmd.Coordinates[0]);
            }

            Assert.Pass();
        }
    }

}
