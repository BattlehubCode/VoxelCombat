using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public delegate void UnitSelectionChangedHandler(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected);

    public interface IUnitSelection
    {
        event UnitSelectionChangedHandler SelectionChanged;

        bool HasSelected(int selectorIndex);

        bool HasSelected(int selectorIndex, int unitOwnerIndex);

        bool IsSelected(int selectorIndex, int unitOwnerIndex, long unitId);
        
        long[] GetSelection(int selectorIndex, int unitOwnerIndex);


        void Select(int selectorIndex, int unitOwnerIndex, long[] unitIds);

        void Unselect(int selectorIndex, int unitOwnerIndex, long[] unitIds);

        void AddToSelection(int selectorIndex, int unitOwnerIndex, long[] unitIds);

        void ClearSelection(int selectorIndex);

       // void ClearSelection();
    }

    public class UnitSelection : MonoBehaviour, IUnitSelection
    {
        public event UnitSelectionChangedHandler SelectionChanged;

        private readonly Dictionary<int, Dictionary<int, List<long>>> m_selection = new Dictionary<int, Dictionary<int, List<long>>>();

        public bool HasSelected(int selectorIndex)
        {
            Dictionary<int, List<long>> selection;
            if (m_selection.TryGetValue(selectorIndex, out selection))
            {
                return selection.Any(kvp => kvp.Value != null && kvp.Value.Count > 0);   
            }
            return false;
        }

        public bool HasSelected(int selectorIndex, int unitOwnerIndex)
        {
            Dictionary<int, List<long>> selection;
            if (m_selection.TryGetValue(selectorIndex, out selection))
            {
                List<long> unitIds;
                if (selection.TryGetValue(unitOwnerIndex, out unitIds))
                {
                    return unitIds.Count > 0;
                }
            }

            return false;
        }

    
        public bool IsSelected(int selectorIndex, int unitOwnerIndex, long unitId)
        {
            Dictionary<int, List<long>> selection;
            if (m_selection.TryGetValue(selectorIndex, out selection))
            {
                List<long> unitIds;
                if (selection.TryGetValue(unitOwnerIndex, out unitIds))
                {
                    return unitIds.Contains(unitId);
                }
            }
            return false;
        }

        public long[] GetSelection(int selectorIndex, int unitOwnerIndex)
        {
            Dictionary<int, List<long>> selection;
            if(m_selection.TryGetValue(selectorIndex, out selection))
            {
                List<long> unitIds;
                if(selection.TryGetValue(unitOwnerIndex, out unitIds))
                {
                    return unitIds.ToArray();
                }
            }

            return new long[0];
        }

        public void Select(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            Dictionary<int, List<long>> selection;
            if(!m_selection.TryGetValue(selectorIndex, out selection))
            {
                selection = new Dictionary<int, List<long>>();
                m_selection.Add(selectorIndex, selection);
            }

            List<long> unselected;
            if(!selection.TryGetValue(unitOwnerIndex, out unselected))
            {
                unselected = new List<long>();
            }

            List<long> selected = new List<long>();
            if (unitIds == null)
            {
                selection[unitOwnerIndex] = new List<long>();
            }
            else
            {
                for(int i = 0; i < unitIds.Length; ++i)
                {
                    if(!selected.Contains(unitIds[i]))
                    {
                        selected.Add(unitIds[i]);
                    }
                }
                
                selection[unitOwnerIndex] = selected.ToList();
            }

            for (int i = selected.Count - 1; i >= 0; i--) 
            {
                long unitId = selected[i];

                if(unselected.Contains(unitId))
                {
                    unselected.Remove(unitId);

                    selected.RemoveAt(i);
                }
            }

            if(selected.Count > 0 || unselected.Count > 0)
            {
                if (SelectionChanged != null)
                {
                    SelectionChanged(selectorIndex, unitOwnerIndex, selected.ToArray(), unselected.ToArray());
                }
            }
        }

        public void Unselect(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            List<long> selectedUnitIds = GetSelection(selectorIndex, unitOwnerIndex).ToList();
            for(int i = 0; i < unitIds.Length; ++i)
            {
                long unitId = unitIds[i];
                selectedUnitIds.Remove(unitId);
            }

            Select(selectorIndex, unitOwnerIndex, selectedUnitIds.ToArray());
        }

        public void AddToSelection(int selectorIndex, int unitOwnerIndex, long[] unitIds)
        {
            HashSet<long> selectedUnitsHS = new HashSet<long>(GetSelection(selectorIndex, unitOwnerIndex));
            for(int i = 0; i < unitIds.Length; ++i)
            {
                long unitId = unitIds[i];
                if(!selectedUnitsHS.Contains(unitId))
                {
                    selectedUnitsHS.Add(unitId);
                }
            }
            Select(selectorIndex, unitOwnerIndex, selectedUnitsHS.ToArray());
        }

        public void ClearSelection(int selectorIndex)
        {
            Dictionary<int, List<long>> selection;
            if (!m_selection.TryGetValue(selectorIndex, out selection))
            {
                selection = new Dictionary<int, List<long>>();
                m_selection.Add(selectorIndex, selection);
            }

            foreach(int ownerIndex in selection.Keys.ToArray())
            {
                Select(selectorIndex, ownerIndex, null);
            }
        }
    }
}

