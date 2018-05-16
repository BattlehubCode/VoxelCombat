using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEventSystem = UnityEngine.EventSystems.EventSystem;
using System;

namespace Battlehub.UIControls
{
    public interface IIndependentMoveHandler : IMoveHandler
    {

    }

    public interface IBeforeSelectHandler : IEventSystemHandler
    {
        void OnBeforeSelect(BaseEventData eventData);
    }

    [RequireComponent(typeof(Selectable))]
    public class IndependentSelectable : MonoBehaviour, IIndependentMoveHandler
    {
        [SerializeField]
        private Selectable[] m_upSelectables;
        [SerializeField]
        private Selectable[] m_downSelectables;
        [SerializeField]
        private Selectable[] m_leftSelectables;
        [SerializeField]
        private Selectable[] m_rightSelectables;

        public static ExecuteEvents.EventFunction<IIndependentMoveHandler> moveHandler
        {
            get {return (handler, eventData) => handler.OnMove(ExecuteEvents.ValidateEventData<AxisEventData>(eventData)); }
        }

        public static ExecuteEvents.EventFunction<IBeforeSelectHandler> beforeSelectHandler
        {
            get { return (handler, eventData) => handler.OnBeforeSelect(null); }
        }

        public static ExecuteEvents.EventFunction<IUpdateFocusedHandler> updateFocusedHandler
        {
            get { return (handler, eventData) => handler.OnUpdateFocused(eventData); }
        }

        [SerializeField]
        private IndependentEventSystem m_eventSystem;

        [SerializeField]
        private Selectable m_selectable;

        private int m_selectOnEventSystemLateUpdate;
        private bool m_unselect;

        public IndependentEventSystem EventSystem
        {
            get { return m_eventSystem; }
            set
            {
                if(!isActiveAndEnabled)
                {
                    m_eventSystem = value;
                }
                else
                {
                    if (m_eventSystem != null)
                    {
                        m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
                        m_eventSystem.EventSystemLateUpdate -= OnEventSystemLateUpdate;
                    }

                    m_eventSystem = value;

                    if (m_eventSystem != null)
                    {
                        m_eventSystem.EventSystemUpdate += OnEventSystemUpdate;
                        m_eventSystem.EventSystemLateUpdate += OnEventSystemLateUpdate;
                    }
                }
            }
        }
        protected virtual void Start()
        {
            if(m_eventSystem == null)
            {
                m_eventSystem = GetComponentInParent<IndependentEventSystem>();
            }


            if(m_eventSystem == null)
            {
                m_eventSystem = UnityEventSystem.current as IndependentEventSystem;
            }
            
            if(m_selectable == null)
            {
                m_selectable = GetComponent<Selectable>();
            }

            m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
            m_eventSystem.EventSystemLateUpdate -= OnEventSystemLateUpdate;
            m_eventSystem.EventSystemUpdate += OnEventSystemUpdate;
            m_eventSystem.EventSystemLateUpdate += OnEventSystemLateUpdate;
        }

        protected virtual void OnEnable()
        {
            if(m_eventSystem != null)
            {
                m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
                m_eventSystem.EventSystemLateUpdate -= OnEventSystemLateUpdate;
                m_eventSystem.EventSystemUpdate += OnEventSystemUpdate;
                m_eventSystem.EventSystemLateUpdate += OnEventSystemLateUpdate;
            }   
        }

        protected virtual void OnDisable()
        {
            m_selectOnEventSystemLateUpdate = 0;
            m_unselect = false;

            if (m_eventSystem != null)
            {
                m_eventSystem.EventSystemUpdate -= OnEventSystemUpdate;
                m_eventSystem.EventSystemLateUpdate -= OnEventSystemLateUpdate;
            }
        }

        protected virtual void OnDestroy()
        {
        }

        private void Navigate(AxisEventData eventData, Selectable sel)
        {
            if (sel != null && sel.IsActive())
            {
                ExecuteEvents.Execute(sel.gameObject, null, beforeSelectHandler); //execute this event to prevent Activation of input field

                eventData.selectedObject = sel.gameObject;
            }
                
        }

        // Find the selectable object to the left of this one.
        public virtual Selectable FindSelectableOnLeft()
        {
            Selectable[] selectables = m_leftSelectables;
            if(selectables != null && selectables.Length > 0)
            {
                for(int i = 0; i < selectables.Length; ++i)
                {
                    Selectable sel = selectables[i];
                    if(sel != null && sel.IsActive() && sel.IsInteractable())
                    {
                        return sel;
                    }
                }
            }

            if (m_selectable.navigation.mode == Navigation.Mode.Explicit)
            {
                return m_selectable.navigation.selectOnLeft;
            }
            if ((m_selectable.navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.left);
            }
            return null;
        }

        // Find the selectable object to the right of this one.
        public virtual Selectable FindSelectableOnRight()
        {
            Selectable[] selectables = m_rightSelectables;
            if (selectables != null && selectables.Length > 0)
            {
                for (int i = 0; i < selectables.Length; ++i)
                {
                    Selectable sel = selectables[i];
                    if (sel != null && sel.IsActive() && sel.IsInteractable())
                    {
                        return sel;
                    }
                }
            }
            if (m_selectable.navigation.mode == Navigation.Mode.Explicit)
            {
                return m_selectable.navigation.selectOnRight;
            }
            if ((m_selectable.navigation.mode & Navigation.Mode.Horizontal) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.right);
            }
            return null;
        }

        // Find the selectable object above this one
        public virtual Selectable FindSelectableOnUp()
        {
            Selectable[] selectables = m_upSelectables;
            if (selectables != null && selectables.Length > 0)
            {
                for (int i = 0; i < selectables.Length; ++i)
                {
                    Selectable sel = selectables[i];
                    if (sel != null && sel.IsActive() && sel.IsInteractable())
                    {
                        return sel;
                    }
                }
            }
            if (m_selectable.navigation.mode == Navigation.Mode.Explicit)
            {
                return m_selectable.navigation.selectOnUp;
            }
            if ((m_selectable.navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.up);
            }
            return null;
        }

        // Find the selectable object below this one.
        public virtual Selectable FindSelectableOnDown()
        {
            Selectable[] selectables = m_downSelectables;
            if (selectables != null && selectables.Length > 0)
            {
                for (int i = 0; i < selectables.Length; ++i)
                {
                    Selectable sel = selectables[i];
                    if (sel != null && sel.IsActive() && sel.IsInteractable())
                    {
                        return sel;
                    }
                }
            }
            if (m_selectable.navigation.mode == Navigation.Mode.Explicit)
            {
                return m_selectable.navigation.selectOnDown;
            }
            if ((m_selectable.navigation.mode & Navigation.Mode.Vertical) != 0)
            {
                return FindSelectable(transform.rotation * Vector3.down);
            }
            return null;
        }

        public Selectable FindSelectable(Vector3 dir)
        {
            dir = dir.normalized;
            Vector3 localDir = Quaternion.Inverse(transform.rotation) * dir;
            Vector3 pos = transform.TransformPoint(GetPointOnRectEdge(transform as RectTransform, localDir));
            float maxScore = Mathf.NegativeInfinity;
            Selectable bestPick = null;
            for (int i = 0; i < Selectable.allSelectables.Count; ++i)
            {
                Selectable sel = Selectable.allSelectables[i];

                if (sel == this || sel == null)
                    continue;

                if (!sel.IsInteractable() || sel.navigation.mode == Navigation.Mode.None)
                    continue;

                IndependentSelectable independentSelectable = sel.GetComponent<IndependentSelectable>();
                if(independentSelectable == null)
                {
                    continue;
                }

                if(independentSelectable.EventSystem != m_eventSystem)
                {
                    continue;
                }

                var selRect = sel.transform as RectTransform;
                Vector3 selCenter = selRect != null ? (Vector3)selRect.rect.center : Vector3.zero;
                Vector3 myVector = sel.transform.TransformPoint(selCenter) - pos;

                // Value that is the distance out along the direction.
                float dot = Vector3.Dot(dir, myVector);

                // Skip elements that are in the wrong direction or which have zero distance.
                // This also ensures that the scoring formula below will not have a division by zero error.
                if (dot <= 0)
                    continue;

                // This scoring function has two priorities:
                // - Score higher for positions that are closer.
                // - Score higher for positions that are located in the right direction.
                // This scoring function combines both of these criteria.
                // It can be seen as this:
                //   Dot (dir, myVector.normalized) / myVector.magnitude
                // The first part equals 1 if the direction of myVector is the same as dir, and 0 if it's orthogonal.
                // The second part scores lower the greater the distance is by dividing by the distance.
                // The formula below is equivalent but more optimized.
                //
                // If a given score is chosen, the positions that evaluate to that score will form a circle
                // that touches pos and whose center is located along dir. A way to visualize the resulting functionality is this:
                // From the position pos, blow up a circular balloon so it grows in the direction of dir.
                // The first Selectable whose center the circular balloon touches is the one that's chosen.
                float score = dot / myVector.sqrMagnitude;

                if (score > maxScore)
                {
                    maxScore = score;
                    bestPick = sel;
                }
            }
            return bestPick;
        }

        private static Vector3 GetPointOnRectEdge(RectTransform rect, Vector2 dir)
        {
            if (rect == null)
                return Vector3.zero;
            if (dir != Vector2.zero)
                dir /= Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            dir = rect.rect.center + Vector2.Scale(rect.rect.size, dir * 0.5f);
            return dir;
        }

        public void OnMove(AxisEventData eventData)
        {
            switch (eventData.moveDir)
            {
                case MoveDirection.Right:
                    Navigate(eventData, FindSelectableOnRight());
                    break;

                case MoveDirection.Up:
                    Navigate(eventData, FindSelectableOnUp());
                    break;

                case MoveDirection.Left:
                    Navigate(eventData, FindSelectableOnLeft());
                    break;

                case MoveDirection.Down:
                    Navigate(eventData, FindSelectableOnDown());
                    break;
            }
        }

        protected virtual void OnEventSystemUpdate()
        {
           
        }

        protected virtual void OnEventSystemLateUpdate()
        {
            if (m_selectOnEventSystemLateUpdate == 1)
            {
                m_selectOnEventSystemLateUpdate--;

                Selectable selectable = gameObject.GetComponent<Selectable>();
                
                if(m_unselect)
                {
                    if (m_eventSystem.currentSelectedGameObject == gameObject)
                    {
                        EventSystem.SetSelectedGameObject(null);
                    }
                    m_unselect = false;
                }
                else
                {
                    if (selectable == null || selectable.IsInteractable())
                    {
                        EventSystem.SetSelectedGameObject(gameObject);
                    }
                }  
            }
            else if(m_selectOnEventSystemLateUpdate > 1)
            {
                m_selectOnEventSystemLateUpdate--;
            }

        }

        public static bool IsSelected(GameObject go)
        {
            IndependentSelectable selectable = go.GetComponent<IndependentSelectable>();
            if (selectable != null)
            {
                return selectable.EventSystem.currentSelectedGameObject == go;
            }
            return false;
        }


        public static void Select(GameObject go, int skipFrames = 0)
        {
            IndependentSelectable selectable = go.GetComponent<IndependentSelectable>();
            if (selectable != null)
            {
                selectable.m_unselect = false;
                selectable.m_selectOnEventSystemLateUpdate = 1 + skipFrames;
            }
        }


        public static void Select(UIBehaviour ui, int skipFrames = 0)
        {
            IndependentSelectable selectable = ui.GetComponent<IndependentSelectable>();
            if (selectable != null)
            {
                selectable.m_unselect = false;
                selectable.m_selectOnEventSystemLateUpdate = 1 + skipFrames;
            }
        }

        public static void Unselect(GameObject go, int skipFrames = 0)
        {
            IndependentSelectable selectable = go.GetComponent<IndependentSelectable>();
            if (selectable != null)
            {
                selectable.m_unselect = true;
                selectable.m_selectOnEventSystemLateUpdate = 1 + skipFrames;
            }
        }


        public static void Unselect(UIBehaviour ui, int skipFrames = 0)
        {
            IndependentSelectable selectable = ui.GetComponent<IndependentSelectable>();
            if (selectable != null)
            {
                selectable.m_unselect = true;
                selectable.m_selectOnEventSystemLateUpdate = 1 + skipFrames;
            }
        }

        public static IndependentEventSystem GetEventSystem(GameObject go)
        {
            IndependentSelectable selectable = go.GetComponent<IndependentSelectable>();
            if(selectable != null)
            {
                return selectable.EventSystem;
            }

            return null;
        }
        public static IndependentEventSystem GetEventSystem(UIBehaviour ui)
        {
            IndependentSelectable selectable = ui.GetComponent<IndependentSelectable>();
            if (selectable != null)
            {
                return selectable.EventSystem;
            }

            return null;
        }

    }
}

