using System;
using UnityEngine;

namespace UnityEditor.MARS.Simulation
{
    /// <summary>
    /// Popup Window for Simulation View's display options.
    /// </summary>
    class SimulationViewDisplayOptionsWindow : MarsEditorGUI.MarsPopupWindow
    {
        class Styles
        {
            const string k_DesaturateTooltipText = "Desaturate options for the inactive Simulation Scene.\n" +
                "Selection Pulse - Desaturate the inactive simulation scene when the active is selected. Missed clicks " +
                "cause the desaturate of inactive simulation scene to be pulsed.\n" +
                "Always - Always Desaturate the inactive simulation scene.\n" +
                "None - Do not desaturate the inactive simulation scene.";
            public readonly GUIContent showSceneOptionsGUIContent;
            public readonly GUIContent desaturateCompositeContent;

            public readonly GUIContent showViewOptionsGUIContent;
            public readonly GUIContent xrayViewLabelGUIContent;
            public readonly GUIContent xrayViewLabelTooltip;

            public readonly GUIContent showLayerOptionsGUIContent;
            public readonly GUIContent environmentLabelGUIContent;
            public readonly GUIContent environmentLabelTooltip;
            public readonly GUIContent simulatedDataOptionGUIContent;
            public readonly GUIContent simulatedDataOptionTooltip;
            public readonly GUIContent contentObjectsOptionTooltip;

            public Styles()
            {
                showSceneOptionsGUIContent = new GUIContent("Simulation Scene Options");
                desaturateCompositeContent = new GUIContent("Desaturate Composite", k_DesaturateTooltipText);

                showViewOptionsGUIContent = new GUIContent("View Options");
                xrayViewLabelGUIContent = new GUIContent("View X-Ray");
                xrayViewLabelTooltip = new GUIContent { tooltip = "Use X-Ray shading on this view"};

                showLayerOptionsGUIContent = new GUIContent("Layer Options");
                environmentLabelGUIContent = new GUIContent("Simulated Environment");
                environmentLabelTooltip = new GUIContent{ tooltip = "Show the simulated environment." };
                simulatedDataOptionGUIContent = new GUIContent("Simulated Data");
                simulatedDataOptionTooltip = new GUIContent{ tooltip = "Show simulated world data." };
                contentObjectsOptionTooltip = new GUIContent{ tooltip = "Show simulated content." };
            }
        }

        const float k_WindowWidth = 240;
        const int k_IndentWidth = 12;
        const int k_LabelPadding = 8;
        static Styles s_Styles;

        readonly SimulationView m_View;

        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        static float windowHeight => 196;

        public SimulationViewDisplayOptionsWindow(SimulationView view)
        {
            m_View = view;
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(k_WindowWidth, windowHeight);
        }

        protected override void Draw()
        {
            var marsStyles = MarsEditorGUI.Styles;
            using (new EditorGUILayout.VerticalScope(marsStyles.AreaAlignmentLargeMargin))
            {
                var labelSize = Mathf.Max(
                    marsStyles.LabelLeftAligned.CalcSize(styles.environmentLabelGUIContent).x,
                    marsStyles.LabelLeftAligned.CalcSize(styles.simulatedDataOptionTooltip).x,
                    marsStyles.LabelLeftAligned.CalcSize(styles.contentObjectsOptionTooltip).x);
                var labelHeight = marsStyles.LabelLeftAligned.CalcHeight(styles.environmentLabelGUIContent, labelSize);

                EditorGUILayout.LabelField(styles.showSceneOptionsGUIContent, marsStyles.LabelLeftAligned);

                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    m_View.ViewDesaturationMode = (SimulationView.DesaturationMode)EditorGUILayout.EnumPopup(styles.desaturateCompositeContent, m_View.ViewDesaturationMode);
                    if (changed.changed)
                        m_View.Repaint();
                }

                EditorGUILayout.LabelField(styles.showViewOptionsGUIContent, marsStyles.LabelLeftAligned);
                DrawLine(labelSize, labelHeight, styles.xrayViewLabelGUIContent, styles.xrayViewLabelTooltip, m_View.UseXRay, visible =>
                {
                    m_View.UseXRay = visible;
                    m_View.Repaint();
                });

                EditorGUILayout.LabelField(styles.showLayerOptionsGUIContent, marsStyles.LabelLeftAligned);

                // Simulated Environment display option
                var simulationSettings = SimulationSettings.instance;
                DrawLine(labelSize, labelHeight, styles.environmentLabelGUIContent, styles.environmentLabelTooltip, simulationSettings.ShowSimulatedEnvironment, visible =>
                {
                    simulationSettings.ShowSimulatedEnvironment = visible;
                    m_View.Repaint();
                });

                // Simulated Data display option
                DrawLine(labelSize, labelHeight, styles.simulatedDataOptionGUIContent, styles.simulatedDataOptionTooltip, simulationSettings.ShowSimulatedData, visible =>
                {
                    simulationSettings.ShowSimulatedData = visible;
                    m_View.Repaint();
                });
            }
        }

        static void DrawLine(float labelSize, float labelHeight, GUIContent visibilityContent, GUIContent tooltipContent, bool objectVisible, Action<bool> setVisibility,
            GUIContent lockContent = null, bool objectLocked = false, Action<bool> setLocked = null)
        {
            var marsStyles = MarsEditorGUI.Styles;
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayoutUtility.GetRect(GUIContent.none, marsStyles.LabelLeftAligned,
                    GUILayout.Width(k_IndentWidth));

                EditorGUILayout.LabelField(visibilityContent, marsStyles.LabelLeftAligned,
                    GUILayout.Width(labelSize + k_IndentWidth + k_LabelPadding));

                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    objectVisible = EditorGUIUtils.ImageToggle(objectVisible, tooltipContent,
                        marsStyles.AnimationVisibilityToggleOn, marsStyles.AnimationVisibilityToggleOff,
                        marsStyles.SingleLineAlignment, GUILayout.Height(labelHeight),
                        GUILayout.Width(labelHeight));

                    if (changed.changed)
                        setVisibility(objectVisible);
                }

                if (setLocked == null)
                    return;

                var inLock = MarsEditorGUI.InternalEditorStyles.InLock;
                var inLockHeight = inLock.CalcHeight(lockContent, inLock.fixedWidth);
                var lockRect = GUILayoutUtility.GetRect(inLock.fixedWidth, inLockHeight, marsStyles.SingleLineAlignment);
                using (var changed = new EditorGUI.ChangeCheckScope())
                {
                    objectLocked = GUI.Toggle(lockRect, objectLocked, lockContent, MarsEditorGUI.InternalEditorStyles.InLock);

                    if (changed.changed)
                        setLocked(objectLocked);
                }
            }
        }
    }
}
