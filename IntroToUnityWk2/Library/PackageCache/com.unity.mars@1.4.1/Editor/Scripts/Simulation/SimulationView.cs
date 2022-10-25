using System;
using System.Collections.Generic;
using Unity.MARS;
using Unity.MARS.Data.Synthetic;
using Unity.MARS.MARSUtils;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Authoring;
using UnityEditor.MARS.Simulation.Rendering;
using UnityEditor.XRTools.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace UnityEditor.MARS.Simulation
{
    /// <summary>
    /// MARS Simulation View displays a separate 3D scene by extending SceneView.
    /// </summary>
    //[EditorWindowTitle(useTypeNameAsIconName = false)]  // TODO update when this is not an Internal attribute
    [Serializable]
    [MovedFrom("Unity.MARS")]
    public partial class SimulationView : SceneView, ISimulationView, ISerializationCallbackReceiver
    {
        [Serializable]
        internal class LookAtData
        {
            public float Size;
            public Vector3 Pivot;
            public Quaternion Rotation;
        }

        [Serializable]
        internal enum DesaturationMode
        {
            SelectionPulse = 0,
            Always,
            None
        }

        [Serializable]
        class ViewSettings
        {
            [SerializeField]
            bool m_DrawGizmos;

            [SerializeField]
            bool m_SceneLighting;

            [SerializeField]
            SceneViewState m_SceneViewState;

            [SerializeField]
            bool m_In2DMode;

            [SerializeField]
            bool m_IsRotationLocked;

            [SerializeField]
            bool m_AudioPlay;

            [SerializeField]
            CameraSettings m_CameraSettings;

            [SerializeField]
            bool m_Orthographic;

            public void CopyFromSceneView(SceneView view)
            {
                m_DrawGizmos = view.drawGizmos;
                m_SceneLighting = view.sceneLighting;
                m_SceneViewState = view.sceneViewState;
                m_In2DMode = view.in2DMode;
                m_IsRotationLocked = view.isRotationLocked;
                m_AudioPlay = view.audioPlay;
                m_CameraSettings = view.cameraSettings;
                m_Orthographic = view.orthographic;
            }

            public void ApplyToSceneView(SceneView view)
            {
                view.drawGizmos = m_DrawGizmos;
                view.sceneLighting = m_SceneLighting;
                view.sceneViewState = m_SceneViewState;
                view.in2DMode = m_In2DMode;
                view.isRotationLocked = m_IsRotationLocked;
                view.audioPlay = m_AudioPlay;
                view.cameraSettings = m_CameraSettings;
                view.orthographic = m_Orthographic;
            }
        }

        class FadeVisual
        {
            internal bool FadeActive;
            internal double FadeTime;

            internal double FadeHoldTime = 3d;
            internal double FadeInTime = 0.15d;
            internal double FadeoutTime = 0.15d;

            internal bool LockFade;

            internal float FadeAmount
            {
                get
                {
                    if (!FadeActive)
                        return 0;

                    var time = EditorApplication.timeSinceStartup;
                    double amount;
                    if (time < FadeTime - (FadeHoldTime + FadeoutTime))
                    {
                        amount = (FadeTime - (FadeHoldTime + FadeoutTime) - time) / FadeInTime;
                    }
                    else if (time < FadeTime - (FadeHoldTime + FadeInTime))
                    {
                        amount = 0;
                    }
                    else
                    {
                        amount = (time - (FadeTime - FadeoutTime)) / FadeoutTime;
                    }

                    return 1 - Mathf.Clamp01((float)amount);
                }
            }

            internal void ShowFadeVisual()
            {
                LockFade = false;

                var time = EditorApplication.timeSinceStartup;
                if (FadeActive)
                {
                    if (time > FadeTime - (FadeHoldTime + FadeoutTime))
                        HoldFadeVisual();

                    EditorApplication.QueuePlayerLoopUpdate();
                    return;
                }

                FadeTime = time + FadeInTime + FadeHoldTime + FadeoutTime;
                FadeActive = true;
                EditorApplication.QueuePlayerLoopUpdate();
            }

            internal void HoldFadeVisual(bool lockFade = false)
            {
                FadeTime = EditorApplication.timeSinceStartup + FadeHoldTime + FadeoutTime;
                FadeActive = true;

                // Need to redraw if staring in a lock
                if (!LockFade && lockFade)
                    EditorApplication.QueuePlayerLoopUpdate();

                LockFade = lockFade;
            }

            internal void UnlockFadeVisual()
            {
                if (LockFade)
                    EditorApplication.QueuePlayerLoopUpdate();

                LockFade = false;
            }

            internal void UpdateFade(EditorWindow editorWindow)
            {
                if (LockFade)
                {
                    HoldFadeVisual(true);
                    return;
                }

                var time = EditorApplication.timeSinceStartup;
                if (time > FadeTime - (FadeHoldTime + FadeoutTime) && time < FadeTime - FadeoutTime)
                    return;

                if (time > FadeTime)
                {
                    RemoveFadeVisual();
                    return;
                }

                editorWindow.Repaint();
                EditorApplication.QueuePlayerLoopUpdate();
            }

            internal void RemoveFadeVisual()
            {
                LockFade = false;

                if (!FadeActive)
                    return;

                FadeTime = 0;
                FadeActive = false;

                // Need to queue another frame to continue fade till finished or holding
                EditorApplication.QueuePlayerLoopUpdate();
            }
        }

        class Styles
        {
            internal const float SwitchLabelVerticalOffset = 36f;
            internal const float SwitchLabelWithToolOverlayVerticalOffset = 28f;
            const string k_ContentTypeSwitch = "Content Selection Type";
            const string k_EnvironmentTypeSwitch = "Environment Selection Type";

            internal readonly GUIContent ContentTypeSwitchContent;
            internal readonly GUIContent EnvironmentTypeSwitchContent;

            internal readonly Vector2 ContentTypeSwitchSize;
            internal readonly Vector2 EnvironmentTypeSwitchhSize;

            internal readonly float MaxSwitchWidth;

            internal Styles()
            {
                ContentTypeSwitchContent = new GUIContent(k_ContentTypeSwitch);
                EnvironmentTypeSwitchContent = new GUIContent(k_EnvironmentTypeSwitch);

                ContentTypeSwitchSize = EditorStyles.whiteLabel.CalcSize(ContentTypeSwitchContent);
                EnvironmentTypeSwitchhSize = EditorStyles.whiteLabel.CalcSize(EnvironmentTypeSwitchContent);

                MaxSwitchWidth = Mathf.Max(ContentTypeSwitchSize.x, EnvironmentTypeSwitchhSize.x);
            }
        }

        /// <summary>
        /// Title of the window when in simulation view mode
        /// </summary>
        public const string SimulationViewWindowTitle = "Simulation View";
        /// <summary>
        /// Title of the window when in device view mode
        /// </summary>
        public const string DeviceViewWindowTitle = "Device View";

        const string k_CustomMARSViewWindowTitle = "Custom MARS View";
        const string k_SceneBackgroundPrefsKey = "Scene/Background";
        const string k_DeviceControlsHelp = "Press 'Play' in the Simulation controls, then hold the right mouse " +
            "button to look around, and WASD to move.";

        const string k_ContentSelectionNotification = "Cannot select Environment objects when in Content Selection Type";
        const string k_EnvironmentSelectionNotification = "Cannot select Content objects when in Environment Selection Type";

        static readonly Vector2 k_LabelOffset = new Vector2(16, 36);
        static readonly List<SimulationView> k_SimulationViews = new List<SimulationView>();

        static SimulationView s_ActiveSimulationView;
        static SceneView s_LastHoveredSimulationOrDeviceView;
        static Styles s_Styles;
        Scene m_CustomSceneCache;
        Material m_SkyBoxMaterial;

        static Action s_CloseDeviceControlsHelp = () => { MarsHints.ShowDeviceViewControlsHint = false; };

        // Delay creation of Styles till first access
        static Styles styles => s_Styles ?? (s_Styles = new Styles());

        [SerializeField]
        ViewSceneType m_SceneType = ViewSceneType.None;

        [SerializeField]
        bool m_EnvironmentSceneActive;

        [SerializeField]
        bool m_UseXRay = true;

        [SerializeField]
        ViewSettings m_SimSceneViewSettings;

        [SerializeField]
        ViewSettings m_SimCameraViewSettings;

        [SerializeField]
        LookAtData m_DefaultLookAtData;

        [SerializeField]
        List<GameObject> m_EnvironmentPrefabs = new List<GameObject>();

        [SerializeField]
        List<LookAtData> m_LookAtDataList = new List<LookAtData>();

        [SerializeField]
        DesaturationMode m_ViewDesaturationMode = DesaturationMode.SelectionPulse;

        readonly Dictionary<GameObject, LookAtData> m_LookAtDataPerEnvironment = new Dictionary<GameObject, LookAtData>();

        bool m_FramedOnStart;
        Bounds m_MovementBounds;

        CameraFPSModeHandler m_FPSModeHandler;
        Vector2 m_MouseDelta;
        int m_EventButton;
        bool m_UpdateEventButton;

        FadeVisual m_SelectionNotificationFade = new FadeVisual { FadeInTime = 0d };
        FadeVisual m_SelectionTypeFade = new FadeVisual { FadeInTime = 0d };
        FadeVisual m_SelectionScreenFade = new FadeVisual { FadeInTime = 0.25d, FadeHoldTime = 0.2d, FadeoutTime = 0.25d };

        GUIContent m_SimulationViewTitleContent;
        GUIContent m_DeviceViewTitleContent;
        GUIContent m_CustomViewTitleContent;

        GUIContent CurrentTitleContent
        {
            get
            {
                switch (SceneType)
                {
                    case ViewSceneType.Simulation:
                        return m_SimulationViewTitleContent;
                    case ViewSceneType.Device:
                        return m_DeviceViewTitleContent;
                    default:
                        return m_CustomViewTitleContent;
                }
            }
        }

        bool UseMovementBounds => MarsUserPreferences.RestrictCameraToEnvironmentBounds && m_MovementBounds != default;

        /// <summary>
        /// The editor background color form the user's editor preferences
        /// </summary>
        public static Color EditorBackgroundColor => EditorMaterialUtils.PrefToColor(EditorPrefs.GetString(k_SceneBackgroundPrefsKey));

        /// <summary>
        /// Whether this view is in Sim or Device mode
        /// </summary>
        public ViewSceneType SceneType
        {
            get => m_SceneType;
            set => SetupSceneTypeData(value);
        }

        /// <summary>
        /// The primary scene view.  Not a Simulation View.
        /// </summary>
        public static SceneView NormalSceneView { get; private set; }

        /// <summary>
        /// List of all Simulation Views
        /// </summary>
        public static List<SimulationView> SimulationViews => k_SimulationViews;

        /// <summary>
        /// The last Simulation View the user focused
        /// </summary>
        public static SceneView ActiveSimulationView
        {
            get
            {
                if (!s_ActiveSimulationView && k_SimulationViews.Count > 0)
                    s_ActiveSimulationView = k_SimulationViews[0];

                return s_ActiveSimulationView;
            }
        }

        /// <summary>
        /// The Simulation or Device View that was most recently hovered by the mouse
        /// </summary>
        public static SceneView LastHoveredSimulationOrDeviceView
        {
            get
            {
                if (s_LastHoveredSimulationOrDeviceView == null)
                    return ActiveSimulationView;

                return s_LastHoveredSimulationOrDeviceView;
            }
            private set { s_LastHoveredSimulationOrDeviceView = value; }
        }

        /// <summary>
        /// Is the simulation environment layer the active scene in the view
        /// </summary>
        public bool EnvironmentSceneActive
        {
            get { return m_EnvironmentSceneActive; }
            set
            {
                var changed = m_EnvironmentSceneActive != value;
                m_EnvironmentSceneActive = value;

                var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                if (!changed || compositeRenderModule == null
                    || !compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                {
                    return;
                }

                context.SetBackgroundSceneActive(value);
                SwitchDesaturateType();

                m_SelectionTypeFade.ShowFadeVisual();
                if (value)
                {
                    var marsEditorToolsModule = ModuleLoaderCore.instance.GetModule<MarsEditorToolsModule>();
                    marsEditorToolsModule?.EnsureProxyToolIsNotActive();
                }
            }
        }

        /// <summary>
        /// Desaturate the inactive composite layer
        /// </summary>
        internal DesaturationMode ViewDesaturationMode
        {
            get
            {
                return m_ViewDesaturationMode;
            }
            set
            {
                var changed = m_ViewDesaturationMode != value;
                m_ViewDesaturationMode = value;

                if (changed)
                    SwitchDesaturateType();
            }
        }

        /// <summary>
        /// Use x-ray rendering in the view
        /// </summary>
        public bool UseXRay
        {
            get { return m_UseXRay; }
            set
            {
                m_UseXRay = value;
                var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                if (compositeRenderModule == null || !compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                    return;

                context.UseXRay = value;
            }
        }

        /// <summary>
        /// Initialize the the simulation view window in simulation view mode.
        /// </summary>
        [MenuItem(MenuConstants.MenuPrefix + SimulationViewWindowTitle, priority = MenuConstants.SimulationViewPriority)]
        public static void InitWindowInSimulationView()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = SimulationViewWindowTitle, active = true});
            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Simulation;
            s_ActiveSimulationView = window;
            window.Show();
            window.ShowTab();
        }

        internal static void NewTabSimulationView(object userData)
        {
            if (!FindNormalSceneView())
            {
                NormalSceneView = MarsEditorUtils.CustomAddTabToHere(typeof(SceneView)) as SceneView;
                NormalSceneView.Show();
            }

            if (MarsEditorUtils.CustomAddTabToHere(userData) is SimulationView window)
            {
                window.SceneType = ViewSceneType.Simulation;
                s_ActiveSimulationView = window;
            }

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = SimulationViewWindowTitle, active = true});
        }

        /// <summary>
        /// Initialize the the simulation view window in device view mode.
        /// </summary>
        [MenuItem(MenuConstants.MenuPrefix + DeviceViewWindowTitle, priority = MenuConstants.DeviceViewPriority)]
        public static void InitWindowInDeviceView()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = DeviceViewWindowTitle, active = true});
            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Device;
            window.Show();
            window.ShowTab();
        }

        internal static void NewTabDeviceView(object userData)
        {
            if (!FindNormalSceneView())
            {
                NormalSceneView = MarsEditorUtils.CustomAddTabToHere(typeof(SceneView)) as SceneView;
                NormalSceneView.Show();
            }

            if (MarsEditorUtils.CustomAddTabToHere(userData) is SimulationView window)
                window.SceneType = ViewSceneType.Device;

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = DeviceViewWindowTitle, active = true});
        }

        internal static void EnsureDeviceViewAvailable()
        {
            foreach (var simView in SimulationViews)
            {
                if (simView.SceneType == ViewSceneType.Device)
                    return;
            }

            if (!FindNormalSceneView())
            {
                NormalSceneView = GetWindow<SceneView>();
                NormalSceneView.Show();
            }

            var window = GetWindow<SimulationView>();
            window.SceneType = ViewSceneType.Device;
            window.Show();
            window.ShowTab();
        }

        /// <summary>
        /// Scene view does not support stage handling and will not switch when stage is changed
        /// </summary>
        /// <returns><c>False</c> view will not change when the stage is changed</returns>
        protected override bool SupportsStageHandling() { return false; }

        /// <inheritdoc/>
        public override void AddItemsToMenu(GenericMenu menu)
        {
            this.MarsCustomMenuOptions(menu);
            base.AddItemsToMenu(menu);
        }

        /// <inheritdoc/>
        public override void OnEnable()
        {
            // Suppress the error message about missing scene icon. It is not an exception, but in case any other
            // exceptions happen in base.OnEnable, we use try/catch to log them re-enable logging
            var logEnabled = Debug.unityLogger.logEnabled;
            try
            {
                Debug.unityLogger.logEnabled = false;
                base.OnEnable();
            }
            catch (Exception e)
            {
                Debug.LogFormat("Exception in SimulationView.OnEnable: {0}\n{1}", e.Message, e.StackTrace);
            }
            finally
            {
                Debug.unityLogger.logEnabled = logEnabled;
            }

            m_SimulationViewTitleContent = new GUIContent(SimulationViewWindowTitle, MarsUIResources.instance.SimulationViewIcon);
            m_DeviceViewTitleContent = new GUIContent(DeviceViewWindowTitle, MarsUIResources.instance.SimulationViewIcon);
            m_CustomViewTitleContent = new GUIContent(k_CustomMARSViewWindowTitle, MarsUIResources.instance.SimulationViewIcon);

            titleContent = CurrentTitleContent;
            autoRepaintOnSceneChange = true;

            k_SimulationViews.Add(this);

            // Name our scene view camera so it is easier to track
            camera.name = $"{SimulationViewWindowTitle} Camera {GetInstanceID()}";
            camera.gameObject.hideFlags = HideFlags.HideAndDontSave;

            m_FPSModeHandler = new CameraFPSModeHandler();

            var moduleLoaderCore = ModuleLoaderCore.instance;
            // Used for one time module subscribing and setup of values from environment manager
            if (moduleLoaderCore.ModulesAreLoaded)
            {
                EditorApplication.delayCall += OnModulesLoaded;
            }

            moduleLoaderCore.ModulesLoaded += OnModulesLoaded;
            Selection.selectionChanged += SelectionChanged;

            if (SceneType == ViewSceneType.None)
                SceneType = ViewSceneType.Simulation;
        }

        void OnModulesLoaded()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            var environmentManager = moduleLoaderCore.GetModule<MARSEnvironmentManager>();
            var compositeRenderViewModule = moduleLoaderCore.GetModule<CompositeRenderModule>();

            // These will be null in module tests
            if (environmentManager == null || compositeRenderViewModule == null)
                return;

            MARSEnvironmentManager.onEnvironmentSetup += OnEnvironmentSetup;
            if (environmentManager.EnvironmentSetup)
                OnEnvironmentSetup();
        }

        void OnEnvironmentSetup()
        {
            m_MovementBounds = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>().EnvironmentBounds;
            // Need to make sure camera is assigned to the sim scene if scene has changed.
            SetupViewAsSimUser();

            EditorApplication.delayCall += SwitchDesaturateType;
        }

        /// <inheritdoc/>
        public override void OnDisable()
        {
            var moduleLoaderCore = ModuleLoaderCore.instance;
            moduleLoaderCore.ModulesLoaded -= OnModulesLoaded;

            MARSEnvironmentManager.onEnvironmentSetup -= OnEnvironmentSetup;
            Selection.selectionChanged -= SelectionChanged;

            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            compositeRenderModule?.DisposeCompositeContext(camera);

            k_SimulationViews.Remove(this);
            m_FPSModeHandler.StopMoveInput(Vector2.zero);
            m_FPSModeHandler = null;

            CheckActiveSimulationView();

            base.OnDisable();
        }

        /// <summary>
        /// OnDestroy is called to close the EditorWindow window.
        /// </summary>
        protected new virtual void OnDestroy()
        {
            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = "Simulation View", active = false});
            base.OnDestroy();
        }

        void OnFocus()
        {
            if (SceneType != ViewSceneType.Simulation)
                return;

            s_ActiveSimulationView = this;
        }

        void Update()
        {
            if (m_ViewDesaturationMode != DesaturationMode.None && m_SelectionScreenFade.FadeActive)
            {
                var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                if (compositeRenderModule == null || !compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                    return;

                UpdateFadePulseAmount(context);
            }

            if (m_SelectionNotificationFade.FadeActive)
                m_SelectionNotificationFade.UpdateFade(this);

            if (m_SelectionTypeFade.FadeActive)
                m_SelectionTypeFade.UpdateFade(this);

            if (maximized)
                return;

            // Check if normal scene view is closed and close simulation because otherwise Window -> Scene will focus
            // simulation view instead of reopening normal scene view
            if (NormalSceneView == null && !FindNormalSceneView())
                Close();
        }

        static bool FindNormalSceneView()
        {
            var allSceneViews = Resources.FindObjectsOfTypeAll(typeof (SceneView)) as SceneView[];
            if (allSceneViews == null)
                return false;

            foreach (var view in allSceneViews)
            {
                if (view is SimulationView)
                    continue;

                NormalSceneView = view;
                return true;
            }

            return false;
        }

#if UNITY_2021_2_OR_NEWER
        /// <inheritdoc />
        protected override void OnSceneGUI()
#else
        /// <inheritdoc />
        protected override void OnGUI()
#endif
        {
            if (!Entitlements.EntitlementsCheckGUI(position.width))
                return;

            // Called before base.OnGUI to consume input
            this.DrawSimulationViewToolbar();

            var currentEvent = Event.current;
            var type = currentEvent.type;
            if (type == EventType.MouseDrag)
                m_MouseDelta = currentEvent.delta;

            var resetEventButton = false;
            if (type == EventType.MouseDown)
            {
                m_EventButton = currentEvent.button;
                m_UpdateEventButton = true;
            }
            else if (type == EventType.MouseUp)
            {
                resetEventButton = true;
            }
            else if(type == EventType.ValidateCommand && (Event.current.commandName == "Delete"
                || Event.current.commandName == "SoftDelete"))
            {
                // User trying to delete the simulation environment root from the scene view,
                // we "intercept" the delete command preventing the deletion
                var selectedObjs = Selection.gameObjects;
                MarsEditorUtils.TryDeleteGameObjectsFromSimEnv(selectedObjs);
                return;
            }

            // Custom Scene is the target for drag and drop and is needed for Scene Placement Module
            m_CustomSceneCache = customScene;
            customScene = camera.scene;

            if (type == EventType.Repaint)
            {
                m_SkyBoxMaterial = RenderSettings.skybox;
                Unsupported.SetOverrideLightingSettings(m_CustomSceneCache);
                RenderSettings.skybox = null;
            }

#if UNITY_2021_2_OR_NEWER
            base.OnSceneGUI();
#else
            base.OnGUI();
#endif

            if (type == EventType.Repaint)
            {
                RenderSettings.skybox = m_SkyBoxMaterial;
                Unsupported.RestoreOverrideLightingSettings();
            }

            if (m_SelectionTypeFade.FadeActive)
                DrawSelectionTypeSwitchLabel();

            this.DrawSimulationViewToolbar();

            var toolbarHeightOffset = MarsEditorGUI.Styles.ToolbarHeight - 1;
            var rect = new Rect(0, toolbarHeightOffset, position.width,
                position.height - toolbarHeightOffset);

            if (SceneType == ViewSceneType.Device)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();

                if (focusedWindow == this && environmentManager != null && environmentManager.IsMovementEnabled &&
                    // User has pressed "Play" on device mode to move.
                    querySimulationModule != null && querySimulationModule.simulatingTemporal)
                {
                    if (m_UpdateEventButton)
                        currentEvent.button = m_EventButton;

                    m_FPSModeHandler.MovementBounds = m_MovementBounds;
                    m_FPSModeHandler.UseMovementBounds = UseMovementBounds;
                    m_FPSModeHandler.HandleGUIInput(rect, currentEvent, type, m_MouseDelta);
                }
                else
                {
                    m_FPSModeHandler.StopMoveInput(currentEvent.mousePosition);
                }
            }

            rect.yMax -= 12;

            if (resetEventButton)
                m_UpdateEventButton = false;

            if (mouseOverWindow != null && mouseOverWindow is SimulationView view)
                LastHoveredSimulationOrDeviceView = view;

            var helpAreaRect = SimulationControlsGUI.DrawHelpArea(rect, SceneType);

            var helpHintDrawing = rect != helpAreaRect;

            if (!helpHintDrawing && SceneType == ViewSceneType.Device && MarsHints.ShowDeviceViewControlsHint)
            {
                var buttonContent = new GUIContent("✕");
                helpAreaRect = SimulationControlsGUI.DrawHelpItem(rect, k_DeviceControlsHelp, MessageType.Info,
                    buttonContent, s_CloseDeviceControlsHelp);

            }

            if (m_SelectionNotificationFade.FadeActive)
                DrawSelectionNotification(helpAreaRect);

            UpdateCamera();
        }


        internal static void AddElementToLastHoveredWindow(VisualElement element)
        {
            var view = LastHoveredSimulationOrDeviceView;
            if (view != null && element.parent != view.rootVisualElement)
            {
                view.rootVisualElement.Add(element);
                element.BringToFront();
            }
        }

        internal Rect DrawSelectionNotification(Rect rect)
        {
            if (MarsUserPreferences.HideSelectionModeNotification)
                return rect;

            var simulationSettings = SimulationSettings.instance;
            if (simulationSettings.EnvironmentMode != EnvironmentMode.Synthetic
                || simulationSettings.UseSyntheticRecording)
                return rect;

            var label = EnvironmentSceneActive ? k_EnvironmentSelectionNotification : k_ContentSelectionNotification;

            var guiColor = GUI.color;
            GUI.color = new Color(1, 1, 1, m_SelectionNotificationFade.FadeAmount);
            rect = SimulationControlsGUI.DrawHelpItem(rect, label);
            GUI.color = guiColor;

            return rect;
        }

        internal void DrawSelectionTypeSwitchLabel()
        {
            if (MarsUserPreferences.HideSelectionModeNotification)
                return;

            var label = EnvironmentSceneActive ? styles.EnvironmentTypeSwitchContent : styles.ContentTypeSwitchContent;

            var labelSize = EnvironmentSceneActive ? styles.EnvironmentTypeSwitchhSize : styles.ContentTypeSwitchSize;
            var verticalOffset = 0f;

            if (k_LabelOffset.x * 2f + styles.MaxSwitchWidth > SimulationControlsGUI.GetToolbarStart(this))
            {
                var activeToolIsProxyTool = MarsEditorToolsModule.ActiveToolIsProxyTool();
                verticalOffset += activeToolIsProxyTool ? Styles.SwitchLabelWithToolOverlayVerticalOffset : Styles.SwitchLabelVerticalOffset;
            }

            var labelRect = new Rect(k_LabelOffset.x, k_LabelOffset.y + verticalOffset, labelSize.x, labelSize.y);

            var guiColor = GUI.color;
            GUI.color = new Color(1, 1, 1, m_SelectionTypeFade.FadeAmount);
            EditorGUI.LabelField(labelRect, label, MarsEditorGUI.Styles.GreyLabelStyle);
            GUI.color = guiColor;
        }

        /// <summary>
        /// Cache the LookAt information for the current synthetic environment
        /// </summary>
        /// <param name="point">The position in world space to frame.</param>
        /// <param name="direction">The direction that the Scene view should view the target point from.</param>
        /// <param name="newSize">The amount of camera zoom. Sets <c>size</c>.</param>
        public void CacheLookAt(Vector3 point, Quaternion direction, float newSize)
        {
            var environment = SimulationSettings.instance.EnvironmentPrefab;
            if (environment == null)
                return;

            var lookAtData = new LookAtData { Pivot = point, Rotation = direction, Size = Mathf.Abs(newSize) };
            m_LookAtDataPerEnvironment[environment] = lookAtData;
        }

        /// <inheritdoc/>
        public void SetupViewAsSimUser(bool forceFrame = false)
        {
            switch (SceneType)
            {
                case ViewSceneType.Simulation:
                case ViewSceneType.Device:
                {
                    if (camera == null)
                        break;

                    var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                    if (compositeRenderModule != null && compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                    {
                        context.SetBackgroundColor(EditorBackgroundColor);
                        context.AssignCameraToSimulation();
                    }

                    if (!m_FramedOnStart || forceFrame)
                    {
                        MARSEnvironmentManager.instance.TryFrameSimViewOnEnvironment(this, true);
                        m_FramedOnStart = true;
                    }

                    break;
                }
                default:
                {
                    Debug.LogFormat("Scene type {0} not supported in Simulation View.", SceneType);
                    SceneType = ViewSceneType.Simulation;
                    break;
                }
            }
        }

        internal void UpdateCamera()
        {
            if (SceneType != ViewSceneType.Device)
                return;

            // Do not let Device View enter orthographic mode
            orthographic = false;
            in2DMode = false;
            isRotationLocked = true;

            var controllingCamera = MarsRuntimeUtils.GetActiveCamera();

            if (controllingCamera == null)
            {
                var envManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                var devicePose = envManager == null ? Pose.identity : envManager.DeviceStartingPose;
                rotation = devicePose.rotation;
                size = 1f; // Size is locked when there is no controlling camera to prevent clip planes from changing
                pivot = devicePose.position + (devicePose.rotation * Vector3.forward) * cameraDistance;
                return;
            }

            var controllingCameraTransform = controllingCamera.transform;

            // need to set size before FOV and clip planes
            size = MARSSession.GetWorldScale();
            camera.fieldOfView = controllingCamera.fieldOfView;
            camera.nearClipPlane = controllingCamera.nearClipPlane;
            camera.farClipPlane = controllingCamera.farClipPlane;

            if (controllingCamera.usePhysicalProperties)
            {
                camera.usePhysicalProperties = true;
                camera.focalLength = controllingCamera.focalLength;
            }
            else
            {
                camera.usePhysicalProperties = false;
            }

            rotation = controllingCameraTransform.rotation;
            pivot = controllingCameraTransform.position + controllingCameraTransform.forward * cameraDistance;
        }

        void SetupSceneTypeData(ViewSceneType newType)
        {
            if (m_SceneType == newType)
                return;

            var oldType = m_SceneType;
            switch (oldType)
            {
                case ViewSceneType.Simulation:
                    if (m_SimSceneViewSettings == null)
                        m_SimSceneViewSettings = new ViewSettings();

                    m_SimSceneViewSettings.CopyFromSceneView(this);
                    SaveSceneViewLookAtData();
                    break;
                case ViewSceneType.Device:
                    if (m_SimCameraViewSettings == null)
                        m_SimCameraViewSettings = new ViewSettings();

                    m_SimCameraViewSettings.CopyFromSceneView(this);
                    break;
            }

            switch (newType)
            {
                case ViewSceneType.Simulation:
                    m_SimSceneViewSettings?.ApplyToSceneView(this);
                    ApplySavedLookAtToSceneView();
                    break;
                case ViewSceneType.Device:
                    m_SimCameraViewSettings?.ApplyToSceneView(this);
                    break;
            }

            m_SceneType = newType;
            titleContent = CurrentTitleContent;

            switch (newType)
            {
                case ViewSceneType.None:
                    CheckActiveSimulationView();
                    break;
                case ViewSceneType.Simulation:
                    s_ActiveSimulationView = this;
                    break;
                case ViewSceneType.Device:
                    CheckActiveSimulationView();
                    drawGizmos = false;
                    in2DMode = false;
                    isRotationLocked = true;
                    sceneLighting = true;
                    orthographic = false;
                    break;
            }
        }

        void CheckActiveSimulationView()
        {
            if (s_ActiveSimulationView != this)
                return;

            for (var i = k_SimulationViews.Count - 1; i >= 0; i--)
            {
                if (k_SimulationViews[i].SceneType != ViewSceneType.Simulation)
                    continue;

                s_ActiveSimulationView = k_SimulationViews[i];
                return;
            }
        }

        void SaveSceneViewLookAtData()
        {
            if (m_DefaultLookAtData == null)
                m_DefaultLookAtData = new LookAtData();

            var activeLookAtData = m_DefaultLookAtData;
            var simulationSettings = SimulationSettings.instance;
            if (simulationSettings.EnvironmentMode == EnvironmentMode.Synthetic)
            {
                var environment = simulationSettings.EnvironmentPrefab;
                if (environment != null && m_LookAtDataPerEnvironment.ContainsKey(environment))
                    activeLookAtData = m_LookAtDataPerEnvironment[environment];
            }

            CopyLookAtFromViewWithInverseWorldOffset(activeLookAtData);
        }

        void ApplySavedLookAtToSceneView()
        {
            var simulationSettings = SimulationSettings.instance;
            if (simulationSettings.EnvironmentMode == EnvironmentMode.Synthetic)
            {
                var environment = simulationSettings.EnvironmentPrefab;
                if (environment != null)
                {
                    if (m_LookAtDataPerEnvironment.TryGetValue(environment, out var lookAtData))
                        ApplyLookAtToViewWithWorldOffset(lookAtData);

                    return;
                }
            }

            if (m_DefaultLookAtData != null)
                ApplyLookAtToViewWithWorldOffset(m_DefaultLookAtData);
        }

        internal void SaveCurrentLookAtForEnvironment(GameObject environmentPrefab)
        {
            if (!m_LookAtDataPerEnvironment.TryGetValue(environmentPrefab, out var lookAtData))
            {
                lookAtData = new LookAtData();
                m_LookAtDataPerEnvironment[environmentPrefab] = lookAtData;
            }

            CopyLookAtFromViewWithInverseWorldOffset(lookAtData);
        }

        internal bool TryGetLookAtForEnvironment(GameObject environmentPrefab, out LookAtData lookAtData)
        {
            return m_LookAtDataPerEnvironment.TryGetValue(environmentPrefab, out lookAtData);
        }

        void ApplyLookAtToViewWithWorldOffset(LookAtData rawLookAtData)
        {
            var positionOffset = Vector3.zero;
            var rotationOffset = Quaternion.identity;
            var worldScale = 1f;
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            if (environmentManager != null)
            {
                var environmentParent = environmentManager.EnvironmentParent;
                if (environmentParent != null)
                {
                    var environmentParentTrans = environmentParent.transform;
                    positionOffset = environmentParentTrans.position;
                    rotationOffset = environmentParentTrans.rotation;
                    worldScale = environmentParentTrans.localScale.x;
                }
            }

            pivot = rotationOffset * rawLookAtData.Pivot * worldScale + positionOffset;
            size = rawLookAtData.Size * worldScale;
            rotation = rotationOffset * rawLookAtData.Rotation;
        }

        void CopyLookAtFromViewWithInverseWorldOffset(LookAtData lookAtData)
        {
            var inversePositionOffset = Vector3.zero;
            var inverseRotationOffset = Quaternion.identity;
            var inverseWorldScale = 1f;
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
            if (environmentManager != null)
            {
                var environmentParent = environmentManager.EnvironmentParent;
                if (environmentParent != null)
                {
                    var environmentParentTrans = environmentParent.transform;
                    inversePositionOffset = -environmentParentTrans.position;
                    inverseRotationOffset = Quaternion.Inverse(environmentParentTrans.rotation);
                    inverseWorldScale = 1f / environmentParentTrans.localScale.x;
                }
            }

            lookAtData.Pivot = inverseRotationOffset * (pivot + inversePositionOffset) * inverseWorldScale;
            lookAtData.Rotation = inverseRotationOffset * rotation;
            lookAtData.Size = size * inverseWorldScale;
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_EnvironmentPrefabs.Clear();
            m_LookAtDataList.Clear();
            foreach (var kvp in m_LookAtDataPerEnvironment)
            {
                m_EnvironmentPrefabs.Add(kvp.Key);
                m_LookAtDataList.Add(kvp.Value);
            }
        }

        /// <inheritdoc />
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_LookAtDataPerEnvironment.Clear();
            var prefabsCount = m_EnvironmentPrefabs.Count;
            var lookAtCount = m_LookAtDataList.Count;

            if (prefabsCount != lookAtCount)
                Debug.LogError("Environment prefabs list and look-at data list have gone out of sync");

            for (var i = 0; i < prefabsCount; ++i)
            {
                var environment = m_EnvironmentPrefabs[i];
                if (environment != null)
                    m_LookAtDataPerEnvironment[environment] = m_LookAtDataList[i];
            }
        }

        void SelectionChanged()
        {
            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            if (compositeRenderModule == null || !compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                return;

            var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();
            if (simSceneModule == null || !simSceneModule.IsSimulationReady)
            {
                ResetFadeSelection(context);
                m_SelectionNotificationFade.RemoveFadeVisual();
                return;
            }

            // Check if current selection causes desaturate and hold if it does otherwise reset
            var desaturate = HasSelectionOfRoot(Selection.gameObjects, m_EnvironmentSceneActive ?
                simSceneModule.EnvironmentRoot.transform : simSceneModule.ContentRoot.transform);

            if (desaturate)
                m_SelectionNotificationFade.RemoveFadeVisual();

            if (m_ViewDesaturationMode != DesaturationMode.SelectionPulse)
                return;

            if (desaturate)
            {
                HoldFadePulse(context, true);
            }
            else
            {
                if (focusedWindow != this)
                {
                    ResetFadeSelection(context);
                    m_SelectionNotificationFade.RemoveFadeVisual();
                }
                else
                {
                    StartFadePulse(context);
                }
            }
        }

        bool HasSelectionOfRoot(GameObject[] gameObjects, Transform root)
        {
            for (var i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i].transform.IsChildOf(root))
                    return true;
            }

            return false;
        }

        void SwitchDesaturateType()
        {
            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            if (compositeRenderModule == null || !compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                return;

            switch (m_ViewDesaturationMode)
            {
                case DesaturationMode.None:
                    ResetFadeSelection(context);
                    m_SelectionNotificationFade.RemoveFadeVisual();
                    break;
                case DesaturationMode.Always:
                    HoldFadePulse(context, true);
                    break;
                case DesaturationMode.SelectionPulse:
                    var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();
                    if (simSceneModule == null)
                    {
                        context.DesaturateComposited = false;
                        return;
                    }

                    // Check if current selection causes desaturate and hold if it does otherwise reset
                    var desaturate = HasSelectionOfRoot(Selection.gameObjects, m_EnvironmentSceneActive ?
                        simSceneModule.EnvironmentRoot.transform : simSceneModule.ContentRoot.transform);

                    if (desaturate)
                        HoldFadePulse(context, true);
                    else
                        ResetFadeSelection(context);

                    break;
            }
        }

        internal void StartFadePulse()
        {
            if (m_ViewDesaturationMode == DesaturationMode.SelectionPulse)
            {
                var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                if (compositeRenderModule == null  || !compositeRenderModule.TryGetCompositeRenderContext(camera, out var context))
                    return;

                StartFadePulse(context);
            }

            m_SelectionNotificationFade.ShowFadeVisual();
        }

        void StartFadePulse(CompositeRenderContext context)
        {
            if (focusedWindow != this)
                return;

            if (m_SelectionScreenFade.LockFade)
                m_SelectionScreenFade.UnlockFadeVisual();

            m_SelectionScreenFade.ShowFadeVisual();
            context.DesaturateComposited = m_SelectionScreenFade.FadeActive;
            context.FadeAmount = m_SelectionScreenFade.FadeAmount;
        }

        void UpdateFadePulseAmount(CompositeRenderContext context)
        {
            m_SelectionScreenFade.UpdateFade(this);
            context.DesaturateComposited = m_SelectionScreenFade.FadeActive;
            context.FadeAmount = m_SelectionScreenFade.FadeAmount;
        }

        void HoldFadePulse(CompositeRenderContext context, bool lockFade = false)
        {
            m_SelectionScreenFade.HoldFadeVisual(lockFade);
            context.DesaturateComposited = m_SelectionScreenFade.FadeActive;
            context.FadeAmount = m_SelectionScreenFade.FadeAmount;
        }

        void ResetFadeSelection(CompositeRenderContext context)
        {
            m_SelectionScreenFade.RemoveFadeVisual();
            context.DesaturateComposited = m_SelectionScreenFade.FadeActive;
            context.FadeAmount = m_SelectionScreenFade.FadeAmount;
        }

        internal void ForceRepaint()
        {
            // Calling single repaint does not always get the latest updates in the sim view rendering
            Repaint();
            EditorApplication.delayCall += Repaint;
        }

        internal static void ForceRepaintAll()
        {
            foreach (var simView in SimulationViews)
            {
                simView.ForceRepaint();
            }
        }
    }
}
