using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public class LocationPickerArgs
    {
        public enum PickStatus
        {
            Picked,
            Failed,
            Cancelled,
        }

        public PickStatus Status
        {
            get;
            private set;
        }

        public Coordinate Coordinate
        {
            get;
            private set;
        }

        public LocationPickerArgs(PickStatus status, Coordinate coordinate)
        {
            Status = status;
            Coordinate = coordinate;
        }
    }

    public interface ILocationPicker
    {
        void PickLocationToConvert(VoxelData unit, int targetType, Action<LocationPickerArgs> callback);
        void EndPickLocation();
    }

    

    public class LocationPicker : MonoBehaviour, ILocationPicker
    {
        [SerializeField]
        private Material m_previewMaterial;

        private IVoxelGame m_game;
        private IUnitSelection m_selection;
        private IGameViewport m_viewport;
        private IPlayerCameraController m_cameraController;
        private IVoxelInputManager m_inputManager;
        private IVoxelMap m_map;

        private int m_targetWeight;
        private VoxelData m_voxelData;
        private Voxel m_preview;
        private readonly List<Voxel> m_previews = new List<Voxel>();
        private IVoxelFactory m_factory;
        
        private Action<LocationPickerArgs> m_callback;

        private void Awake()
        {
            m_factory = Dependencies.VoxelFactory;
            m_game = Dependencies.GameState;
            m_game.ContextAction += OnContextAction;

            m_map = Dependencies.Map;
            m_inputManager = Dependencies.InputManager;

            m_viewport = GetComponentInParent<GameViewport>();

            m_selection = Dependencies.UnitSelection;
            m_selection.SelectionChanged += OnSelectionChanged;
        
       
            enabled = false;
        }

        private void Start()
        {
            m_cameraController = Dependencies.GameView.GetCameraController(m_viewport.LocalPlayerIndex);
        }

        private void OnDestroy()
        {
            if(m_selection != null)
            {
                m_selection.SelectionChanged -= OnSelectionChanged;
            }

            if(m_game != null)
            {
                m_game.ContextAction -= OnContextAction;
            }
        }

        private void OnContextAction(int index)
        {
            if(m_viewport.LocalPlayerIndex != index)
            {
                return;
            }

            if (!enabled)
            {
                return;
            }

            if (!m_game.IsContextActionInProgress(index))
            {
                m_callback(new LocationPickerArgs(LocationPickerArgs.PickStatus.Cancelled, new Coordinate()));
                EndPickLocation();
            }
        }

        private void OnSelectionChanged(int selectorIndex, int unitOwnerIndex, long[] selected, long[] unselected)
        {
            if(!enabled)
            {
                return;
            }

            if(!m_selection.IsSelected(m_voxelData.Owner, m_voxelData.Owner, m_voxelData.UnitOrAssetIndex))
            {
                Cleanup();
                m_callback(new LocationPickerArgs(LocationPickerArgs.PickStatus.Failed, new Coordinate()));
            }
        }

        private void Update()
        {     
            if (m_targetWeight !=  m_voxelData.Weight)
            {
                m_targetWeight = m_voxelData.Weight;
                m_preview.Weight = m_targetWeight;
                m_preview.Height = 1 << m_targetWeight;
            }

            VoxelData beneath = null;
            MapCell cell = m_map.GetCell(m_cameraController.MapCursor, m_cameraController.Weight, null);
            if(cell != null)
            {
                Vector3 position = m_map.GetWorldPosition(m_cameraController.MapCursor, m_cameraController.Weight);

                VoxelData target;
                beneath = cell.GetDefaultTargetFor(m_voxelData.Type, m_voxelData.Weight, m_voxelData.Owner, false, out target);
                if (beneath != null)
                {
                    position.y = (beneath.Altitude + beneath.Height) * GameConstants.UnitSize;
                    if(!m_preview.gameObject.activeSelf)
                    {
                        m_preview.gameObject.SetActive(true);
                    }  
                }
                else
                {
                    if (m_preview.gameObject.activeSelf)
                    {
                        m_preview.gameObject.SetActive(false);
                    } 
                }

                m_preview.transform.position = position;
            }
            else
            {
                if (m_preview.gameObject.activeSelf)
                {
                    m_preview.gameObject.SetActive(false);
                }
            }

            if (beneath != null)
            {
                if (m_inputManager.GetButtonDown(InputAction.A, m_viewport.LocalPlayerIndex, true, false) ||
                    m_inputManager.GetButtonDown(InputAction.LMB, m_viewport.LocalPlayerIndex, true, false))
                {
                    m_previews.Add(m_preview);
                    enabled = false;
                    m_callback(new LocationPickerArgs(
                        LocationPickerArgs.PickStatus.Picked,
                        new Coordinate(cell.GetPosition(), m_targetWeight, beneath.Altitude + beneath.Height)));
                    
                }
            }

            if(m_inputManager.GetButtonDown(InputAction.B, m_viewport.LocalPlayerIndex, true, false) ||
               m_inputManager.GetButtonDown(InputAction.RMB, m_viewport.LocalPlayerIndex, true, false))
            {
                m_game.IsContextActionInProgress(m_viewport.LocalPlayerIndex, false);
            }
        }

        public void PickLocationToConvert(VoxelData unit, int targetType, Action<LocationPickerArgs> callback)
        {
            if (enabled)
            {
                throw new InvalidOperationException("Previous Pick operation is not completed");
            }

            m_callback = callback;
            m_voxelData = unit;
            m_targetWeight = unit.Weight;
            m_callback = callback;

            Voxel preview = m_factory.Acquire(targetType | (int)KnownVoxelTypes.Preview);
            preview.Weight = m_targetWeight;
            preview.Height = 1 << m_targetWeight;
            m_preview = preview;

            m_game.IsContextActionInProgress(m_viewport.LocalPlayerIndex, true);

            enabled = true;
        }

        public void EndPickLocation()
        {
            Cleanup();
            for (int i = 0; i < m_previews.Count; ++i)
            {
                Voxel voxel = m_previews[i];
                m_factory.Release(voxel);
            }
            m_previews.Clear();
            m_game.IsContextActionInProgress(m_viewport.LocalPlayerIndex, false);
        }

        private void Cleanup()
        {
            enabled = false;
            m_voxelData = null;
            m_callback = null;
            if(m_preview != null)
            {
                m_factory.Release(m_preview);
                m_previews.Remove(m_preview);
                m_preview = null;
            }
            
        }
    }

}

