using System;
using UnityEngine;

namespace Battlehub.VoxelCombat.Tests
{
    public class UnitSelectionMock : MonoBehaviour, IUnitSelection
    {
        public event UnitSelectionChangedHandler SelectionChanged;

        public void RaiseSelectionChanged(int selectorIndex, int unitIndex, long[] selected, long[] unselected)
        {
            if(SelectionChanged != null)
            {
                SelectionChanged(selectorIndex, unitIndex, selected, unselected);
            }   
        }

        public void AddToSelection(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            throw new NotImplementedException();
        }

        public void ClearSelection(int selectorIndex)
        {
            throw new NotImplementedException();
        }

        public long[] GetSelection(int selectorIndex, int unitOwnerIndex)
        {
            throw new NotImplementedException();
        }

        public bool HasSelected(int selectorIndex)
        {
            throw new NotImplementedException();
        }

        public bool HasSelected(int selectorIndex, int unitOwnerIndex)
        {
            throw new NotImplementedException();
        }

        public bool IsSelected(int selectorIndex, int unitOwnerIndex, long unitId)
        {
            throw new NotImplementedException();
        }

        public void Select(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            throw new NotImplementedException();
        }

        public void Unselect(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            throw new NotImplementedException();
        }
    }
}
