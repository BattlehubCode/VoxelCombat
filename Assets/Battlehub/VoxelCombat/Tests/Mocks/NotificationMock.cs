using NUnit.Framework;
using System;
using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class NotificationMock : MonoBehaviour, INotification
    {
        public bool FailOnError = true;

        public void Close()
        {
        }

        public INotification GetChild(int index)
        {
            return null;
        }

        public void Show(string text, GameObject selectOnClose = null)
        {
        }

        public void ShowError(Error error, GameObject selectOnClose = null)
        {
            if(FailOnError)
            {
                Assert.Fail(error.ToString());
            }
        }

        public void ShowError(string error, GameObject selectOnClose = null)
        {
            if (FailOnError)
            {
                Assert.Fail(error);
            }
        }

        public void ShowErrorWithAction(Error error, Action onClose = null)
        {
            if (FailOnError)
            {
                Assert.Fail(error.ToString());
            }
        }

        public void ShowErrorWithAction(string error, Action onClose = null)
        {
            if (FailOnError)
            {
                Assert.Fail(error);
            }
        }

        public void ShowWithAction(string text, Action onClose = null)
        {
        }
    }
}
