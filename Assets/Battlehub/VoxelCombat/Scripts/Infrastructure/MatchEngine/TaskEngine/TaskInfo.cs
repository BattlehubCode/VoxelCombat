using ProtoBuf;
using System.Runtime.Serialization;
using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public class ExpressionCode
    {
        public const int Var = 1;

        //Binary expressions
        public const int And = 2;
        public const int Or = 3;
        public const int Not = 4;

        //Comparation expression
        public const int Eq = 10;
        public const int NotEq = 11;
        public const int Lt = 12;
        public const int Lte = 13;
        public const int Gt = 14;
        public const int Gte = 15;

        //Complex expressions
        public const int UnitExists = 100;
        public const int UnitCoordinate = 101;
        public const int UnitState = 102;
        public const int TaskStatus = 120;

        //Complex search expressions
        public const int EnemyVisible = 200;
        public const int FoodVisible = 201;
    }

    public enum TaskType
    {
        Command = 1,
        Sequence = 2,
        Branch = 3,
        Repeat = 4,
    }

    public enum TaskState
    {
        Idle,
        Active,
        Completed,
        Failed,
        Terminated
    }

    [ProtoContract]
    public class ExpressionInfo
    {
        [ProtoMember(1)]
        private int m_code;

        [ProtoMember(2, DynamicType = true)]
        private object m_value;

        [ProtoMember(3)]
        private ExpressionInfo[] m_children;

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

        public static ExpressionInfo PrimitiveVar<T>(T val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Var,
                Value = PrimitiveContract.Create(val)
            };
        }

        public static ExpressionInfo Var(object val)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.Var,
                Value = val
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

        public static ExpressionInfo TaskStatus(ExpressionInfo completedCheck, ExpressionInfo failedCheck)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.TaskStatus,
                Children = new[] { completedCheck, failedCheck }
            };
        }

        public static ExpressionInfo UnitExists(long unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitExists,
                Children = new[] { PrimitiveVar(unitId), PrimitiveVar(playerId)}
            };
        }

        public static ExpressionInfo UnitState(long unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitState,
                Children = new[] { PrimitiveVar(unitId), PrimitiveVar(playerId) }
            };
        }

        public static ExpressionInfo UnitCoordinate(long unitId, int playerId)
        {
            return new ExpressionInfo
            {
                Code = ExpressionCode.UnitCoordinate,
                Children = new[] { PrimitiveVar(unitId), PrimitiveVar(playerId) }
            };
        }

        public static ExpressionInfo MoveTaskExpression(long unitId, int playerId, Coordinate coordinate)
        {
            ExpressionInfo completedExpression =
                And(UnitExists(unitId, playerId),
                    And(Eq(UnitState(unitId, playerId), PrimitiveVar(VoxelDataState.Idle)),
                        Eq(UnitCoordinate(unitId, playerId), Var(coordinate))));

            ExpressionInfo failedExpression =
                 Or(Not(UnitExists(unitId, playerId)),
                    And(NotEq(UnitState(unitId, playerId), PrimitiveVar(VoxelDataState.Moving)),
                        NotEq(UnitState(unitId, playerId), PrimitiveVar(VoxelDataState.SearchingPath))));

            return TaskStatus(completedExpression, failedExpression);
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

        public TaskStateInfo()
        {

        }

        public TaskStateInfo(int taskId, int playerId, TaskState state)
        {
            TaskId = taskId;
            PlayerId = playerId;
            State = state;
        }
    }

    [ProtoContract]
    public struct TaskInputInfo
    {
        [ProtoMember(1)]
        public int ScopeId;

        [ProtoMember(2)]
        public int ConnectedTaskId;

        [ProtoMember(3)]
        public int OuputIndex;
    }

    [ProtoContract]
    public class TaskInfo
    {
        [ProtoMember(1)]
        private int m_taskId;
        [ProtoMember(2)]
        private TaskType m_taskType;
        [ProtoMember(3)]
        private Cmd m_cmd;
        [ProtoMember(4)]
        private TaskState m_state;
        [ProtoMember(6)]
        private TaskInfo[] m_children;
        [ProtoMember(7)]
        private ExpressionInfo m_expression;
        [ProtoMember(8)]
        private bool m_requiresClientSidePreprocessing;
        [ProtoMember(9)]
        private TaskInputInfo[] m_inputs;
        [ProtoMember(10)]
        private int m_outputsCount;

        public Cmd m_preprocessedCmd;
        private int m_playerIndex = -1;

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
            set { m_state = value; }
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
            set { m_inputs = value; }
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
            get { return m_preprocessedCmd; }
            set { m_preprocessedCmd = value; }
        }

        [OnDeserialized]
        public void OnDeserializedMethod(StreamingContext context)
        {
            if(Children != null)
            {
                for(int i = 0; i < Children.Length; ++i)
                {
                    Children[i].Parent = this;
                }
            }
        }
    }
}
