using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Battlehub.VoxelCombat
{
    public class ChatControl : MonoBehaviour
    {
        public event Action<string> Message;

        [SerializeField]
        private ScrollRect m_messageScrollRect;

        [SerializeField]
        private InputField m_inputField;

        [SerializeField]
        private int m_maxMessages = 256;

        [SerializeField]
        private Text m_messageUIPrefab;
        private Queue<Text> m_uiQueue;

        private void Awake()
        {
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
        }

        private void OnDestroy()
        {
            m_inputField.onValidateInput -= OnValidateInput;
            m_inputField.onEndEdit.RemoveListener(OnInputEndEdit);
        }

        public void Echo(string senderName, string message, bool scrollToBottom)
        {
            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("message should not be empty");
            }

            Text messageUI = m_uiQueue.Dequeue();
            messageUI.text = FormatMessage(senderName, message);
            messageUI.transform.SetSiblingIndex(m_maxMessages);
            m_uiQueue.Enqueue(messageUI);

            if(scrollToBottom)
            {
                m_messageScrollRect.verticalNormalizedPosition = 0;
            }
        }

        private static string FormatMessage(string senderName, string message)
        {
            return string.Format("{0} > {1}", senderName, message);
        }

        private char OnValidateInput(string text, int charIndex, char addedChar)
        {
            //if (char.IsLetterOrDigit(addedChar) || char.IsWhiteSpace(addedChar) || addedChar == '-' || addedChar == '+' || addedChar == '_')
            {
                return addedChar;
            }
            //return '\0';
        }

        private void OnInputEndEdit(string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                m_inputField.text = string.Empty;
                if (Message != null)
                {
                    Message(value);
                }
            }
        }
    }
}

