
using ProtoBuf;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Battlehub.VoxelCombat
{

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
}
