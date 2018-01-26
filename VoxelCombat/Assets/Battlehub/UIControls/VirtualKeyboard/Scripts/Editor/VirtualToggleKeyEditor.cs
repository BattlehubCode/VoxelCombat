using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace Battlehub.UIControls
{
    [CustomEditor(typeof(VirtualToggleKey))]
    [CanEditMultipleObjects]
    public class VirtualToggleKeyEditor : ButtonEditor
    {
        private SerializedProperty m_group1Prop;
        private SerializedProperty m_group2Prop;
        private SerializedProperty m_group1TextProp;
        private SerializedProperty m_group2TextProp;

        protected override void OnEnable()
        {
            base.OnEnable();

            m_group1Prop = serializedObject.FindProperty("m_group1");
            m_group2Prop = serializedObject.FindProperty("m_group2");
            m_group1TextProp = serializedObject.FindProperty("m_group1Text");
            m_group2TextProp = serializedObject.FindProperty("m_group2Text");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.PropertyField(m_group1Prop, true);
            EditorGUILayout.PropertyField(m_group2Prop, true);

            EditorGUILayout.PropertyField(m_group1TextProp);
            EditorGUILayout.PropertyField(m_group2TextProp);

            serializedObject.ApplyModifiedProperties();
        }
    }

}
