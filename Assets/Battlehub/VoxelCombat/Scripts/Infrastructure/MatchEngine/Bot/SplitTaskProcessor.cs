using System;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class SplitTaskProcessor : IBotTaskProcessor
    {
        public bool ChangeStage(BotTask task)
        {
            return false;
        }

        public Cmd CreateCommand(BotTask task)
        {
            return new Cmd(CmdCode.Split, task.Unit.Id);
        }

        public bool IsCompleted(BotTask task)
        {
            return true;
        }

        public void Process(BotTask task, Action<BotTask> cancelledCallback, Action<BotTask> processedCallback)
        {
            processedCallback(task);
        }

        public bool ShouldBeCancelled(BotTask task)
        {
            return false;
        }
    }

}

