using Battlehub.UIControls;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

namespace Battlehub.VoxelCombat
{
    public class ConsolePanel : ButtonsPanel, IConsole
    {
        public event ConsoleCommandHandler Command;

        [SerializeField]
        private InputField m_inputField;

        [SerializeField]
        private ScrollRect m_messageScrollRect;

        [SerializeField]
        private int m_maxMessages = 64;

        [SerializeField]
        private Text m_messageUIPrefab;

        private Queue<DateTime> m_timeQueue;
        private Queue<string> m_messageQueue;
        private string[] m_messages;
        private int m_messageIndex = -1;

        private Queue<Text> m_uiQueue;

        private bool m_isExpanded;

        private IVoxelInputManager m_inputManager;

        private void Awake()
        {
            m_inputManager = Dependencies.InputManager;

            m_timeQueue = new Queue<System.DateTime>();
            m_messageQueue = new Queue<string>();
            m_uiQueue = new Queue<Text>();

            if (m_messageUIPrefab == null)
            {
                Debug.LogError("set messageUIPrefab");
                return;
            }

            if (m_inputField == null)
            {
                Debug.LogError("set inputField");
                return;
            }

            if (m_messageScrollRect == null)
            {
                Debug.LogError("set messageScrollRect");
                return;
            }

            m_inputField.onValidateInput += OnValidateInput;
            m_inputField.onEndEdit.AddListener(OnInputEndEdit);

            ClearUIQueue();
        }

        protected override void Start()
        {
            base.Start();

            GameViewport parentViewport = GetComponentInParent<GameViewport>();

            InputFieldWithVirtualKeyboard ifwk = m_inputField.GetComponent<InputFieldWithVirtualKeyboard>();
            ifwk.VirtualKeyboardEnabled = !m_inputManager.IsKeyboardAndMouse(parentViewport.LocalPlayerIndex);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(m_inputField != null)
            {
                m_inputField.onValidateInput -= OnValidateInput;
                m_inputField.onEndEdit.RemoveListener(OnInputEndEdit);
            }    
        }

        private IEnumerator m_coSelect;
        protected override void OnEnable()
        {
            base.OnEnable();

            m_coSelect = CoSelect();
            StartCoroutine(m_coSelect);

            m_isExpanded = true;

            PopulateUIQueue();
        }

        private IEnumerator CoSelect()
        {
            yield return new WaitForEndOfFrame();

            IndependentSelectable.Select(m_inputField.gameObject);
            InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);

            m_coSelect = null;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (m_coSelect != null)
            {
                StopCoroutine(m_coSelect);
                m_coSelect = null;
            }

            m_isExpanded = false;

     
            //  m_inputField.DeactivateInputField();

            //  IndependentSelectable.GetEventSystem(m_inputField).SetSelectedGameObject(null);

            ClearUIQueue();
        }

        private void Update()
        {
            if (m_isExpanded && m_messageQueue.Count > 0)
            {
                if(m_inputManager.GetButtonDown(InputAction.CursorY, LocalPlayerIndex, false))
                {
                    if (m_inputManager.GetAxisRaw(InputAction.CursorY, LocalPlayerIndex, false) > 0)
                    {
                        if (m_messages == null)
                        {
                            m_messages = m_messageQueue.ToArray();
                            m_messageIndex = 0;
                        }
                        else
                        {
                            while (m_messageIndex < m_messages.Length - 1)
                            {
                                m_messageIndex++;
                                if (m_inputField.text != m_messages[m_messages.Length - m_messageIndex - 1])
                                {
                                    break;
                                }
                            }
                        }

                        m_inputField.text = null;
                        m_inputField.text = m_messages[m_messages.Length - m_messageIndex - 1];
                        IndependentSelectable.Select(m_inputField.gameObject);
                        InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);
                    }
                    else
                    {
                        if (m_messageIndex > 0)
                        {
                            while (m_messageIndex > 0)
                            {
                                m_messageIndex--;
                                if (m_inputField.text != m_messages[m_messages.Length - m_messageIndex - 1])
                                {
                                    break;
                                }
                            }

                            m_inputField.text = null;
                            m_inputField.text = m_messages[m_messages.Length - m_messageIndex - 1];
                            IndependentSelectable.Select(m_inputField.gameObject);
                            InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);
                        }
                    }
                }
            }
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            m_messageScrollRect.verticalNormalizedPosition = 0;
        }

        private void PopulateUIQueue()
        {
            if (m_isExpanded)
            {
                for (int i = 0; i < m_maxMessages; ++i)
                {
                    Text messageUI = Instantiate(m_messageUIPrefab);
                    messageUI.transform.SetParent(m_messageScrollRect.content, false);
                    m_uiQueue.Enqueue(messageUI);
                }

                IEnumerator<Text> uiQueueEnumerator = m_uiQueue.GetEnumerator();
                for (int i = 0; i < m_maxMessages - m_messageQueue.Count; ++i)
                {
                    uiQueueEnumerator.MoveNext();
                }

                IEnumerator<DateTime> timeEnumerator = m_timeQueue.GetEnumerator();
                foreach (string message in m_messageQueue)
                {
                    timeEnumerator.MoveNext();
                    uiQueueEnumerator.MoveNext();
                    uiQueueEnumerator.Current.text = FormatMessage(timeEnumerator.Current, message);
                }
                m_messageScrollRect.content.localScale = Vector2.one;
                StartCoroutine(ScrollToBottom());
            }
        }

        private void ClearUIQueue()
        {
            foreach (Text messageUI in m_uiQueue)
            {
                Destroy(messageUI.gameObject);
            }
            m_messageScrollRect.content.localScale = Vector2.zero;
            m_uiQueue.Clear();
            m_messages = null;
            m_messageIndex = -1;
        }

        private static string FormatMessage(DateTime dateTime, string message)
        {
            return string.Format("{0} > {1}", dateTime.ToString("HH:mm:ss"), message);
        }

        private void OnInputEndEdit(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                m_inputField.text = string.Empty;
                Write(value);
            }

            if (m_isExpanded)
            {
                IndependentSelectable.Select(m_inputField.gameObject);
                InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);
            }
        }

        private char OnValidateInput(string text, int charIndex, char addedChar)
        {
            if (char.IsLetterOrDigit(addedChar) || char.IsWhiteSpace(addedChar) || addedChar == '-' || addedChar == '+' || addedChar == '_')
            {
                return addedChar;
            }
            return '\0';
        }

        public void Echo(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("message should not be empty");
            }

            DateTime now = DateTime.Now;

            if (m_isExpanded)
            {
                Text messageUI = m_uiQueue.Dequeue();
                messageUI.text = FormatMessage(now, message);
                messageUI.transform.SetSiblingIndex(m_maxMessages);
                m_uiQueue.Enqueue(messageUI);
            }

            m_messages = null;
            m_messageIndex = -1;
        }

        public void Write(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("message should not be empty");
            }

            m_messageQueue.Enqueue(message);
            DateTime now = DateTime.Now;
            m_timeQueue.Enqueue(now);

            if (m_messageQueue.Count > m_maxMessages)
            {
                m_messageQueue.Dequeue();
            }

            if (m_isExpanded)
            {
                Text messageUI = m_uiQueue.Dequeue();
                messageUI.text = FormatMessage(now, message);
                messageUI.transform.SetSiblingIndex(m_maxMessages);
                m_uiQueue.Enqueue(messageUI);
            }
            m_messages = null;
            m_messageIndex = -1;
            if (Command != null)
            {
                string[] args = message.Split(' ');
                string cmd = args[0];
                for (int i = 1; i < args.Length; ++i)
                {
                    args[i - 1] = args[i];
                }
                Array.Resize(ref args, args.Length - 1);
                Command(this, cmd, args);
            }
        }

        public IConsole GetChild(int index)
        {
            throw new NotSupportedException();
        }
    }
}
