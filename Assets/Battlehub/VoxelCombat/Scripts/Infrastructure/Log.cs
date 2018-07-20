
#if !SERVER
using UnityEngine;
#else
using System.Diagnostics;
#endif

namespace Battlehub.VoxelCombat
{
    public interface ILogger
    {
        void Log(string str);
        void LogFormat(string str, params object[] args);
        void LogWarning(string str);
        void LogWarningFormat(string str, params object[] args);
        void LogError(string str);
        void LogErrorFormat(string str, params object[] args);
    }


    public class Logger : ILogger
    {
#if !SERVER
        public void Log(string str)
        {
            Debug.Log(str);
        }

        public void LogFormat(string str, params object[] args)
        {
            Debug.LogFormat(str, args);
        }

        public void LogWarning(string str)
        {
            Debug.LogWarning(str);
        }

        public void LogWarningFormat(string str, params object[] args)
        {
            Debug.LogWarningFormat(str, args);
        }

        public void LogError(string str)
        {
            Debug.LogError(str);
        }

        public void LogErrorFormat(string str, params object[] args)
        {
            Debug.LogErrorFormat(str, args);
        }
#else
        public void Log(string str)
        {
            Debug.WriteLine(str);
        }

        public void LogFormat(string str, params object[] args)
        {
            Debug.WriteLine(str, args);
        }

        public void LogWarning(string str)
        {
            Debug.WriteLine("Warning! : " + str);
        }

        public void LogWarningFormat(string str, params object[] args)
        {
            Debug.WriteLine("Warning! : " + str, args);
        }

        public void LogError(string str)
        {
            Debug.WriteLine("Error! : " + str);
        }

        public void LogErrorFormat(string str, params object[] args)
        {
            Debug.WriteLine("Error! : " + str, args);
        }
#endif
    }
}



