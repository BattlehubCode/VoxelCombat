using System;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    public class InputFieldWithVirtualKeyboard : IndependentSelectable, IBeforeSelectHandler, ISelectHandler, IDeselectHandler, IPointerClickHandler, IUpdateFocusedHandler
    {
        [SerializeField]
        private bool m_virtualKeyboardEnabled = true;
        [SerializeField]
        private InputField m_inputField;
        [SerializeField]
        private VirtualKeyboard m_virtualKeyboard;
        [SerializeField]
        private Vector3 m_virtualKeyboardOffset;
        [SerializeField]
        private bool m_setVirtualKeyboardPostion = true;
        //[SerializeField]
        //private bool m_openOnSelect;

        private bool m_isEditing;
        private bool IsEditing
        {
            get { return m_isEditing; }
            set
            {
                m_isEditing = value;
            }
        }

        public bool VirtualKeyboardEnabled
        {
            get { return m_virtualKeyboardEnabled; }
            set
            {
                if(!value)
                {
                    if (m_virtualKeyboard != null)
                    {
                        if(m_inputField != null)
                        {
                            m_inputField.DeactivateInputField();
                        }
                        m_virtualKeyboard.KeyPressed -= OnKeyPressed;
                    }

                    if(m_inputField != null)
                    {
                        m_inputField.readOnly = m_isInputFieldReadOnly;
                    }
                    DestroyHelpers();
                }
                m_virtualKeyboardEnabled = value;
            }
        }
       
        public VirtualKeyboard VirtualKeyboard
        {
            get { return m_virtualKeyboard; }
            set
            {
                if(m_virtualKeyboard != null)
                {
                    m_inputField.DeactivateInputField();
                    m_virtualKeyboard.KeyPressed -= OnKeyPressed;
                }
                m_virtualKeyboard = value;
            }
        }

        public Vector3 VirtualKeyboardOffset
        {
            get { return m_virtualKeyboardOffset; }
            set { m_virtualKeyboardOffset = value; }
        }

        private VirtualKey m_enterKey;
        private DisableInputFieldKeyboard m_disableInputFieldKeyboardInput;
        private InputFieldIsFocusedListener m_isFocusedListener;

        private bool m_isInputFieldReadOnly;
        private string m_text;
        private Text m_textComponent;
        private bool m_skipUpdate;

        private bool m_activateOnEventSystemLateUpdate;

        [SerializeField]
        private float m_repeatDelay = 0.035f;
        private float m_repeatT;

        private RectTransform m_inputRT;
        private RectTransform m_kbRT;

        private void Awake()
        {
            if (m_inputField == null)
            {
                m_inputField = GetComponent<InputField>();
            }
            m_isInputFieldReadOnly = m_inputField.readOnly;
            m_textComponent = m_inputField.textComponent;
        }

        protected override void Start()
        {
            base.Start();
            if(m_virtualKeyboard != null)
            {
                m_enterKey = m_virtualKeyboard.Keys.Where(k => k.KeyCode == KeyCode.Return).FirstOrDefault();
            }
           
        }

        private void OnApplicationQuit()
        {
            if (m_inputField != null)
            {
                m_inputField.DeactivateInputField();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            m_activateOnEventSystemLateUpdate = false;
            m_skipUpdate = false;
            if (m_textComponent != null)
            {
                m_inputField.textComponent = m_textComponent;
            }

            m_virtualKeyboard.IsOn = false;
            HandleDeselect();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            m_inputField = null;
            DestroyHelpers();
        }

        private void OnGUI()
        {
            if(VirtualKeyboardEnabled)
            {
                if (IsEditing)
                {
                    if (!m_virtualKeyboard.IsOn)
                    {
                        m_virtualKeyboard.Target = m_inputField;
                        m_virtualKeyboard.IsOn = true;
                    }
                }
                else
                {
                    if (m_virtualKeyboard.IsOn && m_virtualKeyboard.Target == m_inputField)
                    {
                        m_virtualKeyboard.IsOn = false;
                    }
                }
            }
           

            if (m_setVirtualKeyboardPostion)
            {
                SetVirtualKeyboardPosition();
            }
        }
    

        public void OnBeforeSelect(BaseEventData eventData)
        {
            //if (!m_openOnSelect)
            {
                if (m_inputField.textComponent != null)
                {
                    m_textComponent = m_inputField.textComponent;
                    m_inputField.textComponent = null;
                    m_skipUpdate = true;
                }
            }
        }

        /// <summary>
        /// Must be executed befroe OnSelect of InputField
        /// </summary>
        /// <param name="eventData"></param>
        public void OnSelect(BaseEventData eventData)
        {
            if(gameObject.GetComponent<DisableInputFieldKeyboard>() == null)
            {
                m_disableInputFieldKeyboardInput = gameObject.AddComponent<DisableInputFieldKeyboard>();
                m_disableInputFieldKeyboardInput.EventSystem = EventSystem;
            }

            //if (m_openOnSelect)
            //{
            //    IsEditing = true;
            //}
            //else
            {
                ClearTextComponent();
            }

            if(m_virtualKeyboard != null)
            {
                m_virtualKeyboard.KeyPressed -= OnKeyPressed;
                m_virtualKeyboard.KeyPressed += OnKeyPressed;
            }
            

            m_isInputFieldReadOnly = m_inputField.readOnly;
            if(VirtualKeyboardEnabled)
            {
                m_inputField.readOnly = true;
            }

            if (gameObject.GetComponent<InputFieldIsFocusedListener>() == null)
            {
                m_isFocusedListener = gameObject.AddComponent<InputFieldIsFocusedListener>();
                m_isFocusedListener.IsFocusedChanged += OnIsFocusedChanged;
            }

            m_inputRT = m_inputField.GetComponent<RectTransform>();

            if (m_virtualKeyboard != null)
            {
                m_kbRT = m_virtualKeyboard.GetComponent<RectTransform>();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            ClearTextComponent();
        }

        private void ClearTextComponent()
        {
            if (m_inputField.textComponent != null)
            {
                m_textComponent = m_inputField.textComponent;
                m_inputField.textComponent = null;
                m_skipUpdate = true;
            }
        }

        public void OnDeselect(BaseEventData eventData)
        {
            HandleDeselect();
        }

        private void HandleDeselect()
        {
            DestroyHelpers();

            IsEditing = false;
            if (m_virtualKeyboard != null)
            {
                m_virtualKeyboard.KeyPressed -= OnKeyPressed;
            }

            m_inputField.readOnly = m_isInputFieldReadOnly;
        }

        private void DestroyHelpers()
        {
            if (m_disableInputFieldKeyboardInput != null)
            {
                m_disableInputFieldKeyboardInput.Destroy();
                m_disableInputFieldKeyboardInput = null;
            }

            if (m_isFocusedListener != null)
            {
                m_isFocusedListener.IsFocusedChanged -= OnIsFocusedChanged;
                Destroy(m_isFocusedListener);
                m_isFocusedListener = null;
            }
        }


        private void OnIsFocusedChanged(object sender, EventArgs e)
        {
            //if (!m_openOnSelect)
            //{
                if (!m_inputField.isFocused)
                {
                    IsEditing = false;
                }
            //}
            //else
            //{
            //    IsEditing = m_inputField.isFocused;
            //}

            if (m_inputField.isFocused)
            {
                m_text = m_inputField.text;
            }
        }

        private void OnKeyPressed(VirtualKeyboard keyboard, VirtualKey key)
        {
            if (!m_inputField.isFocused)
            {
                return;
            }

            if (key.IsFunctional)
            {
                switch (key.KeyCode)
                {
                    case KeyCode.Backspace:
                        string text = m_inputField.text;
                        if (m_inputField.selectionAnchorPosition != m_inputField.selectionFocusPosition)
                        {
                            int start = Mathf.Min(m_inputField.selectionAnchorPosition, m_inputField.selectionFocusPosition);
                            int end = Math.Max(m_inputField.selectionAnchorPosition, m_inputField.selectionFocusPosition);

                            m_inputField.text = text.Remove(start, end - start);
                            m_inputField.caretPosition = start;
                        }
                        else
                        {
                            if (m_inputField.caretPosition > 0)
                            {
                                int prevCaretPosition = m_inputField.caretPosition;

                                m_inputField.text = text.Remove(m_inputField.caretPosition - 1, 1);

                                if (prevCaretPosition == m_inputField.caretPosition)
                                {
                                    m_inputField.caretPosition -= 1;
                                }
                            }
                        }

                        break;
                    case KeyCode.Return:
                        m_inputField.DeactivateInputField();
                        break;
                }
            }
            else
            {
                string text = m_inputField.text;
                if (m_inputField.selectionAnchorPosition != m_inputField.selectionFocusPosition)
                {
                    int start = Mathf.Min(m_inputField.selectionAnchorPosition, m_inputField.selectionFocusPosition);
                    int end = Math.Max(m_inputField.selectionAnchorPosition, m_inputField.selectionFocusPosition);

                    text = text.Remove(start, end - start);
                    m_inputField.text = text.Insert(start, key.Char);
                    m_inputField.caretPosition = start + key.Char.Length;
                }
                else
                {
                    m_inputField.text = text.Insert(m_inputField.caretPosition, key.Char);
                    m_inputField.caretPosition += key.Char.Length;
                }
            }

            m_inputField.ForceLabelUpdate();
        }


        protected override void OnEventSystemLateUpdate()
        {
            base.OnEventSystemLateUpdate();

            //if (!m_openOnSelect)
            {
                if (!IsEditing)
                {
                    if (m_inputField.isFocused)
                    {
                        m_inputField.DeactivateInputField();
                    }
                }
            }

            RestoreTextComponent();
            TryActivateInputFieldInternal();
        }

        private void TryActivateInputFieldInternal()
        {
            if (m_activateOnEventSystemLateUpdate)
            {
                m_activateOnEventSystemLateUpdate = false;
                m_inputField.SendMessage("ActivateInputFieldInternal");
            }
        }

        private void RestoreTextComponent()
        {
            if (m_inputField.textComponent == null)
            {
                if (m_skipUpdate)
                {
                    m_inputField.textComponent = m_textComponent;
                }
                else
                {
                    m_skipUpdate = false;
                }
            }
        }

        protected override void OnEventSystemUpdate()
        {
            base.OnEventSystemUpdate();
 
            InputModule inputModule = EventSystem.currentInputModule as InputModule;
            if (inputModule != null && inputModule.InputProvider != null)
            {
                InputProvider input = inputModule.InputProvider;
            
                if(input.IsMouseButtonDown(0))
                {
                    if (IsSelected(m_inputField.gameObject))
                    {
                        IsEditing = true;
                        m_activateOnEventSystemLateUpdate = true;
                    }
                    return;
                }

                if (input.IsSubmitButtonDown)
                {
                    if(IsSelected(m_inputField.gameObject))
                    {
                        if (IsEditing)
                        {
                            if(!VirtualKeyboardEnabled)
                            {
                                IsEditing = false;
                                m_inputField.DeactivateInputField();
                            }
                        }
                        else
                        {
                            IsEditing = true;
                            m_activateOnEventSystemLateUpdate = true;
                        }
                    }
                    
                    return;
                }

                if (input.IsCancelButtonDown)
                {
                    if (IsEditing && IsSelected(m_inputField.gameObject))
                    {
                        m_inputField.text = m_text;
                        m_inputField.DeactivateInputField();
                    }
                    return;
                }

                if (input.IsFunctional2ButtonDown && IsEditing)
                {
                    if(m_enterKey != null)
                    {
                        m_enterKey.OnSubmit(null);
                        m_enterKey.OnEndSubmit(null);
                    }
                  
                    return;
                }

                float h = input.HorizontalAxis2;
                if (h > 0.5 || h > 0 && input.IsHorizontal2ButtonDown)
                {
                    h = 1;
                }
                else if (h < -0.5 || h < 0 && input.IsHorizontal2ButtonDown)
                {
                    h = -1;
                }
                else
                {
                    h = 0;
                }

                if (!Mathf.Approximately(h, 0))
                {
                    if (m_repeatT < Time.time)
                    {
                        if (input.IsHorizontal2ButtonDown)
                        {
                            m_repeatT = Time.time + m_repeatDelay * 3;
                        }
                        else
                        {
                            m_repeatT = Time.time + m_repeatDelay;
                        }

                        if (h > 0)
                        {
                            if (input.IsFunctionalButtonPressed)
                            {
                                m_inputField.selectionFocusPosition++;
                            }
                            else
                            {
                                if (m_inputField.selectionAnchorPosition != m_inputField.selectionFocusPosition)
                                {
                                    m_inputField.caretPosition =
                                        m_inputField.selectionFocusPosition > m_inputField.selectionAnchorPosition ?
                                        m_inputField.selectionFocusPosition :
                                        m_inputField.selectionAnchorPosition;
                                }
                                else
                                {
                                    m_inputField.caretPosition++;
                                }
                            }
                            m_inputField.ForceLabelUpdate();
                        }
                        else if (h < 0)
                        {
                            if (input.IsFunctionalButtonPressed)
                            {
                                m_inputField.selectionFocusPosition--;
                            }
                            else
                            {
                                if (m_inputField.selectionAnchorPosition != m_inputField.selectionFocusPosition)
                                {
                                    m_inputField.caretPosition = m_inputField.selectionFocusPosition < m_inputField.selectionAnchorPosition ?
                                        m_inputField.selectionFocusPosition :
                                        m_inputField.selectionAnchorPosition;
                                }
                                else
                                {
                                    m_inputField.caretPosition--;
                                }
                            }
                            m_inputField.ForceLabelUpdate();
                        }
                    }
                }
            }
        }

        private void SetVirtualKeyboardPosition()
        {
            if (m_kbRT != null && m_virtualKeyboard.Target == m_inputField)
            {
                if (m_kbRT.pivot != m_inputRT.pivot)
                {
                    m_kbRT.pivot = m_inputRT.pivot;
                }
                if (m_kbRT.position != (m_inputRT.position + m_virtualKeyboardOffset))
                {
                    m_kbRT.position = m_inputRT.position + m_virtualKeyboardOffset;
                }
            }
        }

        public static void ActivateInputField(InputField field)
        {
            InputFieldWithVirtualKeyboard ifwvk = field.GetComponent<InputFieldWithVirtualKeyboard>();
            if (ifwvk != null)
            {
                ifwvk.IsEditing = true;
                ifwvk.m_activateOnEventSystemLateUpdate = true;
            }
        }

        public void OnUpdateFocused(BaseEventData eventData)
        {
            if(m_inputField.isFocused)
            {
                eventData.Use();
            }
        }
    }
}

