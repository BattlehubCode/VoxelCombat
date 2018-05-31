using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    /*
    public abstract class ChooseTaskProcessor : SearchTaskProcessor
    {
        public ChooseTaskProcessor(IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner) : base(matchView, pathFinder, taskRunner)
        {
        }


        protected override void Search(SearchContext ctx)
        {
            if(ctx.FindAll)
            {
                throw new InvalidOperationException("ctx.FindAll should be equal to false when using ChooseTaskProcessor");
            }

            ctx.AlreadyChosenUnitsOrAssets = new HashSet<long>[m_matchView.PlayersCount];
            for(int i = 0; i < m_matchView.PlayersCount; ++i)
            {
                ctx.AlreadyChosenUnitsOrAssets[i] = new HashSet<long>();
            }

            GetSuitableData(ctx);
            ContinueSearch(ctx);
        }

        protected override bool NextIteration(SearchContext ctx)
        {
            int minDistance = int.MaxValue;
            ctx.ClosestPlayerIndex = -1;
            ctx.ClosestUnitOrAssetId = -1;
            int exceptPlayerIndex = ctx.Task.Unit.Data.Owner;
            for(int p = 0; p < m_matchView.PlayersCount; ++p)
            {
                if(p == exceptPlayerIndex)
                {
                    continue;
                }

                IMatchPlayerView player = m_matchView.GetPlayerView(p);
                if(player.ControllableUnitsCount == 0)
                {
                    continue;
                }

                for(int u = 0; u < player.Units.Length; ++u)
                {
                    TryChooseClosestUnit(player, u, p, ctx, ref minDistance);
                }

                foreach(IMatchUnitAssetView target in player.Assets)
                {
                    if (ctx.AlreadyChosenUnitsOrAssets[p].Contains(target.Id))
                    {
                        continue;
                    }

                    if (target.IsDead)
                    {
                        continue;
                    }

                    int distance = ctx.Task.Unit.Position.SqDistanceTo(target.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        ctx.ClosestPlayerIndex = p;
                        ctx.ClosestUnitOrAssetId = target.Id;
                    }
                }
            }

            if(ctx.ClosestUnitOrAssetId >= 0)
            {
                ctx.AlreadyChosenUnitsOrAssets[ctx.ClosestPlayerIndex].Add(ctx.ClosestUnitOrAssetId);
                return true;
            }

            return false;
        }

        private void TryChooseClosestUnit(IMatchPlayerView player, int u, int p, SearchContext ctx, ref int minDistance)
        {
            IMatchUnitAssetView target = player.Units[u];
            if (ctx.AlreadyChosenUnitsOrAssets[p].Contains(target.Id))
            {
                return;
            }

            if (target.IsDead)
            {
                return;
            }

            int distance = ctx.Task.Unit.Position.SqDistanceTo(target.Position);
            if (distance < minDistance)
            {
                minDistance = distance;
                ctx.ClosestPlayerIndex = p;
                ctx.ClosestUnitOrAssetId = target.Id;
            }
        }


        protected override void GetSuitableData(SearchContext ctx)
        {
            ctx.Task.SuitableCell = null;
            ctx.Task.SuitableData.Clear();
            ctx.Task.TargetUnitOrAsset = null;

            IMatchPlayerView player = m_matchView.GetPlayerView(ctx.ClosestPlayerIndex);
            IMatchUnitAssetView unitOrAsset = player.GetUnitOrAsset(ctx.ClosestUnitOrAssetId);

            if(unitOrAsset != null && !unitOrAsset.IsDead)
            {
                ctx.Task.TargetUnitOrAsset = unitOrAsset;

                MapPos position = unitOrAsset.Position;
                int weight = unitOrAsset.Data.Weight;

                Coordinate coord = new Coordinate(position, weight, 0);
                coord = coord.ToWeight(ctx.Task.Unit.Data.Weight);
                
                ctx.Task.SuitableCell = m_map.Get(coord.Row, coord.Col, coord.Weight);
                ctx.GetSuitableDataCallback(ctx.Task, ctx.Task.SuitableCell, ctx.Task.SuitableData);
                if (ctx.Task.SuitableData.Count == 0 || ctx.Task.SuitableData[0] != ctx.Task.TargetUnitOrAsset.Data)
                {
                    ctx.Task.TargetUnitOrAsset = null;
                    ctx.Task.SuitableCell = null;
                    ctx.Task.SuitableData.Clear();
                }
            }
        }

        protected override void OnPathFound(SearchContext ctx, Coordinate to)
        {
            if(ctx.Task.TargetUnitOrAsset != null)
            {
                ctx.Task.TargetCoordinates = new[] { to };
                ctx.Task.TargetDataArray = new[] { ctx.Task.TargetUnitOrAsset.Data };

                ctx.ProcessedCallback(ctx.Task);
            }
            else
            {
                NextSearchIteration(ctx);
            }
        }
    }
    */
}