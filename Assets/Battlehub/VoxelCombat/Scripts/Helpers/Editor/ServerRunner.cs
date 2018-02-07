using System;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    [InitializeOnLoad]
    public static class ServerLauncher 
    {
        static ServerLauncher()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static bool AutoLaunchServer
        {
            get { return EditorPrefs.GetBool("Tools/VoxelCombat/LaunchServer"); }
            set { EditorPrefs.SetBool("Tools/VoxelCombat/LaunchServer", value); }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.ExitingEditMode)
            {
                if(AutoLaunchServer)
                {
                    Launch();
                }
            }
        }

        [MenuItem("Tools/VoxelCombat/Turn On Auto Launch", validate = true)]
        public static bool CanAutoLauchServerOn()
        {
            return !AutoLaunchServer;
        }

        [MenuItem("Tools/VoxelCombat/Turn On Auto Launch")]
        public static void AutoLauchServerOn()
        {
            AutoLaunchServer = true;
        }

        [MenuItem("Tools/VoxelCombat/Turn Off Auto Launch", validate = true)]
        public static bool CanAutoLauchServerOff()
        {
            return AutoLaunchServer;
        }

        [MenuItem("Tools/VoxelCombat/Turn Off Auto Launch")]
        public static void AutoLauchServerOff()
        {
            AutoLaunchServer = false;
        }

        [MenuItem("Tools/VoxelCombat/Launch Server")]
        public static void Launch()
        {
            Process process = new Process();

            process.StartInfo.UseShellExecute = false;
    
            int assetsIndex = Application.dataPath.LastIndexOf("Assets");
            string workingDirectory = Application.dataPath.Remove(assetsIndex);
            string path = workingDirectory + "ServerBuildAndRun.bat";
            process.StartInfo.FileName = path;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }
            else
            {
                process.StartInfo.WorkingDirectory = Application.temporaryCachePath; // nb. can only be called on the main thread
            }

            process.Start();
        }
    }
}
