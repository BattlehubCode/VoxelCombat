using Battlehub.UIControls;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace Battlehub.VoxelCombat
{
    public delegate void ConsoleCommandHandler(string cmd, params string[] args);

    public interface IConsole
    {
        event ConsoleCommandHandler Command;

        IConsole GetChild(int index);

        void Echo(string message);
        void Write(string message);
    }

    public class VoxelConsole : MonoBehaviour, IConsole
    {
        public event ConsoleCommandHandler Command;

        private IVoxelInputManager m_input;
        private RectTransform m_consoleUIRoot;
        [SerializeField]
        private ScrollRect m_messageScrollRect;

        [SerializeField]
        private InputField m_inputField;

        [SerializeField]
        private float m_toggleSpeed = 1000.0f;

        [SerializeField]
        private int m_maxMessages = 256;

        [SerializeField]
        private Text m_messageUIPrefab;

        private Queue<DateTime> m_timeQueue;
        private Queue<string> m_messageQueue;
        private string[] m_messages;
        private int m_messageIndex = -1;

        private Queue<Text> m_uiQueue;

        private bool m_isExpanded;
        public bool IsExpanded
        {
            get { return m_isExpanded; }
            set
            {
                m_isExpanded = value;
                m_isAnimationInProgress = true;
            }
        }

        private bool m_canExpand = true;
        public bool CanExpand
        {
            get { return m_canExpand; }
            set
            {
                m_canExpand = value;
                if(!m_canExpand)
                {
                    IsExpanded = false;
                }
            }
        }

        private bool m_isAnimationInProgress;

        private int m_consoleOwner = -1;
      
        private float Height
        {
            get { return Screen.height / 3; }
        }
        
        private void Awake()
        {
            m_timeQueue = new Queue<System.DateTime>();
            m_messageQueue = new Queue<string>();
            m_uiQueue = new Queue<Text>();
            m_input = Dependencies.InputManager;

            if(m_messageUIPrefab == null)
            {
                Debug.LogError("set messageUIPrefab");
                return;
            }

            if(m_inputField == null)
            {
                Debug.LogError("set inputField");
                return;
            }

            if(m_messageScrollRect == null)
            {
                Debug.LogError("set messageScrollRect");
                return;
            }

            m_consoleUIRoot = GetComponent<RectTransform>();
            if (m_consoleUIRoot == null)
            {
                Debug.LogError("set console ui root");
                return;
            }

            m_inputField.onValidateInput += OnValidateInput;
            m_inputField.onEndEdit.AddListener(OnInputEndEdit);

            //InputFieldWithVirtualKeyboard ifwk = m_inputField.GetComponent<InputFieldWithVirtualKeyboard>();
            //ifwk.VirtualKeyboardEnabled = false;// !m_input.IsKeyboardAndMouse(m_consoleOwner);

            ClearAndHide();
        }

        private void OnDestroy()
        {
            m_inputField.onValidateInput -= OnValidateInput;
            m_inputField.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        private void Update()
        {
            if(m_input.GetButtonDown(InputAction.ToggleConsole, m_consoleOwner, false))
            {
                m_isAnimationInProgress = true;
                m_isExpanded = !m_isExpanded && CanExpand;
                if(m_isExpanded)
                {
                    m_messageScrollRect.gameObject.SetActive(true);
                }
            }

            if(m_isExpanded && m_messageQueue.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.UpArrow))
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
                            if(m_inputField.text != m_messages[m_messages.Length - m_messageIndex - 1])
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
                else if (Input.GetKeyDown(KeyCode.DownArrow))
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

            if(m_isAnimationInProgress)
            {
                Vector2 offset = Vector2.down * Time.deltaTime* m_toggleSpeed;
                if (m_isExpanded)
                {
                    m_consoleUIRoot.sizeDelta -= offset;
                    if(m_consoleUIRoot.sizeDelta.y >= Height)
                    {
                        if(m_uiQueue.Count == 0)
                        {
                            StartCoroutine(PopulateUIQueue());
                        }
                        
                        m_consoleUIRoot.sizeDelta = new Vector2(m_consoleUIRoot.sizeDelta.x, Height);
                        m_isAnimationInProgress = false;

                        m_inputField.gameObject.SetActive(true);
                        IndependentSelectable.Select(m_inputField.gameObject);
                        InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);
                    }
                }
                else
                {
                    m_consoleUIRoot.sizeDelta += offset;
                    if(m_consoleUIRoot.sizeDelta.y <= 0)
                    {
                        ClearAndHide();
                    }
                }
            }
        }

        private void ClearAndHide()
        {
            m_consoleUIRoot.sizeDelta = new Vector2(m_consoleUIRoot.sizeDelta.x, 0);
            m_isAnimationInProgress = false;

            ClearUIQueue();

            m_inputField.text = string.Empty;
            //m_inputField.DeactivateInputField();
        
            EventSystem eventSystem = IndependentSelectable.GetEventSystem(m_inputField.gameObject);
            if (eventSystem == m_inputField.gameObject)
            {
                eventSystem.SetSelectedGameObject(null);
            }
            m_messageScrollRect.gameObject.SetActive(false);
           
        }

        private IEnumerator PopulateUIQueue()
        {
            yield return new WaitForEndOfFrame();

            if(m_isExpanded)
            {
                for (int i = 0; i < m_maxMessages; ++i)
                {
                    Text messageUI = Instantiate(m_messageUIPrefab);
                    messageUI.transform.SetParent(m_messageScrollRect.content, false);
                    m_uiQueue.Enqueue(messageUI);
                }

                IEnumerator<Text> uiQueueEnumerator = m_uiQueue.GetEnumerator();
                for(int i = 0; i < m_maxMessages - m_messageQueue.Count; ++i)
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

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            m_messageScrollRect.verticalNormalizedPosition = 0;
        }

        private void ClearUIQueue()
        {
            foreach(Text messageUI in m_uiQueue)
            {
                Destroy(messageUI.gameObject);
            }
            m_messageScrollRect.content.localScale = Vector2.zero;
            m_uiQueue.Clear();
            m_messages = null;
            m_messageIndex = -1;
        }

        public void Echo(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new System.ArgumentException("message should not be empty");
            }

            System.DateTime now = System.DateTime.Now;
      
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
            if(string.IsNullOrEmpty(message))
            {
                throw new System.ArgumentException("message should not be empty");
            }

            m_messageQueue.Enqueue(message);
            System.DateTime now = System.DateTime.Now;
            m_timeQueue.Enqueue(now);

            if(m_messageQueue.Count > m_maxMessages)
            {
                m_messageQueue.Dequeue();
            }

            if(m_isExpanded)
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
                System.Array.Resize(ref args, args.Length - 1);
                Command(cmd, args);
            }
        }

        private static string FormatMessage(System.DateTime dateTime, string message)
        {
            return string.Format("{0} > {1}", dateTime.ToString("HH:mm:ss"), message);
        }

        private char OnValidateInput(string text, int charIndex, char addedChar)
        {
            if(char.IsLetterOrDigit(addedChar) || char.IsWhiteSpace(addedChar) || addedChar == '-' || addedChar == '+' || addedChar == '_')
            {
                return addedChar;
            }
            return '\0';
        }

        private void OnInputEndEdit(string value)
        {
            if(!string.IsNullOrEmpty(value))
            {
                m_inputField.text = string.Empty;
                Write(value);
            }

            if(m_isExpanded)
            {
                IndependentSelectable.Select(m_inputField.gameObject);
                InputFieldWithVirtualKeyboard.ActivateInputField(m_inputField);
            }
        }

        public IConsole GetChild(int index)
        {
            throw new NotImplementedException();
        }
    }
}

