//#define DEBUG_OUTPUT
using ProtoBuf;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Battlehub.VoxelCombat
{
    public class ExpressionCode
    {
        public const int Value = 1;
        public const int Assign = 2;
        public const int Itertate = 3;
        public const int Get = 4;
        //public const int Set = 5;

        //Binary expressions
        public const int And = 10;
        public const int Or = 11;
        public const int Not = 12;

        //Comparation expression
        public const int Eq = 20;
        public const int NotEq = 21;
        public const int Lt = 22;
        public const int Lte = 23;
        public const int Gt = 24;
        public const int Gte = 25;

        //Arithmetic
        public const int Add = 30;
        public const int Sub = 31;

        //Complex expressions
        public const int UnitExists = 100;
        public const int UnitCoordinate = 101;
        public const int UnitState = 102;
        public const int UnitCanGrow = 103;
        public const int UnitCanSplit4 = 105;
        public const int UnitCanConvert = 106;
 
        //Complex search expressions
        public const int EnemyVisible = 200;
        public const int FoodVisible = 201;

        //Task
        public const int TaskStatusCode = 500;
        public const int TaskSucceded = 501;
        public const int TaskFailed = 502;
        public const int CmdResultCode = 552;
        public const int CmdSucceded = 553;
        public const int CmdHardFailed = 554;

    }

    public enum TaskType
    {
        Command = 1,
        Sequence = 2,
        Branch = 3,
        Repeat = 4,
        Procedure = 5,
        Break = 10,
        Continue = 11,
        Return = 12,
        Nop = 13,
        
        //Switch = 7,
        EvalExpression = 50,
        FindPath = 100,
        FindPathToRandomLocation = 101,
        SearchForFood = 150,
        SearchForGrowLocation = 151,
        SearchForSplit4Location = 152,
        TEST_MockImmediate = 1000,
        TEST_Mock = 1001,
        TEST_Fail = 1002,
        TEST_Pass = 1003,
        TEST_Assert = 1004,
        TEST_SearchForWall = 1100,
        DEBUG_Log = 2000,
        DEBUG_LogWarning = 2001,
        DEBUG_LogError = 2002,
    }

    public enum TaskState
    {
        Idle,
        Active,
        Completed,
        //Failed,
        Terminated
    }

    public class ExpressionInfo
    {
        public int Code;
        public object Value;
        public ExpressionInfo[] Children;

        public ExpressionInfo()
        {

        }

        public ExpressionInfo(int code, object value, params ExpressionInfo[] children)
        {
            Code = code;
            Value = value;
            Children = children;
        }

     
        public bool IsEvaluating
        {
            get;
            set;
        }

        public static ExpressionInfo PrimitiveVal<T>(T val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Value,
                Value = PrimitiveContract.Create(val)
            };
        }

        public static ExpressionInfo Val<T>(T val) where T : class
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Value,
                Value = val
            };
        }

        public static ExpressionInfo Assign(TaskInfo taskInfo, ExpressionInfo val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Assign,
                Value = taskInfo,
                Children = new[] { val, null }
            };
        }

        public static ExpressionInfo Iterate(IEnumerable enumerable)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Itertate,
                Value = enumerable.GetEnumerator()
            };
        }

        public static ExpressionInfo Assign(TaskInfo taskInfo, ExpressionInfo val, ExpressionInfo output)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Assign,
                Value = taskInfo,
                Children = new [] { val, output }
            };
        }

        public static ExpressionInfo Get(ExpressionInfo obj, ExpressionInfo propertyGetter)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Get,
                Children = new[] { obj, propertyGetter }
            };
        }

        public static ExpressionInfo Add(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Add,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Sub(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Sub,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo And(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.And,
                Children = new [] { left, right }
            };
        }

        public static ExpressionInfo Or(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Or,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Not(ExpressionInfo expressionInfo)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Not,
                Children = new[] { expressionInfo }
            };
        }

        public static ExpressionInfo Eq(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Eq,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo NotEq(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.NotEq,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Lt(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Lt,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Lte(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Lte,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Gt(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Gt,
                Children = new[] { left, right }
            };
        }

        public static ExpressionInfo Gte(ExpressionInfo left, ExpressionInfo right)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Gte,
                Children = new[] { left, right }
            };
        }


        public static ExpressionInfo UnitExists(ExpressionInfo unitId, ExpressionInfo playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitExists,
                Children = new[] { unitId, playerId}
            };
        }

        public static ExpressionInfo UnitState(ExpressionInfo unitId, ExpressionInfo playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitState,
                Children = new[] { unitId, playerId }
            };
        }

        public static ExpressionInfo UnitCoordinate(ExpressionInfo unitId, ExpressionInfo playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCoordinate,
                Children = new[] { unitId, playerId }
            };
        }

        public static ExpressionInfo UnitCanGrow(ExpressionInfo unitId, ExpressionInfo playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCanGrow,
                Children = new[] { unitId, playerId }
            };
        }

        public static ExpressionInfo UnitCanSplit4(ExpressionInfo unitId, ExpressionInfo playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCanSplit4,
                Children = new[] { unitId, playerId }
            };
        }

        public static ExpressionInfo UnitCanConvert(ExpressionInfo unitId, ExpressionInfo playerId, ExpressionInfo targetType)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCanConvert,
                Children = new[] { unitId, playerId, targetType }
            };
        }

        public static ExpressionInfo TaskSucceded(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.TaskSucceded,
                Value = task,
            };
        }

        public static ExpressionInfo TaskFailed(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.TaskFailed,
                Value = task,
            };
        }

        public static ExpressionInfo TaskStatus(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.TaskStatusCode,
                Value = task,
            };
        }

        public static ExpressionInfo CmdResult(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.CmdResultCode,
                Value = task
            };
        }

        public static ExpressionInfo CmdSucceded(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.CmdSucceded,
                Value = task
            };
        }

        public static ExpressionInfo CmdHardFailed(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.CmdHardFailed,
                Value = task
            };
        }
    }

    [ProtoContract]
    public class TaskStateInfo
    {
        [ProtoMember(1)]
        public long TaskId;

        [ProtoMember(2)]
        public TaskState State;

        [ProtoMember(3)]
        public int PlayerId;

        [ProtoMember(4)]
        public int StatusCode;

        public bool IsFailed
        {
            get { return StatusCode != TaskInfo.TaskSucceded; }
        }

        public TaskStateInfo()
        {

        }

        public TaskStateInfo(long taskId, int playerId, TaskState state, int statusCode)
        {
            TaskId = taskId;
            PlayerId = playerId;
            State = state;
            StatusCode = statusCode;
        }
    }

    public class TaskInputInfo
    {
        public TaskInputInfo ExtensionSocket;

        private TaskInfo m_scope;
        private TaskInfo m_output;
        private int m_outputIndex;

        public TaskInfo Scope
        {
            get { return ExtensionSocket.m_scope; }
            set { ExtensionSocket.m_scope = value; }

        }
        public TaskInfo OutputTask
        {
            get { return ExtensionSocket.m_output; }
            set { ExtensionSocket.m_output = value; }
        }

        public int OutputIndex
        {
            get { return ExtensionSocket.m_outputIndex; }
            set { ExtensionSocket.m_outputIndex = value; }
        }

        public void SetScope()
        {
            Scope = OutputTask.Parent;
        }

        public TaskInputInfo()
        {
            ExtensionSocket = this;
        }

        public TaskInputInfo(TaskInfo task, int outputIndex)
        {
            ExtensionSocket = this;
            OutputTask = task;
            OutputIndex = outputIndex;
        }

        public TaskInputInfo(TaskInputInfo taskInput)
        {
            ExtensionSocket = this;
            Scope = taskInput.Scope;
            OutputTask = taskInput.OutputTask;
            OutputIndex = taskInput.OutputIndex;
        }
    }

    public enum TaskTemplateType
    {
        None,
        EatGrowSplit4,
        ConvertTo, 
    }

    
    [ProtoContract]
    public class SerializedNamedTaskLaunchInfo : SerializedTaskLaunchInfo
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public int Row; 

        [ProtoMember(3)]
        public int Col;

        public int Index
        {
            get { return Row * 5 + Col; }
        }
    }

    [ProtoContract]
    [ProtoInclude(1, typeof(SerializedNamedTaskLaunchInfo))]
    public class SerializedTaskLaunchInfo
    {
        [ProtoMember(4)]
        public TaskTemplateType Type;

        [ProtoMember(5)]
        public SerializedTask[] Parameters;

        private TaskInfo[] m_deserializedParameters;
        public TaskInfo[] DeserializedParameters
        {
            get
            {
                if(m_deserializedParameters == null && Parameters != null)
                {
                    m_deserializedParameters = new TaskInfo[Parameters.Length];

                    for(int i = 0; i < Parameters.Length; ++i)
                    {
                        m_deserializedParameters[i] = SerializedTask.ToTaskInfo(Parameters[i]);
                    }   
                }
                return m_deserializedParameters;
            }
            set
            {
                if(value == null)
                {
                    Parameters = null;
                    m_deserializedParameters = null;
                }
                else
                {
                    m_deserializedParameters = value;
                    Parameters = new SerializedTask[m_deserializedParameters.Length];
                    for (int i = 0; i < m_deserializedParameters.Length; ++i)
                    {
                        TaskInfo parameter = m_deserializedParameters[i];
                        Parameters[i] = SerializedTask.FromTaskInfo(parameter);
                    }
                }
                
            }
        }
    }

    [ProtoContract]
    public class SerializedTask
    {
        private class Pair<T1, T2>
        {
            public T1 Serialized;
            public T2 Info;
        }

        [ProtoMember(1)]
        public SerializedSubtask[] Tasks;

        [ProtoMember(2)]
        public SerializedTaskInput[] Inputs;

        [ProtoMember(3)]
        public SerializedExpression[] Expressions;


        public static TaskInfo[] ToTaskInfo(SerializedTask[] serializedTasks)
        {
            if (serializedTasks == null)
            {
                return null;
            }
            TaskInfo[] result = new TaskInfo[serializedTasks.Length];
            for (int i = 0; i < serializedTasks.Length; ++i)
            {
                SerializedTask t = serializedTasks[i];
                if (t != null)
                {
                    result[i] = ToTaskInfo(t);
                }
            }
            return result;
        }


        public static TaskInfo ToTaskInfo(SerializedTask serializedTask)
        {
            if(serializedTask.Tasks == null || serializedTask.Tasks.Length == 0)
            {
                return null;
            }

            if( serializedTask.Tasks.Length == 1 && 
                (serializedTask.Inputs == null || serializedTask.Inputs.Length == 0) && 
                (serializedTask.Expressions == null || serializedTask.Expressions.Length == 0))
            {
                return CreateTaskInfo(serializedTask.Tasks[0]);
            }
            else
            {
                Dictionary<int, Pair<SerializedSubtask, TaskInfo>> tasks = serializedTask.Tasks.ToDictionary(s => s.Address, s => new Pair<SerializedSubtask, TaskInfo> { Serialized = s });
                Dictionary<int, Pair<SerializedTaskInput, TaskInputInfo>> inputs =
                    serializedTask.Inputs != null ?
                    serializedTask.Inputs.ToDictionary(s => s.Address, s => new Pair<SerializedTaskInput, TaskInputInfo> { Serialized = s }) :
                    new Dictionary<int, Pair<SerializedTaskInput, TaskInputInfo>>();

           
                Dictionary<int, Pair<SerializedExpression, ExpressionInfo>> expressions =
                    serializedTask.Expressions != null ?
                    serializedTask.Expressions.ToDictionary(s => s.Address, s => new Pair<SerializedExpression, ExpressionInfo> { Serialized = s }) :
                    new Dictionary<int, Pair<SerializedExpression, ExpressionInfo>>();

                Pair<SerializedSubtask, TaskInfo> task;
                if(!tasks.TryGetValue(1, out task))
                {
                    throw new ArgumentException("serializedTask is invalid", "serializedTask");
                }

                Compose(task, tasks, inputs, expressions);
                task.Info.SetParents();
                return task.Info;
            }
        }

        public static SerializedTask[] FromTaskInfo(TaskInfo[] taskInfo)
        {
            if (taskInfo == null)
            {
                return null;
            }
            SerializedTask[] result = new SerializedTask[taskInfo.Length];
            for (int i = 0; i < taskInfo.Length; ++i)
            {
                TaskInfo t = taskInfo[i];
                if (t != null)
                {
                    result[i] = FromTaskInfo(t);
                }
            }
            return result;
        }

        public static SerializedTask FromTaskInfo(TaskInfo taskInfo)
        {
            if (taskInfo.Inputs == null && taskInfo.Expression == null && taskInfo.Children == null)
            {
                return new SerializedTask
                {
                    Tasks = new[] { CreateSubtask(taskInfo, 1) }
                };
            }

            int address = 1;
            Dictionary<TaskInfo, SerializedSubtask> tasks = new Dictionary<TaskInfo, SerializedSubtask>();
            Dictionary<TaskInputInfo, SerializedTaskInput> inputs = new Dictionary<TaskInputInfo, SerializedTaskInput>();
            Dictionary<ExpressionInfo, SerializedExpression> expressions = new Dictionary<ExpressionInfo, SerializedExpression>();

            Decompose(ref address, taskInfo, tasks, inputs, expressions);

            return new SerializedTask
            {
                Tasks = tasks.Values.ToArray(),
                Inputs = inputs.Values.ToArray(),
                Expressions = expressions.Values.ToArray(),
            };
        }


        private static TaskInfo CreateTaskInfo(SerializedSubtask task)
        {
            TaskInfo taskInfo = new TaskInfo();
            taskInfo.TaskId = task.TaskId;
            taskInfo.TaskType = task.TaskType;
            taskInfo.State = task.TaskState;
            taskInfo.Cmd = task.Cmd;
            taskInfo.RequiresClientSidePreprocessing = task.RequiresClientSideProcessing;
            taskInfo.OutputsCount = task.OutputsCount;
#if DEBUG_OUTPUT
            taskInfo.DebugString = task.DebugString;
#endif

            return taskInfo;
        }

        private static void Compose(
            Pair<SerializedSubtask, TaskInfo> task, 
            Dictionary<int, Pair<SerializedSubtask, TaskInfo>> tasks,
            Dictionary<int, Pair<SerializedTaskInput, TaskInputInfo>> inputs,
            Dictionary<int, Pair<SerializedExpression, ExpressionInfo>> expressions)
        {
            TaskInfo taskInfo = CreateTaskInfo(task.Serialized);
            task.Info = taskInfo;

            if (task.Serialized.ExpressionAddress > 0)
            {
                Pair<SerializedExpression, ExpressionInfo> expression = expressions[task.Serialized.ExpressionAddress];
                if (expression.Info == null)
                {
                    Compose(expression, tasks, inputs, expressions);
                }
                taskInfo.Expression = expression.Info;
            }

            if (task.Serialized.InputAddresses != null)
            {
                taskInfo.Inputs = new TaskInputInfo[task.Serialized.InputAddresses.Length];
                for (int i = 0; i < task.Serialized.InputAddresses.Length; ++i)
                {
                    int inputAddress = task.Serialized.InputAddresses[i];
                    if (inputAddress > 0)
                    {
                        Pair<SerializedTaskInput, TaskInputInfo> input = inputs[inputAddress];
                        if (input.Info == null)
                        {
                            Compose(input, tasks, inputs, expressions);
                        }
                        taskInfo.Inputs[i] = input.Info;
                    }
                }
            }

            if (task.Serialized.ChildrenAddresses != null)
            {
                taskInfo.Children = new TaskInfo[task.Serialized.ChildrenAddresses.Length];
                for (int i = 0; i < task.Serialized.ChildrenAddresses.Length; ++i)
                {
                    int childAddress = task.Serialized.ChildrenAddresses[i];
                    if (childAddress > 0)
                    {
                        Pair<SerializedSubtask, TaskInfo> child = tasks[childAddress];
                        if (child.Info == null)
                        {
                            Compose(child, tasks, inputs, expressions);
                        }
                        taskInfo.Children[i] = child.Info;
                    }
                }
            }
        }

        private static void Compose(
            Pair<SerializedTaskInput, TaskInputInfo> input,
            Dictionary<int, Pair<SerializedSubtask, TaskInfo>> tasks,
            Dictionary<int, Pair<SerializedTaskInput, TaskInputInfo>> inputs,
            Dictionary<int, Pair<SerializedExpression, ExpressionInfo>> expressions)
        {
            TaskInputInfo taskInputInfo = new TaskInputInfo();
            taskInputInfo.OutputIndex = input.Serialized.OutputIndex;
            input.Info = taskInputInfo;

            if(input.Serialized.OutputTaskAddress > 0)
            {
                Pair<SerializedSubtask, TaskInfo> outputTask = tasks[input.Serialized.OutputTaskAddress];
                if(outputTask.Info == null)
                {
                    Compose(outputTask, tasks, inputs, expressions);
                }
                taskInputInfo.OutputTask = outputTask.Info;
            }

            if(input.Serialized.ScopeAddress > 0)
            {
                Pair<SerializedSubtask, TaskInfo> scopeTask = tasks[input.Serialized.ScopeAddress];
                if(scopeTask.Info == null)
                {
                    Compose(scopeTask, tasks, inputs, expressions);
                }
                taskInputInfo.Scope = scopeTask.Info;
            }

        }

        private static void Compose(
            Pair<SerializedExpression, ExpressionInfo> expression,
            Dictionary<int, Pair<SerializedSubtask, TaskInfo>> tasks,
            Dictionary<int, Pair<SerializedTaskInput, TaskInputInfo>> inputs,
            Dictionary<int, Pair<SerializedExpression, ExpressionInfo>> expressions)
        {
            ExpressionInfo expressionInfo = new ExpressionInfo();
            expressionInfo.Code = expression.Serialized.Code;
            expression.Info = expressionInfo;

            if(expression.Serialized.ValueType == SerializedExpression.ExpressionValueType.Task)
            {
                if(expression.Serialized.ValueAddress > 0)
                {
                    Pair<SerializedSubtask, TaskInfo> valueTask = tasks[expression.Serialized.ValueAddress];
                    if(valueTask.Info == null)
                    {
                        Compose(valueTask, tasks, inputs, expressions);
                    }
                    expressionInfo.Value = valueTask.Info;
                }
            }
            else if(expression.Serialized.ValueType == SerializedExpression.ExpressionValueType.TaskInput)
            {
                if(expression.Serialized.ValueAddress > 0)
                {
                    Pair<SerializedTaskInput, TaskInputInfo> valueInput = inputs[expression.Serialized.ValueAddress];
                    if(valueInput.Info == null)
                    {
                        Compose(valueInput, tasks, inputs, expressions);
                    }
                    expressionInfo.Value = valueInput.Info;
                }
            }
            else if(expression.Serialized.ValueType == SerializedExpression.ExpressionValueType.Expression)
            {
                if(expression.Serialized.ValueAddress > 0)
                {
                    Pair<SerializedExpression, ExpressionInfo> valueExpression = expressions[expression.Serialized.ValueAddress];
                    if(valueExpression.Info == null)
                    {
                        Compose(valueExpression, tasks, inputs, expressions);
                    }
                    expressionInfo.Value = valueExpression.Info;
                }
            }
            else
            {
                expressionInfo.Value = expression.Serialized.Value;
            }

            if(expression.Serialized.Children != null)
            {
                expressionInfo.Children = new ExpressionInfo[expression.Serialized.Children.Length];
                for(int i = 0; i < expression.Serialized.Children.Length; ++i)
                {
                    int childAddress = expression.Serialized.Children[i];
                    if(childAddress > 0)
                    {
                        Pair<SerializedExpression, ExpressionInfo> child = expressions[childAddress];
                        if (child.Info == null)
                        {
                            Compose(child, tasks, inputs, expressions);
                        }
                        expressionInfo.Children[i] = child.Info;
                    }
                }
            }
        }

        private static SerializedSubtask CreateSubtask(TaskInfo task, int address)
        {
            SerializedSubtask serializedTask = new SerializedSubtask();
            serializedTask.Cmd = task.Cmd;
            serializedTask.TaskId = task.TaskId;
            serializedTask.TaskState = task.State;
            serializedTask.TaskType = task.TaskType;
            serializedTask.RequiresClientSideProcessing = task.RequiresClientSidePreprocessing;
            serializedTask.OutputsCount = task.OutputsCount;
            serializedTask.Address = address;

#if DEBUG_OUTPUT
            serializedTask.DebugString = task.DebugString;
#endif
            return serializedTask;
        }

        private static void Decompose(ref int address,
            TaskInfo task, 
            Dictionary<TaskInfo, SerializedSubtask> tasks,
            Dictionary<TaskInputInfo, SerializedTaskInput> inputs, 
            Dictionary<ExpressionInfo, SerializedExpression> expressions)
        {
            if(address <= 0)
            {
                throw new ArgumentException("address <= 0", "address");
            }

            if (tasks.ContainsKey(task))
            {
                return;
            }

            SerializedSubtask serializedTask = CreateSubtask(task, address);
            address++;

            tasks.Add(task, serializedTask);
            if (task.Expression != null)
            {
                SerializedExpression serializedExpression;
                if(expressions.TryGetValue(task.Expression, out serializedExpression))
                {
                    serializedTask.ExpressionAddress = serializedExpression.Address;
                }
                else
                {
                    serializedTask.ExpressionAddress = address;
                    Decompose(ref address, task.Expression, tasks, inputs, expressions);
                }
            }

            if (task.Inputs != null)
            {
                serializedTask.InputAddresses = new int[task.Inputs.Length];
                for (int i = 0; i < task.Inputs.Length; ++i)
                {
                    TaskInputInfo input = task.Inputs[i];
                    if (input != null)
                    {
                        SerializedTaskInput serializedInput;
                        if(inputs.TryGetValue(input, out serializedInput))
                        {
                            serializedTask.InputAddresses[i] = serializedInput.Address;
                        }
                        else
                        {
                            serializedTask.InputAddresses[i] = address;
                            Decompose(ref address, input, tasks, inputs, expressions);
                        }
                    }
                }
            }

            if (task.Children != null)
            {
                serializedTask.ChildrenAddresses = new int[task.Children.Length];
                for (int i = 0; i < task.Children.Length; ++i)
                {
                    TaskInfo child = task.Children[i];
                    if (child != null)
                    {
                        SerializedSubtask serializedChildTask;
                        if(tasks.TryGetValue(child, out serializedChildTask))
                        {
                            serializedTask.ChildrenAddresses[i] = serializedChildTask.Address;
                        }
                        else
                        {
                            serializedTask.ChildrenAddresses[i] = address;
                            Decompose(ref address, child, tasks, inputs, expressions);
                        }
                        
                    }
                }
            }
        }

        private static void Decompose(ref int address,
           ExpressionInfo expression,
           Dictionary<TaskInfo, SerializedSubtask> tasks,
           Dictionary<TaskInputInfo, SerializedTaskInput> inputs,
           Dictionary<ExpressionInfo, SerializedExpression> expressions)
        {
            if (expressions.ContainsKey(expression))
            {
                return;
            }

            SerializedExpression serializedExpression = new SerializedExpression();
            serializedExpression.Code = expression.Code;
            serializedExpression.Address = address;
            address++;

            expressions.Add(expression, serializedExpression);
            if (expression.Value is TaskInfo)
            {
                TaskInfo taskInfo = (TaskInfo)expression.Value;
                serializedExpression.ValueType = SerializedExpression.ExpressionValueType.Task;

                SerializedSubtask serializedTask;
                if(tasks.TryGetValue(taskInfo, out serializedTask))
                {
                    serializedExpression.ValueAddress = serializedTask.Address;
                }
                else
                {
                    serializedExpression.ValueAddress = address;
                    Decompose(ref address, taskInfo, tasks, inputs, expressions);
                }    
            }
            else if (expression.Value is TaskInputInfo)
            {
                TaskInputInfo taskInputInfo = (TaskInputInfo)expression.Value;
                serializedExpression.ValueType = SerializedExpression.ExpressionValueType.TaskInput;

                SerializedTaskInput serializedTaskInput;
                if(inputs.TryGetValue(taskInputInfo, out serializedTaskInput))
                {
                    serializedExpression.ValueAddress = serializedTaskInput.Address;
                }
                else
                {
                    serializedExpression.ValueAddress = address;
                    Decompose(ref address, taskInputInfo, tasks, inputs, expressions);
                }
            }
            else if(expression.Value is ExpressionInfo)
            {
                ExpressionInfo expressionInfo = (ExpressionInfo)expression.Value;
                serializedExpression.ValueType = SerializedExpression.ExpressionValueType.Expression;

                SerializedExpression serializedExpressionValue;
                if(expressions.TryGetValue(expressionInfo, out serializedExpressionValue))
                {
                    serializedExpression.ValueAddress = serializedExpressionValue.Address;
                }
                else
                {
                    serializedExpression.ValueAddress = address;
                    Decompose(ref address, expressionInfo, tasks, inputs, expressions);
                }
            }
            else
            {
                serializedExpression.ValueType = SerializedExpression.ExpressionValueType.Value;
                serializedExpression.Value = expression.Value;
            }

            if (expression.Children != null)
            {
                serializedExpression.Children = new int[expression.Children.Length];
                for (int i = 0; i < expression.Children.Length; ++i)
                {
                    ExpressionInfo childExpression = expression.Children[i];
                    if(childExpression != null)
                    {
                        SerializedExpression serializedChildExpression;
                        if(expressions.TryGetValue(childExpression, out serializedChildExpression))
                        {
                            serializedExpression.Children[i] = serializedChildExpression.Address;
                        }
                        else
                        {
                            serializedExpression.Children[i] = address;
                            Decompose(ref address, childExpression, tasks, inputs, expressions);
                        }
                    }
                }
            }
        }


        private static void Decompose(ref int address,
            TaskInputInfo input,
            Dictionary<TaskInfo, SerializedSubtask> tasks,
            Dictionary<TaskInputInfo, SerializedTaskInput> inputs,
            Dictionary<ExpressionInfo, SerializedExpression> expressions)
        {
            if (inputs.ContainsKey(input))
            {
                return;
            }

            SerializedTaskInput serializedTaskInput = new SerializedTaskInput();
            serializedTaskInput.Address = address;
            address++;
            serializedTaskInput.OutputIndex = input.OutputIndex;

            inputs.Add(input, serializedTaskInput);

            if(input.OutputTask != null)
            {
                SerializedSubtask serializedOutputTask;
                if (tasks.TryGetValue(input.OutputTask, out serializedOutputTask))
                {
                    serializedTaskInput.OutputTaskAddress = serializedOutputTask.Address;
                }
                else
                {
                    serializedTaskInput.OutputTaskAddress = address;
                    Decompose(ref address, input.OutputTask, tasks, inputs, expressions);
                }
            }

            if(input.Scope != null)
            {
                SerializedSubtask serializedScope;
                if(tasks.TryGetValue(input.Scope, out serializedScope))
                {
                    serializedTaskInput.ScopeAddress = serializedScope.Address;
                }
                else
                {
                    serializedTaskInput.ScopeAddress = address;
                    Decompose(ref address, input.Scope, tasks, inputs, expressions);
                }
            }  
        }
    }

    [ProtoContract]
    public class SerializedSubtask
    {
        [ProtoMember(1)]
        public int Address;

        [ProtoMember(2)]
        public long TaskId;

        [ProtoMember(3)]
        public TaskType TaskType;

        [ProtoMember(4)]
        public TaskState TaskState;

        [ProtoMember(5)]
        public Cmd Cmd;

        [ProtoMember(6)]
        public int[] ChildrenAddresses;

        [ProtoMember(7)]
        public int ExpressionAddress;

        [ProtoMember(8)]
        public int[] InputAddresses;

        [ProtoMember(9)]
        public int OutputsCount;

        [ProtoMember(10)]
        public bool RequiresClientSideProcessing;

#if DEBUG_OUTPUT
        [ProtoMember(11)]
        public string DebugString;
#endif

        public override string ToString()
        {
            return TaskId + " " + TaskType;
        }
    }

    [ProtoContract]
    public class SerializedExpression
    {
        public enum ExpressionValueType
        {
            Value,
            Task,
            TaskInput,
            Expression,
        }

        [ProtoMember(1)]
        public int Address;

        [ProtoMember(2)]
        public int Code;

        [ProtoMember(3)]
        public int[] Children;

        [ProtoMember(4)]
        public ExpressionValueType ValueType;

        [ProtoMember(5, DynamicType = true)]
        public object Value;

        [ProtoMember(6)]
        public int ValueAddress;
    }

    [ProtoContract]
    public class SerializedTaskInput
    {
        [ProtoMember(1)]
        public int Address;

        [ProtoMember(2)]
        public int ScopeAddress;

        [ProtoMember(3)]
        public int OutputTaskAddress;

        [ProtoMember(4)]
        public int OutputIndex;
    }

    public class TaskInfo
    {
        public const int TaskSucceded = 0;
        public const int TaskFailed = 1;
        public const int TaskFailedCanRepeat = 2;
        
        public long TaskId;
        public TaskType TaskType;
        public Cmd Cmd;
        public TaskState State;
        public TaskInfo[] Children;
        public ExpressionInfo Expression;
        public bool RequiresClientSidePreprocessing;
        public TaskInputInfo[] Inputs;
        public int OutputsCount;
        public int PlayerIndex = -1;

#if DEBUG_OUTPUT
        public string DebugString;
#endif

        public TaskInfo(TaskType taskType, Cmd cmd, TaskState state, ExpressionInfo expression, TaskInfo parent)
        {
            TaskType = taskType;
            Cmd = cmd;
            State = state;
            Expression = expression;
            Parent = parent;
        }

        public TaskInfo(Cmd cmd, TaskState state, ExpressionInfo expression, TaskInfo parent)
            :this(TaskType.Command, cmd, state, expression, parent)
        {
        }

        public TaskInfo(Cmd cmd, TaskState state, ExpressionInfo expression)
            : this(TaskType.Command, cmd, state, expression, null)
        {
        }

        public TaskInfo(Cmd cmd, ExpressionInfo expression) 
            : this(TaskType.Command, cmd, TaskState.Idle, expression, null)
        {
        }

        public TaskInfo(Cmd cmd)
        : this(cmd, null)
        {
        }

        public TaskInfo(Cmd cmd, int playerIndex)
            : this(cmd, null)
        {
            PlayerIndex = playerIndex;
        }

        public TaskInfo(TaskType type)
            : this(type, null, TaskState.Idle, null, null)
        {
        }

        public TaskInfo(TaskType type, TaskState state)
           : this(type, null, state, null, null)
        {
        }

        public TaskInfo()
        {
        }

        public TaskInfo(TaskInfo taskInfo, bool isCmdTask)
        {
            TaskId = taskInfo.TaskId;
            TaskType = taskInfo.TaskType;
            Cmd = taskInfo.Cmd;
            if(isCmdTask)
            {
                State = TaskState.Idle;
            }
            else
            {
                State = taskInfo.State;
                Children = taskInfo.Children;
                Expression = taskInfo.Expression;
                RequiresClientSidePreprocessing = taskInfo.RequiresClientSidePreprocessing;
                Inputs = taskInfo.Inputs;
                OutputsCount = taskInfo.OutputsCount;
            }
        }

        public void Reset()
        {
            State = TaskState.Idle;
            StatusCode = TaskSucceded;
            PreprocessedCmd = null;
        }
    
       
        public TaskInfo Parent { get; set; }

        public TaskInfo Root
        {
            get
            {
                TaskInfo task = this;
                while(task.Parent != null)
                {
                    task = task.Parent;
                }
                return task;
            }
        }

        public Cmd PreprocessedCmd
        {
            get;
            set;
        }

        public int StatusCode
        {
            get;
            set;
        }

        public bool IsFailed
        {
            get { return StatusCode != TaskSucceded; }
        }

        public void SetParents()
        {
            SetParents(this, true);
        }

        public void Initialize(int playerIndex = -1)
        {
            SetInputsScope(this, playerIndex, true);
        }

        private void SetInputScope(ExpressionInfo expression)
        {
            if(expression == null)
            {
                return;
            }

            if (expression.Value is TaskInputInfo)
            {
                TaskInputInfo input = (TaskInputInfo)expression.Value;
                input.SetScope();
            }

            if(expression.Children != null)
            {
                for(int i = 0; i < expression.Children.Length; ++i)
                {
                    SetInputScope(expression.Children[i]);
                }
            }
        }

        private void SetInputsScope(TaskInfo taskInfo, int playerIndex, bool recursive)
        {
            if (taskInfo.Inputs != null)
            {
                for (int i = 0; i < taskInfo.Inputs.Length; ++i)
                {
                    taskInfo.Inputs[i].SetScope();
                }
            }

            if(taskInfo.Expression != null)
            {
                SetInputScope(taskInfo.Expression);
            }
            
            taskInfo.PlayerIndex = playerIndex;
            if (taskInfo.Children != null)
            {
                for (int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    if (taskInfo.Children[i] != null)
                    {
                        if (recursive)
                        {
                            SetInputsScope(taskInfo.Children[i], playerIndex, recursive);
                        }
                    }
                }
            }
        }
        private static void SetParents(TaskInfo taskInfo, bool recursive)
        {
            if (taskInfo.Children != null)
            {
                for (int i = 0; i < taskInfo.Children.Length; ++i)
                {
                    if(taskInfo.Children[i] != null)
                    {
                        taskInfo.Children[i].Parent = taskInfo;
                        if (recursive)
                        {
                            SetParents(taskInfo.Children[i], recursive);
                        }
                    }  
                }
            }
        }

        public static TaskInfo FindById(long id, TaskInfo task)
        {
            if(task.TaskId == id)
            {
                return task;
            }

            if(task.Children != null)
            {
                for(int i = 0; i < task.Children.Length; ++i)
                {
                    TaskInfo child = task.Children[i];
                    if(child != null)
                    {
                        TaskInfo result = FindById(id, child);
                        if(result != null)
                        {
                            return result;
                        }
                    }
                }
            }
            return null;
        }

   

        public override string ToString()
        {
            return TaskType + " " + TaskId + " "
#if DEBUG_OUTPUT
                + DebugString + " "
#endif
                + State + " " 
                + (IsFailed ? "IsFailed=True" : "IsFailed=False");
        }

        public static TaskInfo Assert(Func<TaskBase, TaskInfo, TaskState> callback)
        {
            return new TaskInfo(TaskType.TEST_Assert)
            {
                Expression = new ExpressionInfo
                {
                    Code = ExpressionCode.Value,
                    Value = callback
                }
            };
        }

        public static TaskInfo Log(string text)
        {
            return new TaskInfo(TaskType.DEBUG_Log)
            {
                Expression = ExpressionInfo.PrimitiveVal(text)
            };
        }

        public static TaskInfo LogWarning(string text)
        {
            return new TaskInfo(TaskType.DEBUG_LogWarning)
            {
                Expression = ExpressionInfo.PrimitiveVal(text)
            };
        }

        public static TaskInfo LogError(string text)
        {
            return new TaskInfo(TaskType.DEBUG_LogError)
            {
                Expression = ExpressionInfo.PrimitiveVal(text)
            };
        }

        public static TaskInfo Log(ExpressionInfo expr)
        {
            return new TaskInfo(TaskType.DEBUG_Log)
            {
                Expression = expr
            };
        }

        public static TaskInfo LogWarning(ExpressionInfo expr)
        {
            return new TaskInfo(TaskType.DEBUG_LogWarning)
            {
                Expression = expr
            };
        }

        public static TaskInfo LogError(ExpressionInfo expr)
        {
            return new TaskInfo(TaskType.DEBUG_LogError)
            {
                Expression = expr
            };
        }

        public static TaskInfo Var()
        {
            return new TaskInfo(TaskType.Nop)
            {
                OutputsCount = 1
            };
        }

        public static TaskInfo Repeat(int iterations, params TaskInfo[] sequence)
        { 
            TaskInputInfo input = new TaskInputInfo(null, 0);
            ExpressionInfo setToZero = ExpressionInfo.Val(PrimitiveContract.Create(0));
            
            ExpressionInfo add = ExpressionInfo.Add(
                ExpressionInfo.Val(input),
                ExpressionInfo.Val(PrimitiveContract.Create(1)));
            ExpressionInfo lessThanValue = ExpressionInfo.Lt(
                ExpressionInfo.Val(input),
                ExpressionInfo.Val(PrimitiveContract.Create(iterations)));

            TaskInfo setToZeroTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                OutputsCount = 1,
                Expression = setToZero,
            };
            input.OutputTask = setToZeroTask;

            ExpressionInfo increment = ExpressionInfo.Assign(setToZeroTask, add);
            TaskInfo incrementTask = new TaskInfo
            {
                TaskType = TaskType.EvalExpression,
                Expression = increment,
                OutputsCount = 1,
                Inputs = new[] { input },
            };

            TaskInfo repeatTask = new TaskInfo();
            repeatTask.TaskType = TaskType.Repeat;
            repeatTask.Expression = lessThanValue;

            Array.Resize(ref sequence, sequence.Length + 1);
            sequence[sequence.Length - 1] = incrementTask;
            repeatTask.Children = sequence; 

            TaskInfo task = new TaskInfo
            {
                TaskType = TaskType.Sequence,
                Children = new[]
                {
                    setToZeroTask,
                    repeatTask
                }
            };
            return task;
        }

        public static TaskInfo Procedure(params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Procedure)
            {
                Children = sequence,
            };
        }

        public static TaskInfo Repeat(string debugString, ExpressionInfo expression, params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Repeat)
            {
#if DEBUG_OUTPUT
                DebugString = debugString,
#endif
                Expression = expression,
                Children = sequence,
            };
        }

        public static TaskInfo Repeat(ExpressionInfo expression, params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Repeat)
            {
                Expression = expression,
                Children = sequence,
            };
        }

        public static TaskInfo Sequence(string debugString, params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Sequence)
            {
#if DEBUG_OUTPUT
                DebugString = debugString,
#endif
                Children = sequence,
            };
        }

        public static TaskInfo Sequence(params TaskInfo[] sequence)
        {
            return new TaskInfo(TaskType.Sequence)
            {
                Children = sequence,
            };
        }


        public static TaskInfo Branch(string debugString, ExpressionInfo expression, TaskInfo yes, TaskInfo no = null)
        {
            return new TaskInfo(TaskType.Branch)
            {
#if DEBUG_OUTPUT
                DebugString = debugString,
#endif
                Expression = expression,
                Children = new[] { yes, no }
            };
        }


        public static TaskInfo Branch(ExpressionInfo expression, TaskInfo yes, TaskInfo no = null)
        {
            return new TaskInfo(TaskType.Branch)
            {
                Expression = expression,
                Children = new[] { yes, no }
            };
        }

        public static TaskInfo Break()
        {
            return new TaskInfo(TaskType.Break);
        }

        public static TaskInfo Continue()
        {
            return new TaskInfo(TaskType.Continue);
        }

        public static TaskInfo Return(ExpressionInfo expression = null)
        {
            return new TaskInfo(TaskType.Return)
            {
                Expression = expression,
            };
        }

        public static TaskInfo EvalExpression(string debugString, ExpressionInfo expression)
        {
            return new TaskInfo(TaskType.EvalExpression)
            {
#if DEBUG_OUTPUT
                DebugString = debugString,
#endif
                Expression = expression,
                OutputsCount = 1
            };
        }

        public static TaskInfo EvalExpression(ExpressionInfo expression)
        {
            return new TaskInfo(TaskType.EvalExpression)
            {
                Expression = expression,
                OutputsCount = 1
            };
        }


        public static TaskInfo UnitOrAssetIndex(long unitOrAssetIndex)
        {
            return EvalExpression(ExpressionInfo.PrimitiveVal(unitOrAssetIndex));
        }

        public static TaskInfo Move(TaskInputInfo unitIndexInput, TaskInputInfo pathInput)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new Cmd(CmdCode.Move),
                Inputs = new[] { unitIndexInput, pathInput }
            };
        }

        public static TaskInfo SetHealth(TaskInputInfo unitIndexInput, int health)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new ChangeParamsCmd(CmdCode.SetHealth)
                {
                    IntParams = new[] { health },
                },
                Inputs = new [] { unitIndexInput }
            };
        }

        public static TaskInfo Grow(TaskInputInfo unitIndexInput)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new Cmd(CmdCode.Grow),
                Inputs = new[] { unitIndexInput }
            };
        }

        public static TaskInfo Split4(TaskInputInfo unitIndexInput)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new Cmd(CmdCode.Split4),
                Inputs = new[] { unitIndexInput }
            };
        }

        public static TaskInfo Split(TaskInputInfo unitIndexInput)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new Cmd(CmdCode.Split),
                Inputs = new[] { unitIndexInput }
            };
        }

        public static TaskInfo Diminish(TaskInputInfo unitIndexInput)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new Cmd(CmdCode.Diminish),
                Inputs = new[] { unitIndexInput }
            };
        }

        public static TaskInfo Command(TaskInputInfo unitIndexInput, TaskInputInfo cmdInput)
        {
            return new TaskInfo(TaskType.Command)
            {
                Inputs = new[] { unitIndexInput, cmdInput }
            };
        }

        public static TaskInfo Command(Cmd cmd)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = cmd
            };
        }

        public static TaskInfo Convert(TaskInputInfo unitIndexInput, int type)
        {
            return new TaskInfo(TaskType.Command)
            {
                Cmd = new ChangeParamsCmd(CmdCode.Convert)
                {
                    IntParams = new[] { type }
                },
                Inputs = new[] { unitIndexInput }
            };
        }

        public static TaskInfo PathToRandomLocation(TaskInputInfo unitIndexInput, TaskInputInfo radiusInput)
        {
            return new TaskInfo(TaskType.FindPathToRandomLocation)
            {
                OutputsCount = 1,
                Inputs = new[] { unitIndexInput, radiusInput }
            };
        }

        public static TaskInfo FindPath(TaskInputInfo unitIndex, TaskInputInfo coordinate)
        {
            TaskInfo findPath = new TaskInfo(TaskType.FindPath)
            {
                OutputsCount = 1,
                Inputs = new [] { unitIndex, coordinate }
            };
            return findPath;
        }

        public static TaskInfo SearchFor(TaskType taskType, TaskInputInfo unitIndex)
        {
            TaskInfo searchTask = new TaskInfo(taskType)
            {
                OutputsCount = 2
            };
            TaskInputInfo searchContextInput = new TaskInputInfo
            {
                OutputIndex = 0,
                OutputTask = searchTask,
            };

            searchTask.Inputs = new[] { searchContextInput, unitIndex };
            return searchTask;
        }

        public static TaskInfo SearchForPath(TaskType taskType, TaskInfo pathVar, TaskInputInfo unitIndexInput)
        {
            TaskInfo searchForTask = SearchFor(taskType, unitIndexInput);
#if DEBUG_OUTPUT
            searchForTask.DebugString = "searchForTask";
#endif
            ExpressionInfo searchForSucceded = ExpressionInfo.TaskSucceded(searchForTask);

            TaskInputInfo coordinateInput = new TaskInputInfo(searchForTask, 1);
            TaskInfo findPathTask = FindPath(unitIndexInput, coordinateInput);
            ExpressionInfo findPathSucceded = ExpressionInfo.TaskSucceded(findPathTask);

            TaskInputInfo pathVariableInput = new TaskInputInfo(findPathTask, 0);
            ExpressionInfo assignPathVariable = ExpressionInfo.Assign(
                pathVar,
                ExpressionInfo.Val(pathVariableInput));

            ExpressionInfo whileTrue = ExpressionInfo.PrimitiveVal(true);

            return
                Procedure(
                    Repeat(
                        whileTrue,
                        searchForTask,
                        Branch(
                            searchForSucceded,
                            Sequence(
#if DEBUG_OUTPUT
                                "find path sequence",
#endif
                                findPathTask,
                                Branch(
                                    findPathSucceded,
                                    Sequence(
                                        EvalExpression(assignPathVariable),
                                        Return()
                                    ),
                                    Continue()
                                )
                            ),
                            Return(ExpressionInfo.PrimitiveVal(TaskFailed))
                        )
                    )
                );
        }

        public static TaskInfo MoveToRandomLocation(TaskInputInfo unitIndexInput, int radius = 10, int randomLocationPickAttempts = 20)
        {
            TaskInfo randomRadiusVar = EvalExpression(ExpressionInfo.PrimitiveVal(radius));
            TaskInputInfo radiusInput = new TaskInputInfo(randomRadiusVar, 0);

            TaskInfo randomLocationPath = PathToRandomLocation(unitIndexInput, radiusInput);
            ExpressionInfo randomLocationPathFound = ExpressionInfo.TaskSucceded(randomLocationPath);

            TaskInputInfo pathInput = new TaskInputInfo(randomLocationPath, 0);
            TaskInfo moveTask = Move(unitIndexInput, pathInput);
#if DEBUG_OUTPUT
            moveTask.DebugString = "randomMoveTask";
#endif

            return Procedure
            (
                randomRadiusVar,
                Repeat(
                    randomLocationPickAttempts,
                    randomLocationPath,
                    Branch(
                        randomLocationPathFound,
                        Sequence
                        (
#if DEBUG_OUTPUT
                            "randomMoveTaskSequence",
#endif
                            moveTask,
                            Return(ExpressionInfo.TaskStatus(moveTask))
                        )
                    )
                ),
                Return(ExpressionInfo.PrimitiveVal(TaskFailed))
            );
        }

       
        public static TaskInfo SearchMoveOrRandomMove(TaskType taskType, TaskInputInfo unitIndexInput, int maxRandLocationPicks = 20, TaskInputInfo randMovementsInput = null, int maxRandMovements = 5)
        {
            TaskInfo pathVar = Var();
            TaskInfo searhForPathTask = SearchForPath(taskType, pathVar, unitIndexInput);
            ExpressionInfo pathFound = ExpressionInfo.TaskSucceded(searhForPathTask);
            TaskInputInfo pathInput = new TaskInputInfo(pathVar, 0);
            TaskInfo moveTask = Move(unitIndexInput, pathInput);
#if DEBUG_OUTPUT
            moveTask.DebugString = "moveTask";
#endif

            TaskInfo moveToRandomLocation = MoveToRandomLocation(unitIndexInput, 10, maxRandLocationPicks);
#if DEBUG_OUTPUT
            moveToRandomLocation.DebugString = "moveToRandomLocation";
#endif

            if (randMovementsInput == null)
            {
                return
                    Procedure
                    (
                        pathVar,
                        searhForPathTask,
                        Branch(
#if DEBUG_OUTPUT
                            "if path found",
#endif
                            pathFound,
                            Sequence
                            (
#if DEBUG_OUTPUT
                                "moveTaskSequence",
#endif
                                moveTask,
                                Return(ExpressionInfo.TaskStatus(moveTask))
                            ),
                            Sequence
                            (
                                moveToRandomLocation,
                                Return(ExpressionInfo.TaskStatus(moveToRandomLocation))
                            )
                        )
                    );
            }
            else
            {
                return
                    Procedure
                    (
                        pathVar,
                        searhForPathTask,
                        Branch(
#if DEBUG_OUTPUT
                            "if path found",
#endif
                            pathFound,
                            Sequence
                            (
#if DEBUG_OUTPUT
                                "moveTaskSequence",
#endif
                                EvalExpression(ExpressionInfo.Assign(randMovementsInput.OutputTask, ExpressionInfo.PrimitiveVal(0))),
                                moveTask,
                                Return(ExpressionInfo.TaskStatus(moveTask))
                            ),
                            Sequence
                            (
                                EvalExpression(
                                    "Add 1 to randMovements counter",
                                    ExpressionInfo.Assign(randMovementsInput.OutputTask,
                                    ExpressionInfo.Add(
                                        ExpressionInfo.Val(randMovementsInput),
                                        ExpressionInfo.PrimitiveVal(1)))),
                                Branch(
                                    ExpressionInfo.Lte(ExpressionInfo.Val(randMovementsInput), ExpressionInfo.PrimitiveVal(maxRandMovements)),
                                    Sequence
                                    (
                                        moveToRandomLocation,
                                        Return(ExpressionInfo.TaskStatus(moveToRandomLocation))
                                    ),
                                    Return(ExpressionInfo.PrimitiveVal(TaskFailed))
                                )
                               
                            )
                        )
                    );
            }
          
        }

        public static TaskInfo SearchMoveGrow(TaskInputInfo unitIndexInput, TaskInputInfo playerIdInput, int maxRandLocationPicks = 20, TaskInputInfo randMovementsInput = null, int maxRandMovements = int.MaxValue)
        {
            TaskInfo canGrowTask = EvalExpression(
               ExpressionInfo.UnitCanGrow(ExpressionInfo.Val(unitIndexInput), ExpressionInfo.Val(playerIdInput)));
            TaskInfo searchAndMoveTask = SearchMoveOrRandomMove(TaskType.SearchForGrowLocation, unitIndexInput, maxRandLocationPicks, randMovementsInput, maxRandMovements);
            TaskInfo growTask = Grow(unitIndexInput);

            return SearchMoveExecute(canGrowTask, searchAndMoveTask, growTask);
        }

        public static TaskInfo SearchMoveSplit4(TaskInputInfo unitIndexInput, TaskInputInfo playerIdInput, int maxRandLocationPicks = 20, TaskInputInfo randMovementsInput = null, int maxRandMovements = int.MaxValue)
        {
            TaskInfo canSplit4Task = EvalExpression(
                ExpressionInfo.UnitCanSplit4(ExpressionInfo.Val(unitIndexInput), ExpressionInfo.Val(playerIdInput)));
            TaskInfo searchAndMoveTask = SearchMoveOrRandomMove(TaskType.SearchForSplit4Location, unitIndexInput, maxRandLocationPicks, randMovementsInput, maxRandMovements);
            TaskInfo split4Task = Split4(unitIndexInput);

            return SearchMoveExecute(canSplit4Task, searchAndMoveTask, split4Task);
        }

        public static TaskInfo SearchMoveExecute(
            TaskInfo canRunTask,
            TaskInfo searchAndMoveTask,
            TaskInfo runTask)
        {
            TaskInputInfo canRunInput = new TaskInputInfo
            {
                OutputTask = canRunTask,
                OutputIndex = 0,
            };

            ExpressionInfo invalidLocation = ExpressionInfo.Eq(
                ExpressionInfo.PrimitiveVal(CmdResultCode.Fail_InvalidLocation),
                ExpressionInfo.Val(canRunInput));

            ExpressionInfo collapsedOrBlocked = ExpressionInfo.Eq(
                ExpressionInfo.PrimitiveVal(CmdResultCode.Fail_CollapsedOrBlocked),
                ExpressionInfo.Val(canRunInput));

            ExpressionInfo canRun = ExpressionInfo.Eq(
                ExpressionInfo.PrimitiveVal(CmdResultCode.Success),
                ExpressionInfo.Val(canRunInput));

            ExpressionInfo whileTrue = ExpressionInfo.PrimitiveVal(true);

            return
                Procedure(
                    Repeat
                    (
                        whileTrue,
                        canRunTask,
                        Branch(
                            canRun,
                            Sequence(
                                runTask,
                                Return(ExpressionInfo.TaskStatus(runTask))
                            ),
                            Branch(
                                ExpressionInfo.Or(invalidLocation, collapsedOrBlocked),
                                Sequence(
                                    searchAndMoveTask,
                                    Branch(
                                        ExpressionInfo.TaskSucceded(searchAndMoveTask),
                                        Continue(),
                                        Return(ExpressionInfo.TaskStatus(searchAndMoveTask))
                                    )
                                ),
                                Return(ExpressionInfo.PrimitiveVal(TaskFailed))
                            )
                        )
                    )
                );
        }

        public static TaskInfo EatGrowSplit4(int maxRandLocationPicks = 20, int maxRandMovements = 5)
        {
            return EatGrowSplit4(new TaskInputInfo(), new TaskInputInfo(), maxRandLocationPicks, maxRandMovements);
        }

        public static TaskInfo EatGrowSplit4(TaskInputInfo unitIndexInput, TaskInputInfo playerIdInput, int maxRandLocationPicks = 20, int maxRandMovements = 5)
        {
            TaskInfo canGrowTask = EvalExpression(
                 ExpressionInfo.UnitCanGrow(ExpressionInfo.Val(unitIndexInput), ExpressionInfo.Val(playerIdInput)));
#if DEBUG_OUTPUT
            canGrowTask.DebugString = "canGrowTask";
#endif
            TaskInputInfo canGrowInput = new TaskInputInfo
            {
                OutputTask = canGrowTask,
                OutputIndex = 0,
            };

            ExpressionInfo needMoreFood = ExpressionInfo.Eq(
                    ExpressionInfo.PrimitiveVal(CmdResultCode.Fail_NeedMoreResources),
                    ExpressionInfo.Val(canGrowInput));

            ExpressionInfo whileTrue = ExpressionInfo.PrimitiveVal(true);

            TaskInfo randomMovementsVar = Var();
            TaskInputInfo randomMovementsInput = new TaskInputInfo(randomMovementsVar, 0);

            TaskInfo eatTask = SearchMoveOrRandomMove(TaskType.SearchForFood, unitIndexInput, maxRandLocationPicks, randomMovementsInput, maxRandMovements);
#if DEBUG_OUTPUT
            eatTask.DebugString = "eatTask";
#endif
            TaskInfo growTask = SearchMoveGrow(unitIndexInput, playerIdInput);
#if DEBUG_OUTPUT
            growTask.DebugString = "growTask";
#endif
            TaskInfo split4Task = SearchMoveSplit4(unitIndexInput, playerIdInput);
#if DEBUG_OUTPUT
            split4Task.DebugString = "split4Task";
#endif

            TaskInfo eatSplitGrow = 
                Procedure(
                    randomMovementsVar,
                    EvalExpression(ExpressionInfo.Assign(randomMovementsVar, ExpressionInfo.PrimitiveVal(0))),
                    Repeat
                    (
                        whileTrue,
                        canGrowTask,
                        Branch(
#if DEBUG_OUTPUT
                            "if need more food",
#endif
                            needMoreFood,
                            Sequence(
                                eatTask,
                                Branch(
#if DEBUG_OUTPUT
                                    "if eat task succeded",
#endif
                                    ExpressionInfo.TaskSucceded(eatTask),
                                    Continue(),
                                    Return(ExpressionInfo.TaskStatus(eatTask))
                                )
                            ),
                            Sequence(
#if DEBUG_OUTPUT
                                "grow, split4",
#endif
                                growTask,
                                Branch(
#if DEBUG_OUTPUT
                                    "if grow task succeded",
#endif
                                    ExpressionInfo.TaskSucceded(growTask),
                                    Sequence(
#if DEBUG_OUTPUT
                                        "split4 and return status",
#endif
                                        split4Task,
                                        Return(ExpressionInfo.TaskStatus(split4Task))
                                    ),
                                    Return(ExpressionInfo.TaskStatus(growTask))
                                )
                            )
                        )
                    )
                );

            eatSplitGrow.Inputs = new[] { unitIndexInput, playerIdInput };
            return eatSplitGrow;
        }

        public static TaskInfo Define(Coordinate coordinate)
        {
            TaskInputInfo unitIndexInput = new TaskInputInfo();
            TaskInputInfo playerIndexInput = new TaskInputInfo();

            TaskInfo taskInfo = EvalExpression(ExpressionInfo.PrimitiveVal(new[] { coordinate }));
            taskInfo.Inputs = new[] { unitIndexInput, playerIndexInput };

            return taskInfo;
        }

        public static TaskInfo DefineConvertCmd(int targetType)
        {
            TaskInputInfo unitIndexInput = new TaskInputInfo();
            TaskInputInfo playerIndexInput = new TaskInputInfo();

            TaskInfo taskInfo = EvalExpression(ExpressionInfo.Val(new ChangeParamsCmd(CmdCode.Convert) { IntParams = new[] { targetType } }));
            taskInfo.Inputs = new[] { unitIndexInput, playerIndexInput };

            return taskInfo;
        }

        public static TaskInfo DefineCanConvertExpr( int targetType)
        {
            TaskInputInfo unitIndexInput = new TaskInputInfo();
            TaskInputInfo playerIndexInput = new TaskInputInfo();

            ExpressionInfo canConvert = ExpressionInfo.UnitCanConvert(
                ExpressionInfo.Val(unitIndexInput),
                ExpressionInfo.Val(playerIndexInput),
                ExpressionInfo.PrimitiveVal(targetType));

            TaskInfo taskInfo = EvalExpression(ExpressionInfo.Val(canConvert));
            taskInfo.Inputs = new[] { unitIndexInput, playerIndexInput };

            return taskInfo;
        }

        public static TaskInfo MoveAndConvertTo(Coordinate targetCoordinate, int targetType)
        {
            TaskInputInfo unitIndexInput = new TaskInputInfo();
            TaskInputInfo playerIndexInput = new TaskInputInfo();
            TaskInfo task = MoveAndConvertTo(unitIndexInput, playerIndexInput, targetCoordinate, targetType);
            task.Inputs = new[] { unitIndexInput, playerIndexInput };
            return task;
        }

       
        public static TaskInfo MoveAndConvertTo(TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput, Coordinate targetCoordinate, int targetType)
        {
            TaskInfo defineGoalTask = EvalExpression(ExpressionInfo.Val(new[] { targetCoordinate } ));
            TaskInfo defineCmd = EvalExpression(ExpressionInfo.Val(new ChangeParamsCmd(CmdCode.Convert) { IntParams = new[] { targetType } }));

            ExpressionInfo canConvert = ExpressionInfo.UnitCanConvert(
                ExpressionInfo.Val(unitIndexInput),
                ExpressionInfo.Val(playerIndexInput),
                ExpressionInfo.PrimitiveVal(targetType));

            TaskInfo defineExpr = EvalExpression(ExpressionInfo.Val(canConvert));

            TaskInfo moveAndConvertTo = MoveToAndExecCmd(
                unitIndexInput, 
                playerIndexInput, 
                new TaskInputInfo(defineCmd, 0), 
                new TaskInputInfo(defineExpr, 0), 
                new TaskInputInfo(defineGoalTask, 0));

            TaskInfo task = Procedure
            (
                defineGoalTask,
                defineCmd,
                defineExpr,
                moveAndConvertTo,
                Return(ExpressionInfo.TaskStatus(moveAndConvertTo))  
            );
            
            return task;
        }

        public static TaskInfo MoveToAndExecCmd()
        {
            TaskInputInfo unitIndexInput = new TaskInputInfo();
            TaskInputInfo playerIndexInput = new TaskInputInfo();
            TaskInputInfo cmdInput = new TaskInputInfo();
            TaskInputInfo canExecCmdExpressionInput = new TaskInputInfo();
            TaskInputInfo atCoordInput = new TaskInputInfo();
            
            TaskInfo task = MoveToAndExecCmd(unitIndexInput, playerIndexInput, cmdInput, canExecCmdExpressionInput, atCoordInput);
            task.Inputs = new[] { unitIndexInput, playerIndexInput, cmdInput, canExecCmdExpressionInput, atCoordInput };
            return task;
        }

        public static TaskInfo MoveToAndExecCmd(TaskInputInfo unitIndexInput, TaskInputInfo playerIndexInput, TaskInputInfo cmdInput, TaskInputInfo canExecuteExpressionInput, TaskInputInfo atCoordinateInput)
        {
            TaskInfo findPathTask = FindPath(unitIndexInput, atCoordinateInput);
            TaskInfo moveTask = Move(unitIndexInput, new TaskInputInfo(findPathTask, 0));
            TaskInfo execCmdTask = Command(unitIndexInput, cmdInput);
            TaskInfo evalExpression = EvalExpression(ExpressionInfo.Val(canExecuteExpressionInput));
            
            TaskInfo task = Procedure
                (
                    findPathTask,
                    moveTask,

                    Branch(ExpressionInfo.Or(ExpressionInfo.TaskFailed(findPathTask), ExpressionInfo.TaskFailed(moveTask)),
                        Return(ExpressionInfo.PrimitiveVal(TaskFailed))),

                    evalExpression,

                    Branch(ExpressionInfo.NotEq(ExpressionInfo.Val(new TaskInputInfo(evalExpression, 0)), ExpressionInfo.PrimitiveVal(CmdResultCode.Success)),
                        Return(ExpressionInfo.PrimitiveVal(TaskFailed))),
                    
                    Log("Can RuntTask Completed"),
                    execCmdTask,

                    Return(ExpressionInfo.TaskStatus(execCmdTask))
                );

            task.Inputs = new[] { unitIndexInput, playerIndexInput };
            return task;
        }

    }
}
