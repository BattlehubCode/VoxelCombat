using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading;

namespace Battlehub.Dispatcher
{
    public class DispatcherTest : MonoBehaviour
    {
        [SerializeField]
        private Text Output;

        private void Start()
        {
            for (int i = 0; i < 10; ++i)
            {
                Thread t = new Thread(ThreadFunction);
                t.Start("Dispatched from Thread " + i);
            }
        }
        
        private void ThreadFunction(object param)
        {
            Dispatcher.Current.BeginInvoke(() =>
            {
                Output.text += param;
                Output.text += Environment.NewLine;
            });
        }
    }
}
