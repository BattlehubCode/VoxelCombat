using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public interface IMatchView
    {
        //event Action<int, Cmd> OnSubmitted;

        int PlayersCount
        {
            get;
        }

        MapRoot Map
        {
            get;
        }

        IMatchPlayerView GetPlayerView(int index);
        IMatchPlayerView GetPlayerView(Guid guid);

        bool IsSuitableCmdFor(Guid playerId, long unitIndex, int cmdCode);
        void Submit(int playerId, Cmd cmd);
    }

    public delegate void MatchPlayerEventHandler<T>(T arg);

    public interface IMatchPlayerView
    {
        event MatchPlayerEventHandler<IMatchUnitAssetView> UnitCreated;
        event MatchPlayerEventHandler<IMatchUnitAssetView> UnitRemoved;
        event MatchPlayerEventHandler<IMatchUnitAssetView> AssetCreated;
        event MatchPlayerEventHandler<IMatchUnitAssetView> AssetRemoved;

          int Index
        {
            get;
        }

        int UnitsCount
        {
            get;
        }

        int ControllableUnitsCount
        {
            get;
        }

        int AssetsCount
        {
            get;
        }

        System.Collections.IEnumerable Units
        {
            get;
        }

  
        System.Collections.IEnumerable Assets
        {
            get;
        }

        IMatchUnitAssetView GetUnit(long id);
        IMatchUnitAssetView GetAsset(long id);
        IMatchUnitAssetView GetUnitOrAsset(long id);
    }

    public interface IMatchUnitAssetView 
    {
        event Action<CmdResultCode> CmdExecuted;

        long Id
        {
            get;
        }

        bool IsAlive
        {
            get;
        }

        MapPos Position
        {
            get;
        }

        VoxelData Data
        {
            get;
        }

        IVoxelDataController DataController
        {
            get;
        }
    }

    public interface IBotStartegy
    {
        void OnAssetCreated(IMatchUnitAssetView asset);
        void OnUnitCreated(IMatchUnitAssetView asset);
        void OnAssetRemoved(IMatchUnitAssetView unit);
        void OnUnitRemoved(IMatchUnitAssetView unit, RunningTaskInfo activeTask, TaskInfo taskInfo);
        void OnTaskCompleted(IMatchUnitAssetView unit, RunningTaskInfo completedTask, TaskInfo taskInfo);
        void Think(float time, IBotSubmitTask bot);
    }

    public class DefaultStrategy : IBotStartegy
    {
        private BotController.State m_state;
        private ILogger m_logger;

        public DefaultStrategy(BotController.State state)
        {
            m_state = state;
            m_logger = Dependencies.Logger;
        }

        public void OnAssetCreated(IMatchUnitAssetView asset)
        {
        }

        public void OnAssetRemoved(IMatchUnitAssetView asset)
        {
        }

        public void OnUnitCreated(IMatchUnitAssetView unit)
        {
        }

        public void OnUnitRemoved(IMatchUnitAssetView unit, RunningTaskInfo activeTask, TaskInfo taskInfo)
        {
        }

        public void OnTaskCompleted(IMatchUnitAssetView unit, RunningTaskInfo completedTask, TaskInfo taskInfo)
        {
            m_logger.LogFormat("Unit {0} task {1} {2} with status {3}", unit.Id, completedTask.TaskId, taskInfo.State, taskInfo.IsFailed ? "failed" : "succeded");
        }

        public void Think(float time, IBotSubmitTask bot)
        {
            Dictionary<long, IMatchUnitAssetView> freeEaters = m_state.FreeUnits[KnownVoxelTypes.Eater];
            if (freeEaters.Count > 0)
            {
                List<IMatchUnitAssetView> selectedUnits = new List<IMatchUnitAssetView>();
                foreach(IMatchUnitAssetView freeEater in freeEaters.Values)
                {
                    selectedUnits.Add(freeEater);
                }

                for(int i = 0; i < selectedUnits.Count; ++i)
                {
                    IMatchUnitAssetView unit = selectedUnits[i];
                    bot.SubmitTask(time, TaskTemplateType.EatGrowSplit4, unit);
                }
            }
        }
    }

    public interface IBotSubmitTask
    {
        void SubmitTask(float time, TaskTemplateType type, IMatchUnitAssetView unit);
    }


    public interface IBotController
    {
        void Init();
        void Reset();
        void Update(float time);
        void Destroy();
    }


    public class RunningTaskInfo
    {
        public readonly TaskTemplateType Type;
        public readonly IMatchUnitAssetView Target;
        public readonly int TaskId;
        public readonly float StartTime;

        public RunningTaskInfo(TaskTemplateType type, IMatchUnitAssetView target, int taskId, float startTime)
        {
            Type = type;
            Target = target;
            TaskId = taskId;
            StartTime = startTime;
        }
    }


    public class BotController : IBotController, IBotSubmitTask
    {
        public class State
        {
            public readonly Dictionary<TaskTemplateType, TaskInfo> TaskTemplates = new Dictionary<TaskTemplateType, TaskInfo>
            {
                { TaskTemplateType.EatGrowSplit4, TaskInfo.EatGrowSplit4() }
            };

            public readonly Dictionary<int, RunningTaskInfo> TaskIdToTask = new Dictionary<int, RunningTaskInfo>();
            public readonly Dictionary<long, RunningTaskInfo> UnitIdToTask = new Dictionary<long, RunningTaskInfo>();
            public readonly Dictionary<KnownVoxelTypes, Dictionary<long, IMatchUnitAssetView>> BusyUnits = new Dictionary<KnownVoxelTypes, Dictionary<long, IMatchUnitAssetView>>();
            public readonly Dictionary<KnownVoxelTypes, Dictionary<long, IMatchUnitAssetView>> FreeUnits = new Dictionary<KnownVoxelTypes, Dictionary<long, IMatchUnitAssetView>>();
        }

        private readonly ILogger m_log;
        private readonly ITaskEngine m_taskEngine;
        private readonly IMatchPlayerView m_playerView;
        private readonly State m_state;
        private readonly IBotStartegy m_strategy;

        public BotController(Player player, ITaskEngine taskEngine)
        {
            m_log = Dependencies.Logger;

            m_state = new State();

            m_strategy = new DefaultStrategy(m_state);

            m_taskEngine = taskEngine;
            m_playerView = m_taskEngine.MatchEngine.GetPlayerView(player.Id);
        }

        public void Init()
        {
            Reset();

            m_state.FreeUnits.Add(KnownVoxelTypes.Eater, new Dictionary<long, IMatchUnitAssetView>());
            m_state.FreeUnits.Add(KnownVoxelTypes.Bomb, new Dictionary<long, IMatchUnitAssetView>());
            m_state.FreeUnits.Add(KnownVoxelTypes.Spawner, new Dictionary<long, IMatchUnitAssetView>());
            m_state.BusyUnits.Add(KnownVoxelTypes.Eater, new Dictionary<long, IMatchUnitAssetView>());
            m_state.BusyUnits.Add(KnownVoxelTypes.Bomb, new Dictionary<long, IMatchUnitAssetView>());
            m_state.BusyUnits.Add(KnownVoxelTypes.Spawner, new Dictionary<long, IMatchUnitAssetView>());
            foreach (IMatchUnitAssetView unit in m_playerView.Units)
            {
                Dictionary<long, IMatchUnitAssetView> units = m_state.FreeUnits[(KnownVoxelTypes)unit.Data.Type];
                units.Add(unit.Id, unit);
            }

            m_playerView.UnitCreated += OnUnitCreated;
            m_playerView.UnitRemoved += OnUnitRemoved;
            m_playerView.AssetCreated += OnAssetCreated;
            m_playerView.AssetRemoved += OnAssetRemoved;
            m_taskEngine.TaskStateChanged += OnTaskStateChanged;
        }

        public void Reset()
        {
            m_taskEngine.TaskStateChanged -= OnTaskStateChanged;
            m_playerView.UnitCreated -= OnUnitCreated;
            m_playerView.UnitRemoved -= OnUnitRemoved;
            m_playerView.AssetCreated -= OnAssetCreated;
            m_playerView.AssetRemoved -= OnAssetRemoved;

            m_taskEngine.TerminateAll();
            m_state.TaskIdToTask.Clear();
            m_state.UnitIdToTask.Clear();
            m_state.BusyUnits.Clear();
            m_state.FreeUnits.Clear();
        }

        public void Destroy()
        {
            Reset();
            m_state.TaskTemplates.Clear();
        }

       
        private void OnAssetCreated(IMatchUnitAssetView asset)
        {
            m_strategy.OnAssetCreated(asset);
        }

        private void OnAssetRemoved(IMatchUnitAssetView asset)
        {
            m_strategy.OnAssetRemoved(asset);
        }

        private void OnUnitCreated(IMatchUnitAssetView unit)
        {
            Dictionary<long, IMatchUnitAssetView> freeUnits;
            if (m_state.FreeUnits.TryGetValue((KnownVoxelTypes)unit.Data.Type, out freeUnits))
            {
                freeUnits.Add(unit.Id, unit);
            }
            m_strategy.OnUnitCreated(unit);
        }

        private void OnUnitRemoved(IMatchUnitAssetView unit)
        {
            bool freeUnitRemoved = false;
            Dictionary<long, IMatchUnitAssetView> freeUnits;
            if (m_state.FreeUnits.TryGetValue((KnownVoxelTypes)unit.Data.Type, out freeUnits))
            {
                if(freeUnits.ContainsKey(unit.Id))
                {
                    freeUnits.Remove(unit.Id);
                    freeUnitRemoved = true;
                }
            }

            Dictionary<long, IMatchUnitAssetView> busyUnits;
            if(m_state.BusyUnits.TryGetValue((KnownVoxelTypes)unit.Data.Type, out busyUnits))
            {
                if(busyUnits.ContainsKey(unit.Id))
                {
                    if (freeUnitRemoved)
                    {
                        m_log.LogWarningFormat("unit {0} appears to be in both collections free and busy !?", unit.Id);
                    }

                    busyUnits.Remove(unit.Id);
                }   
            }

            RunningTaskInfo activeTask = null;
            TaskInfo taskInfo = null;
            if(m_state.UnitIdToTask.TryGetValue(unit.Id, out activeTask))
            {
                //int taskId = activeTask.TaskId;
                //taskInfo = m_taskEngine.TerminateTask(taskId);
                //m_state.TaskIdToTask.Remove(activeTask.TaskId);
                //m_state.UnitIdToTask.Remove(unit.Id);
                //m_strategy.OnTaskCompleted(unit, activeTask, taskInfo);
            }

            m_strategy.OnUnitRemoved(unit, activeTask, taskInfo);
        }

        private void OnTaskStateChanged(TaskInfo taskInfo)
        {
            RunningTaskInfo completedTask;
            if (m_state.TaskIdToTask.TryGetValue(taskInfo.TaskId, out completedTask))
            {
                if (taskInfo.State != TaskState.Active)
                {
                    IMatchUnitAssetView unit = completedTask.Target;
                    Dictionary<long, IMatchUnitAssetView> busyUnits;
                    if (m_state.BusyUnits.TryGetValue((KnownVoxelTypes)unit.Data.Type, out busyUnits))
                    {
                        if(busyUnits.ContainsKey(unit.Id))
                        {
                            busyUnits.Remove(unit.Id);
                            m_state.FreeUnits[(KnownVoxelTypes)unit.Data.Type].Add(unit.Id, unit);
                        }  
                    }

                    m_state.TaskIdToTask.Remove(taskInfo.TaskId);
                    m_state.UnitIdToTask.Remove(unit.Id);
                    m_strategy.OnTaskCompleted(unit, completedTask, taskInfo);
                }
            }  
        }

        void IBotSubmitTask.SubmitTask(float time, TaskTemplateType type, IMatchUnitAssetView unit)
        {
            if(m_state.BusyUnits[(KnownVoxelTypes)unit.Data.Type].ContainsKey(unit.Id))
            {
                throw new InvalidOperationException("unit " + unit.Id + " of type  " + (KnownVoxelTypes)unit.Data.Type + " is busy");
            }

            TaskInfo taskInfo = ProtobufSerializer.DeepClone(m_state.TaskTemplates[type]);
            TaskInfo unitIdTask = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVal(unit.Id));
            TaskInfo playerIdTask = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVal(m_playerView.Index));

            taskInfo.Inputs[0].OutputTask = unitIdTask;
            taskInfo.Inputs[1].OutputTask = playerIdTask;

            TaskInfo rootTask = TaskInfo.Procedure
            (
                unitIdTask,
                playerIdTask,
                taskInfo,
                TaskInfo.Return(ExpressionInfo.TaskStatus(taskInfo))
            );
            rootTask.SetParents();
            rootTask.Initialize(m_playerView.Index);

            m_state.FreeUnits[(KnownVoxelTypes)unit.Data.Type].Remove(unit.Id);
            m_state.BusyUnits[(KnownVoxelTypes)unit.Data.Type].Add(unit.Id, unit);

            m_taskEngine.SubmitTask(rootTask);

            RunningTaskInfo runningTaskInfo = new RunningTaskInfo(type, unit, rootTask.TaskId, time);
            m_state.TaskIdToTask.Add(rootTask.TaskId, runningTaskInfo);
            m_state.UnitIdToTask.Add(unit.Id, runningTaskInfo);
        }

        private const float m_thinkInterval = 0.25f;
        private float m_thinkTime = m_thinkInterval;
        public void Update(float time)
        {
            if(time < m_thinkTime)
            {
                return;
            }
            m_thinkTime = time + m_thinkInterval;
            m_strategy.Think(time, this);
        }
    }
}

