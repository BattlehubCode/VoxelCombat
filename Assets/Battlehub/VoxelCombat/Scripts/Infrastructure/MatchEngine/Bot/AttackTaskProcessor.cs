using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public class AttackTaskProcessor : ChooseTaskProcessor
    {
        public AttackTaskProcessor(IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner) : base(matchView, pathFinder, taskRunner)
        {
        }

        public override void Process(BotTask task, Action<BotTask> failedCallback, Action<BotTask> processedCallback)
        {
            VoxelData data = task.Unit.Data;
            Search(new SearchContext(task, data.Weight, 0, task.Unit.Position, 0, false, IsAttackable, GetAttackable, failedCallback, processedCallback));
        }

        public override bool IsCompleted(BotTask task)
        {
            if (task.TargetUnitOrAsset == null || task.TargetUnitOrAsset.IsDead)
            {
                return true;
            }

            return false;
        }

        private bool IsAttackable(BotTask task, VoxelData data)
        {
            VoxelData destroyer = task.Unit.Data;
            return
                task.TargetUnitOrAsset != null &&
               !task.TargetUnitOrAsset.IsDead &&

                (task.TargetUnitOrAsset.Data.IsExplodableBy(destroyer.Type, destroyer.Weight) ||
                 task.TargetUnitOrAsset.Data.IsAttackableBy(destroyer));
        }

        private void GetAttackable(BotTask task, MapCell cell, List<VoxelData> result)
        {
            VoxelData destroyer = task.Unit.Data;
            if(task.TargetUnitOrAsset == null)
            {
                return;
            }

            if(task.TargetUnitOrAsset.IsDead)
            {
                return;
            }

            if(task.TargetUnitOrAsset.Data.IsExplodableBy(destroyer.Type, destroyer.Weight))
            {
                result.Add(task.TargetUnitOrAsset.Data);
            }

            else if(task.TargetUnitOrAsset.Data.IsAttackableBy(destroyer))
            {
                result.Add(task.TargetUnitOrAsset.Data);
            }  
        }

        public override Cmd CreateCommand(BotTask task)
        {
            UnityEngine.Debug.Assert(task.TargetData.Owner > -1);

            return new MovementCmd(CmdCode.MoveConditional)
            {
                UnitIndex = task.Unit.Id,
                Coordinates = new[] { task.TargetCoordinate },
                HasTarget = true,
                TargetIndex = task.TargetData.UnitOrAssetIndex,
                TargetPlayerIndex = task.TargetData.Owner
            };
        }

    }

}
