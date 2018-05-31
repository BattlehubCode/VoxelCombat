using ProtoBuf;
using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IReplaySystem
    {
        void Load(ReplayData replay);

        ReplayData Save();        

        void Record(int playerIndex, Cmd cmd, long tick);

        void Tick(IMatchEngine engine, long tick);
    }

    [ProtoContract]
    public class ReplayInfo
    {
        [ProtoMember(1)]
        public Guid Id;

        [ProtoMember(2)]
        public Guid MapId;

        [ProtoMember(3)]
        public string[] PlayerNames;

        [ProtoMember(4)]
        public string Name;

        [ProtoMember(6)]
        public long DateTime;
    }

    [ProtoContract]
    public class ReplayData
    {
        [ProtoMember(1)]
        public Guid Id;

        [ProtoMember(2)]
        public long[] Ticks;

        [ProtoMember(3)]
        public int[] Players;

        [ProtoMember(4)]
        public Cmd[] Commands;
    }

    public class ReplayRecorder : ReplaySystem
    {
        public override void Tick(IMatchEngine engine, long tick)
        {
            
        }
    }

    public class ReplayPlayer : ReplaySystem
    {
        public override void Record(int playerId, Cmd cmd, long tick)
        {
            
        }
    }

    public class ReplaySystem : IReplaySystem
    {
        private Queue<long> m_ticks = new Queue<long>();
        private Queue<int> m_playerIndices = new Queue<int>();
        private Queue<Cmd> m_commands = new Queue<Cmd>();
       
        public virtual void Load(ReplayData replay)
        {
            m_ticks = new Queue<long>(replay.Ticks);
            m_commands = new Queue<Cmd>(replay.Commands);
            m_playerIndices = new Queue<int>(replay.Players);
        }

        public virtual ReplayData Save()
        {
            return new ReplayData
            {
                Ticks = m_ticks.ToArray(),
                Commands = m_commands.ToArray(),
                Players = m_playerIndices.ToArray()
            };
        }

        public virtual void Record(int playerIndex, Cmd cmd, long tick)
        {
            m_playerIndices.Enqueue(playerIndex);
            m_ticks.Enqueue(tick);
            m_commands.Enqueue(cmd);
        }

        public virtual void Tick(IMatchEngine engine, long tick)
        {
            while (m_ticks.Count > 0 && m_ticks.Peek() == tick)
            {
                Cmd cmd = m_commands.Dequeue();
                int playerIndex = m_playerIndices.Dequeue();
                m_ticks.Dequeue();
                engine.Submit(playerIndex, cmd);
            }
        }
    }

}
