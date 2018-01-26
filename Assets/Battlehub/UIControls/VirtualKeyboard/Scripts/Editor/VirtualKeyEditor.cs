using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Battlehub.UIControls
{
  
    [CustomEditor(typeof(VirtualKey))]
    [CanEditMultipleObjects]
    public class VirtualKeyEditor : ButtonEditor
    {
        private SerializedProperty m_keyCodeProp;
        private SerializedProperty m_isFunctionalProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_keyCodeProp = serializedObject.FindProperty("m_keyCode");
            m_isFunctionalProp = serializedObject.FindProperty("m_isFunctional");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_keyCodeProp, new GUIContent("KeyCode"));
            EditorGUILayout.PropertyField(m_isFunctionalProp, new GUIContent("Is Functional"));

            serializedObject.ApplyModifiedProperties();
        }
    }
}
