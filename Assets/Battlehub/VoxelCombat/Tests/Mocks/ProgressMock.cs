using System;
using UnityEngine;
namespace Battlehub.VoxelCombat.Tests
{
    public class ProgressMock : MonoBehaviour, IProgressIndicator
    {
        public bool IsVisible
        {
            get;
            set;
        }

        public IProgressIndicator GetChild(int index)
        {
            return null;
        }

        public void SetText(string text)
        {
        }
    }
}