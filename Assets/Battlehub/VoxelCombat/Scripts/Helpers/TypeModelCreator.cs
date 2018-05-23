using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Linq;

namespace Battlehub.VoxelCombat
{
    public abstract class PrimitiveContract
    {
        public static PrimitiveContract<T> Create<T>(T value)
        {
            return new PrimitiveContract<T>(value);
        }

        public static PrimitiveContract Create(Type type)
        {
            Type d1 = typeof(PrimitiveContract<>);
            Type constructed = d1.MakeGenericType(type);
            return (PrimitiveContract)Activator.CreateInstance(constructed);
        }

        public object ValueBase
        {
            get { return ValueImpl; }
            set { ValueImpl = value; }
        }
        protected abstract object ValueImpl { get; set; }
        protected PrimitiveContract() { }
    }

    [ProtoContract]
    public class PrimitiveContract<T> : PrimitiveContract
    {
        public PrimitiveContract() { }
        public PrimitiveContract(T value) { Value = value; }
        [ProtoMember(1)]
        public T Value { get; set; }
        protected override object ValueImpl
        {
            get { return Value; }
            set { Value = (T)value; }
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if(obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            PrimitiveContract<T> other = (PrimitiveContract<T>)obj;
            if (ValueImpl == null)
            {
                if(other.ValueImpl == null)
                {
                    return true;
                }
                return false;
            }
            return Value.Equals(other.Value);
        }
    }

    public class TypeModelCreator
    {
        public RuntimeTypeModel Create()
        {
            RuntimeTypeModel model = TypeModel.Create();

            RegisterTypes(model);

            return model;
        }

        protected void RegisterTypes(RuntimeTypeModel model)
        {
            Type[] serializableTypes = Reflection.GetAllFromCurrentAssembly().Where(type => type.IsDefined(typeof(ProtoContractAttribute), false)).ToArray();

            foreach (Type type in serializableTypes)
            {
                if (type.IsGenericType())
                {
                    continue;
                }

                try
                {
                    model.Add(type, true);
                }
                catch(Exception  e)
                {
#if UNITY_EDITOR
                    UnityEngine.Debug.LogErrorFormat("model.Add({0}, true) failed {1}", type.FullName, e);
#endif
                    throw;
                }
            }

            Type[] primitiveTypes = new[] {
                typeof(bool),
                typeof(char),
                typeof(byte),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(string),
                typeof(float),
                typeof(double),
                typeof(decimal) };

            foreach (Type type in primitiveTypes)
            {
                if (type.IsGenericType())
                {
                    continue;
                }
                model.Add(typeof(PrimitiveContract<>).MakeGenericType(type), true);
            }
        }
    }
}
