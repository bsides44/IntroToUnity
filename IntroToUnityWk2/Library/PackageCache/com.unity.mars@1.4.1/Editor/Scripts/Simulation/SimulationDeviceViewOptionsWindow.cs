using UnityEngine;

namespace UnityEditor.MARS.Simulation
{
    /// <summary>
    /// Popup Window for selecting between Device / Simulation Views on the sim view toolbar
    /// </summary>
    class SimulationDeviceViewOptionsWindow : MarsEditorGUI.MarsPopupWindow
    {
        const int k_ControlWidth = 130;
        const int k_ControlHeight = 24;

        readonly Vector2 k_WindowSize = new Vector2(136, 66);
        readonly SimulationView m_View;

        class Styles
        {
            internal readonly GUIContent SimViewContent;
            internal readonly GUIContent DeviceViewContent;

            internal Styles()
            {
                // The spaces in the strings bellow are used to give a space between the text and the icon
                DeviceViewContent = new GUIContent(" Device view", MarsUIResources.instance.SimulationIconData.DeviceViewIcon.Icon);
                SimViewContent = new GUIContent(" Simulation View", MarsUIResources.instance.SimulationIconData.SimulationViewIcon.Icon);
            }
        }

        static Styles s_Styles;
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        internal SimulationDeviceViewOptionsWindow(SimulationView view)
        {
            m_View = view;
        }

        protected override void Draw()
        {
            editorWindow.maxSize = k_WindowSize;

            EditorGUILayout.Space();

            if (GUILayout.Button(styles.SimViewContent, MarsEditorGUI.Styles.DropDownItem,
                GUILayout.Width(k_ControlWidth), GUILayout.Height(k_ControlHeight)))
            {
                m_View.SceneType = ViewSceneType.Simulation;
                m_View.Repaint();
                editorWindow.Close();
            }

            if (GUILayout.Button(styles.DeviceViewContent, MarsEditorGUI.Styles.DropDownItem,
                GUILayout.Width(k_ControlWidth), GUILayout.Height(k_ControlHeight)))
            {
                m_View.SceneType = ViewSceneType.Device;
                m_View.Repaint();
                editorWindow.Close();
            }
        }
    }
}
