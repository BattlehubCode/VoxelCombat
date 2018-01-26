using ProtoBuf.Meta;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public static class TypeModelCreatorMenu
    {
        private const string Root = "Battlehub/VoxelCombat";
        private const string TypeModelPath = @"/" + Root + "/Deps/";

        [MenuItem("Tools/VoxelCombat/Build Type Model")]
        public static void CreateTypeModel()
        {
            RuntimeTypeModel model = new TypeModelCreator().Create();
            string dllName = "VCTypeModel.dll";

            model.Compile(new RuntimeTypeModel.CompilerOptions() { OutputPath = dllName, TypeName = "VCTypeModel" });

            string srcPath = Application.dataPath.Remove(Application.dataPath.LastIndexOf("Assets")) + dllName;
            string dstPath = Application.dataPath + TypeModelPath + dllName;
            Debug.LogFormat("Done! Moved {0} to {1} ...", srcPath, dstPath);
            File.Delete(dstPath);
            File.Move(srcPath, dstPath);

            AssetDatabase.Refresh();
        }
    }    
}
