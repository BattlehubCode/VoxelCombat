using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class TargetHighlight : MonoBehaviour
    {
        [SerializeField]
        private Transform m_cursor;
 
        private IUnitSelection m_selection;
        private IVoxelGame m_gameState;
        private IVoxelMap m_map;

        private int m_localPlayerIndex;
        public int LocalPlayerIndex
        {
            get { return m_localPlayerIndex; }
            set
            {
                m_localPlayerIndex = value;

                m_cursor.gameObject.layer = GameConstants.PlayerLayers[m_localPlayerIndex];

                Renderer renderer = m_cursor.GetComponent<Renderer>();

                int index = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
                if (renderer != null && index >= 0)
                {
                    renderer.sharedMaterial = Dependencies.MaterialsCache.GetPrimaryMaterial(index);
                }
            }
        }

        private bool m_isTargetSelectionMode;

        public bool IsTargetSelectionMode
        {
            get { return m_isTargetSelectionMode; }
            set
            {
                m_isTargetSelectionMode = value;
            }
        }

        private MapPos m_cursorPos;
        public MapPos CursorPos
        {
            get { return m_cursorPos; }
            set
            {
                m_cursorPos = value;
                UpdateCursorPositions();
            }
        }

        private bool m_selectionVisibility;
        private bool m_locationVisibility;

        private bool IsCursorVisible
        {
            get { return m_cursor.gameObject.activeSelf; }
            set { m_cursor.gameObject.SetActive(value); }
        }


        private void Awake()
        {
            m_map = Dependencies.Map;
            m_gameState = Dependencies.GameState;
            m_selection = Dependencies.UnitSelection;
            m_selection.SelectionChanged += OnSelectionChanged;

            UpdateCursorsVisibility();
        }

        private void Start()
        {
            Renderer renderer = m_cursor.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = Dependencies.MaterialsCache.GetPrimaryMaterial(m_gameState.LocalToPlayerIndex(LocalPlayerIndex));
            }
        }

        private void OnDestroy()
        {
            if (m_selection != null)
            {
                m_selection.SelectionChanged -= OnSelectionChanged;
            }
        }

        private void OnSelectionChanged(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected)
        {
            UpdateCursorsVisibility();
            UpdateCursorPositions();
        }

        private void UpdateCursorsVisibility()
        {
            int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);
            m_selectionVisibility = m_selection.HasSelected(playerIndex, playerIndex);
            IsCursorVisible = m_selectionVisibility && m_locationVisibility;
        }

        private void UpdateCursorPositions()
        {
            int playerIndex = m_gameState.LocalToPlayerIndex(LocalPlayerIndex);

            MapPos cursorPos = CursorPos;

            int weight = GameConstants.MinVoxelActorWeight;
            int defaultType = (int)KnownVoxelTypes.Eater;

            Vector3 position = m_map.GetWorldPosition(cursorPos, weight);

            MapCell cell = m_map.GetCell(cursorPos, weight, null);
            if(cell != null)
            {
                VoxelData target;
                VoxelData beneath = cell.GetDefaultTargetFor(defaultType, weight, playerIndex, false, out target);

                while (beneath != null && target == null && (beneath.Weight - weight) > 0)
                {
                    cursorPos.Row /= 2;
                    cursorPos.Col /= 2;

                    weight++;

                    beneath = cell.GetDefaultTargetFor(defaultType, weight, playerIndex, false, out target);
                }

                if (target != null)
                {
                    m_locationVisibility = false;

                }
                else
                {
                    if (beneath != null)
                    {
                        position.y = (beneath.Altitude + beneath.Height) * GameConstants.UnitSize;
                        m_locationVisibility = true;
                    }
                    else
                    {
                        m_locationVisibility = false;
                    }
                }



                IsCursorVisible = m_locationVisibility && m_selectionVisibility;

                m_cursor.position = position;
            }
        }

        private void Update()
        {
            UpdateCursorPositions();
        }

    }
}


