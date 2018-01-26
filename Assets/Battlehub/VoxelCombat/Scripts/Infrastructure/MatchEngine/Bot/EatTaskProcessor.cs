using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public class EatTaskProcessor : SearchTaskProcessor
    {
      

        public EatTaskProcessor(IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner) :
            base(matchView, pathFinder, taskRunner)
        {
        }

        public override void Process(BotTask task, Action<BotTask> failedCallback, Action<BotTask> processedCallback)
        {
            VoxelData data = task.Unit.Data;
            VoxelAbilities abilities = task.Unit.DataController.Abilities;

            int mapWidth = m_map.GetMapSizeWith(data.Weight);
            int searchRadius = 32;// mapWidth;

            Search(new SearchContext(task, data.Weight, mapWidth, task.Unit.Position, searchRadius, false, IsEatable, GetEatable, failedCallback, processedCallback));
        }

        private bool IsEatable(BotTask task, VoxelData data)
        {
            VoxelData destroyer = task.Unit.Data;
            return IsEatable(data, destroyer);
        }

        private static bool IsEatable(VoxelData data, VoxelData destroyer)
        {
            if (data.IsAttackableBy(destroyer))
            {
                if (data.Type == (int)KnownVoxelTypes.Eatable)
                {
                    if (data.IsNeutral)
                    {
                        return true;
                    }

                    if (data.Owner == destroyer.Owner)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void GetEatable(BotTask task, MapCell cell, List<VoxelData> result)
        {
            VoxelData destroyer = task.Unit.Data;
            VoxelData eatable = cell.GetDescendantsWithVoxelData(data => IsEatable(data, destroyer));
            if (eatable != null)
            {
                result.Add(eatable);
            }
        }
    }

}