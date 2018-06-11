﻿using System;
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

        //IPathFinder PathFinder
        //{
        //    get;
        //}

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
        void SetTaskState(int taskId, TaskState state);

        void Tick();

        void Destroy();
    }


    public interface ITaskMemory
    {
        void CreateOutputs(int scopeId, int taskId, int count);
        void WriteOutput(int scopeId, int taskId, int index, object value);
        object ReadOutput(int scopeId, int taskId, int index);
        void DestroyScope(int scopeId);
    }
    public class TaskMemory : ITaskMemory
    {
        private readonly Dictionary<int, Dictionary<int, object[]>> m_memory = new Dictionary<int, Dictionary<int, object[]>>();

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

    public class TaskEngine : ITaskEngine
    {
        public event TaskEngineEvent<TaskInfo> TaskStateChanged;
        public event TaskEngineEvent<ClientRequest> ClientRequest;
        
        private readonly bool m_isClient;
        public bool IsClient
        {
            get { return m_isClient; }
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
        private const long m_timeoutTicks = 1200; //rougly equal to 1 minute;
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
                { ExpressionCode.Var, new VarExpression() },
                { ExpressionCode.And, new AndExpression() },
                { ExpressionCode.Or, new OrExpression() },
                { ExpressionCode.Not, new NotExpression() },
                { ExpressionCode.Eq, new EqExpression() },
                { ExpressionCode.NotEq, new NotEqExpression() },
                { ExpressionCode.UnitExists, new UnitExistsExpression() },
                { ExpressionCode.UnitState, new UnitStateExpression() },
                { ExpressionCode.UnitCoordinate, new UnitCoordinateExpression() },
                { ExpressionCode.TaskStatus, new TaskStatusExpression() },
            };
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
                    taskInfo.State = TaskState.Failed;
                    RaiseTaskStateChanged(taskInfo);
                }
            }
            
            taskInfo.State = TaskState.Active;
            TaskBase task = InstantiateTask(taskInfo);
            task.ChildTaskActivated += OnChildTaskActivated;
            m_activeTasks.Add(task);
            m_idToActiveTask.Add(task.TaskInfo.TaskId, task);
            if (taskInfo.RequiresClientSidePreprocessing)
            {
                RaiseClientRequest(taskInfo);
            }
            else
            {
                task.Run();
            }

            RaiseTaskStateChanged(taskInfo);
        }

        public void SetTaskState(int taskId, TaskState state)
        {
            TaskBase task;
            if(m_idToActiveTask.TryGetValue(taskId, out task))
            {
                task.TaskInfo.State = state;
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
                        task.TaskInfo.State = TaskState.Failed;
                    }
                    else
                    {
                        request.TaskInfo.PreprocessedCmd = cliResponse.Cmd;

                        if (task.TaskInfo.State == TaskState.Active)
                        {
                            task.Run();
                        }
                    }
                }
                m_requests.Remove(cliResponse.TaskId);
            }
        }

        private void OnClientRequestTimeout(PendingClientRequest request)
        {
            request.TaskInfo.State = TaskState.Failed;
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

        public void Tick()
        {
            if(m_nextTimeoutCheck == m_tick)
            {
                if(m_requests.Count > 0)
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

                    if(m_timeoutRequests.Count > 0)
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

            for(int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                TaskBase activeTask = m_activeTasks[i];
                bool stateChanged = activeTask.Tick();
                if(stateChanged)
                {
                    Debug.Assert(activeTask.TaskInfo.State != TaskState.Active);
                    m_activeTasks.RemoveAt(i);
                    m_idToActiveTask.Remove(activeTask.TaskInfo.TaskId);
                    RaiseTaskStateChanged(activeTask.TaskInfo);
                }
            }

            m_tick++;
        }

        private void OnChildTaskActivated(TaskInfo taskInfo)
        {
            TaskBase task = InstantiateTask(taskInfo);
            m_activeTasks.Add(task);
            m_idToActiveTask.Add(taskInfo.TaskId, task);
            if (taskInfo.RequiresClientSidePreprocessing)
            {
                RaiseClientRequest(taskInfo);
            }
            else
            {
                task.Run();
            }
        }

        public void Destroy()
        {
            for (int i = m_activeTasks.Count - 1; i >= 0; --i)
            {
                TaskBase task = m_activeTasks[i];
                task.TaskInfo.State = TaskState.Terminated;
                RaiseTaskStateChanged(task.TaskInfo);
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

        public void GenerateIdentitifers(TaskInfo taskInfo)
        {
            unchecked
            {
                m_taskIdentity++;
            }
            
            taskInfo.TaskId = m_taskIdentity;
            if(taskInfo.Children != null)
            {
                for(int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    GenerateIdentitifers(taskInfo.Children[i]);
                }
            }
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
                    if(!ValidateIdentifiers(taskInfo.Children[i]))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public TaskBase InstantiateTask(TaskInfo taskInfo)
        {
            switch (taskInfo.TaskType)
            {
                case TaskType.Command:
                    {
                        Debug.Assert(taskInfo.Cmd != null && taskInfo.Cmd.Code != CmdCode.Nop);

                        if(taskInfo.Expression != null)
                        {
                            return new ExecuteCmdTaskWithExpression(taskInfo, this);
                        }
                        else
                        {
                            if(taskInfo.Cmd.Code == CmdCode.Move)
                            {
                                return new ExecuteMoveTask(taskInfo, this);
                            }
                            return new ExecuteCmdTask(taskInfo, this);
                        } 
                    }
                    
                case TaskType.Sequence:
                    return new SequentialTask(taskInfo, this);
                case TaskType.Branch:
                    return new BranchTask(taskInfo, this);
                case TaskType.Repeat:
                    return new RepeatTask(taskInfo, this);
            }

            throw new NotSupportedException();
        }
    }
}