
using System;
using System.IO;

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

                if (Type.GetType(args.FormattedName) == null)
                {
                    args.Type = typeof(NilContainer);
                }
            };

#if UNITY_EDITOR
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
