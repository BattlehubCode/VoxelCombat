using cakeslice;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
 
    public interface IPlayerSelectionController
    {
       
    }

    public class PlayerSelectionController : MonoBehaviour, IPlayerSelectionController
    {
        [SerializeField]
        private Color m_ownColor = Color.green;
        [SerializeField]
        private Color m_enemyColor = Color.red;
        [SerializeField]
        private Color m_neutralColor = Color.black;

        private OutlineEffect m_outlineEffect;
        private IGameViewport m_viewport;
        private IPlayerCameraController m_cameraController;

        private int m_localPlayerIndex;
        public int LocalPlayerIndex 
        {
            get { return m_localPlayerIndex; }
            set
            {
                if (m_localPlayerIndex != value)
                {
                    m_localPlayerIndex = value;
                    if (m_viewport != null) //if start method was called
                    {
                        if(m_outlineEffect != null)
                        {
                            Destroy(m_outlineEffect);
                        }
                        
                        m_viewport = Dependencies.GameView.GetViewport(LocalPlayerIndex);
                        m_cameraController = Dependencies.GameView.GetCameraController(LocalPlayerIndex);

                        m_outlineEffect = m_viewport.Camera.GetComponent<OutlineEffect>();
                        if(m_outlineEffect == null)
                        {
                            m_outlineEffect = m_viewport.Camera.gameObject.AddComponent<OutlineEffect>();
                        }
                        InitializeOutlineEffect();
                    }
                }
            }
        }

        private int PlayerIndex
        {
            get
            {
                Guid playerId = m_gameState.GetLocalPlayerId(LocalPlayerIndex);
                return m_gameState.GetPlayerIndex(playerId);
            }
        }

        private IUnitSelection m_unitSelection;
        private IUnitSelection m_targetSelection;
        private IVoxelInputManager m_inputManager;
        private IVoxelGame m_gameState;
        private IVoxelMap m_map;

        private readonly HashSet<long> m_wasSelected = new HashSet<long>();
        private MapPos m_mapCursor = new MapPos(-1, -1);
        private float m_selectInterval;
        private float m_unselectInterval;
        private bool m_multiselectMode;

 
        private void Awake()
        {
            m_unitSelection = Dependencies.UnitSelection;
            m_targetSelection = Dependencies.TargetSelection;
            m_inputManager = Dependencies.InputManager;
            m_gameState = Dependencies.GameState;
            m_map = Dependencies.Map;
        }

        private void Start()
        {
            m_viewport = Dependencies.GameView.GetViewport(LocalPlayerIndex);
            m_cameraController = Dependencies.GameView.GetCameraController(LocalPlayerIndex);

            m_outlineEffect = m_viewport.Camera.gameObject.AddComponent<OutlineEffect>();
            InitializeOutlineEffect();
        }

        private void InitializeOutlineEffect()
        {
            m_outlineEffect.fillAmount = 0;
            m_outlineEffect.lineThickness = 3.5f;
            m_outlineEffect.lineIntensity = 10;
            m_outlineEffect.scaleWithScreenSize = false;
            m_outlineEffect.lineColor0 = m_ownColor;
            m_outlineEffect.lineColor1 = m_enemyColor;
            m_outlineEffect.lineColor2 = m_neutralColor;
        }

        private void OnDestroy()
        {
            if(m_outlineEffect != null)
            {
                Destroy(m_outlineEffect);
            }
        }

        private void Update()
        {
            if (m_gameState.IsContextActionInProgress(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsMenuOpened(LocalPlayerIndex))
            {
                return;
            }

            if (m_gameState.IsPaused || m_gameState.IsPauseStateChanging)
            {
                return;
            }

            if(m_gameState.IsReplay)
            {
                return;
            }

            int playerIndex = PlayerIndex;

            m_selectInterval -= Time.deltaTime;
            m_unselectInterval -= Time.deltaTime;

            bool multiselect = m_inputManager.GetButton(InputAction.RB, LocalPlayerIndex);

            if (m_inputManager.GetButtonDown(InputAction.LB, LocalPlayerIndex))
            {
                bool select = true;
                
                if(multiselect)
                {
                    long[] units = m_gameState.GetUnits(playerIndex).ToArray();
                    long unitIndex = GetAt(units, m_cameraController.MapCursor);

                    if(m_unitSelection.IsSelected(playerIndex, playerIndex, unitIndex))
                    {
                        m_unitSelection.Unselect(playerIndex, playerIndex, new[] { unitIndex });
                        m_wasSelected.Remove(unitIndex);
                        select = false;
                    }
                }
                
                if(select)
                {
                    Select(playerIndex, multiselect);
                    m_selectInterval = 0.3f;
                    m_unselectInterval = float.PositiveInfinity;
                    m_multiselectMode = true;
                }
                else
                {
                    m_selectInterval = float.PositiveInfinity;
                    m_unselectInterval = 0.3f;
                }
                
            }
            else if(m_inputManager.GetButton(InputAction.LB, LocalPlayerIndex))
            {
                if (m_selectInterval <= 0)
                {
                    Select(playerIndex, multiselect);
                    m_selectInterval = 0.2f;
                }
                
                if(m_unselectInterval <= 0)
                {
                    Unselect(playerIndex);
                    m_unselectInterval = 0.2f;
                }

                m_cameraController.IsInputEnabled = false;
            }            
            else if(m_inputManager.GetButtonUp(InputAction.LB, LocalPlayerIndex))
            {
                m_cameraController.IsInputEnabled = true;
            }

            if (m_inputManager.GetButtonDown(InputAction.RB, LocalPlayerIndex))
            {
                m_multiselectMode = false;
            }
            else if (m_inputManager.GetButtonUp(InputAction.RB, LocalPlayerIndex))
            {
                if(!m_multiselectMode)
                {
                    if(!m_targetSelection.HasSelected(playerIndex))
                    {
                        m_wasSelected.Clear();
                        m_unitSelection.ClearSelection(playerIndex);
                    }
                }
            }    
        }

        private void Unselect(int playerIndex)
        {
            long selectedIndex = -1;
            long[] selection = m_unitSelection.GetSelection(playerIndex, playerIndex);
            if (selection.Length > 0)
            {
                selectedIndex = selection[0];
            }

            long unitIndex = FindClosestTo(selectedIndex, selection, true);
            if(unitIndex >= 0)
            {
                m_unitSelection.Unselect(playerIndex, playerIndex, new[] { unitIndex });
                m_wasSelected.Remove(unitIndex);
            }
        }

        private void Select(int playerIndex, bool multiselect)
        {
            long[] units = m_gameState.GetUnits(playerIndex).ToArray();

            long selectedIndex = -1;
            long[] selection = m_unitSelection.GetSelection(playerIndex, playerIndex);
            if (selection.Length > 0)
            {
                selectedIndex = selection[0];
            }

            if(!multiselect)
            {
                if (m_mapCursor != m_cameraController.MapCursor)
                {
                    m_wasSelected.Clear();
                }
            }
            
            long unitIndex = FindClosestTo(selectedIndex, units, false);
            if (unitIndex >= 0)
            {
                if (m_wasSelected.Count == 0)
                {
                    m_unitSelection.Select(playerIndex, playerIndex, new[] { unitIndex });
                }
                else
                {
                    m_unitSelection.AddToSelection(playerIndex, playerIndex, new[] { unitIndex });
                }

                m_wasSelected.Add(unitIndex);

                if (m_wasSelected.Count == 1)
                {
                    IVoxelDataController dc = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
                    Coordinate coord = dc.Coordinate;
                    coord = coord.ToWeight(m_cameraController.Weight);
                    coord.Altitude += dc.ControlledData.Height;

                    m_cameraController.MapPivot = coord.MapPos;
                    m_cameraController.SetVirtualMousePosition(coord, true);
                    m_mapCursor = m_cameraController.MapCursor;
                }
            }
        }

        private long FindClosestTo(long selectedIndex, long[] units, bool unselectMode)
        {
            int playerIndex = PlayerIndex;

            MapPos mapCursor = m_cameraController.MapCursor;
            Vector3 selectedPosition;
            if (selectedIndex == -1 || m_mapCursor != mapCursor)
            {
                selectedPosition = m_cameraController.Cursor;
            }
            else
            {
                selectedPosition = GetUnitPosition(playerIndex, selectedIndex);
            }

            float minDistance = float.PositiveInfinity;
            long closestIndex = -1;

            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_viewport.Camera);

            for (int i = 0; i < units.Length; ++i)
            {
                long unitIndex = units[i];

                if(unselectMode)
                {
                    if (!m_wasSelected.Contains(unitIndex))
                    {
                        continue;
                    }
                }
                else
                {
                    if (m_wasSelected.Contains(unitIndex))
                    {
                        continue;
                    }

                    if (unitIndex == selectedIndex && m_mapCursor != mapCursor)
                    {
                        continue;
                    }
                }


                IVoxelDataController controller = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
                
                Vector3 position = m_map.GetWorldPosition(controller.Coordinate);
                if (IsVisible(planes, controller.ControlledData.VoxelRef) && VoxelData.IsControllableUnit(controller.ControlledData.Type))
                {
                    Vector3 toVector = (position - selectedPosition);
                    
                    float distance = toVector.sqrMagnitude;
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        closestIndex = unitIndex;
                    }
                }
            }

            return closestIndex;
        }

        private long GetAt(long[] units, MapPos position)
        {
            int playerIndex = PlayerIndex;

            for (int i = 0; i < units.Length; ++i)
            {
                long unitIndex = units[i];

                IVoxelDataController controller = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
                if(controller.Coordinate.MapPos == new Coordinate(position, m_cameraController.Weight, 0).ToWeight(controller.Coordinate.Weight).MapPos)
                {
                    return unitIndex;
                }
            }

            return -1;
        }

        //private bool IsVisible(int playerIndex, long unitIndex)
        //{
        //    if (!m_gameState.ContainsUnit(playerIndex, unitIndex))
        //    {
        //        return false;
        //    }

        //    IVoxelDataController controller = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
        //    Plane[] planes = GeometryUtility.CalculateFrustumPlanes(m_viewport.Camera);
        //    return IsVisible(planes, controller.ControlledData.VoxelRef);
        //}

        private bool IsVisible(Plane[] planes, Voxel voxel)
        {
            if (voxel == null)
            {
                return false;
            }

            Bounds bounds = voxel.Renderer.bounds;
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }

        private Vector3 GetUnitPosition(int playerIndex, long unitIndex)
        {
            IVoxelDataController controller = m_gameState.GetVoxelDataController(playerIndex, unitIndex);
            Vector3 position = m_map.GetWorldPosition(controller.Coordinate);
            return position;
        }
    }

}
