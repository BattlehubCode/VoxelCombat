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

#if UNITY_EDITOR  || UNITY_WSA || SERVER
            try
            {
                model.CompileInPlace();

                int i = 0;
                i++;
            }
            catch(Exception e)
            {
                int i = 0;
                i++;
            }
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

    public class ProtobufConcurrentDeserialization
    {

        // A UnityTest behaves like a coroutine in PlayMode
        // and allows you to yield null to skip a frame in EditMode
        [UnityTest]
        public IEnumerator NewTestScriptWithEnumeratorPasses()
        {

            Thread[] t = new Thread[5];
            for (int i = 0; i < t.Length; ++i)
            {
                t[i] = new Thread(ThreadProc);
                t[i].Start();
            }

            

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

        private object m_lock = new object();

        private void ThreadProc()
        {
            ProtobufSerializer2 serializer;
            lock (m_lock)
            {
                serializer = new ProtobufSerializer2();
            }
            
            MapRoot mapRoot = new MapRoot(6);


            for (int i = 0; i < 250; ++i)
            {
                byte[] data = serializer.Serialize(mapRoot);
                MapRoot clone = serializer.Deserialize<MapRoot>(data);
            }
        }
    }
}

