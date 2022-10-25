using System;
using UnityEditor;
using UnityEditor.XRTools.Utils;
using UnityEngine;

namespace Unity.MARS
{
    static class MarsUserPreferences
    {
        const string k_ResetColorsEvent = "Reset Colors";

        static readonly Color k_DefaultHighlightedSimulatedObjectColor = Color.yellow;
        static readonly Color k_DefaultConditionFailTextColor = Color.red;

        const string k_PrefPrefix = "MARS";
        const string k_HighlightedSimulatedObjectColorPref = k_PrefPrefix + ".HighightedSimulatedObjectsColor";
        const string k_DefaultConditionFailTextColorPref = k_PrefPrefix + ".DefaultConditionFailTextColor";
        const string k_TintImageMarkersPref = k_PrefPrefix + ".TintImageMarkers";
        const string k_RestrictCameraToEnvironmentBoundsPref = k_PrefPrefix + ".RestrictCameraToEnvironmentBounds";
        const string k_ShowMarsComponentsInInspectorPref = k_PrefPrefix + ".ShowMarsComponentsInInspector";
        const string k_SelectionModeNotificationPref = k_PrefPrefix + ".HideHideSelectionModeNotification";
        const string k_BuildValidationShowAllPref = k_PrefPrefix + ".BuildValidationShowAll";

        internal const string HighlightedSimulatedObjectColorTooltip = "Text color used to highlight the simulated versions of selected content in the MARS Hierarchy";
        internal const string ConditionFailTextColorTooltip = "Text color used by the MARS Compare Tool for data that fails to pass a Condition";

        static Color s_HighlightedSimulatedObjectColor;
        static Color s_ConditionFailTextColor;
        static bool s_RestrictCameraToEnvironmentBounds;
        static bool s_TintImageMarkers;
        static bool s_ShowMarsComponentsInInspector;
        static bool s_HideSelectionModeNotification;
        static bool s_BuildValidationShowAll;

        internal static event Action AfterPreferencesChanged;

        internal static Color HighlightedSimulatedObjectColor
        {
            get { return s_HighlightedSimulatedObjectColor; }
            set
            {
                s_HighlightedSimulatedObjectColor = value;
                var colorString = EditorMaterialUtils.ColorToColorPref(k_HighlightedSimulatedObjectColorPref, value);
                EditorPrefs.SetString(k_HighlightedSimulatedObjectColorPref, colorString);
            }
        }

        internal static Color ConditionFailTextColor
        {
            get { return s_ConditionFailTextColor; }
            set
            {
                s_ConditionFailTextColor = value;
                var colorString = EditorMaterialUtils.ColorToColorPref(k_DefaultConditionFailTextColorPref, value);
                EditorPrefs.SetString(k_DefaultConditionFailTextColorPref, colorString);
            }
        }

        internal static bool TintImageMarkers
        {
            get { return s_TintImageMarkers; }
            set
            {
                s_TintImageMarkers = value;
                EditorPrefs.SetBool(k_TintImageMarkersPref, value);
            }
        }

        internal static bool RestrictCameraToEnvironmentBounds
        {
            get { return s_RestrictCameraToEnvironmentBounds; }
            set
            {
                s_RestrictCameraToEnvironmentBounds = value;
                EditorPrefs.SetBool(k_RestrictCameraToEnvironmentBoundsPref, value);
            }
        }

        internal static bool ShowMarsComponentsInInspector
        {
            get { return s_ShowMarsComponentsInInspector; }
            set
            {
                s_ShowMarsComponentsInInspector = value;
                EditorPrefs.SetBool(k_ShowMarsComponentsInInspectorPref, value);
                AfterPreferencesChanged?.Invoke();
            }
        }

        internal static bool HideSelectionModeNotification
        {
            get { return s_HideSelectionModeNotification; }
            set
            {
                s_HideSelectionModeNotification = value;
                EditorPrefs.SetBool(k_SelectionModeNotificationPref, value);
            }
        }

        internal static bool BuildValidationShowAll
        {
            get { return s_BuildValidationShowAll; }
            set
            {
                s_BuildValidationShowAll = value;
                EditorPrefs.SetBool(k_BuildValidationShowAllPref, value);
            }
        }

        // Using InitializeOnLoadMethod instead of constructor to make sure these values exist before serialization
        [InitializeOnLoadMethod]
        static void SetupMarsUserPreferences()
        {
            var defaultColor = EditorMaterialUtils.ColorToColorPref(k_HighlightedSimulatedObjectColorPref, k_DefaultHighlightedSimulatedObjectColor);
            var colorString = EditorPrefs.GetString(k_HighlightedSimulatedObjectColorPref, defaultColor);
            s_HighlightedSimulatedObjectColor = EditorMaterialUtils.PrefToColor(colorString);

            defaultColor = EditorMaterialUtils.ColorToColorPref(k_DefaultConditionFailTextColorPref, k_DefaultConditionFailTextColor);
            colorString = EditorPrefs.GetString(k_DefaultConditionFailTextColorPref, defaultColor);
            s_ConditionFailTextColor = EditorMaterialUtils.PrefToColor(colorString);

            s_TintImageMarkers = EditorPrefs.GetBool(k_TintImageMarkersPref);
            s_RestrictCameraToEnvironmentBounds = EditorPrefs.GetBool(k_RestrictCameraToEnvironmentBoundsPref, true);
            s_ShowMarsComponentsInInspector = EditorPrefs.GetBool(k_ShowMarsComponentsInInspectorPref);
            s_HideSelectionModeNotification = EditorPrefs.GetBool(k_SelectionModeNotificationPref);
            s_BuildValidationShowAll = EditorPrefs.GetBool(k_BuildValidationShowAllPref);
        }

        public static void ResetColors()
        {
            s_HighlightedSimulatedObjectColor = k_DefaultHighlightedSimulatedObjectColor;
            s_ConditionFailTextColor = k_DefaultConditionFailTextColor;

            EditorEvents.UiComponentUsed.Send(new UiComponentArgs { label = k_ResetColorsEvent, active = true });

            SceneView.RepaintAll();
        }
    }
}
