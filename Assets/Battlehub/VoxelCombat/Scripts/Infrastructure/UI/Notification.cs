using Battlehub.UIControls;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class Notification : MonoBehaviour, INotification
    {
        private static Notification m_notificationRoot;

        [SerializeField]
        private GameObject[] m_deactivate;

        [SerializeField]
        private GameObject m_root;

        [SerializeField]
        private Button m_closeButton;

        [SerializeField]
        private Text m_text;

        private Action m_doOnClose;

        private readonly List<Notification> m_children = new List<Notification>();

        private IEnumerator m_coSelect;

        private void Awake()
        {
            m_closeButton.onClick.AddListener(OnCloseClick);
        }

        private void Start()
        {
            
        }

        private void OnEnable()
        {
            if (m_notificationRoot == null)
            {
                m_notificationRoot = this;
            }
            if (m_notificationRoot != null && m_notificationRoot != this)
            {
                m_notificationRoot.m_children.Add(this);
            }
        }

        private void OnDisable()
        {
            if (m_notificationRoot != null && m_notificationRoot != this)
            {
                m_notificationRoot.m_children.Remove(this);
            }

            if(m_coSelect != null)
            {
                StopCoroutine(m_coSelect);
                m_coSelect = null;
            }
        }

        private void OnDestroy()
        {
            if(m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseClick);
            }

            if (m_notificationRoot == this)
            {
                m_notificationRoot = null;
            }
        }


        public void Show(string text, GameObject selectOnClose = null)
        {
            ShowWithAction(text, () => SelectOnClose(selectOnClose));
        }

        private IEnumerator CoSelect()
        {
            yield return new WaitForEndOfFrame();
            IndependentSelectable.Select(m_closeButton.gameObject);

        }

        public void Close()
        {
            m_root.SetActive(false);

            for (int i = 0; i < m_deactivate.Length; ++i)
            {
                m_deactivate[i].SetActive(true);
            }

            if (m_coSelect != null)
            {
                StopCoroutine(m_coSelect);
                m_coSelect = null;
            } 
        }

        private void OnCloseClick()
        {
            Close();

            if(m_doOnClose != null)
            {
                m_doOnClose();
            }
        }

        private void SelectOnClose(GameObject selectOnClose)
        {
            if (selectOnClose != null && selectOnClose.activeInHierarchy)
            {
                IndependentSelectable.Select(selectOnClose.gameObject);
            }
            else
            {
                IndependentSelectable selectable = m_closeButton.GetComponent<IndependentSelectable>();
                if (selectable != null)
                {
                    Selectable nextSelectable = selectable.FindSelectableOnDown();
                    if (nextSelectable == null)
                    {
                        nextSelectable = selectable.FindSelectableOnLeft();
                        if (nextSelectable == null)
                        {
                            nextSelectable = selectable.FindSelectableOnRight();
                            if (nextSelectable == null)
                            {
                                nextSelectable = selectable.FindSelectableOnUp();
                            }
                        }
                    }

                    if (nextSelectable != null)
                    {
                        IndependentSelectable.Select(nextSelectable.gameObject);
                    }
                }
            }
        }

        public void ShowError(string error, GameObject selectOnClose = null)
        {
            Show(error, selectOnClose);
            Debug.LogError(error);
        }

        public void ShowError(Error error, GameObject selectOnClose = null)
        {
            Show(error.ToString(), selectOnClose);
            Debug.LogError(error.ToString());
        }

        public INotification GetChild(int index)
        {
            for (int i = 0; i < m_children.Count; ++i)
            {
                Notification notification = m_children[i];
                if (notification != null)
                {
                    PlayerUIZone zone = notification.GetComponentInParent<PlayerUIZone>();
                    if (zone.LocalPlayerIndex == index)
                    {
                        return notification;
                    }
                }
            }

            return null;
        }

        public void ShowWithAction(string text, Action onClose = null)
        {
            m_root.SetActive(true);
            m_text.text = text;

            for (int i = 0; i < m_deactivate.Length; ++i)
            {
                m_deactivate[i].SetActive(false);
            }

            m_coSelect = CoSelect();
            StartCoroutine(m_coSelect);

            m_doOnClose = onClose;
        }

        public void ShowErrorWithAction(string error, Action onClose = null)
        {
            ShowWithAction(error, onClose);
            Debug.LogError(error);
        }

        public void ShowErrorWithAction(Error error, Action onClose = null)
        {
            ShowWithAction(error.ToString(), onClose);
            Debug.LogError(error);
        }
    }

}
