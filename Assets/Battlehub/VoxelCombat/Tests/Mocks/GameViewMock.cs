using System;
using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class GameViewMock : MonoBehaviour, IGameView
    {
        public bool IsInitialized
        {
            get; set;
        }

        public bool IsOn
        {
            get; set;
        }

        public event EventHandler Initialized;

        public void RaiseInitialized()
        {
            if(Initialized != null)
            {
                Initialized(this, EventArgs.Empty);
            }
        }

        public IBoxSelector GetBoxSelector(int index)
        {
            throw new NotImplementedException();
        }

        public IPlayerCameraController GetCameraController(int index)
        {
            throw new NotImplementedException();
        }

        public IPlayerSelectionController GetSelectionController(int index)
        {
            throw new NotImplementedException();
        }

        public ITargetSelectionController GetTargetSelectionController(int index)
        {
            throw new NotImplementedException();
        }

        public IPlayerUnitController GetUnitController(int index)
        {
            throw new NotImplementedException();
        }

        public IGameViewport GetViewport(int index)
        {
            throw new NotImplementedException();
        }

        public IVirtualMouse GetVirtualMouse(int index)
        {
            throw new NotImplementedException();
        }

        public void Initialize(int viewportsCount, bool isOn)
        {
            
        }
    }
}
