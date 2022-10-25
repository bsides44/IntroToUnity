using Unity.MARS.Simulation;

namespace UnityEditor.MARS.Simulation
{
    [CustomEditor(typeof(SimulatedMeshingSettings))]
    class SimulatedMeshingSettingsEditor : Editor
    {
        SimulatedMeshingSettingsDrawer m_Drawer;

        void OnEnable()
        {
            m_Drawer = new SimulatedMeshingSettingsDrawer(serializedObject);
        }

        public override void OnInspectorGUI()
        {
            m_Drawer.InspectorGUI(serializedObject);
        }
    }

    class SimulatedMeshingSettingsDrawer
    {
        SerializedProperty m_IndexFormat;
        SerializedProperty m_ExcludeExteriorXRayGeometry;

        internal SimulatedMeshingSettingsDrawer(SerializedObject serializedObject)
        {
            m_IndexFormat = serializedObject.FindProperty("m_IndexFormat");
            m_ExcludeExteriorXRayGeometry = serializedObject.FindProperty("m_ExcludeExteriorXRayGeometry");
        }

        internal void InspectorGUI(SerializedObject serializedObject)
        {
            serializedObject.Update();
            EditorGUIUtility.labelWidth = MarsEditorGUI.SettingsLabelWidth;

            using (var changed = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(m_IndexFormat);
                EditorGUILayout.PropertyField(m_ExcludeExteriorXRayGeometry);

                if (changed.changed)
                    serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
