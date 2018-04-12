using UnityEditor;
using UnityEditor.EventSystems;

namespace Battlehub.VoxelCombat
{
    [CustomEditor(typeof(HUDControlBehavior))]
    [CanEditMultipleObjects]
    public class HUDControlBehaviorEditor : EventTriggerEditor
    {
        private SerializedProperty m_graphics;

        protected override void OnEnable()
        { 
            base.OnEnable();
            m_graphics = serializedObject.FindProperty("m_graphicsEx");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_graphics, true);
            serializedObject.ApplyModifiedProperties();
        }
    }
}

