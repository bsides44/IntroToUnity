using UnityEditor.MARS.UIElements;
using UnityEngine;

namespace UnityEditor.MARS.Simulation.UI
{
    class EnvironmentControlsWindow : EditorWindow
    {
        const float k_WindowWidth = 290f;
        const float k_WindowHeight = 68f;

        static double s_LastClosedTime;
        static Rect s_LastActivatorRect;

        static EnvironmentControlsWindow s_Instance;

        static bool CheckWindowExist()
        {
            if (s_Instance != null)
            {
                s_Instance.Close();
                return true;
            }

            return false;
        }

        // From UnityEditor.PopupWindow
        static bool ShouldShowWindow(Rect activatorRect)
        {
            const double kJustClickedTime = 0.2;
            var justClosed = (EditorApplication.timeSinceStartup - s_LastClosedTime) < kJustClickedTime;
            if (!justClosed || activatorRect != s_LastActivatorRect)
            {
                s_LastActivatorRect = activatorRect;
                return true;
            }
            return false;
        }

        internal static void ShowAsDropDown(Rect buttonRect)
        {
            if (CheckWindowExist() || !ShouldShowWindow(buttonRect))
                return;

            var buttonScreenRect = GUIUtility.GUIToScreenRect(buttonRect);
            var windowSize = new Vector2(k_WindowWidth, k_WindowHeight);
            s_Instance = CreateInstance<EnvironmentControlsWindow>();
            s_Instance.ShowAsDropDown(buttonScreenRect, windowSize);
        }

        void OnEnable()
        {
            var root = rootVisualElement;
            root.Clear();

            var environmentControls = new EnvironmentControls();
            root.Add(environmentControls);
        }

        void OnDisable()
        {
            s_LastClosedTime = EditorApplication.timeSinceStartup;
        }
    }
}
