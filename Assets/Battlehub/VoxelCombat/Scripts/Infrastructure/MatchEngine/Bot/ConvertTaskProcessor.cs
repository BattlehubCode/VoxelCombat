using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class ConvertTaskProcessor : SearchTaskProcessor
    {
        public ConvertTaskProcessor(IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner) : base(matchView, pathFinder, taskRunner)
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
            if (task.Stage == 0)
            {
                return false;
            }

            if (task.TargetCoordinate.MapPos == task.Unit.DataController.Coordinate.MapPos)
            {
                if (task.TargetData != null && task.TargetData.IsAlive && IsBaseForConvertion(task, task.TargetData))
                {
                    return true;
                }
            }

            return false;
        }

        public override Cmd CreateCommand(BotTask task)
        {
            if (task.Stage == 0)
            {
                return new MovementCmd(CmdCode.Move)
                {
                    UnitIndex = task.Unit.Id,
                    Coordinates = new[] { task.TargetCoordinate }
                };
            }
            else
            {
                return new ChangeParamsCmd(CmdCode.Convert)
                {
                    UnitIndex = task.Unit.Id,
                    IntParams = new[] { task.TaskType == BotTaskType.ConvertToBomb ? (int)KnownVoxelTypes.Bomb : (int)KnownVoxelTypes.Spawner }
                };
            }
        }

        public override void Process(BotTask task, Action<BotTask> failedCallback, Action<BotTask> processedCallback)
        {
            VoxelData data = task.Unit.Data;
            VoxelAbilities abilities = task.Unit.DataController.Abilities;

            int mapWidth = m_map.GetMapSizeWith(data.Weight);
            int searchRadius = 32;// mapWidth;

            MapPos basePosition;
            if(task.TaskType == BotTaskType.ConvertToBomb)
            {
                basePosition = task.Unit.Position;
            }
            else
            {
                basePosition = task.Unit.Position; //new Coordinate(task.Strategy.BaseCampPostion, GameConstants.MinVoxelActorWeight, 0).ToWeight(data.Weight).MapPos;
            }

            Search(new SearchContext(task, data.Weight, mapWidth, basePosition, searchRadius, false, IsBaseForConvertion, GetBaseForConvertion, failedCallback, processedCallback));
        }

        private bool IsBaseForConvertion(BotTask task, VoxelData data)
        {
            VoxelData convertable = task.Unit.Data;
            int type = task.TaskType == BotTaskType.ConvertToBomb ? (int)KnownVoxelTypes.Bomb : (int)KnownVoxelTypes.Spawner;
            return data.IsBaseFor(type, convertable.Weight)
                && data.Next == null
                && data.Type == (int)KnownVoxelTypes.Ground;
        }

        private void GetBaseForConvertion(BotTask task, MapCell cell, List<VoxelData> result)
        {
            VoxelData convertable = task.Unit.Data;
            int type = task.TaskType == BotTaskType.ConvertToBomb ? (int)KnownVoxelTypes.Bomb : (int)KnownVoxelTypes.Spawner;
            VoxelData target;
            VoxelData convertBase = cell.GetDefaultTargetFor(type, convertable.Weight, convertable.Owner, false, out target);
            if (convertBase != null && !cell.HasDescendantsWithVoxelData(data => data.Type == (int)KnownVoxelTypes.Spawner))
            {
                result.Add(convertBase);
            }
        }
    }

}
