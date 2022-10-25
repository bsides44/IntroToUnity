using System;
using System.Reflection;
using Unity.MARS;
using Unity.MARS.Data;
using Unity.MARS.Settings;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine;
using UnityEngine.Assertions;

namespace UnityEditor.MARS
{
    /// <summary>
    /// Collection of editor utility methods for the MARS editor GUI
    /// </summary>
    public static class MarsEditorUtils
    {
        const string k_PrefabDeleteTittle = "Cannot restructure environment";
        const string k_PrefabDeleteMessage = "Simulation environments in MARS are set up as prefabs; Children of a " +
            "Prefab instance cannot be deleted or moved, and components cannot be reordered.\n\nYou can open the " +
            "environment in prefab isolation mode to do modifications.";
        const string k_OpenPrefabLabel = "Open Prefab";
        const string k_CancelOpenPrefab = "Cancel";
        const string k_AddSessionTooltip = "This will turn the active scene into a MARS scene";
        const string k_DockArea = "UnityEditor.DockArea";
        const string k_DockAreaAddTab = "AddTabToHere";
        const string k_DockAreaHasFocus = "hasFocus";

        static readonly GUIContent k_StopShowingHintButton = new GUIContent("X", "Stop showing this hint");
        static readonly GUIContent k_AddSessionContent = new GUIContent("Add MARS Session", k_AddSessionTooltip);

        const string k_MARSMenuLabel = "MARS/";
        const string k_MARSPanelLabel = k_MARSMenuLabel + MarsPanel.WindowTitle;
        const string k_SimulationViewLabel = k_MARSMenuLabel + SimulationView.SimulationViewWindowTitle;
        const string k_SimDeviceViewLabel = k_MARSMenuLabel + SimulationView.DeviceViewWindowTitle;
        const string k_DatabaseViewerLabel = k_MARSMenuLabel + MARSDatabaseViewer.WindowTitle;
        const string k_SimTestRunnerLabel = k_MARSMenuLabel + SimulationTestRunner.WindowTitle;

        static MethodInfo s_AddTabHereInfo;
        static object s_DockArea;
        static Type s_DockAreaType;
        static PropertyInfo s_HasFocusProp;

        /// <summary>
        /// Whether MARS is currently simulating the discovery of data over time
        /// </summary>
        public static bool SimulatingDiscovery
        {
            get
            {
                return EditorApplication.isPlayingOrWillChangePlaymode ?
                    MARSSceneModule.simulatedDiscoveryInPlayMode : QuerySimulationModule.instance.simulatingTemporal;
            }
        }

        /// <summary>
        /// Draw a hint box, with a 'learn more' button and URL, and a close button/action
        /// </summary>
        /// <param name="shouldShowHint">Whether the hint box should currently be displayed</param>
        /// <param name="hintMessage">The primary message to show in the hint box</param>
        /// <param name="analyticsLabel">Label for this hint to be identified in analytics events</param>
        /// <param name="learnMoreButton">GUIContent of the 'learn more' button</param>
        /// <param name="learnMoreURL">URL to open when the user clicks the 'learn more' button</param>
        /// <param name="stopShowingHintButton">GUIContent of the 'close' button</param>
        /// <param name="stopShowingHintAction">Action to call when the user presses the 'close' button</param>
        /// <returns>True, unless the user presses the 'close' button</returns>
        public static bool HintBox(bool shouldShowHint, string hintMessage, string analyticsLabel, GUIContent learnMoreButton, string learnMoreURL, GUIContent stopShowingHintButton, Action stopShowingHintAction = null)
        {
            if (!shouldShowHint)
                return false;

            EditorGUILayout.Separator();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.HelpBox(hintMessage, MessageType.Info);

                using (new EditorGUILayout.VerticalScope())
                {
                    if (learnMoreButton != null)
                    {
                        if (GUILayout.Button(learnMoreButton, GUILayout.Width(20)))
                        {
                            EditorEvents.HintButtonUsed.Send(new HintHelpButtonUsedArgs { label = analyticsLabel, url = learnMoreURL });
                            Application.OpenURL(learnMoreURL);
                        }
                    }

                    if (GUILayout.Button(stopShowingHintButton, GUILayout.Width(20)))
                    {
                        if (stopShowingHintAction != null)
                            stopShowingHintAction();

                        EditorEvents.HintDismissed.Send(new HintDismissedArgs { label = analyticsLabel });

                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Draw a hint box with a close button/action
        /// </summary>
        /// <param name="shouldShowHint">Whether the hint box should currently be displayed</param>
        /// <param name="hintMessage">The primary message to show in the hint box</param>
        /// <param name="analyticsLabel">Label for this hint to be identified in analytics events</param>
        /// /// <returns>True, unless the user presses the 'close' button</returns>
        public static bool HintBox(bool shouldShowHint, string hintMessage, string analyticsLabel)
        {
            return HintBox(shouldShowHint, hintMessage, analyticsLabel, null, null, k_StopShowingHintButton);
        }

        /// <summary>
        /// Draw a hint box with a button to add a MARS Session
        /// </summary>
        /// <param name="shouldShowHint">Whether the hint box should currently be displayed</param>
        /// <param name="hintMessage">The primary message to show in the hint box</param>
        /// <param name="messageType">What type of message box to show (info / warning)</param>
        /// /// <returns>True, unless the user presses the 'close' button</returns>
        public static bool NoActiveSessionHintBox(bool shouldShowHint, string hintMessage, MessageType messageType)
        {
            if (shouldShowHint)
            {
                EditorGUILayout.HelpBox(hintMessage, messageType);
                if (GUILayout.Button(k_AddSessionContent))
                {
                    MARSSession.EnsureRuntimeState();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds the MARS custom menu options on a MARS Window using IHasCustomMenu
        /// </summary>
        /// <param name="window">Parent window we for custom menu</param>
        /// <param name="menu">The menu that is part of IHasCustomMenu</param>
        public static void MarsCustomMenuOptions(this EditorWindow window, GenericMenu menu)
        {
            menu.AddItem(new GUIContent(k_MARSPanelLabel), false, MarsPanel.NewTab, typeof(MarsPanel));
            menu.AddItem(new GUIContent(k_SimulationViewLabel), false, SimulationView.NewTabSimulationView, typeof(SimulationView));
            menu.AddItem(new GUIContent(k_SimDeviceViewLabel), false, SimulationView.NewTabDeviceView, typeof(SimulationView));
            menu.AddItem(new GUIContent(k_DatabaseViewerLabel), false, AddTabToHere, typeof(MARSDatabaseViewer));
            menu.AddSeparator(k_MARSMenuLabel);
            menu.AddItem(new GUIContent(k_SimTestRunnerLabel), false, AddTabToHere, typeof(SimulationTestRunner));
        }

        /// <summary>
        /// Add an editor window to the tab of the active window
        /// </summary>
        /// <param name="userData">Window to open's data</param>
        /// <returns>Editor window open when menu item is selected</returns>
        public static EditorWindow CustomAddTabToHere(object userData)
        {
            if (s_DockAreaType == null)
            {
                s_DockAreaType = Assembly.GetAssembly(typeof(EditorWindow)).GetType(k_DockArea);
                Assert.IsNotNull(s_DockAreaType);

                const BindingFlags bindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy
                                                  | BindingFlags.Instance | BindingFlags.Static;

                s_AddTabHereInfo = s_DockAreaType.GetMethod(k_DockAreaAddTab, bindingFlags);

                if (s_AddTabHereInfo == null)
                    return null;

                s_HasFocusProp = s_DockAreaType.GetProperty(k_DockAreaHasFocus, bindingFlags);

                if (s_HasFocusProp == null)
                    return null;
            }

            var allDockAreas = Resources.FindObjectsOfTypeAll(s_DockAreaType);
            if (allDockAreas.Length > 0)
            {
                foreach (var dockArea in allDockAreas)
                {
                    s_DockArea = dockArea;
                    if ((bool)s_HasFocusProp.GetValue(dockArea, null))
                        break;
                }
            }

            if (s_DockArea == null)
                return null;

            s_AddTabHereInfo.Invoke(s_DockArea, new [] {userData});

            var window = EditorWindow.GetWindow(userData as Type);
            return window;
        }

        static void AddTabToHere(object userData)
        {
            var window = CustomAddTabToHere(userData);
            if (window != null)
                EditorEvents.WindowUsed.Send(new UiComponentArgs { label = window.titleContent.text, active = true });
        }

        /// <summary>
        /// Tries to delete the GameObjects passed in an array, it compares against the root in the current loaded
        /// environment and if it cannot delete a GameObject it displays a dialog box letting the user open the prefab
        /// on isolation mode
        /// </summary>
        /// <param name="ids">Instance IDs of the objs to delete</param>
        /// <returns>Returns true if it managed to delete all GameObjects, false if one of the GameObjects is part of
        /// the current loaded sim env</returns>
        internal static void TryDeleteGameObjectsFromSimEnv(int[] selectedIDs)
        {
            var envModule = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            if (envModule == null)
                return;

            var envPrefabInstance = envModule.CurrentLoadedPrefabInstanceEnvironment;
            if (envPrefabInstance == null)
                return;

            var loadedEnvInstanceID = envPrefabInstance.GetInstanceID();

            // Sim environment are prefab instances and you cannot delete children GameObjects on prefab instances
            for (var i = 0; i < selectedIDs.Length; i++)
            {
                var selectedObj = EditorUtility.InstanceIDToObject(selectedIDs[i]) as GameObject;

                if (selectedObj != null)
                {
                    var outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(selectedObj);
                    if (outermostPrefab != null && outermostPrefab.GetInstanceID() == loadedEnvInstanceID)
                    {
                        // Object is part of environment prefab
                        if (EditorUtility.DisplayDialog(k_PrefabDeleteTittle, k_PrefabDeleteMessage,
                            k_OpenPrefabLabel, k_CancelOpenPrefab))
                        {
                            AssetDatabase.OpenAsset(SimulationSettings.instance.EnvironmentPrefab);
                            SimulationView.NormalSceneView.ShowTab();
                        }

                        break;
                    }

                    Undo.DestroyObjectImmediate(selectedObj);
                    GUIUtility.ExitGUI();
                }
            }
        }

        internal static void TryDeleteGameObjectsFromSimEnv(GameObject[] objs)
        {
            var objIds = new int[objs.Length];
            for (var i = 0; i < objs.Length; i++)
            {
                objIds[i] = objs[i] != null ? objs[i].GetInstanceID() : -1;
            }

            TryDeleteGameObjectsFromSimEnv(objIds);
        }
    }
}
