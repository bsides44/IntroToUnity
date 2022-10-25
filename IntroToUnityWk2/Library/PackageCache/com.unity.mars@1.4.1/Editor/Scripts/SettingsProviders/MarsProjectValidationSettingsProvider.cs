using Unity.MARS.Settings;
using UnityEditor;
using UnityEditor.MARS.Build;
using UnityEngine.UIElements;

namespace Unity.MARS
{
    class MarsProjectValidationSettingsProvider : SettingsProvider
    {
        SerializedObject m_BuildValidatorSettingsObject;
        MarsProjectValidationDrawer m_ValidationDrawer;

        internal const string ProjectValidationSettingsPath = MARSCore.ProjectSettingsMarsRoot + "/Project Validation";

        [SettingsProvider]
        internal static SettingsProvider CreateMarsPreferencesProvider()
        {
            return new MarsProjectValidationSettingsProvider();
        }

        MarsProjectValidationSettingsProvider(string path = ProjectValidationSettingsPath,
            SettingsScope scopes = SettingsScope.Project)
            : base(path, scopes) { }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            m_BuildValidatorSettingsObject = new SerializedObject(BuildValidatorSettings.instance);
            m_ValidationDrawer = new MarsProjectValidationDrawer(m_BuildValidatorSettingsObject, BuildTargetGroup.Unknown);
        }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);
            m_ValidationDrawer.OnGUI(m_BuildValidatorSettingsObject);
        }

        public override void OnInspectorUpdate()
        {
            base.OnInspectorUpdate();
            if (m_ValidationDrawer.UpdateIssues(true, false))
                Repaint();
        }
    }
}
