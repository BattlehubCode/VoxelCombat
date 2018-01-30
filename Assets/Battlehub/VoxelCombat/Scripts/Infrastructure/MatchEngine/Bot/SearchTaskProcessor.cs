using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IBotTaskProcessor
    {
        void Process(BotTask task, Action<BotTask> cancelledCallback, Action<BotTask> processedCallback);

        /// <summary>
        /// return true is task completed
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        bool IsCompleted(BotTask task);

        bool ChangeStage(BotTask task);

        Cmd CreateCommand(BotTask task);

        /// <summary>
        /// return true if task could not be completed (for example voxel stuck or something like that)
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        bool ShouldBeCancelled(BotTask task);
    }


    public abstract class SearchTaskProcessor : IBotTaskProcessor
    {
        protected readonly ITaskRunner m_taskRunner;
        protected readonly IPathFinder m_pathFinder;
        protected readonly IMatchView m_matchView;
        protected readonly MapRoot m_map;

        public SearchTaskProcessor(IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner)
        {
            m_map = matchView.Map;
            m_matchView = matchView;
            m_pathFinder = pathFinder;
            m_taskRunner = taskRunner;
        }

        public virtual bool ChangeStage(BotTask task)
        {
            bool stageChanged = task.PrevStage != task.Stage;

            task.PrevStage = task.Stage;

            return stageChanged;
        }

        public virtual bool IsCompleted(BotTask task)
        {
            if (task.TargetCoordinate.MapPos == task.Unit.DataController.Coordinate.MapPos)
            {
                return true;
            }

            return false;
        }

        public virtual Cmd CreateCommand(BotTask task)
        {
            return new MovementCmd(CmdCode.Move)
            {
                UnitIndex = task.Unit.Id,
                Coordinates = new[] { task.TargetCoordinate }
            };
        }

        public virtual bool ShouldBeCancelled(BotTask task)
        {
            if (task.Unit.IsDead)
            {
                return true;
            }

            if (!task.TargetData.IsAlive)
            {
                return true;
            }

            if (task.PrevPos != task.Unit.Position)
            {
                task.IdleIteration = 0;
                task.PrevPos = task.Unit.Position;
            }
            else
            {
                task.IdleIteration++;
            }

            if (task.IdleIteration > task.MaxIdleIterations)
            {
                return true;
            }

            return false;
        }

        public abstract void Process(BotTask task, Action<BotTask> cancelledCallback, Action<BotTask> processedCallback);

        protected class SearchContext
        {
            public int DeltaRow;
            public int DeltaCol;
            public int Radius;

            public HashSet<long>[] AlreadyChosenUnitsOrAssets;
            public int ClosestPlayerIndex;
            public long ClosestUnitOrAssetId;
            
            public readonly MapPos Position;

            public readonly int Weight;
            public readonly int MapSize;

            public readonly int MaxRadius;
            public readonly bool FindAll;

            public readonly List<VoxelData> AllFoundData = new List<VoxelData>();
            public readonly List<Coordinate> AllFoundCoordinates = new List<Coordinate>();

            public readonly Func<BotTask, VoxelData, bool> IsSuitableDataCallback;
            public readonly Action<BotTask, MapCell, List<VoxelData>> GetSuitableDataCallback;
            public readonly BotTask Task;
            public readonly Action<BotTask> FailedCallback;
            public readonly Action<BotTask> ProcessedCallback;


            public MapPos PrevPos = new MapPos(-1, -1);
            public int MaxIdleIterations = 10;
            public int IdleIteration;
            public int Stage;
            public int PrevStage;
            public readonly List<VoxelData> SuitableData = new List<VoxelData>();
            public MapCell SuitableCell;

            public SearchContext(
                BotTask task,                
                int weight,
                int mapSize,
                MapPos position,
                int maxRadius,
                bool findAll,
                Func<BotTask, VoxelData, bool> isSuitableDataCallback,
                Action<BotTask, MapCell, List<VoxelData>> getSuitableDataCallback,
                Action<BotTask> failedCallback,
                Action<BotTask> processedCallback)
            {
                Task = task;
                FailedCallback = failedCallback;
                ProcessedCallback = processedCallback;
                Position = position;
                IsSuitableDataCallback = isSuitableDataCallback;
                GetSuitableDataCallback = getSuitableDataCallback;
                MaxRadius = maxRadius;
                FindAll = findAll;
                DeltaRow = -1;
                DeltaCol = -1;
                Radius = 1;
                Weight = weight;
                MapSize = mapSize;
            }
        }

        protected virtual void Search(SearchContext ctx)
        {
            GetSuitableData(ctx);
            ContinueSearch(ctx);
        }

        protected void ContinueSearch(SearchContext ctx)
        {
            if (ctx.Task.SuitableCell != null && ctx.Task.SuitableData.Count > 0)
            {
                ctx.Task.SuitableData.Reverse();
                FindPathToSuitableData(ctx, ctx.Task.SuitableCell.GetPosition());
            }
            else
            {

                if (m_taskRunner.IsRunning(ctx.Task.Unit.Id, ctx.Task.Unit.Data.Owner))
                {
                    UnityEngine.Debug.LogError("m_taskRunner Already running");
                }

                m_taskRunner.Run(ctx.Task.Unit.Id, ctx.Task.Unit.Data.Owner, ctx,
                    FindSuitableData,
                    FindSuitableDataCompleted,
                    (unitIndex, context) =>
                    {
                        UnityEngine.Debug.Log("FindSuitableData terminated");
                    });

            }
        }

        private void FindPathToSuitableData(SearchContext ctx, MapPos toPos)
        {
            BotTask task = ctx.Task;
            VoxelData toData = task.SuitableData[task.SuitableData.Count - 1];

            Coordinate to = new Coordinate(toPos, toData);
            to.Weight = task.Unit.Data.Weight;

            task.SuitableData.RemoveAt(task.SuitableData.Count - 1);

            if(m_pathFinder.IsRunning(task.Unit.Id, task.Unit.Data.Owner))
            {
                UnityEngine.Debug.LogError("m_pathFinder Already running");
            }

            m_pathFinder.Find(task.Unit.Id, toData.UnitOrAssetIndex, task.Unit.DataController.Clone(), new[] { task.Unit.DataController.Coordinate, to }, (unitIndex, path) =>
            {
                if (path[path.Length - 1].MapPos == to.MapPos)
                {
                    //path found
                    if (ctx.FindAll)
                    {
                        ctx.AllFoundData.Add(toData);
                        ctx.AllFoundCoordinates.Add(to);

                        if (task.SuitableData.Count > 0)
                        {
                            FindPathToSuitableData(ctx, toPos);
                        }
                        else
                        {
                            NextSearchIteration(ctx);
                        }
                    }
                    else
                    {
                        OnPathFound(ctx, to);
                    }
                }
                else
                {
                    //path was not found
                    if (task.SuitableData.Count > 0)
                    {
                        FindPathToSuitableData(ctx, toPos);
                    }
                    else
                    {
                        NextSearchIteration(ctx);
                    }
                }
            }, 
            unitIndex =>
            {
                UnityEngine.Debug.Log("FindPathToSuitableData terminated");
            });
        }

        protected virtual void OnPathFound(SearchContext ctx,  Coordinate to)
        {
            BotTask task = ctx.Task;

            task.SuitableData.Clear();
            UnityEngine.Debug.Assert(task.SuitableCell != null);
            ctx.GetSuitableDataCallback(ctx.Task, task.SuitableCell, task.SuitableData);
            if (task.SuitableData.Count > 0)
            {
                task.TargetDataArray = new[] { task.SuitableData[0] };
                task.SuitableData.Clear();

                task.TargetCoordinates = new[] { to };
                ctx.ProcessedCallback(task);
            }
            else
            {
                NextSearchIteration(ctx);
            }
        }

        protected virtual bool NextIteration(SearchContext ctx)
        {
            ctx.DeltaCol++;

            if (ctx.DeltaRow == ctx.Radius && ctx.DeltaCol == ctx.Radius + 1)
            {
                ctx.Radius++;
                if (ctx.Radius >= ctx.MaxRadius)
                {
                    return false;
                }

                ctx.DeltaRow = -ctx.Radius;
                ctx.DeltaCol = -ctx.Radius;
            }

            else if (ctx.DeltaCol == ctx.Radius + 1)
            {
                ctx.DeltaRow++;
                ctx.DeltaCol = -ctx.Radius;
            }

            return true;
        }

        protected void NextSearchIteration(SearchContext ctx)
        {
            if(!NextIteration(ctx))
            {
                HandleLastIteration(ctx);
                return;
            }

            ContinueSearch(ctx);
        }


        protected virtual void GetSuitableData(SearchContext ctx)
        {
            int row = ctx.Position.Row + ctx.DeltaRow;
            int col = ctx.Position.Col + ctx.DeltaCol;

            ctx.Task.SuitableCell = null;
            ctx.Task.SuitableData.Clear();

            if (row >= 0 && col >= 0 && row < ctx.MapSize && col < ctx.MapSize)
            {
                if (ctx.DeltaRow != 0 || ctx.DeltaCol != 0)
                {
                    ctx.Task.SuitableCell = m_map.Get(row, col, ctx.Weight);

                    UnityEngine.Debug.Assert(ctx.Task.SuitableCell != null);
                    ctx.GetSuitableDataCallback(ctx.Task, ctx.Task.SuitableCell, ctx.Task.SuitableData);
                }
            }
        }

        private object FindSuitableData(long unitIndex, object context)
        {
            SearchContext ctx = (SearchContext)context;
            if (!NextIteration(ctx))
            {
                return false;
            }

            GetSuitableData(ctx);
            if (ctx.Task.SuitableCell == null || ctx.Task.SuitableData.Count == 0)
            {
                return null;
            }

            return true;
        }

        private void FindSuitableDataCompleted(long unitIndex, object context, object result)
        {
            SearchContext ctx = (SearchContext)context;
            bool found = (bool)result;
            if (found)
            {
                ContinueSearch(ctx);
            }
            else
            {
                HandleLastIteration(ctx);
            }
        }

      
        private static void HandleLastIteration(SearchContext ctx)
        {
            if (ctx.FindAll)
            {
                if (ctx.AllFoundData.Count > 0)
                {
                    for (int i = ctx.AllFoundData.Count - 1; i >= 0; i--)
                    {
                        if (!ctx.IsSuitableDataCallback(ctx.Task, ctx.AllFoundData[i]))
                        {
                            ctx.AllFoundData.RemoveAt(i);
                            ctx.AllFoundCoordinates.RemoveAt(i);
                        }
                    }

                    if (ctx.AllFoundData.Count > 0)
                    {
                        ctx.Task.TargetDataArray = ctx.AllFoundData.ToArray();
                        ctx.Task.TargetCoordinates = ctx.AllFoundCoordinates.ToArray();
                        ctx.ProcessedCallback(ctx.Task);
                    }
                    else
                    {
                        ctx.FailedCallback(ctx.Task);
                    }
                }
                else
                {
                    ctx.FailedCallback(ctx.Task);
                }
            }
            else
            {
                ctx.FailedCallback(ctx.Task);
            }
        }
    }
}
