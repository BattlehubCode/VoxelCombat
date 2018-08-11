
using ProtoBuf;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Battlehub.VoxelCombat
{
    

    public class SerializersPool : Pool<ProtobufSerializer>
    {
        public SerializersPool(int size = 100)
        {
            Initialize(size);
        }

        protected override ProtobufSerializer Instantiate(int index)
        {
            ProtobufSerializer serializer = new ProtobufSerializer();
            return serializer;
        }

        protected override void Destroy(ProtobufSerializer obj)
        {
        }
    }


    [ProtoBuf.ProtoContract]
    public class NilContainer { }

    public class ProtobufSerializer
    {
#if !UNITY_EDITOR && !UNITY_WSA && !SERVER 
        private VCTypeModel model = new VCTypeModel();
#else
        private ProtoBuf.Meta.RuntimeTypeModel model = new TypeModelCreator().Create();
#endif
        private static object m_syncRoot = new object();

        public ProtobufSerializer()
        {
            lock(m_syncRoot)
            {
                Init();
            }
        }

        private void Init()
        {
            model.DynamicTypeFormatting += (sender, args) =>
            {
                if (args.FormattedName == null)
                {
                    return;
                }
                string typename = args.FormattedName;
                typename = Regex.Replace(typename, @", Version=\d+.\d+.\d+.\d+", string.Empty);
                typename = Regex.Replace(typename, @", Culture=\w+", string.Empty);
                typename = Regex.Replace(typename, @", PublicKeyToken=\w+", string.Empty);
                typename = typename.Replace(", Battlehub.VoxelCombat.Server", string.Empty);
                typename = typename.TrimEnd(' ');
                typename = typename.TrimEnd(',');
                Type type = Type.GetType(typename);
                if (type == null)
                {
                    args.Type = typeof(NilContainer);
                }
                else
                {
                    args.Type = type;
                }
            };

#if UNITY_EDITOR  || UNITY_WSA || SERVER
            model.CompileInPlace();
#endif
        }

        public TData DeepClone<TData>(TData data)
        {
            return (TData)model.DeepClone(data);
        }

        public TData Deserialize<TData>(byte[] b)
        {
            using (var stream = new MemoryStream(b))
            {
                TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
                return deserialized;
            }
        }

        public byte[] Serialize<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                model.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
    /*
    [ProtoBuf.ProtoContract]
    public class NilContainer { }

    public static class ProtobufSerializer
    {
#if !UNITY_EDITOR && !UNITY_WSA && !SERVER 
        private static VCTypeModel model = new VCTypeModel();
#else
        private static ProtoBuf.Meta.RuntimeTypeModel model = new TypeModelCreator().Create();
#endif
        static ProtobufSerializer()
        {
            model.DynamicTypeFormatting += (sender, args) =>
            {
                if (args.FormattedName == null)
                {
                    return;
                }
                string typename = args.FormattedName;
                typename = Regex.Replace(typename, @", Version=\d+.\d+.\d+.\d+", string.Empty);
                typename = Regex.Replace(typename, @", Culture=\w+", string.Empty);
                typename = Regex.Replace(typename, @", PublicKeyToken=\w+", string.Empty);
                typename = typename.Replace(", Battlehub.VoxelCombat.Server", string.Empty);
                typename = typename.TrimEnd(' ');
                typename = typename.TrimEnd(',');
                Type type = Type.GetType(typename);
                if (type == null)
                {
                    args.Type = typeof(NilContainer);
                }
                else
                {
                    args.Type = type;
                }
            };

#if UNITY_EDITOR  || UNITY_WSA || SERVER
            model.CompileInPlace();
#endif
        }

        public static TData DeepClone<TData>(TData data)
        {
            return (TData)model.DeepClone(data);
        }

        public static TData Deserialize<TData>(byte[] b)
        {
            using (var stream = new MemoryStream(b))
            {
                TData deserialized = (TData)model.Deserialize(stream, null, typeof(TData));
                return deserialized;
            }
        }

        public static byte[] Serialize<TData>(TData data)
        {
            using (var stream = new MemoryStream())
            {
                model.Serialize(stream, data);
                stream.Flush();
                stream.Position = 0;
                return stream.ToArray();
            }
        }
    }
    */

}