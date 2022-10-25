using System.Collections.Generic;
using System.Linq;
using Unity.MARS;
using UnityEngine;

namespace UnityEditor.MARS.Build
{
    class MarsProjectValidationDrawer
    {
        class Styles
        {
            internal const float Space = 15.0f;
            internal const float FixButtonWidth = 80.0f;
            internal const float ShowAllChecksWidth = 96f;
            internal const float IgnoreBuildErrorsWidth = 140f;
            internal static readonly Vector2 IconSize = new Vector2(16.0f, 16.0f);

            internal readonly GUIStyle Wrap;
            internal readonly GUIContent FixButton;
            internal readonly GUIContent EditButton;
            internal readonly GUIContent HelpButton;
            internal readonly GUIContent PlayMode;

            internal GUIStyle IssuesBackground;
            internal GUIStyle ListLabel;
            internal GUIStyle IssuesTitleLabel;
            internal GUIStyle FixAllStyle;
            internal GUIStyle IconStyle;

            public Styles()
            {
                FixButton = new GUIContent("Fix");
                EditButton = new GUIContent("Edit");
                HelpButton = new GUIContent(MarsEditorGUI.InternalEditorStyles.HelpIcon.image);
                PlayMode = new GUIContent("Exit play mode before fixing project validation issues.", EditorGUIUtility.IconContent("console.infoicon").image);

                IssuesBackground = "ScrollViewAlt";

                ListLabel = new GUIStyle("TV Selection")
                {
                    border = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(5, 5, 0, 0),
                    margin = new RectOffset(5, 5, 5, 5)
                };

                IssuesTitleLabel = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    padding = new RectOffset(10, 10, 0, 0)

                };

                Wrap = new GUIStyle(EditorStyles.label)
                {
                    wordWrap = true,
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(0, 5, 1, 1)
                };

                IconStyle = new GUIStyle(EditorStyles.label)
                {
                    margin = new RectOffset(5, 5, 0, 0),
                    fixedWidth = IconSize.x * 2
                };

                FixAllStyle = new GUIStyle(EditorStyles.miniButton)
                {
                    stretchWidth = false,
                    fixedWidth = 80,
                    margin = new RectOffset(0,10,2,2)
                };
            }
        }

        /// <summary>
        /// Last time the issues in the window were updated
        /// </summary>
        double m_LastUpdate;

        /// <summary>
        /// Interval that that issues should be updated
        /// </summary>
        const double k_UpdateInterval = 1.0d;

        /// <summary>
        /// Interval that that issues should be updated when the window does not have focus
        /// </summary>
        const double k_BackgroundUpdateInterval = 3.0d;

        static Styles s_Styles;

        Vector2 m_ScrollViewPos = Vector2.zero;
        bool m_CheckedInPlayMode;

        SerializedProperty m_IgnoreBuildErrorsProperty;

        List<BuildValidationRule> m_BuildRules = new List<BuildValidationRule>();

        // Fix all state
        Queue<BuildValidationRule> m_FixAllQueue = new Queue<BuildValidationRule>();

        HashSet<BuildValidationRule> m_RuleFailures = new HashSet<BuildValidationRule>();

        BuildTargetGroup m_SelectedBuildTargetGroup;

        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        bool CheckInPlayMode
        {
            get
            {
                if (Application.isPlaying)
                {
                    if (!m_CheckedInPlayMode)
                    {
                        m_CheckedInPlayMode = true;
                        return true;
                    }

                    return false;
                }

                m_CheckedInPlayMode = false;
                return false;
            }
        }

        internal MarsProjectValidationDrawer(SerializedObject serializedObject, BuildTargetGroup targetGroup)
        {
            m_SelectedBuildTargetGroup = targetGroup;

            m_IgnoreBuildErrorsProperty = serializedObject.FindProperty("m_IgnoreBuildErrors");
            BuildValidator.GetCurrentValidationIssues(m_RuleFailures, m_SelectedBuildTargetGroup);
        }

        internal void OnGUI(SerializedObject serializedObject)
        {
            EditorGUIUtility.SetIconSize(Styles.IconSize);

            using (var change = new EditorGUI.ChangeCheckScope())
            {
                m_SelectedBuildTargetGroup = EditorGUILayout.BeginBuildTargetSelectionGrouping();
                if (m_SelectedBuildTargetGroup == BuildTargetGroup.Unknown)
                {
                    m_SelectedBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
                }

                if (!BuildValidator.PlatformRules.TryGetValue(m_SelectedBuildTargetGroup, out m_BuildRules))
                {
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField($"'{m_SelectedBuildTargetGroup}' does not have any associated build rules.",
                        styles.IssuesTitleLabel);
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndBuildTargetSelectionGrouping();
                    return;
                }

                if (change.changed)
                {
                    BuildValidator.GetCurrentValidationIssues(m_RuleFailures, m_SelectedBuildTargetGroup);
                }
            }

            EditorGUILayout.BeginVertical();

            if (EditorApplication.isPlaying && m_RuleFailures.Count > 0)
            {
                GUILayout.Space(Styles.Space);
                GUILayout.Label(styles.PlayMode);
            }

            EditorGUILayout.Space();

            DrawIssuesList(serializedObject);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndBuildTargetSelectionGrouping();
        }

        void DrawIssuesList(SerializedObject serializedObject)
        {
            var hasFix = m_RuleFailures.Any(f => f.fixIt != null);
            var hasAutoFix = hasFix && m_RuleFailures.Any(f => f.fixIt != null && f.fixItAutomatic);

            using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying))
            {
                // Header
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Issues ({m_RuleFailures.Count}) of Checks ({m_BuildRules.Count})",
                        styles.IssuesTitleLabel);

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var showAllChecks = EditorGUILayout.ToggleLeft("Show all",
                            MarsUserPreferences.BuildValidationShowAll, GUILayout.Width(Styles.ShowAllChecksWidth));

                        if (change.changed)
                            MarsUserPreferences.BuildValidationShowAll = showAllChecks;
                    }

                    using (var change = new EditorGUI.ChangeCheckScope())
                    {
                        var ignoreErrorContent = new GUIContent(m_IgnoreBuildErrorsProperty.displayName,
                            m_IgnoreBuildErrorsProperty.tooltip);
                        m_IgnoreBuildErrorsProperty.boolValue = EditorGUILayout.ToggleLeft(ignoreErrorContent,
                            m_IgnoreBuildErrorsProperty.boolValue, GUILayout.Width(Styles.IgnoreBuildErrorsWidth));

                        if (change.changed)
                            serializedObject.ApplyModifiedProperties();
                    }

                    // FixAll button
                    if (hasAutoFix)
                    {
                        using (new EditorGUI.DisabledScope(m_FixAllQueue.Count > 0))
                        {
                            if (GUILayout.Button("Fix All", styles.FixAllStyle, GUILayout.Width(Styles.FixButtonWidth)))
                            {
                                foreach (var ruleFailure in m_RuleFailures)
                                {
                                    if (ruleFailure.fixIt != null && ruleFailure.fixItAutomatic)
                                        m_FixAllQueue.Enqueue(ruleFailure);
                                }
                            }
                        }
                    }
                }

                m_ScrollViewPos = EditorGUILayout.BeginScrollView(m_ScrollViewPos, styles.IssuesBackground,
                    GUILayout.ExpandHeight(true));

                foreach (var result in m_BuildRules)
                {
                    var rulePassed = !m_RuleFailures.Contains(result);
                    if (MarsUserPreferences.BuildValidationShowAll || !rulePassed)
                        DrawIssue(result, rulePassed, hasFix);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        void DrawIssue(BuildValidationRule result, bool rulePassed, bool hasFix)
        {
            EditorGUILayout.BeginHorizontal(styles.ListLabel);

            GUILayout.Label(rulePassed ? MarsEditorGUI.InternalEditorStyles.TestPassedIcon :
                result.error ? MarsEditorGUI.InternalEditorStyles.ErrorIcon
                : MarsEditorGUI.InternalEditorStyles.WarningIcon, styles.IconStyle,
                GUILayout.Width(Styles.IconSize.x));

            var message = string.IsNullOrEmpty(result.name) ? result.message
                : $"[{result.name}] {result.message}";

            GUILayout.Label(message, styles.Wrap);
            GUILayout.FlexibleSpace();

            if (!string.IsNullOrEmpty(result.helpText) || !string.IsNullOrEmpty(result.helpLink))
            {
                styles.HelpButton.tooltip = result.helpText;
                if (GUILayout.Button(styles.HelpButton, styles.IconStyle, GUILayout.Width(Styles.IconSize.x
                    + styles.IconStyle.padding.horizontal)))
                {
                    if (!string.IsNullOrEmpty(result.helpLink))
                        Application.OpenURL(result.helpLink);
                }
            }
            else
            {
                GUILayout.Label("", GUILayout.Width(Styles.IconSize.x + styles.IconStyle.padding.horizontal));
            }

            using (new EditorGUI.DisabledScope(!m_RuleFailures.Contains(result)))
            {
                if (result.fixIt != null)
                {
                    using (new EditorGUI.DisabledScope(m_FixAllQueue.Count != 0))
                    {
                        var button = result.fixItAutomatic ? styles.FixButton : styles.EditButton;
                        button.tooltip = result.fixItMessage;
                        if (GUILayout.Button(button, GUILayout.Width(Styles.FixButtonWidth)))
                        {
                            if (result.fixItAutomatic)
                                m_FixAllQueue.Enqueue(result);
                            else
                                result.fixIt();
                        }
                    }
                }
                else if (hasFix)
                {
                    GUILayout.Label("", GUILayout.Width(Styles.FixButtonWidth));
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        internal bool UpdateIssues(bool focused, bool force)
        {
            if (CheckInPlayMode)
                force = true;
            else if (Application.isPlaying)
                return false;

            var interval = focused ? k_UpdateInterval : k_BackgroundUpdateInterval;
            if (!force && EditorApplication.timeSinceStartup - m_LastUpdate < interval)
                return false;

            if (m_FixAllQueue.Count > 0)
            {
                // Fixit actions can popup dialogs that may cause the action to be called
                // again form `UpdateIssues` if it is not removed before invoking.
                var fixIt = m_FixAllQueue.Dequeue().fixIt;
                fixIt?.Invoke();
            }

            var activeBuildTargetGroup = m_SelectedBuildTargetGroup;
            if (activeBuildTargetGroup == BuildTargetGroup.Unknown)
                activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);

            if (!BuildValidator.HasRulesForPlatform(activeBuildTargetGroup))
                return false;

            var failureCount = m_BuildRules.Count;

            BuildValidator.GetCurrentValidationIssues(m_RuleFailures, activeBuildTargetGroup);

            // Repaint the window if the failure count has changed
            var needsRepaint = m_BuildRules.Count > 0 || failureCount > 0;

            m_LastUpdate = EditorApplication.timeSinceStartup;
            return needsRepaint;
        }
    }
}
