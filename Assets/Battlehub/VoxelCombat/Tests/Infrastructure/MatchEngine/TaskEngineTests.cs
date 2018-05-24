using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using System.Diagnostics;
using System.IO;

namespace Battlehub.VoxelCombat.Tests
{
    public class MatchEngineTestsBase
    {
        private int m_ticksCount = 0;
        protected IMatchEngine m_engine;
        private float m_prevTickTime;
        private long m_tick;

        //4 Players, Depth 6, Flat square, Size 4x4, Cell weight 4
        protected readonly string TestEnv0 = "021ef2f8-789c-44ff-b59b-0f43064c581b.data";

        protected void BeginTest(string mapName, int playersCount)
        {
            string dataPath = Application.streamingAssetsPath + "/Maps/";

            string filePath = dataPath + mapName;
           
            MapData mapData = ProtobufSerializer.Deserialize<MapData>(File.ReadAllBytes(filePath));
            MapRoot map = ProtobufSerializer.Deserialize<MapRoot>(mapData.Bytes);
            m_engine = MatchFactory.CreateMatchEngine(map, playersCount);
        }

        protected void RunEngine()
        {
            m_engine.PathFinder.Update();
            m_engine.TaskRunner.Update();
            m_engine.BotPathFinder.Update();
            m_engine.BotTaskRunner.Update();
            m_engine.TaskEngine.Update();
            //Stopwatch sw = new Stopwatch();
            while ((Time.realtimeSinceStartup - m_prevTickTime) >= GameConstants.MatchEngineTick)
            {
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

            yield return null;

            Assert.DoesNotThrow(() =>
            {
                EndTest();
            });
        }
    }

}
