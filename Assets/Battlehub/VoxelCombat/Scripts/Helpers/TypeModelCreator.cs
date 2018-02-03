using ProtoBuf;
using ProtoBuf.Meta;
using System;
using System.Linq;

namespace Battlehub.VoxelCombat
{
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
        }
    }
}
