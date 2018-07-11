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
            
        }

        public void ClearSelection(int selectorIndex)
        {
            
        }

        public long[] GetSelection(int selectorIndex, int unitOwnerIndex)
        {
            return new long[0];
        }

        public bool HasSelected(int selectorIndex)
        {
            return false;
        }

        public bool HasSelected(int selectorIndex, int unitOwnerIndex)
        {
            return false;
        }

        public bool IsSelected(int selectorIndex, int unitOwnerIndex, long unitId)
        {
            return false;
        }

        public void Select(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            
        }

        public void Unselect(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            
        }
    }
}
