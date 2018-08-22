using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using System.Threading;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

namespace Battlehub.VoxelCombat
{
    [ProtoBuf.ProtoContract]
    public class NilContainer2 { }

    public class ProtobufSerializer2
    {
#if !UNITY_EDITOR && !UNITY_WSA && !SERVER 
        private static VCTypeModel model = new VCTypeModel();
#else
        private ProtoBuf.Meta.RuntimeTypeModel model;
#endif
        public ProtobufSerializer2()
        {
            model = new TypeModelCreator().Create();
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
                    args.Type = typeof(NilContainer2);
                }
                else
                {
                    args.Type = type;
                }
            };

#if UNITY_EDITOR || UNITY_WSA || SERVER
            model.MetadataTimeoutMilliseconds *= 1000;
            model.CompileInPlace();

#endif
        }

        public TData DeepClone<TData>(TData data)
        {
            return (TData)model.DeepClone(data);
        }

        public TData Deserialize<TData>(byte[] b) where TData : new()
        {
            using (var stream = new MemoryStream(b))
            {
                TData result = new TData();
                TData deserialized = (TData)model.Deserialize(stream, result, typeof(TData));
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

    public class ProtobufConcurrentDeserialization
    {

        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {

            Thread[] t = new Thread[1];
            for (int i = 0; i < t.Length; ++i)
            {
                t[i] = new Thread(ThreadProc);
                t[i].Start(new ProtobufSerializer2());
            }

            //ThreadProc();
            //Assert.Pass();

            while (true)
            {
                if(t.All(thread => !thread.IsAlive))
                {
                    Assert.Pass();
                    break;
                }
                yield return null;
            }
        }

        private void ThreadProc(object s)
        {
            ProtobufSerializer2 serializer = (ProtobufSerializer2)s;
         

            MapRoot mapRoot = new MapRoot(8);

            //for (int i = 0; i < 40; ++i)
            for (int i = 0; i < 15; ++i)
            {
                // lock(m_lock)
                {
                    byte[] data = serializer.Serialize(mapRoot);
                    serializer.Deserialize<MapRoot>(data);
                }

            }
        }
    }
}

