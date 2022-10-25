using UnityEditor;
using UnityEditor.MARS;

namespace Unity.MARS.Providers.HoloLens
{
    [CustomEditor(typeof(HoloLensMeshProviderOptions))]
    class HoloLensMeshProviderOptionsEditor : Editor
    {
        HoloLensMeshProviderOptionsDrawer m_HoloLensMeshProviderOptionsDrawer;

        void OnEnable()
        {
            m_HoloLensMeshProviderOptionsDrawer = new HoloLensMeshProviderOptionsDrawer(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            m_HoloLensMeshProviderOptionsDrawer.InspectorGUI(serializedObject);
        }
    }

    class HoloLensMeshProviderOptionsDrawer
    {
        SerializedProperty m_BoundingRadius;
        SerializedProperty m_RefreshInterval;
        SerializedProperty m_LevelOfDetail;
        SerializedProperty m_UseSceneUnderstanding;

        public HoloLensMeshProviderOptionsDrawer(SerializedObject serializedObject)
        {
            m_BoundingRadius = serializedObject.FindProperty("m_BoundingRadius");
            m_RefreshInterval = serializedObject.FindProperty("m_RefreshInterval");
            m_LevelOfDetail = serializedObject.FindProperty("m_LevelOfDetail");
            m_UseSceneUnderstanding = serializedObject.FindProperty("m_UseSceneUnderstanding");
        }

        internal void InspectorGUI(SerializedObject serializedObject)
        {
            serializedObject.Update();
            EditorGUIUtility.labelWidth = MarsEditorGUI.SettingsLabelWidth;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_BoundingRadius);
                EditorGUILayout.PropertyField(m_RefreshInterval);
                EditorGUILayout.PropertyField(m_LevelOfDetail);
                EditorGUILayout.PropertyField(m_UseSceneUnderstanding);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
