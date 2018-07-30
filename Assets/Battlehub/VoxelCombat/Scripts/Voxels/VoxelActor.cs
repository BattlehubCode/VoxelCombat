using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battlehub.VoxelCombat
{
    public struct IntVec
    {
        public int X;

        public int Y;

        public int Z;

        public IntVec(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public IntVec(Vector3 vec)
        {
            X = Mathf.FloorToInt(vec.x);
            Y = Mathf.FloorToInt(vec.y);
            Z = Mathf.FloorToInt(vec.z);
        }

        public override string ToString()
        {
            return X + ", " + Y + ", " + Z;
        }
    }

    public struct VoxelCell
    {
        public int Type;
        //Other attributes
    }

    public enum VoxelActorState
    {
        Idle = 1,
        RotateLeft = 2,
        RotateRight = 4,
        Rotate = RotateLeft | RotateRight,
        Move1X = 8,
        Move2X = 16,
        Move = Move1X | Move2X,
        Grow = 32,
        Diminish = 64,
        Resize = Grow | Diminish
    }


    

    /// <summary>
    /// This class represent voxel and ecapsulates its animation and
    /// other functionality
    /// </summary>
    public abstract class VoxelActor : Voxel
    {
        private interface IVoxelMovementState
        {
            void Move();
            void StopMove();
            void RotateLeft();
            void RotateRight();
            void StopRotateLeft();
            void StopRotateRight();
            void Jump();
            void StopJump();
            void Grow();
            void StopGrow();
            void Diminish();
            void StopDiminish();
        }
        private class FrozenMovementState : IVoxelMovementState
        {
            public void Jump() { }
            public void Move() { }
            public void RotateLeft() { }
            public void RotateRight() { }
            public void StopMove() { }
            public void StopRotateLeft() { }
            public void StopRotateRight() { }
            public void StopJump() { }
            public void Grow() { }
            public void StopGrow() { }
            public void Diminish() { }
            public void StopDiminish() { }
        }

        private class NormalMovementState : IVoxelMovementState
        {
            private VoxelActor m_actor;
            public NormalMovementState(VoxelActor actor) { m_actor = actor; }
            public void Jump() { m_actor.m_animator.SetTrigger("jump"); }
            public void Move() { m_actor.m_animator.SetBool("move", true); }
            public void RotateLeft() { m_actor.m_animator.SetBool("rotateLeft", true); }
            public void RotateRight() { m_actor.m_animator.SetBool("rotateRight", true); }
            public void StopMove() { m_actor.m_animator.SetBool("move", false); }
            public void StopRotateLeft() { m_actor.m_animator.SetBool("rotateLeft", false); }
            public void StopRotateRight() { m_actor.m_animator.SetBool("rotateRight", false); }
            public void StopJump() { m_actor.m_animator.ResetTrigger("jump"); }
            public void Grow() { m_actor.m_animator.SetBool("grow", true); }
            public void StopGrow() { m_actor.m_animator.SetBool("grow", false); }
            public void Diminish() { m_actor.m_animator.SetBool("diminish", true); }
            public void StopDiminish() { m_actor.m_animator.SetBool("diminish", false); }
        }

        /// <summary>
        /// Current VoxelActorState
        /// </summary>
        private VoxelActorState m_animationState;

        /// <summary>
        /// Movement State;
        /// </summary>
        private IVoxelMovementState m_movementState;

        /// <summary>
        /// is voxel flipped
        /// </summary>
        private bool m_isFlipped;

        private bool m_isFrozen;


        private abstract class QueuedActionBase
        {
            public long Tick
            {
                get;
                private set;
            }

            public float CompleteTime
            {
                get;
                private set;
            }
            
            public QueuedActionBase(long tick, float duration)
            {
                Tick = tick;
                CompleteTime = Time.time + duration;
            }

            public abstract void Run();
        }

        private class QueuedAction : QueuedActionBase
        {
            private Action<float> m_action;
            public QueuedAction(Action<float> action, long tick, float duration)
                :base(tick, duration)
            {
                m_action = action;
            }

            public override void Run()
            {
                m_action(Mathf.Max(0.01f, CompleteTime - Time.time));
            }
        }


        private class QueuedAction<T> : QueuedActionBase
        {
            private Action<T, float> m_action;
            private T m_value;

            public QueuedAction(Action<T, float> moveAction, T value, long tick, float duration)
                :base(tick, duration)
            {
                m_action = moveAction;
                m_value = value;
            }

            public override void Run()
            {
                m_action(m_value, Mathf.Max(0.01f, CompleteTime - Time.time));
            }
        }

        private long m_tick;
        private bool m_isInProgress;
        private readonly Queue<QueuedActionBase> m_actionQueue = new Queue<QueuedActionBase>();


        protected override void SetMaterials(Material primary, Material secondary)
        {
            base.SetMaterials(Instantiate(primary), Instantiate(secondary));
            Material[] materials = m_renderer.sharedMaterials;
            materials[1] = m_primaryMaterial;
            materials[0] = m_secondaryMaterial;
            m_renderer.sharedMaterials = materials;
        }

        [SerializeField]
        private Animator m_animator;

        [SerializeField]
        private Transform m_bodyBone;

        [SerializeField]
        private Transform m_stomic;
        public Transform Stomic
        {
            get { return m_stomic; }
        }        

        public override Transform Body
        {
            get { return m_bodyBone; }
        }

        public override int MinWeight
        {
            get { return GameConstants.MinVoxelActorWeight; }
        }

        public override int Weight
        {
            get { return m_weight; }
            set
            {
                m_weight = value;
                float scale = Mathf.Pow(2, m_weight - GameConstants.MinVoxelActorWeight);

                Vector3 localScale = Root.localScale;
                localScale.x = scale;
                localScale.z = scale;
                Root.localScale = localScale;
            }
        }

        public override int Height
        {
            get { return m_height; }
            set
            {
                m_height = value;
                Vector3 localScale = Root.localScale;
                localScale.y = EvalHeight(m_height);
                Root.localScale = localScale;
            }
        }

        protected override float EvalHeight(int height)
        {
            float result = height / 4.0f;
            result = Mathf.Max(0.01f, result);
            return result;
        }

        /// <summary>
        /// These values used for grow and diminish animations
        /// </summary>
        private Vector3 m_animateFromPosition;
        private Vector3 m_animateToPosition;

        protected override bool IsEnabled
        {
            get { return true; } //VoxelActor always enabled
            set
            {                
            }
        }


        protected override void AwakeOverride()
        {
            m_weight = GameConstants.MinVoxelActorWeight;
      
            m_animationState = VoxelActorState.Idle;
            m_movementState = new NormalMovementState(this);

            RoundStomicRotation();
        }

        protected override void StartOverride()
        {
            base.StartOverride();

            m_animateFromPosition = transform.position;
            m_animateToPosition = transform.position;
       
            if (m_primaryMaterial == null)
            {
                Debug.LogError("Set inner material");
                return;
            }

            if (m_animator == null)
            {
                m_animator = GetComponentInParent<Animator>();
                if (m_animator == null)
                {
                    Debug.LogError("Set animator");
                    return;
                }
            }

        }

        protected override void OnEnableOverride()
        {
            base.OnEnableOverride();

            VoxelStateMachineBehavior[] behaviors = m_animator.GetBehaviours<VoxelStateMachineBehavior>();
            if (behaviors.Length == 0)
            {
                Debug.LogError("Unable to find VoxelStateMachineBehavior");
                return;
            }

            for (int i = 0; i < behaviors.Length; ++i)
            {
                VoxelStateMachineBehavior behavior = behaviors[i];
                behavior.SetStateEnterCallback(OnAnimatorStateEnter);
            }
        }

        protected override void OnDisableOverride()
        {
            base.OnDisableOverride();

            m_actionQueue.Clear();
            m_isInProgress = false;
        }

        private void OnAnimatorMove()
        {
            if (m_isFrozen)
            {
                return;
            }

            Vector3 rootPosition = m_animator.rootPosition;

            if(!IsChangingAltitude)
            {
                if (m_previousAltitude != m_altitude)
                {
                    rootPosition.y = Mathf.Lerp(m_previousAltitude * GameConstants.UnitSize, m_altitude * GameConstants.UnitSize,
                        m_previousAltitude < m_altitude ?
                            m_animator.GetFloat("jumpCurve") :
                            m_animator.GetFloat("fallCurve"));
                }
            }

            if (m_animateFromPosition != m_animateToPosition)
            {
                m_animateFromPosition.y = rootPosition.y;
                m_animateToPosition.y = rootPosition.y;

                rootPosition = Vector3.Lerp(m_animateFromPosition, m_animateToPosition,
                    m_animationState == VoxelActorState.Grow ?
                        m_animator.GetFloat("growCurve") :
                        m_animator.GetFloat("diminishCurve"));
            }

            transform.position = rootPosition;
            transform.rotation = m_animator.rootRotation;
        }

        protected virtual void OnFlipped(bool flipped)
        {

        }


        private void ExecuteNextAction()
        {
            m_isInProgress = m_actionQueue.Count > 0;
            if (m_isInProgress)
            {
                QueuedActionBase queuedAction = m_actionQueue.Dequeue();

                m_tick = queuedAction.Tick;

                queuedAction.Run();
            }
        }
 
        private void OnAnimatorStateEnter(AnimatorStateInfo stateInfo)
        {
            if (stateInfo.IsName("Wait"))
            {
                if (m_animationState == VoxelActorState.Move1X)
                {
                    m_isFlipped = !m_isFlipped;
                    OnFlipped(m_isFlipped);
      
                    RoundRotation();
                    RoundPosition();
                    RoundStomicRotation();

                    RaiseMoveCompleted(m_tick);
                    ExecuteNextAction();
                }
                else if ((m_animationState & VoxelActorState.Rotate) != 0)
                {
                    RoundRotation();
                    RoundPosition();

                    RaiseRotateCompleted(m_tick);
                    ExecuteNextAction();
                }
                else if ((m_animationState & VoxelActorState.Resize) != 0)
                {
                    if (m_animationState == VoxelActorState.Grow)
                    {
                        Weight++;
                        Height *= 2;
                    }
                    else
                    {
                        Weight--;
                        Height /= 2;
                    }

                    RoundRotation();
                    RoundPosition();

                    RaiseResizeCompleted(m_tick);
                    ExecuteNextAction();
                }

                m_animationState = VoxelActorState.Idle;
            }
            else if (stateInfo.IsName("RotateLeft"))
            {
                m_animationState = VoxelActorState.RotateLeft;
            }
            else if (stateInfo.IsName("RotateRight"))
            {
                m_animationState = VoxelActorState.RotateRight;
            }
            else if (stateInfo.IsName("Grow"))
            {
                m_animationState = VoxelActorState.Grow;
            }
            else if (stateInfo.IsName("Diminish"))
            {
                m_animationState = VoxelActorState.Diminish;
            }
            else if (stateInfo.IsName("Move"))
            {
                if (m_animationState == VoxelActorState.Move2X)
                {
                    OnMoveCompleted();
                }
                m_animationState = VoxelActorState.Move1X;
            }
            else if (stateInfo.IsName("MoveFast"))
            {
                if (m_animationState == VoxelActorState.Move1X)
                {
                    OnMoveCompleted();
                }

                m_animationState = VoxelActorState.Move2X;
            }
        }

        /// <summary>
        /// Raised by Animation Events from Animation tab of VoxelActorImportSettings
        /// </summary>
        private void OnBeginMove()
        {
            RaiseBeginMove(m_tick);
        }

        /// <summary>
        /// Raised by Animation Events from Animation tab of VoxelActorImportSettings
        /// </summary>
        private void OnBeforeMoveCompleted()
        {
            RaiseBeforeMoveCompleted(m_tick);
        }

        /// <summary>
        /// Raised by Animation Events from Animation tab of VoxelActorImportSettings
        /// </summary>
        private void OnMoveCompleted()
        {
            m_movementState.StopMove();

            WriteDebugInfo();
        }

        private void OnRotateLeftCompleted()
        {
            m_movementState.StopRotateLeft();
        }

        private void OnRotateRightCompleted()
        {
            m_movementState.StopRotateRight();
        }

        private void OnBeforeGrowCompleted()
        {
            RaiseBeforeGrowCompleted(m_tick);
        }

        private void OnBeforeDiminishCompleted()
        {
            RaiseBeforeDiminishCompleted(m_tick);
        }

        private void OnGrowCompleted()
        {
            m_movementState.StopGrow();
            WriteDebugInfo();
        }

        private void OnDiminishCompleted()
        {
            m_movementState.StopDiminish();
            WriteDebugInfo();
        }


        /// <summary>
        /// This method will round rotation (in order to fix root motion inaccuracy)
        /// </summary>
        private void RoundRotation()
        {
            Vector3 euler = Root.rotation.eulerAngles;

            euler.x = Mathf.Round(euler.x / 90) * 90;
            euler.y = Mathf.Round(euler.y / 90) * 90;
            euler.z = Mathf.Round(euler.z / 90) * 90;

            Root.rotation = Quaternion.Euler(euler);
            m_animator.rootRotation = Root.rotation;
        }

        /// <summary>
        /// This method will round position (in order to fix root motion inaccuracy)
        /// </summary>
        private void RoundPosition()
        {
            m_previousAltitude = m_altitude;
            m_animateFromPosition = m_animateToPosition;

            Root.position = new Vector3(
                Mathf.Round(Root.position.x / GameConstants.UnitSize) * GameConstants.UnitSize,
                m_altitude * GameConstants.UnitSize,
                Mathf.Round(Root.position.z / GameConstants.UnitSize) * GameConstants.UnitSize);

            m_animator.rootPosition = Root.position;
        }

        /// <summary>
        /// This method will round stomic rotation 
        /// </summary>
        private void RoundStomicRotation()
        {
            if (m_isFlipped)
            {
                m_stomic.localRotation = Quaternion.AngleAxis(270, Vector3.right);
            }
            else
            {
                m_stomic.localRotation = Quaternion.AngleAxis(90, Vector3.right);
            }
        }

        public override void Move(int altitude, long tick, float duration)
        {
            if(m_isInProgress)
            {
                m_actionQueue.Enqueue(new QueuedAction<int>(MoveAction, altitude, tick, duration));
            }
            else
            {
                m_tick = tick;
                m_isInProgress = true;
                MoveAction(altitude, duration);
            }
        }

        private void MoveAction(int altitude, float duration)
        {
            m_altitude = altitude;

            float speed = 2.0f / duration;

            Debug.Assert(enabled);
            Debug.Assert(gameObject.activeInHierarchy);
            Debug.Assert(gameObject.activeSelf);

            m_animator.CrossFade("Move", m_animationState == VoxelActorState.Move1X ? 0 : 0.1f / speed);
            m_animator.SetFloat("moveMultiplier", speed);
            m_animator.SetFloat("waitMultiplier", speed * 5); //five times faster than movement speed

            m_movementState.Move();
        }

        public override void RotateLeft(long tick, float duration)
        {
            if (m_isInProgress)
            {
                m_actionQueue.Enqueue(new QueuedAction(RotateLeftAction, tick, duration));
            }
            else
            {
                m_tick = tick;
                m_isInProgress = true;
                RotateLeftAction(duration);
            }
        }

        private void RotateLeftAction(float duration)
        {
            float speed = 2.0f / duration;

            m_animator.CrossFade("RotateLeft", m_animationState == VoxelActorState.RotateLeft ? 0 : 0.1f / speed);
            m_animator.SetFloat("rotateLeftMultiplier", speed);
            m_animator.SetFloat("waitMultiplier", speed * 5);

            m_movementState.RotateLeft();
        }

        public override void RotateRight(long tick, float duration)
        {
            if (m_isInProgress)
            {
                m_actionQueue.Enqueue(new QueuedAction(RotateRightAction, tick, duration));
            }
            else
            {
                m_tick = tick;
                m_isInProgress = true;
                RotateRightAction(duration);
            }
        }

        private void RotateRightAction(float duration)
        {
            float speed = 2.0f / duration;

            m_animator.CrossFade("RotateRight", m_animationState == VoxelActorState.RotateRight ? 0 : 0.1f / speed);
            m_animator.SetFloat("rotateRightMultiplier", speed);
            m_animator.SetFloat("waitMultiplier", speed * 5);

            m_movementState.RotateRight();
        }

        public override void Grow(Vector3 position, long tick, float duration)
        {
            if (m_isInProgress)
            {
                m_actionQueue.Enqueue(new QueuedAction<Vector3>(GrowAction, position, tick, duration));
            }
            else
            {
                m_tick = tick;
                m_isInProgress = true;
                GrowAction(position, duration);
            }
        }

        private void GrowAction(Vector3 position, float duration)
        {
            m_animateFromPosition = transform.position;
            m_animateToPosition = position;

            float speed = 2.0f / duration;

            m_animator.CrossFade("Grow", m_animationState == VoxelActorState.Grow ? 0 : 0.1f / speed);
            m_animator.SetFloat("growMultiplier", speed);
            m_animator.SetFloat("waitMultiplier", speed * 5);

            m_movementState.Grow();
        }

        public override void Diminish(Vector3 position, long tick, float duration)
        {
            if (m_isInProgress)
            {
                m_actionQueue.Enqueue(new QueuedAction<Vector3>(DiminishAction, position, tick, duration));
            }
            else
            {
                m_tick = tick;
                m_isInProgress = true;
                DiminishAction(position, duration);
            }
        }

        private void DiminishAction(Vector3 position, float duration)
        {
            m_animateFromPosition = transform.position;
            m_animateToPosition = position;

            float speed = 2.0f / duration;

            m_animator.CrossFade("Diminish", m_animationState == VoxelActorState.Grow ? 0 : 0.1f / speed);
            m_animator.SetFloat("diminishMultiplier", speed);
            m_animator.SetFloat("waitMultiplier", speed * 5);

            m_movementState.Diminish();
        }

        /// <summary>
        /// Can't recieve or handle any commands while frozen
        /// </summary>
        public override void Freeze()
        {
            base.Freeze();

            m_movementState.StopRotateLeft();
            m_movementState.StopRotateRight();
            m_movementState.StopMove();
            m_movementState.StopGrow();
            m_movementState.StopDiminish();
            m_movementState.StopJump();

            m_movementState = new FrozenMovementState();
            m_isFrozen = true;
        }

        public override void Unfreeze()
        {
            base.Unfreeze();
            m_movementState = new NormalMovementState(this);
            m_isFrozen = false;
        }

        public override void ReadFrom(VoxelData data)
        {
            base.ReadFrom(data);

            RoundPosition();

            m_animateFromPosition = transform.position;
            m_animateToPosition = transform.position;
        }


    }

}
