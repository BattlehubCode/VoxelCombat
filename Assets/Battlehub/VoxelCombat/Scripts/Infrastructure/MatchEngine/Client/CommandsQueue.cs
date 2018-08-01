using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    //Maximum Ping is probably will be equal to something like 500ms or 500 ms / 50 ms = 10 ticks
    //When client time will be equal to 0 it is assumed server time will be something between (0 - 500] (but it can be much higher if lag occured)

    public class CommandsQueue 
    {
        private long m_tick;
        public long CurrentTick
        {
            get { return m_tick; }
        }

        private readonly long m_maxPing;
        private readonly Queue<CommandsBundle> m_commands;

        public CommandsQueue(long maxPing)
        {
            m_maxPing = maxPing;
            m_commands = new Queue<CommandsBundle>();
        }

        public void Enqueue(CommandsBundle command)
        {
            long maxExpectedSrvTick = m_tick + m_maxPing;
            if(maxExpectedSrvTick < command.Tick)
            {
                m_tick = command.Tick - m_maxPing;
            }
            m_commands.Enqueue(command);
        }

        /// <summary>
        /// This method will return command which must be executed. Run it while returned command is not null
        /// </summary>
        /// <returns></returns>
        public CommandsBundle Tick(out long tick)
        {
            tick = m_tick;

            CommandsBundle cmd = null;
            if (m_commands.Count > 0)
            {
                CommandsBundle nextCmd = m_commands.Peek();
                if(m_tick > nextCmd.Tick)
                {
                    cmd = m_commands.Dequeue();
                }
                else
                {
                    if (m_tick == nextCmd.Tick)
                    {
                        cmd = m_commands.Dequeue();
                    }
                    m_tick++;
                }
            }
            else
            {
                m_tick++;
            }
            
            return cmd;
        }
    }

}
