using UnityEditor;
using UnityEditor.MARS;
using UnityEngine;

namespace Unity.MARS
{
    class MarsPreferencesProvider : SettingsProvider
    {
        class Styles
        {
            internal const string OSXWithPermissionsHelpBox = "MARS Camera permissions are handled by the operating " +
                "system. To allow or deny acces to the camera, go to System Preferences > Security & Privacy > Camera " +
                "and toggle Unity Hub";
            internal const string CameraPermissionLabel = "Camera permission";
            internal const string ResetAllHints = "Reset All Hints";
            internal const string UseDefaultColors = "Use Default Colors";
            internal const string RestrictCameraToEnvironmentBounds = "Restrict Camera To Environment Bounds";
            internal const string TintImageMarkers = "Tint Image Markers";
            internal const string ShowMarsComponentsInInspector = "Show MARS Components in the Inspector";
            internal const string HideSimulationModeSelectionNotification = "Hide Simulation Selection Mode Notifications";
            internal const int SpaceSize = 5;

            internal readonly GUIContent HighlightedSimulatedObjectColorContent;
            internal readonly GUIContent ConditionFailTextColorTooltipContent;

            public Styles()
            {
                HighlightedSimulatedObjectColorContent = new GUIContent("Highlighted Simulated Object Color",
                    MarsUserPreferences.HighlightedSimulatedObjectColorTooltip);
                ConditionFailTextColorTooltipContent = new GUIContent("Condition Fail Text Color",
                    MarsUserPreferences.ConditionFailTextColorTooltip);
            }
        }

        const string k_PreferencesPath = "Preferences/MARS";

        static Styles s_Styles;
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        [SettingsProvider]
        public static SettingsProvider CreateMARSPreferencesProvider() { return new MarsPreferencesProvider(); }

        MarsPreferencesProvider(string path = k_PreferencesPath, SettingsScope scope = SettingsScope.User)
            : base(path, scope) { }

        public override void OnGUI(string searchContext)
        {
            base.OnGUI(searchContext);

            if (CameraPermissionUtils.IsOSXWithPermissions())
            {
                EditorGUILayout.HelpBox(Styles.OSXWithPermissionsHelpBox, MessageType.Info);
            }
            else
            {
                using (var check = new EditorGUI.ChangeCheckScope())
                {
                    var granted = EditorPrefs.GetBool(CameraPermissionUtils.CameraPermissionPref, false);
                    granted = EditorGUILayout.Toggle(Styles.CameraPermissionLabel, granted);

                    if (check.changed)
                        EditorPrefs.SetBool(CameraPermissionUtils.CameraPermissionPref, granted);
                }
            }

            EditorGUIUtility.labelWidth = MarsEditorGUI.SettingsLabelWidth;

            EditorGUI.BeginChangeCheck();

            if (GUILayout.Button(Styles.ResetAllHints, GUILayout.Width(120)))
                MarsHints.ResetHints();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newValue = EditorGUILayout.ColorField(styles.HighlightedSimulatedObjectColorContent,
                    MarsUserPreferences.HighlightedSimulatedObjectColor);
                if (check.changed)
                    MarsUserPreferences.HighlightedSimulatedObjectColor = newValue;
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newValue = EditorGUILayout.ColorField(styles.ConditionFailTextColorTooltipContent,
                    MarsUserPreferences.ConditionFailTextColor);
                if (check.changed)
                    MarsUserPreferences.ConditionFailTextColor = newValue;
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Styles.UseDefaultColors))
                    MarsUserPreferences.ResetColors();

                GUILayout.FlexibleSpace();
            }

            if (EditorGUI.EndChangeCheck())
            {
                foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
                {
                    window.Repaint();
                }
            }

            GUILayout.Space(Styles.SpaceSize);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newValue = EditorGUILayout.Toggle(Styles.RestrictCameraToEnvironmentBounds,
                    MarsUserPreferences.RestrictCameraToEnvironmentBounds);
                if (check.changed)
                    MarsUserPreferences.RestrictCameraToEnvironmentBounds = newValue;
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newValue = EditorGUILayout.Toggle(Styles.TintImageMarkers,
                    MarsUserPreferences.TintImageMarkers);
                if (check.changed)
                    MarsUserPreferences.TintImageMarkers = newValue;
            }


            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newValue = EditorGUILayout.Toggle(Styles.ShowMarsComponentsInInspector,
                    MarsUserPreferences.ShowMarsComponentsInInspector);
                if (check.changed)
                    MarsUserPreferences.ShowMarsComponentsInInspector = newValue;
            }

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var newValue = EditorGUILayout.Toggle(Styles.HideSimulationModeSelectionNotification,
                    MarsUserPreferences.HideSelectionModeNotification);
                if (check.changed)
                    MarsUserPreferences.HideSelectionModeNotification = newValue;
            }
        }
    }
}
