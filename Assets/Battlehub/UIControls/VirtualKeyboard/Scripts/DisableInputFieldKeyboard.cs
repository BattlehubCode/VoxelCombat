using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battlehub.UIControls
{
    [DefaultExecutionOrder(-2000)]
    public class DisableInputFieldKeyboard : MonoBehaviour
    {
        private InputField m_inputField;

        public IndependentEventSystem EventSystem
        {
            get;set;
        }

        private void Start()
        {
            m_inputField = GetComponent<InputField>();
        }



        public void Destroy()
        {
            Destroy(this);
            //m_destroy = true;
        }

        private void OnGUI()
        {
            if (!m_inputField.isFocused)
            {
                return;
            }
            KeyCode code = Event.current.keyCode;

            if (code == KeyCode.Return ||
               code == KeyCode.Escape ||
               code == KeyCode.DownArrow ||
               code == KeyCode.UpArrow ||
               code == KeyCode.LeftArrow ||
               code == KeyCode.RightArrow)
            {
                Event evt = new Event();
                Event.PopEvent(evt);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (m_inputField.isFocused)
                {
                    Event evt = new Event();
                    Event.PopEvent(evt);
                }
            }

            
        }

        //private void LateUpdate()
        //{
        //    if (m_wasDisabled)
        //    {
        //        if (!Input.GetKey(KeyCode.Return) && !Input.GetKey(KeyCode.Escape) && !Input.GetKey(KeyCode.UpArrow) && !Input.GetKey(KeyCode.DownArrow) && !Input.GetKey(KeyCode.LeftArrow) && !Input.GetKey(KeyCode.RightArrow))
        //        {
        //            m_wasDisabled = false;
        //            if (m_destroy)
        //            {
        //                Destroy(this);
        //            }
        //        }
        //    }
        //    else
        //    {
        //        if (m_destroy)
        //        {
        //            Destroy(this);
        //        }
        //    }
        //}
    }
}
