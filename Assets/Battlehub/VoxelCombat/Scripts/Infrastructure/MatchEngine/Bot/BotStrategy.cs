using System;
using System.Collections;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IBotStrategy
    {
        MapPos BaseCampPostion
        {
            get;
        }

        CompositeCmd Think();

        void Destroy();
    }

    [Flags]
    public enum BotTaskType
    {
        None = 0,
        Eat = 1 << 0,
        Attack = 1 << 1,
        ConvertToSpawner = 1 << 2,
        ConvertToBomb = 1 << 3,
        Grow = 1 << 4,
        Split = 1 << 5,
        Split4 = 1 << 6,
        All = Eat | Attack | ConvertToSpawner | ConvertToBomb | Grow | Split | Split4
    }

    public class BotTask
    {
        public IMatchUnitAssetView Unit;
        public BotTaskType TaskType;

        public Coordinate TargetCoordinate
        {
            get;
            set;
        }

        public IMatchUnitAssetView TargetUnitOrAsset
        {
            get;
            set;
        }

        public VoxelData TargetData
        {
            get;
            set;
        }

        private Coordinate[] m_targetCoordinates;
        public Coordinate[] TargetCoordinates
        {
            get { return m_targetCoordinates; }
            set
            {
                m_targetCoordinates = value;
                TargetCoordinate = m_targetCoordinates[0];
            }
        }

        private VoxelData[] m_targetsDataArray;
        public VoxelData[] TargetDataArray
        {
            get { return m_targetsDataArray; }
            set
            {
                m_targetsDataArray = value;
                TargetData = m_targetsDataArray[0];
            }
        }

        public MapPos PrevPos = new MapPos(-1, -1);
        public int MaxIdleIterations = 10;
        public int IdleIteration;
        public int Stage;
        public int PrevStage;
        public readonly List<VoxelData> SuitableData = new List<VoxelData>();
        public MapCell SuitableCell;


        public readonly IBotStrategy Strategy;

        public BotTask(IMatchUnitAssetView unit, BotTaskType taskType, IBotStrategy strategy)
        {
            Unit = unit;
            TaskType = taskType;
            Strategy = strategy;
        }
    }


    public class DefaultBotStrategy : IBotStrategy
    {
        private IMatchView m_matchView;
        private Player m_player;
        private IMatchPlayerView m_playerView;
        private IPathFinder m_pathFinder;
        private ITaskRunner m_taskRunner;

        private float EatersPc = 0.4f;
        private int MinEaters = 2;

        //does not build wall currently because procedure for finding required place is not implemented

        private readonly Dictionary<int, int> m_voxelTypeCounters = new Dictionary<int, int>();
        private int m_eatersCount;
        private int m_bombsCount;
        private int m_spawnersCount;
        private int m_totalCount;

        private readonly Dictionary<BotTaskType, int> m_currentUnitIndexPerTask = new Dictionary<BotTaskType, int>();

        private readonly Dictionary<BotTaskType, IBotTaskProcessor> m_taskProcessors = new Dictionary<BotTaskType, IBotTaskProcessor>();
        //private readonly Dictionary<long, BotTask> m_pendingTasks = new Dictionary<long, BotTask>();
        private readonly Dictionary<long, BotTask> m_pendingTasks = new Dictionary<long, BotTask>();
        private readonly Dictionary<long, BotTask> m_activeTasks = new Dictionary<long, BotTask>();
        private readonly List<BotTask> m_deactivatedTasks = new List<BotTask>();

        //if task is active and player is not moving for more than 3 think method calls task become failed
        //this include auto action of eater (but does not include auto action of bomb)
        //if path finder failed to find path for pending task -> task become failed
        //if unit succeede with any task failed tasks cleared
        private readonly Dictionary<long, BotTaskType> m_failedTaskTypes = new Dictionary<long, BotTaskType>();


        

        private readonly List<Cmd> m_currentCommands = new List<Cmd>();
        private readonly CompositeCmd m_currentCommand = new CompositeCmd();


        private MapPos m_baseCampPosition;
        public MapPos BaseCampPostion
        {
            get { return m_baseCampPosition; }
        }

        public DefaultBotStrategy(Player player, IMatchView matchView, IPathFinder pathFinder, ITaskRunner taskRunner)
        {
            m_taskProcessors.Add(BotTaskType.Eat, new EatTaskProcessor(matchView, pathFinder, taskRunner));
            m_taskProcessors.Add(BotTaskType.Attack, new AttackTaskProcessor(matchView, pathFinder, taskRunner));
            m_taskProcessors.Add(BotTaskType.Grow, new GrowTaskProcessor(matchView, pathFinder, taskRunner));
            m_taskProcessors.Add(BotTaskType.Split4, new Split4TaskProcessor());
           // m_taskProcessors.Add(BotTaskType.Split, new SplitTaskProcessor());
            m_taskProcessors.Add(BotTaskType.ConvertToBomb, new ConvertTaskProcessor(matchView, pathFinder, taskRunner));
            m_taskProcessors.Add(BotTaskType.ConvertToSpawner, new ConvertTaskProcessor(matchView, pathFinder, taskRunner));
            
            m_player = player;
            m_matchView = matchView;
            m_pathFinder = pathFinder;
            m_taskRunner = taskRunner;

            m_playerView = m_matchView.GetPlayerView(m_player.Id);
            m_playerView.AssetCreated += OnAssetCreated;
            m_playerView.AssetRemoved += OnAssetRemoved;
            m_playerView.UnitCreated += OnUnitCreated;
            m_playerView.UnitRemoved += OnUnitRemoved;

            foreach (BotTaskType taskType in Enum.GetValues(typeof(BotTaskType)))
            {
                m_currentUnitIndexPerTask.Add(taskType, -1);
            }
            foreach (KnownVoxelTypes voxelType in Enum.GetValues(typeof(KnownVoxelTypes)))
            {
                m_voxelTypeCounters.Add((int)voxelType, 0);
            }

            IMatchUnitAssetView[] units = m_playerView.Units;
            for (int i = 0; i < units.Length; ++i)
            {
                m_voxelTypeCounters[units[i].Data.Type]++;
            }

            foreach (IMatchUnitAssetView asset in m_playerView.Assets)
            {
                m_voxelTypeCounters[asset.Data.Type]++;
            }

            m_baseCampPosition = FindBaseCampPosition();
        }

        private MapPos FindBaseCampPosition()
        {
            MapPos center;
            IMatchUnitAssetView[] units = m_playerView.Units;
            if(units.Length > 0)
            {
                center = units[0].Position;
                for(int i = 1; i < units.Length; ++i)
                {
                    MapPos pos = units[i].Position;
                    center.Add(pos.Row, pos.Col);
                }

                center.Row /= m_playerView.UnitsCount;
                center.Col /= m_playerView.UnitsCount;

                MapPos closestPos = center;
                int minDistance = int.MaxValue;

                for (int i = 0; i < units.Length; ++i)
                {
                    MapPos pos = units[i].Position;
                    int distance = center.SqDistanceTo(pos);
                    if(distance < minDistance)
                    {
                        closestPos = pos;
                        minDistance = distance;
                    }
                }

                return closestPos;
            }
            else
            {
                if(m_player.BotType != BotType.Neutral)
                {
                    UnityEngine.Debug.LogWarning("no units");
                }

                int mapSize = m_matchView.Map.GetMapSizeWith(GameConstants.MinVoxelActorWeight);
                return new MapPos(mapSize / 2, mapSize / 2);
            }

            
        }

        public CompositeCmd Think()
        {
            if (m_playerView.ControllableUnitsCount <= 0)
            {
                return null;
            }

            m_eatersCount = m_voxelTypeCounters[(int)KnownVoxelTypes.Eater];
            m_bombsCount = m_voxelTypeCounters[(int)KnownVoxelTypes.Bomb];
            m_spawnersCount = m_voxelTypeCounters[(int)KnownVoxelTypes.Spawner];
            m_totalCount = m_eatersCount + m_bombsCount + m_spawnersCount;

            if (m_eatersCount + m_bombsCount > m_activeTasks.Count + m_pendingTasks.Count)
            {
                foreach(BotTaskType taskType in AvailableTaskTypes())
                {
                     CreateNewTask(taskType);
                }
            }

            //process active tasks
            ProcessActiveTasks();

            if (m_currentCommands.Count > 0)
            {
                m_currentCommand.Commands = m_currentCommands.ToArray();
                m_currentCommands.Clear();
                return m_currentCommand;
            }

            return null;
        }


        private void ProcessActiveTasks()
        {
            foreach (BotTask task in m_activeTasks.Values)
            {
                UnityEngine.Debug.Assert(!m_pendingTasks.ContainsKey(task.Unit.Id));

                IBotTaskProcessor taskProcessor = m_taskProcessors[task.TaskType];

                if (taskProcessor.ChangeStage(task))
                {
                    Cmd cmd = taskProcessor.CreateCommand(task);
                    if(cmd != null)
                    {
                        m_currentCommands.Add(cmd);
                    }
                }

                if (taskProcessor.IsCompleted(task))
                {
                    m_deactivatedTasks.Add(task);
                }
                else if (taskProcessor.ShouldBeCancelled(task))
                {
                    UnityEngine.Debug.Log(task.TaskType + " Should be cancelled " + task.Unit.Id);
                    m_deactivatedTasks.Add(task);
                }
            }

            if (m_deactivatedTasks.Count > 0)
            {
                for (int i = 0; i < m_deactivatedTasks.Count; ++i)
                {
                    m_activeTasks.Remove(m_deactivatedTasks[i].Unit.Id);
                }

                m_deactivatedTasks.Clear();
            }
        }

        public IEnumerable<BotTaskType> AvailableTaskTypes()
        {
            int desiredEatersCount = (int)Math.Max(MinEaters, (m_totalCount * EatersPc));
            int convertableEaters = m_eatersCount - desiredEatersCount;

            //Create grow tasks
            yield return BotTaskType.Grow;

            //Create split4 tasks
            yield return BotTaskType.Split4;

            for (int i = 0; i < convertableEaters; ++i)
            {
                if (m_spawnersCount > m_bombsCount)
                {
                    yield return BotTaskType.ConvertToBomb;
                }
                else
                {
                    yield return BotTaskType.ConvertToSpawner;
                }
            }

            //Create eat tasks
            yield return BotTaskType.Eat;


            //Create attack tasks
            yield return BotTaskType.Attack;


            //Create split tasks if needed
           //yield return BotTaskType.Split;
        }

        private bool AreAllTasksFailed(IMatchUnitAssetView unit)
        {
            foreach(BotTaskType taskType in AvailableTaskTypes())
            {
                if(!IsSuitableTaskFor(unit, taskType))
                {
                    continue;
                }

                BotTaskType failedTaskTypes;
                if (m_failedTaskTypes.TryGetValue(unit.Id, out failedTaskTypes))
                {
                    if ((failedTaskTypes & taskType) == BotTaskType.None)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void CreateNewTask(BotTaskType taskType)
        {
            int currentUnitIndex = m_currentUnitIndexPerTask[taskType];
            if (currentUnitIndex >= m_playerView.UnitsCount - 1)
            {
                currentUnitIndex = -1;
            }

            for (int i = 0; i < m_playerView.UnitsCount; ++i)
            {
                currentUnitIndex++;
                if (currentUnitIndex > m_playerView.UnitsCount - 1)
                {
                    currentUnitIndex = 0;
                }

                IMatchUnitAssetView unitView = m_playerView.Units[currentUnitIndex];
                if(!VoxelData.IsControllableUnit(unitView.Data.Type))
                {
                    continue;
                }

                if (m_pendingTasks.ContainsKey(unitView.Id) ||
                   m_activeTasks.ContainsKey(unitView.Id))
                {
                    continue;
                }

                if (!m_failedTaskTypes.ContainsKey(unitView.Id))
                {
                    m_failedTaskTypes[unitView.Id] = BotTaskType.None;
                }

                if (AreAllTasksFailed(unitView))
                {
                    m_failedTaskTypes[unitView.Id] = BotTaskType.None;
                }

                if (!IsSuitableTaskFor(unitView, taskType))
                {
                    m_failedTaskTypes[unitView.Id] |= taskType;
                    continue;
                }

                BotTaskType failedTaskTypes;
                if (m_failedTaskTypes.TryGetValue(unitView.Id, out failedTaskTypes))
                {
                    if ((failedTaskTypes & taskType) != BotTaskType.None)
                    {
                        continue;
                    }
                }

                BotTask task = new BotTask(unitView, taskType, this);
                m_pendingTasks.Add(task.Unit.Id, task);
            
               // UnityEngine.Debug.Log("Creating new task " + task.Unit.Id);
                IBotTaskProcessor taskProcessor = m_taskProcessors[task.TaskType];
                taskProcessor.Process(task, OnPendingTaskFailed, OnPendingTaskProcessed);

                break;
            }

            currentUnitIndex++;
            m_currentUnitIndexPerTask[taskType] = currentUnitIndex;
        }

        private void OnPendingTaskFailed(BotTask task)
        {
            m_failedTaskTypes[task.Unit.Id] |= task.TaskType;
            m_pendingTasks.Remove(task.Unit.Id);
        }

        private void OnPendingTaskProcessed(BotTask task)
        {
            m_failedTaskTypes[task.Unit.Id] = BotTaskType.None;
            m_pendingTasks.Remove(task.Unit.Id);
            m_activeTasks.Add(task.Unit.Id, task);

            IBotTaskProcessor taskProcessor = m_taskProcessors[task.TaskType];

            Cmd cmd = taskProcessor.CreateCommand(task);
            if (cmd != null)
            {
                m_currentCommands.Add(cmd);
            }
        }

        private bool IsSuitableTaskFor(IMatchUnitAssetView unitView, BotTaskType taskType)
        {
            int cmdCode = BotTaskTypeToCmdCode(taskType);
            if (!m_matchView.IsSuitableCmdFor(m_player.Id, unitView.Id, cmdCode))
            {
                return false;
            }

            if (unitView.Data.Type == (int)KnownVoxelTypes.Bomb)
            {
                return taskType == BotTaskType.Attack;
            }

            if (taskType == BotTaskType.Eat)
            {
                if (unitView.DataController.Abilities.MaxHealth <= unitView.Data.Health)
                {
                    return false;
                }
            }

            if(taskType == BotTaskType.Split)
            {
                if(unitView.DataController.CanGrow() != true)
                {
                    return false;
                }
            }

            return true;
        }

        private int BotTaskTypeToCmdCode(BotTaskType taskType)
        {
            switch (taskType)
            {
                case BotTaskType.Attack:
                    return CmdCode.Move;
                case BotTaskType.ConvertToBomb:
                    return CmdCode.Convert;
                case BotTaskType.ConvertToSpawner:
                    return CmdCode.Convert;
                case BotTaskType.Eat:
                    return CmdCode.Move;
                case BotTaskType.Grow:
                    return CmdCode.Grow;
                case BotTaskType.Split4:
                    return CmdCode.Split4;
                case BotTaskType.Split:
                    return CmdCode.Split;
                default:
                    throw new NotImplementedException();

            }
        }

        public void Destroy()
        {
            m_playerView.AssetCreated -= OnAssetCreated;
            m_playerView.AssetRemoved -= OnAssetRemoved;
            m_playerView.UnitCreated -= OnUnitCreated;
            m_playerView.UnitRemoved -= OnUnitRemoved;
        }

        private void OnUnitCreated(IMatchUnitAssetView unit)
        {
            OnUnitOrAssetCreated(unit);
        }

        private void OnUnitRemoved(IMatchUnitAssetView unit)
        {
            UnityEngine.Debug.LogWarning("Unit Removed " + unit.Id);

            OnUnitOrAssetRemoved(unit);

            m_failedTaskTypes.Remove(unit.Id);
            m_pendingTasks.Remove(unit.Id);
            m_activeTasks.Remove(unit.Id);

            m_taskRunner.Terminate(unit.Id, unit.DataController.PlayerIndex);
            m_pathFinder.Terminate(unit.Id, unit.DataController.PlayerIndex);
        }

        private void OnAssetCreated(IMatchUnitAssetView asset)
        {
            OnUnitOrAssetCreated(asset);
        }

        private void OnAssetRemoved(IMatchUnitAssetView asset)
        {
            OnUnitOrAssetRemoved(asset);
        }

        private void OnUnitOrAssetCreated(IMatchUnitAssetView unit)
        {
            m_voxelTypeCounters[unit.Data.Type]++;
        }

        private void OnUnitOrAssetRemoved(IMatchUnitAssetView unit)
        {
            int counter = m_voxelTypeCounters[unit.Data.Type];
            counter--;
            m_voxelTypeCounters[unit.Data.Type] = counter;

            UnityEngine.Debug.Assert(counter >= 0);
        }
    }

}