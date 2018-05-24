using ProtoBuf;
using System.Runtime.Serialization;

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
        public const int EnemyVisible = 100;
        public const int FoodVisible = 101;
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
        Aborted
    }

    [ProtoContract]
    public class Expression
    {
        [ProtoMember(1)]
        private int m_code;

        [ProtoMember(2, DynamicType = true)]
        private object m_value;

        [ProtoMember(3)]
        private Expression[] m_children;

        public Expression()
        {

        }

        public Expression(int code, object value, params Expression[] children)
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

        public Expression[] Chidren
        {
            get { return m_children; }
            set { m_children = value; }
        }
    }

    [ProtoContract]
    public class TaskStateInfo
    {
        [ProtoMember(1)]
        public int TaskId;

        [ProtoMember(2)]
        public TaskState State;

        public TaskStateInfo(int taskId, TaskState state)
        {
            TaskId = taskId;
            State = state;
        }
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
        private Expression m_expression;

        private TaskInfo m_parent;

        public TaskInfo()
        {
        }

        public TaskInfo(TaskType type, Cmd cmd, TaskState state, Expression expression, TaskInfo parent)
        {
            m_taskType = type;
            m_cmd = cmd;
            m_state = state;
            m_expression = expression;
            m_parent = parent;
        }

        public TaskInfo(TaskType type, Cmd cmd, TaskState state, TaskInfo parent) 
            : this(type, cmd, state, null, parent)
        {
        }

        public TaskInfo(TaskType type, Cmd cmd)
            : this(type, cmd, TaskState.Idle, null, null)
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

        public TaskInfo Parent
        {
            get { return m_parent; }
            set { m_parent = value; }
        }

        public TaskInfo[] Children
        {
            get { return m_children; }
            set { m_children = value; }
        }

        public Expression Expression
        {
            get { return m_expression; }
            set { m_expression = value; }
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
