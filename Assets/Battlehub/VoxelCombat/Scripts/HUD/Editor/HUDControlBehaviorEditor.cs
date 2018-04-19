using UnityEditor;
using UnityEditor.EventSystems;

namespace Battlehub.VoxelCombat
{
    [CustomEditor(typeof(HUDControlBehavior))]
    [CanEditMultipleObjects]
    public class HUDControlBehaviorEditor : EventTriggerEditor
    {
        private SerializedProperty m_graphics;
        private SerializedProperty m_highlightColor;
        private SerializedProperty m_normalColor;
        private SerializedProperty m_snapCursor;
        private SerializedProperty m_disabledColor;

        protected override void OnEnable()
        { 
            base.OnEnable();
            m_graphics = serializedObject.FindProperty("m_graphicsEx");
            m_highlightColor = serializedObject.FindProperty("m_highlightColor");
            m_normalColor = serializedObject.FindProperty("m_normalColor");
            m_snapCursor = serializedObject.FindProperty("m_snapCursor");
            m_disabledColor = serializedObject.FindProperty("m_disabledColor");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_snapCursor, false);
            EditorGUILayout.PropertyField(m_normalColor, false);
            EditorGUILayout.PropertyField(m_highlightColor, false);
            EditorGUILayout.PropertyField(m_disabledColor, false);
            EditorGUILayout.PropertyField(m_graphics, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

