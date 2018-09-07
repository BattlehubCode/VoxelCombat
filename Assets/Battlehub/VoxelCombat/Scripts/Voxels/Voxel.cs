using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public delegate void VoxelEventHandler(Voxel sender);

    public delegate void VoxelActorEvent(long tick);

    [DisallowMultipleComponent]
    public abstract class Voxel : MonoBehaviour
    {
        public event VoxelEventHandler Acquired;
        public event VoxelEventHandler Released;
        public event VoxelActorEvent BeginMove;
        public event VoxelActorEvent BeforeMoveCompleted;
        public event VoxelActorEvent MoveCompleted;
        public event VoxelActorEvent RotateCompleted;
        public event VoxelActorEvent BeforeGrowCompleted;
        public event VoxelActorEvent BeforeDiminishCompleted;
        public event VoxelActorEvent ResizeCompleted;
        //public event VoxelActorEvent ChangeAltitudeCompleted;

        protected void RaiseBeginMove(long tick)
        {
            if(BeginMove != null) { BeginMove(tick); }
        }
        protected void RaiseBeforeMoveCompleted(long tick)
        {
            if (BeforeMoveCompleted != null) { BeforeMoveCompleted(tick); }
        }
        protected void RaiseMoveCompleted(long tick)
        {
            if (MoveCompleted != null) { MoveCompleted(tick); }
        }
        protected void RaiseRotateCompleted(long tick)
        {
            if (RotateCompleted != null) { RotateCompleted(tick); }
        }
        protected void RaiseBeforeGrowCompleted(long tick)
        {
            if (BeforeGrowCompleted != null) { BeforeGrowCompleted(tick); }
        }

        protected void RaiseBeforeDiminishCompleted(long tick)
        {
            if (BeforeDiminishCompleted != null) { BeforeDiminishCompleted(tick); }
        }
        protected void RaiseResizeCompleted(long tick)
        {
            if (ResizeCompleted != null) { ResizeCompleted(tick); }
        }
        
        private IMaterialsCache m_materialsCache;

        [SerializeField]
        protected Material m_primaryMaterial;
        
        [SerializeField]
        protected Material m_secondaryMaterial;

        protected virtual void SetMaterials(Material primary, Material secondary)
        {
            m_primaryMaterial = primary;
            m_secondaryMaterial = secondary;
        }

        [SerializeField]
        protected Renderer m_renderer;
        public Renderer Renderer
        {
            get { return m_renderer; }
        }

        [SerializeField]
        protected Canvas m_debugInfoCanvas;

        [SerializeField]
        protected Text m_debugInfoText;

        [SerializeField]
        private VoxelUI m_uiPrefab;

        private IParticleEffectFactory m_effectFactory;
        protected IParticleEffectFactory EffectFactory
        {
            get { return m_effectFactory; }
        }

        private IVoxelFactory m_voxelFactory;
        protected IVoxelFactory VoxelFactory
        {
            get { return m_voxelFactory; }
        }
        

        private IVoxelGame m_gameState;
        protected IVoxelGame GameState
        {
            get { return m_gameState; }
        }

        private IGlobalSettings m_settings;

        private int m_owner = 0; //unknown
        public int Owner
        {
            get { return m_owner; }
            set
            {
                m_owner = value;

                if (m_materialsCache != null)
                {
                    SetMaterials
                    (
                        m_materialsCache.GetPrimaryMaterial(m_owner),
                        m_materialsCache.GetSecondaryMaterial(m_owner)
                    );
                }
            }
        }

        private int m_health;
        public virtual int Health
        {
            get { return m_health; }
            set { m_health = value; }
        }

        public virtual int MinWeight
        {
            get { return 0; }
        }

        protected int m_weight = 2;
        public virtual int Weight
        {
            get { return m_weight; }
            set
            {
                m_weight = value;

                float scale = Mathf.Pow(2, m_weight);
                Vector3 localScale = Root.localScale;
                localScale.x = scale;
                localScale.z = scale;
                Root.localScale = localScale;
            }
        }

        [SerializeField]
        private AnimationCurve m_altitudeCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private float m_changeAltitudeDelay;
        private float m_changeAltitudeT;
        private float m_changeAltitudeDuration;
        private bool m_isChangingAltitude;
        protected bool IsChangingAltitude
        {
            get { return m_isChangingAltitude; }
        }

        protected int m_previousAltitude;
        protected int m_altitude;

        public virtual int Altitude
        {
            get { return m_altitude; }
            set
            {
                m_altitude = value;

                Vector3 position = Root.position;
                position.y = m_altitude * GameConstants.UnitSize;
                Root.position = position;
            }
        }

        [SerializeField]
        private AnimationCurve m_heightCurve = AnimationCurve.Linear(0, 0, 1, 1);
        private float m_changeHeightDelay;
        private float m_changeHeightT;
        private float m_changeHeightDuration;
        private bool m_isChangingHeight;
        protected bool IsChangingHeight
        {
            get { return m_isChangingHeight; }
        }

        protected int m_previousHeight;
        protected int m_height;
        public virtual int Height
        {
            get { return m_height;  }
            set
            {
                int oldValue = m_height;
                m_height = value;
                
                Vector3 localScale = Root.localScale;
                localScale.y = EvalHeight(m_height) ;
                Root.localScale = localScale;

                if(oldValue != m_height )
                {
                    UpdateUIVisibility();
                }
            }
        }

        [SerializeField]
        protected bool m_isPreview;

        public abstract int Type
        {
            get;
        }

        private VoxelData m_voxelData;
        public VoxelData VoxelData
        {
            get { return m_voxelData; }
        }

        /// <summary>
        /// Root object transform
        /// </summary>
        public virtual Transform Root
        {
            get { return transform; }
        }
    
        /// <summary>
        /// Body transform (for example RotationBone of VoxelActor)
        /// </summary>
        public virtual Transform Body
        {
            get { return transform; }
        }

        public bool IsAcquired
        {
            get { return gameObject.activeSelf; }
        }

        public void GoToAcquiredState()
        {
            gameObject.SetActive(true);

            if (Acquired != null)
            {
                Acquired(this);
            }

            WriteDebugInfo();
        }

      
        public void GoToReleasedState()
        {
            gameObject.SetActive(false);
            Debug.Assert(!gameObject.activeSelf);
            Unfreeze();

            if(m_voxelData != null)
            {
                m_voxelData.VoxelRef = null;
                m_voxelData = null;
            }

            StopAnimations();

            if (Released != null)
            {
                Released(this);
            }
        }


        protected virtual bool IsEnabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        private void Awake()
        {
            AwakeOverride();
        }
        
        private void Start()
        {
            m_settings = Dependencies.Settings;

            OnDebugModeChanged();
            m_settings.DebugModeChanged += OnDebugModeChanged;

            m_gameState = Dependencies.GameState;
            m_voxelFactory = Dependencies.VoxelFactory;
            m_effectFactory = Dependencies.EffectFactory;
            m_materialsCache = Dependencies.MaterialsCache;
            SetMaterials
            (
                m_materialsCache.GetPrimaryMaterial(m_owner),
                m_materialsCache.GetSecondaryMaterial(m_owner)
            );

            m_previousAltitude = Altitude;
            m_previousHeight = Height;

            StartOverride();

            IsEnabled = false; //This is used to enable update method and programmatic animations
        }

        private void OnDestroy()
        {
            if(m_settings != null)
            {
                m_settings.DebugModeChanged -= OnDebugModeChanged;
            }
            OnDestroyOveride();
        }

        private void OnEnable()
        {
            OnEnableOverride();   
        }

        private void OnDisable()
        {
            OnDisableOverride();
        }

        private void Update()
        {
            UpdateOverride();
            if (!m_isChangingAltitude && !m_isChangingHeight)
            {
                IsEnabled = false; //This is used to enable update method and programmatic animations
            }
        }

        protected virtual void AwakeOverride()
        {

        }

        protected virtual void StartOverride()
        {
            
        }

        protected virtual void OnEnableOverride()
        {

        }

        protected virtual void OnDisableOverride()
        {

        }

        protected virtual void OnDestroyOveride()
        {
            
        }


        protected virtual void UpdateOverride()
        {
            if(m_isChangingAltitude)
            {
                if(m_changeAltitudeDelay > 0)
                {
                    m_changeAltitudeDelay -= Time.deltaTime;
                    return;
                }

                m_changeAltitudeT += Time.deltaTime;

                Vector3 position = Root.position;
                position.y = Mathf.Lerp(
                    m_previousAltitude * GameConstants.UnitSize,
                    m_altitude * GameConstants.UnitSize, m_altitudeCurve.Evaluate(m_changeAltitudeT / m_changeAltitudeDuration));
                Root.position = position;
                     
                if(m_changeAltitudeT >= m_changeAltitudeDuration)
                {
                    m_previousAltitude = m_altitude;
                    m_isChangingAltitude = false;
                    //if (ChangeAltitudeCompleted != null)
                    //{
                    //    ChangeAltitudeCompleted();
                    //}
                }
            }

            if(m_isChangingHeight)
            {
                if(m_changeHeightDelay > 0)
                {
                    m_changeHeightDelay -= Time.deltaTime;
                    return;
                }

                m_changeHeightT += Time.deltaTime;

                float heightFrom = EvalHeight(m_previousHeight);
                float heightTo = EvalHeight(m_height);

                Vector3 localScale = Root.localScale;
                localScale.y = Mathf.Lerp(heightFrom, heightTo, m_heightCurve.Evaluate(m_changeHeightT / m_changeHeightDuration));
                Root.localScale = localScale;

                if (m_changeHeightT >= m_changeHeightDuration)
                {
                    m_previousHeight = m_height;
                    m_isChangingHeight = false;
                    UpdateUIVisibility();
                }
            }
        }

        protected virtual float EvalHeight(int height)
        {
            float result = height;
            result = Mathf.Max(0.01f, result);
            return result;
        }


        private ulong m_targetSelection;
        private List<cakeslice.Outline> m_targetSelectionOutlines;
        public void SelectAsTarget(int playerIndex) //this is player index (not owner index)
        {
            if (!IsSelectedAsTarget(playerIndex))
            {
                m_targetSelection |= (1ul << playerIndex);

                if (m_renderer != null)
                {
                    if (m_targetSelectionOutlines == null)
                    {
                        m_targetSelectionOutlines = new List<cakeslice.Outline>();
                    }

                    cakeslice.Outline outline = m_renderer.gameObject.AddComponent<cakeslice.Outline>();
                    m_targetSelectionOutlines.Add(outline);

                    outline.layerMask = GameConstants.PlayerLayerMasks[playerIndex - 1];

                    outline.color = 1;
                }
                else
                {
                    Debug.LogError("MeshRenderer is null");
                }
            }
        }

        public void UnselectAsTarget(int playerIndex) ///this is player index (not owner index)
        {
            if (IsSelectedAsTarget(playerIndex))
            {
                m_targetSelection &= ~(1ul << playerIndex);

                if (m_renderer != null)
                {
                    if (m_targetSelectionOutlines != null)
                    {
                        cakeslice.Outline outline = m_targetSelectionOutlines.Where(o => o.layerMask == GameConstants.PlayerLayerMasks[playerIndex - 1]).FirstOrDefault();

                        if (outline != null)
                        {
                            Destroy(outline);
                        }

                        m_targetSelectionOutlines = null;
                    }
                }
                else
                {
                    Debug.LogError("MeshRenderer is null");
                }
            }
        }

        public bool IsSelectedAsTarget(int playerIndex)
        {
            return (m_targetSelection & (1ul << playerIndex)) != 0;
        }

        private ulong m_selection;
        private List<cakeslice.Outline> m_selectionOutlines;
        protected List<VoxelUI> m_ui;
        public void Select(int playerIndex) //this is player index (not owner index)
        {
            if (!IsSelected(playerIndex))
            {
                m_selection |= (1ul << playerIndex);

                // Dependencies.GameView.GetViewport(m_gameState.PlayerToLocalIndex(pla))
                if (m_gameState == null)
                {
                    m_gameState = Dependencies.GameState;
                }

                if (m_gameState.IsLocalPlayer(playerIndex))
                {
                    if(m_renderer != null)
                    {
                        if (m_selectionOutlines == null)
                        {
                            m_selectionOutlines = new List<cakeslice.Outline>();
                        }

                        cakeslice.Outline outline = m_renderer.gameObject.AddComponent<cakeslice.Outline>();
                        m_selectionOutlines.Add(outline);

                        int localPlayerIndex = m_gameState.PlayerToLocalIndex(playerIndex);
                        outline.layerMask = GameConstants.PlayerLayerMasks[localPlayerIndex];

                        if (Owner == playerIndex)
                        {
                            outline.color = 0;
                        }
                        else
                        {
                            if (Owner != 0)
                            {
                                outline.color = 1;
                            }
                            else
                            {
                                outline.color = 2;
                            }
                        }
                    }

                    if(m_uiPrefab != null)
                    {
                        VoxelUI ui = Instantiate(m_uiPrefab, transform, false);
                        int localPlayerIndex = m_gameState.PlayerToLocalIndex(playerIndex);
                        foreach(Transform t in ui.GetComponentsInChildren<Transform>(true))
                        {
                            t.gameObject.layer = GameConstants.PlayerLayers[localPlayerIndex];
                        }
                        ui.LocalPlayerIndex = localPlayerIndex;
                        if (m_ui == null)
                        {
                            m_ui = new List<VoxelUI>();
                        }
                        m_ui.Add(ui);
                        UpdateUIVisibility();
                    }
                }

                OnSelect(playerIndex);
            }
        }

        protected virtual void OnSelect(int playerIndex)
        {

        }


        public void Unselect(int playerIndex) ///this is player index (not owner index)
        {
            if (IsSelected(playerIndex))
            {
                m_selection &= ~(1ul << playerIndex);

                if (m_selectionOutlines != null)
                {
                    if (m_gameState == null)
                    {
                        m_gameState = Dependencies.GameState;
                    }

                    if (m_gameState.IsLocalPlayer(playerIndex))
                    {
                        int localPlayerIndex = m_gameState.PlayerToLocalIndex(playerIndex);

                        cakeslice.Outline outline = m_selectionOutlines.Where(o => o.layerMask == GameConstants.PlayerLayerMasks[localPlayerIndex]).FirstOrDefault();

                        if (outline != null)
                        {
                            Destroy(outline);
                        }
                    }

                    m_selectionOutlines = null;
                }

                if(m_ui != null)
                {
                    if (m_gameState == null)
                    {
                        m_gameState = Dependencies.GameState;
                    }

                    if(m_gameState.IsLocalPlayer(playerIndex))
                    {
                        int localPlayerIndex = m_gameState.PlayerToLocalIndex(playerIndex);

                       VoxelUI ui = m_ui.Where(o => o.gameObject.layer == GameConstants.PlayerLayers[localPlayerIndex]).FirstOrDefault();
                        if (ui != null)
                        {
                            Destroy(ui.gameObject);
                        }
                    }

                    m_ui = null;
                }

                OnUnselect(playerIndex);
            }
        }
    
        protected virtual void OnUnselect(int playerIndex)
        {

        }

        public bool IsSelected(int playerIndex)
        {
            return (m_selection & (1ul << playerIndex)) != 0;
        }

        public virtual void Move(int altitude, long tick, float duration)
        {
        }

        public virtual void RotateLeft(long tick, float duration)
        {  
        }

        public virtual void RotateRight(long tick, float duration)
        {
        }

        public virtual void BeginSplit(long tick, float duration)
        {

        }

        public virtual void Split(long tick, float duration)
        {

        }

        public virtual void BeginSplit4(long tick, float duration)
        {

        }

        public virtual void Split4(long tick, float duration)
        {

        }

        public virtual void BeginGrow(long tick, float duration)
        {

        }

        public virtual void Grow(Vector3 position, long tick, float duration)
        {
        }

        public virtual void BeginDiminish(long tick, float duration)
        {

        }

        public virtual void Diminish(Vector3 position, long tick, float duration)
        {
        }

        public virtual void BeginConvert(long tick, float duration)
        {

        }

        public virtual void BeginEat(Voxel voxel, long tick)
        {
            voxel.BeginAssimilate(0);
        }

        public virtual void BeginAssimilate(float delay)
        {
            //m_voxelFactory.Release(this);
        }

        public virtual void Assimlate(float delay)
        {
            m_voxelFactory.Release(this);
        }

        public virtual void Smash(float delay, int health)
        {
            m_voxelFactory.Release(this);
        }

        public virtual void Explode(float delay, int health)
        {
            m_voxelFactory.Release(this);
        }

        protected void InstantiateParticleEffect(ParticleEffectType type, float delay, int health)
        {
            ParticleEffect effect = EffectFactory.Acquire(type);
            effect.transform.position = transform.position;
            effect.Data = m_voxelData;
            effect.StartDelay = delay;
            effect.Health = health;
        }

        public virtual void ChangeAltitude(int fromAltitude, int toAltitude, float duration, float delay = 0.0f)
        {
            m_previousAltitude = fromAltitude;
            m_altitude = toAltitude;

            m_changeAltitudeDelay = delay;
            m_changeAltitudeDuration = Mathf.Max(0.01f, duration);
            m_changeAltitudeT = 0;
            m_isChangingAltitude = true;

            IsEnabled = true;
        }

        
        public virtual void Expand(int height, float duration, float delay = 0.0f)
        {
            m_height = height;

            m_changeHeightDelay = delay;
            m_changeHeightDuration = Mathf.Max(0.01f, duration);
            m_changeHeightT = 0;
            m_isChangingHeight = true;

            IsEnabled = true; 
        }

        public virtual void Collapse(float duration, float delay = 0.0f)
        {
            m_height = 0;

            m_changeHeightDelay = delay;
            m_changeHeightDuration = Mathf.Max(0.01f, duration);
            m_changeHeightT = 0;
            m_isChangingHeight = true;

            IsEnabled = true;
        }

        private void StopAnimations()
        {
            m_previousAltitude = m_altitude;
            m_previousHeight = m_height;

            m_changeAltitudeT = 0;
            m_changeHeightT = 0;

            m_changeAltitudeDelay = 0;
            m_changeHeightDelay = 0;

            m_isChangingAltitude = false;
            m_isChangingHeight = false;
        }

        /// <summary>
        /// if animate is false then m_factory.Release method should be called immediately
        /// </summary>
        /// <param name="animate"></param>
        public virtual void Kill()
        {
            m_voxelFactory.Release(this);
        }

        public virtual void Freeze()
        {

        }

        public virtual void Unfreeze()
        {

        }

        public virtual void ReadFrom(VoxelData data)
        {
            m_voxelData = data;

            Altitude = data.Altitude;
            Height = data.Height;
            Weight = data.Weight;
            Owner = data.Owner;
            m_health = data.Health;

            ReadRotation(data);

            m_previousAltitude = data.Altitude;
            m_altitude = data.Altitude;

            UpdateUIVisibility();
        }

        protected virtual void ReadRotation(VoxelData data)
        {
            switch (data.Dir)
            {
                case 0:
                    transform.rotation = Quaternion.identity;
                    break;
                case 1:
                    transform.rotation = Quaternion.AngleAxis(-270, Vector3.up);
                    break;
                case 2:
                    transform.rotation = Quaternion.AngleAxis(-180, Vector3.up);
                    break;
                case 3:
                    transform.rotation = Quaternion.AngleAxis(-90, Vector3.up);
                    break;
                default:
                    throw new System.ArgumentException("data.Dir has wrong value " + data.Dir);
            }
        }

        protected virtual void UpdateUIVisibility()
        {
            if (m_ui == null)
            {
                return;
            }
            for (int i = 0; i < m_ui.Count; ++i)
            {
                m_ui[i].gameObject.SetActive(m_height != 0);
            }
        }

        public virtual void OnStateChanged(VoxelDataState prevState, VoxelDataState newState)
        {
            
        }

        public virtual void OnCancel()
        {

        }

        protected virtual void OnDebugModeChanged()
        {
            if (m_debugInfoCanvas != null)
            {
                m_debugInfoCanvas.gameObject.SetActive(m_settings.DebugMode);

                WriteDebugInfo();

            }
        }

        public void WriteDebugInfo()
        {
            if (m_settings!= null && m_settings.DebugMode && m_voxelData != null && m_debugInfoText != null)
            {
                MapPos pos = Dependencies.Map.Map.GetCellPosition(Root.position, Weight);
                m_debugInfoText.text = "Id " + m_voxelData.UnitOrAssetIndex + System.Environment.NewLine +
                                   "Row " + pos.Row + System.Environment.NewLine +
                                   "Col " + pos.Col;
            }
        }
    }
}
