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
        void RegisterTask(TaskTemplateType type, SerializedTask taskInfo);
        //void SubmitTask(float time, TaskTemplateType type, IMatchUnitAssetView unit, params Func<TaskInputInfo, TaskInputInfo, TaskInfo>[] defines);
        void SubmitTask(float time, TaskTemplateType type, IMatchUnitAssetView unit, params TaskInfo[] defines);

        //Group task will be executed using following algorithm.
        //Each unit will be assigned with its own task. 
        //Whenever unit will fail it will be checking if it possible to solve it's own task or task of one of other failed units
        //
        //void SubmitGroupTask(IMatchUnitAssetView unit, 


        //There is also SubmitGlobalTask method required (it does not matter which unit will solve this task, probably choose strategy should be applied, for example select closest unit)
    }


    public interface IBotController : IBotSubmitTask
    {
        void Init();
        void Reset();
        void Update(float time);
        void Destroy();
    }


    public class RunningTaskGroup
    {
        public readonly RunningTaskInfo[] Tasks;

        //public RunningTaskGroup(Runnit)
    }

    public class RunningTaskInfo
    {
        
        public readonly TaskTemplateType Type;
        public readonly IMatchUnitAssetView Target;
        public readonly long TaskId;
        public readonly float StartTime;

        public RunningTaskInfo(TaskTemplateType type, IMatchUnitAssetView target, long taskId, float startTime)
        {
            Type = type;
            Target = target;
            TaskId = taskId;
            StartTime = startTime;
        }
    }

    public class TaskInfoPool : Pool<TaskInfo>
    {
        private readonly Func<TaskInfo> m_instantiateFunc;
        public TaskInfoPool(Func<TaskInfo> instantiateFunc, int size = 5)
        {
            m_instantiateFunc = instantiateFunc;
            Initialize(size);
        }

        protected override void Destroy(TaskInfo obj)
        {
            obj.Reset();
        }

        protected override TaskInfo Instantiate(int index)
        {
            TaskInfo taskInfo = m_instantiateFunc();
            taskInfo.SetParents();
            return taskInfo;
        }
    }

    public class BotController : IBotController, IBotSubmitTask
    {
        public class State
        {
            public readonly Dictionary<TaskTemplateType, TaskInfoPool> TaskTemplates = new Dictionary<TaskTemplateType, TaskInfoPool>
            {
                {
                    TaskTemplateType.EatGrowSplit4,
                    new TaskInfoPool(() =>
                    {
                        TaskInfo coreTaskInfo = TaskInfo.EatGrowSplit4(10, 5);
                        return TaskInfo.Procedure
                        (
                            null,
                            null,
                            coreTaskInfo,
                            TaskInfo.Return(ExpressionInfo.TaskStatus(coreTaskInfo))
                        );
                    })
                },

                {
                    TaskTemplateType.ConvertTo,
                    new TaskInfoPool(() =>
                    {
                        TaskInfo coreTaskInfo = TaskInfo.MoveToAndExecCmd();
                        return TaskInfo.Procedure
                        (
                            null,
                            null,
                            null,
                            null,
                            null,
                            coreTaskInfo,
                            TaskInfo.Return(ExpressionInfo.TaskStatus(coreTaskInfo))
                        );
                    })
                },
            };

            public readonly Dictionary<long, RunningTaskInfo> TaskIdToTask = new Dictionary<long, RunningTaskInfo>();
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

            foreach(RunningTaskInfo task in m_state.TaskIdToTask.Values)
            {
                TaskInfo taskInfo = m_taskEngine.TerminateTask(task.TaskId);
                m_state.TaskTemplates[task.Type].Release(taskInfo);
            }

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

                    m_state.TaskTemplates[completedTask.Type].Release(taskInfo);
                    m_state.TaskIdToTask.Remove(taskInfo.TaskId);
                    m_state.UnitIdToTask.Remove(unit.Id);
                    m_strategy.OnTaskCompleted(unit, completedTask, taskInfo);
                }
            }  
        }

        void IBotSubmitTask.RegisterTask(TaskTemplateType type, SerializedTask serializedTask)
        {
            m_state.TaskTemplates[type] = new TaskInfoPool(() =>
            {
                TaskInfo coreTaskInfo = SerializedTask.ToTaskInfo(serializedTask);

                return TaskInfo.Procedure
                (
                    null,
                    null,
                    coreTaskInfo,
                    TaskInfo.Return(ExpressionInfo.TaskStatus(coreTaskInfo))
                );
            });
        }

        void IBotSubmitTask.SubmitTask(float time, TaskTemplateType type, IMatchUnitAssetView unit, params TaskInfo[] paramters)
        {
            if (m_state.BusyUnits[(KnownVoxelTypes)unit.Data.Type].ContainsKey(unit.Id))
            {
                throw new InvalidOperationException("unit " + unit.Id + " of type  " + (KnownVoxelTypes)unit.Data.Type + " is busy");
            }

            TaskInfo taskInfo = m_state.TaskTemplates[type].Acquire();
            TaskInfo unitIdTask = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVal(unit.Id));
            TaskInfo playerIdTask = TaskInfo.EvalExpression(ExpressionInfo.PrimitiveVal(m_playerView.Index));
            taskInfo.Children[0] = unitIdTask;
            taskInfo.Children[1] = playerIdTask;

            int argsLength = paramters != null ? paramters.Length : 0;
            TaskInfo rootTask = taskInfo.Children[2 + argsLength];
            if (rootTask == null || taskInfo.Children[2 + argsLength - 1] != null)
            {
                throw new ArgumentException("wrong number of arguments for task template: " + type, "type");
            }
            rootTask.Inputs[0].OutputTask = unitIdTask;
            rootTask.Inputs[1].OutputTask = playerIdTask;

            if (paramters != null)
            {
                for (int i = 0; i < paramters.Length; ++i)
                {
                    TaskInfo define = paramters[i];
                    define.Inputs[0].ExtensionSocket = rootTask.Inputs[0];
                    define.Inputs[1].ExtensionSocket = rootTask.Inputs[1];
                    taskInfo.Children[2 + i] = define;
                    taskInfo.Children[2 + paramters.Length].Inputs[2 + i].OutputTask = define;
                }
            }

            taskInfo.SetParents();
            taskInfo.Initialize(m_playerView.Index);

            m_state.FreeUnits[(KnownVoxelTypes)unit.Data.Type].Remove(unit.Id);
            m_state.BusyUnits[(KnownVoxelTypes)unit.Data.Type].Add(unit.Id, unit);


            m_taskEngine.SubmitTask(taskInfo);

            RunningTaskInfo runningTaskInfo = new RunningTaskInfo(type, unit, taskInfo.TaskId, time);
            m_state.TaskIdToTask.Add(taskInfo.TaskId, runningTaskInfo);
            m_state.UnitIdToTask.Add(unit.Id, runningTaskInfo);
        }

        private const float m_thinkInterval = 0.25f;
        private float m_thinkTime = m_thinkInterval;
        public virtual void Update(float time)
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

