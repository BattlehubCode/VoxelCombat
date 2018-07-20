using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void TaskEngineEvent<T>(T arg);

    public interface ITaskEngine
    {
        event TaskEngineEvent<TaskInfo> TaskStateChanged;
        event TaskEngineEvent<ClientRequest> ClientRequest;

        long CurrentTick
        {
            get;
        }

        bool IsClient
        {
            get;
        }

        IMatchView MatchEngine
        {
            get;
        }

        ITaskRunner TaskRunner
        {
            get;
        }

        IPathFinder PathFinder
        {
            get;
        }

        ITaskMemory Memory
        {
            get;
        }

        IExpression GetExpression(int expressionCode);

        void GenerateIdentitifers(TaskInfo taskInfo);

        /// <summary>
        /// Submit task to task engine. Side effect generates and assignes taskid
        /// </summary>
        /// <param name="taskInfo"></param>
        /// <returns>unique task id</returns>
        void SubmitTask(TaskInfo taskInfo);
        void SubmitResponse(ClientRequest request);
        void SetTaskState(int taskId, TaskState state, int statusCode);
        TaskInfo TerminateTask(int taskId);
        void TerminateAll();

        void Tick();

        void Destroy();
    }


    public interface ITaskMemoryTestView
    {
        Dictionary<int, Dictionary<int, object[]>> Memory
        {
            get;
        }
    }

    public interface ITaskMemory
    {
        bool HasOutput(int scopeId, int taskId, int index);
        void CreateOutputs(int scopeId, int taskId, int count);
        void WriteOutput(int scopeId, int taskId, int index, object value);
        object ReadOutput(int scopeId, int taskId, int index);
        void DestroyScope(int scopeId);
    }
    public class TaskMemory : ITaskMemory, ITaskMemoryTestView
    {
        private readonly Dictionary<int, Dictionary<int, object[]>> m_memory = new Dictionary<int, Dictionary<int, object[]>>();

        Dictionary<int, Dictionary<int, object[]>> ITaskMemoryTestView.Memory
        {
            get { return m_memory; }
        }

        private Dictionary<int, object[]> CreateScope(int scopeId)
        {
            Dictionary<int, object[]> scope;
            if (!m_memory.TryGetValue(scopeId, out scope))
            {
                scope = new Dictionary<int, object[]>();
                m_memory.Add(scopeId, scope);
            }
            return scope;
        }

        private Dictionary<int, object[]> GetScope(int scopeId, int taskId)
        {
            Dictionary<int, object[]> scope;
            if (!m_memory.TryGetValue(scopeId, out scope))
            {
                Debug.LogWarningFormat("Trying to read out of scope input. Scope: {0}, TaskId: {1}", scopeId, taskId);
            }

            return scope;
        }

        public bool HasOutput(int scopeId, int taskId, int index)
        {
            Dictionary<int, object[]> scope;
            if (!m_memory.TryGetValue(scopeId, out scope))
            {
                return false;
            }

            object[] values;
            if(!scope.TryGetValue(taskId, out values))
            {
                return false;
            }

            if(values == null || index < 0 || index >= values.Length)
            {
                return false;
            }

            return true;
        }

        public void CreateOutputs(int scopeId, int taskId, int count)
        {
            Dictionary<int, object[]> scope = CreateScope(scopeId);
            scope[taskId] = new object[count];
        }

        public object ReadOutput(int scopeId, int taskId, int index)
        {
            Dictionary<int, object[]> scope = GetScope(scopeId, taskId);
            return scope[taskId][index];
        }

        public void WriteOutput(int scopeId, int taskId, int index, object value)
        {
            Dictionary<int, object[]> scope = GetScope(scopeId, taskId);
            scope[taskId][index] = value;
        }

        public void DestroyScope(int scopeId)
        {
            m_memory.Remove(scopeId);
        }
    }


    public interface ITaskPool
    {
        TaskBase Acquire();

        void Release(TaskBase task);
        
    }
    public class TaskPool<T> : Pool<T>, ITaskPool where T : TaskBase, new()
    {
        public TaskPool(int size)
        {
            Initialize(size);
        }

        protected override T Instantiate(int index)
        {
            T task = new T();
            return task;
        }
        protected override void Destroy(T obj)
        {
        }

        TaskBase ITaskPool.Acquire()
        {
            return base.Acquire();
        }

        void ITaskPool.Release(TaskBase task)
        {
            base.Release((T)task);
        }
    }

    public interface ITaskEngineTestView
    {
        List<TaskBase> ActiveTasks
        {
            get;
        }

        Dictionary<int, TaskBase> IdToActiveTask
        {
            get;
        }

        int TimedoutRequestsCount
        {
            get;
        }

        int PendingRequestsCount
        {
            get;
        }

        int PoolObjectsCount
        {
            get;
        }

        ITaskMemoryTestView TaskMemory
        {
            get;
        }
    }

    public class TaskEngine : ITaskEngine, ITaskEngineTestView
    {
        public event TaskEngineEvent<TaskInfo> TaskStateChanged;
        public event TaskEngineEvent<ClientRequest> ClientRequest;
        
        private readonly bool m_isClient;
        public bool IsClient
        {
            get { return m_isClient; }
        }

        public long CurrentTick
        {
            get { return m_tick; }
        }

        private readonly IMatchView m_match;
        private readonly ITaskRunner m_taskRunner;
        private readonly IPathFinder m_pathFinder;
        private int m_taskIdentity;
        private readonly List<TaskBase> m_activeTasks;
        private readonly Dictionary<int, TaskBase> m_idToActiveTask;
        private readonly TaskMemory m_mem;

        private class PendingClientRequest
        {
            public long TimeoutTick;
            public TaskInfo TaskInfo;

            public PendingClientRequest(long timeoutTick, TaskInfo taskInfo)
            {
                TimeoutTick = timeoutTick;
                TaskInfo = taskInfo;
            }
        }

        private readonly List<PendingClientRequest> m_timeoutRequests;
        private readonly Dictionary<long, PendingClientRequest> m_requests;
        private const long m_timeoutTicks = GameConstants.TaskEngineClientTimeout;
        private long m_tick;
        private long m_nextTimeoutCheck = m_timeoutTicks / 4;

        private readonly Dictionary<int, IExpression> m_expressions;

        public IMatchView MatchEngine
        {
            get { return m_match; }
        }

        public ITaskRunner TaskRunner
        {
            get { return m_taskRunner; }
        }

        public IPathFinder PathFinder
        {
            get { return m_pathFinder; }
        }

        public IExpression GetExpression(int expressionCode)
        {
            return m_expressions[expressionCode];
        }

        public ITaskMemory Memory
        {
            get { return m_mem; }
        }

        List<TaskBase> ITaskEngineTestView.ActiveTasks
        {
            get { return m_activeTasks;}
        }

        Dictionary<int, TaskBase> ITaskEngineTestView.IdToActiveTask
        {
            get { return m_idToActiveTask; }
        }

        int ITaskEngineTestView.TimedoutRequestsCount
        {
            get { return m_timeoutRequests.Count; }
        }

        int ITaskEngineTestView.PendingRequestsCount
        {
            get { return m_requests.Count; }
        }

        int ITaskEngineTestView.PoolObjectsCount
        {
            get
            {
                return
                  ((IPoolTestView)m_cmdExpressionTaskPool).ObjectsCount +
                  ((IPoolTestView)m_cmdMoveTaskPool).ObjectsCount +
                  ((IPoolTestView)m_cmdGenericTaskPool).ObjectsCount +
                  m_taskPools.Values.Sum(v => ((IPoolTestView)v).ObjectsCount);
            }
        }

        ITaskMemoryTestView ITaskEngineTestView.TaskMemory
        {
            get { return m_mem; }
        }

        private readonly Dictionary<TaskType, ITaskPool> m_taskPools;
        private readonly ITaskPool m_cmdExpressionTaskPool;
        private readonly ITaskPool m_cmdMoveTaskPool;
        private readonly ITaskPool m_cmdGenericTaskPool;

        public TaskEngine(IMatchView match, ITaskRunner taskRunner, IPathFinder pathFinder, bool isClient)
        {
            m_isClient = isClient;
            m_match = match;
            m_taskRunner = taskRunner;
            m_pathFinder = pathFinder;
            m_activeTasks = new List<TaskBase>();
            m_idToActiveTask = new Dictionary<int, TaskBase>();
            m_timeoutRequests = new List<PendingClientRequest>();
            m_requests = new Dictionary<long, PendingClientRequest>();
            m_mem = new TaskMemory();

            m_expressions = new Dictionary<int, IExpression>
            {
                { ExpressionCode.Value, new ValueExpression() },
                { ExpressionCode.Assign, new AssignmentExpression() },
                { ExpressionCode.Itertate, new IterateExpression() },
                { ExpressionCode.Get, new GetExpression() },
                { ExpressionCode.And, new AndExpression() },
                { ExpressionCode.Or, new OrExpression() },
                { ExpressionCode.Not, new NotExpression() },
                { ExpressionCode.Eq, new EqExpression() },
                { ExpressionCode.NotEq, new NotEqExpression() },
                { ExpressionCode.Lt, new LtExpression() },
                { ExpressionCode.Lte, new LteExpression() },
                { ExpressionCode.Gt, new GtExpression() },
                { ExpressionCode.Gte, new GteExpression() },
                { ExpressionCode.Add, new AddExpression() },
                { ExpressionCode.Sub, new SubExpression() },
                { ExpressionCode.UnitExists, new UnitExistsExpression() },
                { ExpressionCode.UnitState, new UnitStateExpression() },
                { ExpressionCode.UnitCoordinate, new UnitCoordinateExpression() },
                { ExpressionCode.UnitCanGrow, new UnitCanGrowImmediateExpression() },
                { ExpressionCode.UnitCanSplit4, new UnitCanSplit4Expression()},
                { ExpressionCode.TaskStatusCode, new TaskStatusExpression() },
                { ExpressionCode.TaskSucceded, new TaskSuccededExpression() },
                { ExpressionCode.CmdSucceded, new CmdSuccededExpression() },
                { ExpressionCode.CmdHardFailed, new CmdHardFailedExpression() },
                { ExpressionCode.CmdResultCode, new CmdResultCodeExpression() },
            };

            const int taskPoolSize = 10;
            m_taskPools = new Dictionary<TaskType, ITaskPool>
            {
                { TaskType.Sequence, new TaskPool<SequentialTask>(taskPoolSize) },
                { TaskType.Repeat, new TaskPool<RepeatTask>(taskPoolSize) },
                { TaskType.Branch, new TaskPool<BranchTask>(taskPoolSize) },
                { TaskType.Procedure, new TaskPool<ProcedureTask>(taskPoolSize) },
                { TaskType.Break, new TaskPool<BreakTask>(taskPoolSize) },
                { TaskType.Continue, new TaskPool<ContinueTask>(taskPoolSize) },
                { TaskType.Return, new TaskPool<ReturnTask>(taskPoolSize) },
                { TaskType.Nop, new TaskPool<NopTask>(taskPoolSize) },
                { TaskType.EvalExpression, new TaskPool<EvaluateExpressionTask>(taskPoolSize) },
                { TaskType.FindPath, new TaskPool<FindPathTask>(taskPoolSize) },
                { TaskType.FindPathToRandomLocation, new TaskPool<FindPathToRandLocationPath>(taskPoolSize) },
                { TaskType.SearchForFood, new TaskPool<SearchAroundForFood>(taskPoolSize) },
                { TaskType.SearchForGrowLocation, new TaskPool<SearchAroundForGrowLocation>(taskPoolSize) },
                { TaskType.SearchForSplit4Location, new TaskPool<SearchAroundForSplit4Location>(taskPoolSize) },
                //For testing purposes
                { TaskType.TEST_Mock, new TaskPool<MockTask>(taskPoolSize) },
                { TaskType.TEST_MockImmediate, new TaskPool<MockImmediateTask>(taskPoolSize) },
                { TaskType.TEST_Fail, new TaskPool<TestFailTask>(taskPoolSize) },
                { TaskType.TEST_Pass, new TaskPool<TestPassTask>(taskPoolSize) },
                { TaskType.TEST_Assert, new TaskPool<TestAssertTask>(taskPoolSize) }
            };

            m_cmdExpressionTaskPool = new TaskPool<ExecuteCmdTaskWithExpression>(taskPoolSize);
            m_cmdMoveTaskPool = new TaskPool<ExecuteMoveTask>(taskPoolSize);
            m_cmdGenericTaskPool = new TaskPool<ExecuteCmdTask>(taskPoolSize);
        }

        public void SubmitTask(TaskInfo taskInfo)
        {
            if(IsClient)
            {
                GenerateIdentitifers(taskInfo);
            }
            else
            {
                if(!ValidateIdentifiers(taskInfo))
                {
                    taskInfo.State = TaskState.Completed;
                    taskInfo.StatusCode = TaskInfo.TaskFailed;
                    RaiseTaskStateChanged(taskInfo);
                }
            }
            
            HandleTaskActivation(null, taskInfo);
        }

        public void SetTaskState(int taskId, TaskState state, int statusCode)
        {
            TaskBase task;
            if(m_idToActiveTask.TryGetValue(taskId, out task))
            {
                task.TaskInfo.State = state;
                task.TaskInfo.StatusCode = statusCode;
            }
        }

        public void SubmitResponse(ClientRequest cliResponse)
        {
            PendingClientRequest request;
            if(m_requests.TryGetValue(cliResponse.TaskId, out request))
            {
                TaskBase task;
                if (m_idToActiveTask.TryGetValue(cliResponse.TaskId, out task))
                {
                    if (cliResponse.Cmd.IsFailed)
                    {
                        task.TaskInfo.State = TaskState.Completed;
                        task.TaskInfo.StatusCode = TaskInfo.TaskFailed;
                    }
                    else
                    {
                        request.TaskInfo.PreprocessedCmd = cliResponse.Cmd;
                        task.ChildTaskActivated += OnChildTaskActivated;
                        task.Construct();
                    }
                }
                m_requests.Remove(cliResponse.TaskId);
            }
        }

        private void OnClientRequestTimeout(PendingClientRequest request)
        {
            request.TaskInfo.State = TaskState.Completed;
            request.TaskInfo.StatusCode = TaskInfo.TaskFailed;
        }

        private void RaiseClientRequest(TaskInfo taskInfo)
        {
            PendingClientRequest pendingRequest = new PendingClientRequest(m_tick + m_timeoutTicks, taskInfo);
            
            m_requests.Add(taskInfo.TaskId, pendingRequest);

            if (ClientRequest != null)
            {
                ClientRequest(new ClientRequest(taskInfo.TaskId, taskInfo.PlayerIndex, taskInfo.Cmd));
            }
        }


        private int m_continueIteration = -1;
        public void Tick()
        {
            if (m_nextTimeoutCheck == m_tick)
            {
                if (m_requests.Count > 0)
                {
                    var keys = m_requests.Keys;
                    foreach (var key in keys)
                    {
                        PendingClientRequest request = m_requests[key];
                        if (request.TimeoutTick <= m_tick)
                        {
                            m_timeoutRequests.Add(request);
                        }
                    }

                    if (m_timeoutRequests.Count > 0)
                    {
                        for (int i = 0; i < m_timeoutRequests.Count; ++i)
                        {
                            PendingClientRequest timeoutRequest = m_timeoutRequests[i];
                            m_requests.Remove(timeoutRequest.TaskInfo.TaskId);
                            OnClientRequestTimeout(timeoutRequest);
                        }
                        m_timeoutRequests.Clear();
                    }
                }

                m_nextTimeoutCheck = m_tick + m_timeoutTicks / 4;
            }
    
            int batchSize = GameConstants.TaskEngineBatchSize;
            while(batchSize > 0 && m_activeTasks.Count > 0)
            {
                if (m_continueIteration < 0)
                {
                    m_continueIteration = m_activeTasks.Count - 1;
                }

                TaskBase activeTask = m_activeTasks[m_continueIteration];
                if (activeTask.TaskInfo.State == TaskState.Active)
                {
                    activeTask.Tick();
                }

                if (activeTask.TaskInfo.State != TaskState.Active)
                {
                    m_activeTasks.RemoveAt(m_continueIteration);
                    HandleActiveTaskRemoved(activeTask.TaskInfo);
                }

                m_continueIteration--;
                batchSize--;
            }

            m_tick++;
        }

        private void HandleActiveTaskRemoved(TaskInfo activeTaskInfo)
        {
            TaskBase activeTask;
            if(!m_idToActiveTask.TryGetValue(activeTaskInfo.TaskId, out activeTask))
            {
                return;
            }

            m_idToActiveTask.Remove(activeTaskInfo.TaskId);
            m_requests.Remove(activeTaskInfo.TaskId);
            Release(activeTask);
            RaiseTaskStateChanged(activeTaskInfo);
        }

        private void OnChildTaskActivated(TaskBase parent, TaskInfo taskInfo)
        {
            HandleTaskActivation(parent, taskInfo);
        }

        private void HandleTaskActivation(TaskBase parent, TaskInfo taskInfo)
        {
            taskInfo.State = TaskState.Active;
            TaskBase task;
            //if (m_idToActiveTask.TryGetValue(taskInfo.TaskId, out task))
            //{
            //    Release(task);
            //    m_idToActiveTask.Remove(taskInfo.TaskId);
            //    m_activeTasks.Remove(task);
            //}

            RaiseTaskStateChanged(taskInfo);
            task = Acquire(parent, taskInfo);
            m_activeTasks.Add(task);
            m_idToActiveTask.Add(task.TaskInfo.TaskId, task);
            if (taskInfo.RequiresClientSidePreprocessing)
            {
                RaiseClientRequest(taskInfo);
            }  
            else
            {
                task.ChildTaskActivated += OnChildTaskActivated;
                task.Construct();
            }
        }

        public TaskInfo TerminateTask(int taskId)
        {
            TaskBase task;
            if (m_idToActiveTask.TryGetValue(taskId, out task))
            {
                if (task.TaskInfo.State == TaskState.Terminated)
                {
                    return task.TaskInfo;
                }
                task.TaskInfo.State = TaskState.Terminated;
                if(task.TaskInfo.Children != null)
                {
                    for (int i = 0; i < task.TaskInfo.Children.Length; ++i)
                    {
                        TaskInfo child = task.TaskInfo.Children[i];
                        if(child != null)
                        {
                            TerminateTask(child.TaskId);
                        }
                    }
                }
                return task.TaskInfo;
            }
            return null;
        }

        public void TerminateAll()
        {
            m_continueIteration = -1;
            for (int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                TaskBase task = m_activeTasks[i];
                task.TaskInfo.State = TaskState.Terminated;
                RaiseTaskStateChanged(task.TaskInfo);
            }
        }

        public void Destroy()
        {
            TerminateAll();
            for (int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                TaskBase task = m_activeTasks[i];
                Release(task);
            }
            m_activeTasks.Clear();
            m_idToActiveTask.Clear();
        }

        private void RaiseTaskStateChanged(TaskInfo task)
        {
            if(TaskStateChanged != null)
            {
                TaskStateChanged(task);
            }
        }

        public static void GenerateIdentitifers(TaskInfo taskInfo, ref int taskIdentitiy)
        {
            unchecked
            {
                taskIdentitiy++;
            }
            
            taskInfo.TaskId = taskIdentitiy;
            if(taskInfo.Children != null)
            {
                for(int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    if(taskInfo.Children[i] != null)
                    {
                        GenerateIdentitifers(taskInfo.Children[i], ref taskIdentitiy);
                    }
                }
            }
        }

        public void GenerateIdentitifers(TaskInfo taskInfo)
        {
            GenerateIdentitifers(taskInfo, ref m_taskIdentity);
        }

        private bool ValidateIdentifiers(TaskInfo taskInfo)
        {
            if(taskInfo.TaskId == 0)
            {
                return false;
            }
            if (taskInfo.Children != null)
            {
                for (int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    if(taskInfo.Children[i] != null && !ValidateIdentifiers(taskInfo.Children[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private TaskBase Acquire(TaskBase parent, TaskInfo taskInfo)
        {
            TaskBase task;
            if (taskInfo.TaskType == TaskType.Command)
            {
                Debug.Assert(taskInfo.Cmd != null && taskInfo.Cmd.Code != CmdCode.Nop);

                if (taskInfo.Expression != null)
                {
                    task = m_cmdExpressionTaskPool.Acquire();
                }
                else
                {
                    if (taskInfo.Cmd.Code == CmdCode.Move)
                    {
                        task = m_cmdMoveTaskPool.Acquire();
                    }
                    else
                    {
                        task = m_cmdGenericTaskPool.Acquire();
                    }   
                }
            }
            else
            {
                task = m_taskPools[taskInfo.TaskType].Acquire();
            }

            task.TaskEngine = this;
            task.Parent = parent;
            task.TaskInfo = taskInfo;
            task.IsAcquired = true;
            return task;
        }

        private void Release(TaskBase task)
        {
            if(!task.IsAcquired)
            {
                throw new InvalidOperationException("!IsAcqired");
            }

            task.ChildTaskActivated -= OnChildTaskActivated;
            task.IsAcquired = false;
            TaskInfo taskInfo = task.TaskInfo;
            if(taskInfo.State != TaskState.Active)
            {
                 Memory.DestroyScope(taskInfo.TaskId);
            }

            if (taskInfo.TaskType == TaskType.Command)
            {
                if (taskInfo.Expression != null)
                {
                    m_cmdExpressionTaskPool.Release(task);
                }
                else
                {
                    if (taskInfo.Cmd.Code == CmdCode.Move)
                    {
                        m_cmdMoveTaskPool.Release(task);
                    }
                    else
                    {
                        m_cmdGenericTaskPool.Release(task);
                    }
                }
            }
            else
            {
                m_taskPools[taskInfo.TaskType].Release(task);
            }
        }

    }
}
