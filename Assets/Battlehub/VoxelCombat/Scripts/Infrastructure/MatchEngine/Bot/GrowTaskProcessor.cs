using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class GrowTaskProcessor : SearchTaskProcessor
    {
        public GrowTaskProcessor(IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner) :
            base(matchView, pathFinder, taskRunner)
        {
        }

        public override bool ChangeStage(BotTask task)
        {
            if (task.TargetCoordinate.MapPos == task.Unit.DataController.Coordinate.MapPos)
            {
                task.Stage = 1;
            }

            return base.ChangeStage(task);
        }

        public override bool IsCompleted(BotTask task)
        {
            if(task.Stage == 0)
            {
                return false;
            }

            if (task.TargetCoordinate.MapPos == task.Unit.DataController.Coordinate.MapPos)
            {
                if(task.TargetData != null && task.TargetData.IsAlive && IsBaseForGrowing(task, task.TargetData))
                {
                    return true;
                }
            }

            return false;
        }

        public override Cmd CreateCommand(BotTask task)
        {
            if(task.Stage == 0)
            {
                return new MovementCmd(CmdCode.Move)
                {
                    UnitIndex = task.Unit.Id,
                    Coordinates = new[] { task.TargetCoordinate }
                };
            }
            else
            {
                return new Cmd(CmdCode.Grow)
                {
                    UnitIndex = task.Unit.Id,
                };
            }
        }

        public override void Process(BotTask task, Action<BotTask> failedCallback, Action<BotTask> processedCallback)
        {
            VoxelData data = task.Unit.Data;
            VoxelAbilities abilities = task.Unit.DataController.Abilities;

            int mapWidth = m_map.GetMapSizeWith(data.Weight);
            int searchRadius = 32;// mapWidth;

            Search(new SearchContext(task, data.Weight, mapWidth, task.Unit.Position, searchRadius, false, IsBaseForGrowing, GetBaseForGrowing, failedCallback, processedCallback));
        }

        private bool IsBaseForGrowing(BotTask task, VoxelData data)
        {
            VoxelData grower = task.Unit.Data;
            return data.IsBaseFor(grower.Type, grower.Weight + 1)
                && !data.IsCollapsableBy(grower.Type, grower.Weight)
                && data.Next == null;
        }

        private void GetBaseForGrowing(BotTask task, MapCell cell, List<VoxelData> result)
        {
            VoxelData grower = task.Unit.Data;

            VoxelData target;
            VoxelData growBase = cell.GetDefaultTargetFor(grower.Type, grower.Weight + 1, grower.Owner, false, out target);
            if (growBase != null && growBase.Next == null)
            {
                result.Add(growBase);
            }
        }
    }
}
