#define DEBUG_OUTPUT
using ProtoBuf;
using System.Runtime.Serialization;
using System.Collections;
using System;

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
 
        //Complex search expressions
        public const int EnemyVisible = 200;
        public const int FoodVisible = 201;

        //Task
        public const int TaskStatusCode = 500;
        public const int TaskSucceded = 501;
        public const int CmdResultCode = 502;
        public const int CmdSucceded = 503;
        public const int CmdHardFailed = 504;

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
    }

    public enum TaskState
    {
        Idle,
        Active,
        Completed,
        //Failed,
        Terminated
    }

    [ProtoContract(AsReferenceDefault = true)]
    public class ExpressionInfo
    {
        [ProtoMember(1)]
        public int m_code;

        [ProtoMember(2, DynamicType = true, AsReference = true)]
        public object m_value;

        [ProtoMember(3, AsReference = true)]
        public ExpressionInfo[] m_children;

        public ExpressionInfo()
        {

        }

        public ExpressionInfo(int code, object value, params ExpressionInfo[] children)
        {
            m_code = code;
            m_value = value;
            m_children = children;
        }

        public int Code
        {
            get { return m_code; }
            set { m_code = value; }
        }

        public object Value
        {
            get { return m_value; }
            set { m_value = value; }
        }

        public ExpressionInfo[] Children
        {
            get { return m_children; }
            set { m_children = value; }
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

        public static ExpressionInfo TaskSucceded(TaskInfo task)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.TaskSucceded,
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
        public int TaskId;

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

        public TaskStateInfo(int taskId, int playerId, TaskState state, int statusCode)
        {
            TaskId = taskId;
            PlayerId = playerId;
            State = state;
            StatusCode = statusCode;
        }
    }

    [ProtoContract(AsReferenceDefault = true)]
    public class TaskInputInfo
    {
        [ProtoMember(1, AsReference = true)]
        public TaskInfo Scope;

        [ProtoMember(2, AsReference = true)]
        public TaskInfo OutputTask;

        [ProtoMember(3)]
        public int OutputIndex;

        public void SetScope()
        {
            Scope = OutputTask.Parent;
        }

        public TaskInputInfo()
        {

        }

        public TaskInputInfo(TaskInfo task, int outputIndex)
        {
            OutputTask = task;
            OutputIndex = outputIndex;
        }

        public TaskInputInfo(TaskInputInfo taskInput)
        {
            Scope = taskInput.Scope;
            OutputTask = taskInput.OutputTask;
            OutputIndex = taskInput.OutputIndex;
        }
    }

    public enum TaskTemplateType
    {
        EatGrowSplit4,
    }

    [ProtoContract]
    public class TaskTemplateInfo
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public int Row; 

        [ProtoMember(3)]
        public int Col;

        [ProtoMember(4)]
        public TaskTemplateType Type;

        public int Index
        {
            get { return Row * 5 + Col; }
        }
    }

    [ProtoContract(AsReferenceDefault = true)]
    public class TaskInfo
    {
        public const int TaskSucceded = 0;
        public const int TaskFailed = 1;

        [ProtoMember(1)]
        public int m_taskId;
        [ProtoMember(2)]
        public TaskType m_taskType;
        [ProtoMember(3, AsReference = true)]
        public Cmd m_cmd;
        [ProtoMember(4)]
        public TaskState m_state;
        [ProtoMember(6, AsReference = true)]
        public TaskInfo[] m_children;
        [ProtoMember(7, AsReference = true)]
        public ExpressionInfo m_expression;
        [ProtoMember(8)]
        public bool m_requiresClientSidePreprocessing;
        [ProtoMember(9, AsReference = true)]
        public TaskInputInfo[] m_inputs;
        [ProtoMember(10)]
        public int m_outputsCount;
        private int m_playerIndex = -1;
        #if DEBUG_OUTPUT
        public string DebugString;
        #endif

        public TaskInfo(TaskType taskType, Cmd cmd, TaskState state, ExpressionInfo expression, TaskInfo parent)
        {
            m_taskType = taskType;
            m_cmd = cmd;
            m_state = state;
            m_expression = expression;
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
            m_playerIndex = playerIndex;
        }

        public TaskInfo(TaskType type)
            : this(type, new Cmd(CmdCode.Nop), TaskState.Idle, null, null)
        {
        }

        public TaskInfo(TaskType type, TaskState state)
           : this(type, new Cmd(CmdCode.Nop), state, null, null)
        {
        }

        public TaskInfo()
        {
        }

        public TaskInfo(TaskInfo taskInfo, bool isCmdTask)
        {
            m_taskId = taskInfo.TaskId;
            m_taskType = taskInfo.TaskType;
            m_cmd = taskInfo.Cmd;
            if(isCmdTask)
            {
                m_state = TaskState.Idle;
            }
            else
            {
                m_state = taskInfo.State;
                m_children = taskInfo.Children;
                m_expression = taskInfo.Expression;
                m_requiresClientSidePreprocessing = taskInfo.RequiresClientSidePreprocessing;
                m_inputs = taskInfo.Inputs;
                m_outputsCount = taskInfo.OutputsCount;
            }

            m_playerIndex = PlayerIndex;
        }

        public void Reset()
        {
            State = TaskState.Idle;
            StatusCode = TaskSucceded;
            PreprocessedCmd = null;
        }
    
        public int TaskId
        {
            get { return m_taskId; }
            set { m_taskId = value; }
        }

        public TaskType TaskType
        {
            get { return m_taskType; }
            set { m_taskType = value; }
        }

        public Cmd Cmd
        {
            get { return m_cmd; }
            set { m_cmd = value; }
        }

        public TaskState State
        {
            get { return m_state; }
            set
            {
                m_state = value;
            }
        }

        public TaskInfo Parent { get; set; }

        public TaskInfo[] Children
        {
            get { return m_children; }
            set { m_children = value; }
        }

        public ExpressionInfo Expression
        {
            get { return m_expression; }
            set { m_expression = value; }
        }

        public int PlayerIndex
        {
            get { return m_playerIndex; }
            set { m_playerIndex = value; }
        }

        public TaskInputInfo[] Inputs
        {
            get { return m_inputs; }
            set
            {
                m_inputs = value;
            }
        }

        public int OutputsCount
        {
            get { return m_outputsCount; }
            set { m_outputsCount = value; }
        }

        public bool RequiresClientSidePreprocessing
        {
            get { return m_requiresClientSidePreprocessing; }
            set { m_requiresClientSidePreprocessing = value; }
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

        [OnDeserialized]
        public void OnDeserializedMethod(StreamingContext context)
        {
            SetParents(this, false);
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
                                Return(ExpressionInfo.TaskStatus(moveTask))
                            ),
                            Sequence
                            (
                                EvalExpression(ExpressionInfo.Assign(randMovementsInput.OutputTask,
                                    ExpressionInfo.Add(
                                        ExpressionInfo.Val(randMovementsInput),
                                        ExpressionInfo.PrimitiveVal(1)))),

                                Branch(
                                    ExpressionInfo.Gte(ExpressionInfo.Val(randMovementsInput), ExpressionInfo.PrimitiveVal(maxRandMovements)),
                                    Return(ExpressionInfo.PrimitiveVal(TaskFailed)),
                                    Sequence
                                    (
                                        moveToRandomLocation,
                                        Return(ExpressionInfo.TaskStatus(moveToRandomLocation))
                                    )
                                )
                            )
                        )
                    );
            }
          
        }

        public static TaskInfo SearchMoveGrow(TaskInputInfo unitIndexInput, TaskInputInfo playerIdInput)
        {
            TaskInfo canGrowTask = EvalExpression(
               ExpressionInfo.UnitCanGrow(ExpressionInfo.Val(unitIndexInput), ExpressionInfo.Val(playerIdInput)));
            TaskInfo searchAndMoveTask = SearchMoveOrRandomMove(TaskType.SearchForGrowLocation, unitIndexInput);
            TaskInfo growTask = Grow(unitIndexInput);

            return SearchMoveExecute(canGrowTask, searchAndMoveTask, growTask);
        }

        public static TaskInfo SearchMoveSplit4(TaskInputInfo unitIndexInput, TaskInputInfo playerIdInput)
        {
            TaskInfo canSplit4Task = EvalExpression(
                ExpressionInfo.UnitCanSplit4(ExpressionInfo.Val(unitIndexInput), ExpressionInfo.Val(playerIdInput)));
            TaskInfo searchAndMoveTask = SearchMoveOrRandomMove(TaskType.SearchForSplit4Location, unitIndexInput);
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

        public static TaskInfo EatGrowSplit4()
        {
            return EatGrowSplit4(new TaskInputInfo(), new TaskInputInfo());
        }

        public static TaskInfo EatGrowSplit4(TaskInputInfo unitIndexInput, TaskInputInfo playerIdInput, int maxRandMovements = 5)
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

            const int maxRandLocationPicks = 20;

           // TaskInfo randMovementsCount = Var();
         //   TaskInputInfo randMovementsInput = new TaskInputInfo(randMovementsCount, 0);
       
            TaskInfo eatTask = SearchMoveOrRandomMove(TaskType.SearchForFood, unitIndexInput, maxRandLocationPicks, null);
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
                    //randMovementsCount,
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

    }
}
