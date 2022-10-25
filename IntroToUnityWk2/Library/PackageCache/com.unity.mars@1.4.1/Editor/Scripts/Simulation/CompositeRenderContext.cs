using System;
using Unity.MARS;
using Unity.MARS.Simulation.Rendering;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

#if INCLUDE_POST_PROCESSING
using UnityEngine.Rendering.PostProcessing;
#endif

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace UnityEditor.MARS.Simulation.Rendering
{
    /// <summary>
    /// The settings used to render the view for the composite context from the CompositeRenderModule.
    /// </summary>
    [MovedFrom("Unity.MARS")]
    public partial class CompositeRenderContext : IDisposable
    {
        struct CameraSettings
        {
            Scene m_Scene;
            int m_CullingMask;
            CameraClearFlags m_ClearFlags;
            Color m_BackgroundColor;
            float m_Depth;
            RenderTexture m_TargetTexture;

            internal CameraSettings(Camera camera)
            {
                m_Scene = camera.scene;
                m_CullingMask = camera.cullingMask;
                m_ClearFlags = camera.clearFlags;
                m_BackgroundColor = camera.backgroundColor;
                m_Depth = camera.depth;
                m_TargetTexture = camera.targetTexture;
            }

            internal void CopySettingToCamera(Camera camera)
            {
                camera.scene = m_Scene;
                camera.cullingMask = m_CullingMask;
                camera.clearFlags = m_ClearFlags;
                camera.backgroundColor = m_BackgroundColor;
                camera.depth = m_Depth;
                camera.targetTexture = m_TargetTexture;
            }
        }

        const string k_CompositeShader = "MARS/CompositeBlit";
        static readonly int k_TemporaryDestTextureID = Shader.PropertyToID("_CompositeTemporaryDest");
        const string k_CompositeBufferName = "Composite Renderer Buffer";
        const float k_FadeScaleFactor = 0.75f;

        static readonly int k_OverlayTexID = Shader.PropertyToID("_CompositeBlitOverlayTex");
        static readonly int k_AlphaTexID = Shader.PropertyToID("_CompositeAlphaSourceTex");
        const string k_PreMultiplyOverlay = "PREMULTIPLY_OVERLAY";
        const string k_SceneViewFadeMat = "SceneView/SceneViewGrayscaleEffectFade.mat";
        static readonly int k_Fade = Shader.PropertyToID("_Fade");
        static readonly int k_Desaturate = Shader.PropertyToID("_Desaturate");
        static Material s_FadeMaterial;
        static bool s_UseCameraSubsystem;

        /// <summary>
        /// Camera subsystem will handle the compositing of the camera images.
        /// </summary>
        internal static bool UseCameraSubsystem
        {
            set => s_UseCameraSubsystem = value;
        }

        /// <summary>
        /// The list of [CameraEvent](https://docs.unity3d.com/ScriptReference/Rendering.CameraEvent.html)s
        /// to add to the [CommandBuffer](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html)
        /// for target camera rendering foreground with [Forward](https://docs.unity3d.com/ScriptReference/RenderingPath.Forward.html)
        /// and [DeferredLighting](https://docs.unity3d.com/ScriptReference/RenderingPath.DeferredLighting.html).
        /// </summary>
        static readonly CameraEvent[] k_DefaultForegroundCameraEvents = new[]
        {
            CameraEvent.BeforeForwardOpaque,
            CameraEvent.BeforeGBuffer
        };

        /// <summary>
        /// The list of [CameraEvent](https://docs.unity3d.com/ScriptReference/Rendering.CameraEvent.html)s
        /// to add to the [CommandBuffer](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html)
        /// for target camera rendering foreground with
        /// [DeferredShading]https://docs.unity3d.com/ScriptReference/RenderingPath.DeferredShading.html).
        /// Included depth occlusion material do not work in DeferredShading.
        /// </summary>
        static readonly CameraEvent[] k_DeferredForegroundCameraEvents = new[]
        {
            CameraEvent.BeforeGBuffer
        };

        /// <summary>
        /// The list of [CameraEvent](https://docs.unity3d.com/ScriptReference/Rendering.CameraEvent.html)s
        /// to add to the [CommandBuffer](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html)
        /// for target camera rendering background with [Forward](https://docs.unity3d.com/ScriptReference/RenderingPath.Forward.html)
        /// and [DeferredLighting](https://docs.unity3d.com/ScriptReference/RenderingPath.DeferredLighting.html).
        /// </summary>
        static readonly CameraEvent[] k_DefaultBackgroundCameraEvents = new []
        {
            CameraEvent.AfterForwardAlpha
        };

        /// <summary>
        /// The list of [CameraEvent](https://docs.unity3d.com/ScriptReference/Rendering.CameraEvent.html)s
        /// to add to the [CommandBuffer](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html)
        /// for target camera rendering background with
        /// [DeferredShading]https://docs.unity3d.com/ScriptReference/RenderingPath.DeferredShading.html).
        /// Included depth occlusion material do not work in DeferredShading.
        /// </summary>
        static readonly CameraEvent[] k_DeferredBackgroundCameraEvents = new []
        {
            CameraEvent.AfterFinalPass
        };

        CompositeViewType m_CompositeViewType;

        Camera m_TargetCamera;
        Camera m_CompositeCamera;
        CompositeRenderBackground m_CompositeRenderBackground;

        bool m_ShowImageEffects;
        bool m_BackgroundSceneActive;
        bool m_DesaturateComposited;
        bool m_UseXRay;
        float m_ShadowDistance;

        Material m_CompositeMaterial;
        LayerMask m_CompositeLayerMask;

#if INCLUDE_POST_PROCESSING
        PostProcessLayer m_CompositePostProcess;
#endif

        CommandBuffer m_CompositeLayerBuffer;
        RenderTextureDescriptor m_TargetDescriptor;
        CameraClearFlags m_BackgroundClearFlags;
        Color m_BackgroundColor;
        Scene m_PrefabStageScene;
        RenderTexture m_CompositeCameraTexture;
        int m_TargetCameraCullingMask;
        float m_FadeAmount;
        bool m_UsingScriptableRenderPipeline;

        /// <summary>
        /// The camera that is the target of the compositing in the context
        /// </summary>
        public Camera TargetCamera => m_TargetCamera;

        /// <summary>
        /// Type of view the context is being used with
        /// </summary>
        internal CompositeViewType CompositeViewType => m_CompositeViewType;

        /// <summary>
        /// Show image effects in the View
        /// </summary>
        public bool ShowImageEffects
        {
            get => m_ShowImageEffects;
            set => m_ShowImageEffects = value;
        }

        /// <summary>
        /// Use the X-Ray wall cut away in the view.
        /// </summary>
        public bool UseXRay
        {
            get => m_UseXRay;
            set => m_UseXRay = value;
        }

        /// <summary>
        /// Whether to desaturate the composite camera's texture when compositing.
        /// </summary>
        public bool DesaturateComposited
        {
            get => m_DesaturateComposited;
            set => m_DesaturateComposited = value;
        }

        /// <summary>
        /// Clear flag settings for the composite camera
        /// </summary>
        public CameraClearFlags BackgroundClearFlag
        {
            get => m_BackgroundClearFlags;
            set => m_BackgroundClearFlags = value;
        }

        /// <summary>
        /// The LayerMask for the background layer of the composite.
        /// This is also the layer any image effects will use in the composite.
        /// </summary>
        public LayerMask CompositeLayerMask
        {
            get => m_CompositeLayerMask;
            set => m_CompositeLayerMask = value;
        }

        /// <summary>
        /// The composite camera being used in the context
        /// </summary>
        public Camera CompositeCamera => m_CompositeCamera;

        /// <summary>
        /// Render texture assigned as the render target for the composite camera
        /// </summary>
        public RenderTexture CompositeCameraTexture => m_CompositeCameraTexture;

        internal float FadeAmount
        {
            get => m_FadeAmount;
            set => m_FadeAmount = value;
        }

        /// <summary>
        /// Is the composite context active
        /// </summary>
        static bool compositeSimulationReady
        {
            get
            {
                var module = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                return module != null && module.SimulationSceneReady;
            }
        }

        /// <summary>
        /// Is the composite targeting a view that is the base type <c>SceneView</c>
        /// </summary>
        public bool IsBaseSceneView => m_CompositeViewType == CompositeViewType.NormalSceneView
            || m_CompositeViewType == CompositeViewType.PrefabIsolation;

        /// <summary>
        /// Is the composite targeting a view that is of the type <c>SceneView</c>
        /// </summary>
        bool IsSimSceneView => m_CompositeViewType == CompositeViewType.SimulationView
            || m_CompositeViewType == CompositeViewType.DeviceView;

        /// <summary>
        /// Is the composite targeting a view that supports the camera render stack
        /// </summary>
        bool IsRenderStackView => m_CompositeViewType == CompositeViewType.GameView
            || m_CompositeViewType == CompositeViewType.OtherView;

        bool UseFallbackRendering => CompositeRenderModuleOptions.instance.UseFallbackCompositeRendering
            && (m_CompositeViewType != CompositeViewType.GameView || !s_UseCameraSubsystem);

        static bool UseCameraStackInFallback => CompositeRenderModuleOptions.instance.UseCameraStackInFallback;

        /// <summary>
        /// Is the composite camera usable for the view type
        /// </summary>
        public bool UseCompositeCamera => !UseFallbackRendering || UseCameraStackInFallback && IsRenderStackView && compositeSimulationReady;

        CameraEvent[] CameraEvents
        {
            get
            {
                if (m_BackgroundSceneActive)
                {
                    return m_TargetCamera.actualRenderingPath != RenderingPath.DeferredShading ?
                        k_DefaultBackgroundCameraEvents : k_DeferredBackgroundCameraEvents;
                }

                return m_TargetCamera.actualRenderingPath != RenderingPath.DeferredShading ?
                    k_DefaultForegroundCameraEvents : k_DeferredForegroundCameraEvents;
            }
        }

        /// <summary>
        /// Called right before the composite camera updates from the target camera
        /// </summary>
        public event Action BeforeCompositeCameraUpdate;

        /// <summary>
        /// Creates a new composite render context for a given view.
        /// </summary>
        /// <param name="compositeViewType">What type of view is the target.</param>
        /// <param name="targetCamera">The camera that renders the view</param>
        /// <param name="cameraTargetDescriptor">the render texture descriptor for the target cameras render texture</param>
        /// <param name="backgroundColor">The background color for compositing the view.</param>
        /// <param name="compositeLayerMask">Layer mask for the composite objects.</param>
        /// <param name="showImageEffects">Show image effects in the view.</param>
        /// <param name="backgroundSceneActive">Is the background of the composite the target camera scene</param>
        /// <param name="desaturateComposited">Desaturate the composited cameras output.</param>
        /// <param name="useXRay">Is the x-ray shader in use in the view.</param>
        CompositeRenderContext(CompositeViewType compositeViewType, Camera targetCamera,
            RenderTextureDescriptor cameraTargetDescriptor, Color backgroundColor, LayerMask compositeLayerMask,
            bool showImageEffects = false, bool backgroundSceneActive = false, bool desaturateComposited = false,
            bool useXRay = true)
        {
            m_CompositeViewType = compositeViewType;
            m_TargetCamera = targetCamera;

            // Camera rendering cannot decide if camera needs an intermediate texture from the command buffer
            // To prevent the camera from rendering to a back buffer and flipping the texture in the composite
            // We force the camera to use an intermediate render texture.
            m_TargetCamera.forceIntoRenderTexture = true;

            m_UseXRay = useXRay;
            m_ShowImageEffects = showImageEffects;
            m_CompositeLayerMask = compositeLayerMask;

            m_BackgroundClearFlags = m_TargetCamera.clearFlags;
            m_TargetCameraCullingMask = m_TargetCamera.cullingMask;

            m_CompositeMaterial = new Material(Shader.Find(k_CompositeShader))
            {
                // Avoid user modification and ensure cleanup of material if there is an exception
                hideFlags = HideFlags.HideAndDontSave
            };

            m_BackgroundSceneActive = backgroundSceneActive;
            m_DesaturateComposited = desaturateComposited;
            m_BackgroundColor = backgroundColor;
            m_TargetDescriptor = cameraTargetDescriptor;
            m_ShadowDistance = QualitySettings.shadowDistance;

            SetupCompositeCamera();

            SetRenderTextureDescriptor(m_TargetDescriptor);

            SimulationSceneModule.SimulationSceneClosing += CleanCompositeView;
            SimulationSceneModule.SimulationSceneOpened += CleanCompositeView;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            m_CompositeRenderBackground = targetCamera.GetComponent<CompositeRenderBackground>();
            m_CompositeRenderBackground.PreCullCamera += PreCompositeCullTargetCamera;
            m_CompositeRenderBackground.PostRenderCamera += PostCompositeRenderTargetCamera;

            m_CompositeRenderBackground.UseCameraCallbacks = () => IsRenderStackView;
            m_CompositeRenderBackground.UseFallbackRendering = () => UseFallbackRendering;
        }

        internal CompositeRenderContext(CompositeRenderData compositeRenderData): this(compositeRenderData.ViewType, compositeRenderData.TargetCamera,
            compositeRenderData.CameraTargetDescriptor, compositeRenderData.BackgroundColor, compositeRenderData.CompositeLayerMask,
            compositeRenderData.ShowImageEffects, compositeRenderData.BackgroundSceneActive, compositeRenderData.DesaturateComposited,
            compositeRenderData.UseXRay) { }

        /// <inheritdoc />
        public void Dispose()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SimulationSceneModule.SimulationSceneClosing -= CleanCompositeView;
            SimulationSceneModule.SimulationSceneOpened -= CleanCompositeView;

            var moduleLoaderCore = ModuleLoaderCore.instance;
            var simSceneModule = moduleLoaderCore.GetModule<SimulationSceneModule>();

            m_CompositeRenderBackground = null;

            if (m_TargetCamera != null)
            {
                TearDownCommandBuffer(CameraEvents, m_TargetCamera);

                if (simSceneModule != null && compositeSimulationReady
                    && (IsSimSceneView || m_CompositeViewType == CompositeViewType.OtherView))
                    simSceneModule.RemoveCameraFromSimulation(m_TargetCamera);

                if (m_TargetCamera)
                    m_TargetCamera.cullingMask = m_TargetCameraCullingMask;

                m_TargetCamera = null;
            }

            if (m_CompositeMaterial != null)
            {
                UnityObjectUtils.Destroy(m_CompositeMaterial);
                m_CompositeMaterial = null;
            }

            if (m_CompositeCamera != null)
            {
                UnityObjectUtils.Destroy(m_CompositeCamera.gameObject);
                m_CompositeCamera = null;
            }

            if (m_CompositeCameraTexture != null)
            {
                m_CompositeCameraTexture.Release();
                UnityObjectUtils.Destroy(m_CompositeCameraTexture);
                m_CompositeCameraTexture = null;
            }
        }

        /// <summary>
        /// Refresh the render setup of the composite context
        /// </summary>
        void RefreshCompositeRenderSetup()
        {
            m_UsingScriptableRenderPipeline = GraphicsSettings.renderPipelineAsset != null;

            if (m_TargetCamera == null)
                return;

            var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();
            if (!compositeSimulationReady || simSceneModule == null)
                return;

            if (UseCompositeCamera && m_CompositeCamera == null)
                SetupCompositeCamera();
            else
                UpdateCompositeCamera();

            switch (m_CompositeViewType)
            {
                case CompositeViewType.NormalSceneView:
                case CompositeViewType.PrefabIsolation:
                {
                    break;
                }
                case CompositeViewType.GameView:
                case CompositeViewType.OtherView:
                {
                    m_TargetCamera.scene = simSceneModule.ContentScene;

                    break;
                }
                case CompositeViewType.DeviceView:
                case CompositeViewType.SimulationView:
                {
                    if (UseFallbackRendering)
                    {
                        m_TargetCamera.scene = simSceneModule.ContentScene;
                        return;
                    }

                    if (m_BackgroundSceneActive)
                    {
                        m_TargetCamera.scene = simSceneModule.EnvironmentScene;
                        m_CompositeCamera.scene = simSceneModule.ContentScene;
                    }
                    else
                    {
                        m_TargetCamera.scene = simSceneModule.ContentScene;
                        m_CompositeCamera.scene = simSceneModule.EnvironmentScene;
                    }
                    break;
                }
            }
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            switch (m_CompositeViewType)
            {
                case CompositeViewType.NormalSceneView:
                case CompositeViewType.PrefabIsolation:
                {
                    if (m_CompositeCamera == null)
                        SetupCompositeCamera();

                    break;
                }
                case CompositeViewType.DeviceView:
                case CompositeViewType.SimulationView:
                {
                    if (m_CompositeCamera == null && compositeSimulationReady)
                        SetupCompositeCamera();

                    break;
                }
                case CompositeViewType.GameView:
                case CompositeViewType.OtherView:
                {
                    break;
                }
            }
        }

        void SetupCompositeCamera()
        {
            if (m_TargetCamera == null)
                return;

            if (m_CompositeCamera != null)
            {
                UnityObjectUtils.Destroy(m_CompositeCamera.gameObject);
                m_CompositeCamera = null;
            }

            if (UseCompositeCamera)
            {
                var compositeCameraGo = new GameObject($"{m_TargetCamera.name} Composite", typeof(Camera))
                {
                    hideFlags = m_CompositeViewType == CompositeViewType.GameView ? HideFlags.DontSave : HideFlags.HideAndDontSave,
                };

                compositeCameraGo.transform.SetParent(m_TargetCamera.transform);

                m_CompositeCamera = compositeCameraGo.GetComponent<Camera>();
                m_CompositeCamera.name = compositeCameraGo.name;
                m_CompositeCamera.backgroundColor = m_BackgroundColor;
                m_CompositeCamera.clearFlags = m_BackgroundClearFlags;
                SetRenderTextureDescriptor(m_TargetDescriptor);
                m_TargetCameraCullingMask = m_TargetCamera.cullingMask;
            }

            UpdateCompositeCamera();

            if (UseCompositeCamera)
                m_CompositeCamera.transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Assigns the cameras in a composite context to the simulation.
        /// </summary>
        public void AssignCameraToSimulation()
        {
            if (!compositeSimulationReady)
                return;

            var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();
            if (simSceneModule == null)
            {
                Debug.LogError("SimulationSceneModule not found. Camera compositing failed to set up.");
                return;
            }

            if (!simSceneModule.IsCameraAssignedToSimulationScene(m_TargetCamera)
                && !(m_CompositeViewType == CompositeViewType.GameView || IsBaseSceneView))
                simSceneModule.AssignCameraToSimulation(m_TargetCamera);

            if (UseCompositeCamera && !simSceneModule.IsCameraAssignedToSimulationScene(m_CompositeCamera))
                simSceneModule.AssignCameraToSimulation(m_CompositeCamera);

            // When using fallback rendering and using the composite camera,
            // the camera is parented to the target camera
            if (!UseFallbackRendering)
            {
                if (m_BackgroundSceneActive && m_CompositeCamera.scene != simSceneModule.ContentScene)
                {
                    m_CompositeCamera.scene = simSceneModule.ContentScene;
                }
                else if (m_CompositeCamera.scene != simSceneModule.EnvironmentScene)
                {
                    m_CompositeCamera.scene = simSceneModule.EnvironmentScene;
                }
            }

            RefreshCompositeRenderSetup();
        }

        /// <summary>
        /// The render texture descriptor to use for the camera renders.
        /// </summary>
        /// <param name="renderTextureDescriptor">The render texture descriptor to use for the render texture.</param>
        internal void SetRenderTextureDescriptor(RenderTextureDescriptor renderTextureDescriptor)
        {
            m_TargetDescriptor = renderTextureDescriptor;

            // Adapted from SceneView.CreateCameraTargetTexture
            // make sure we actually support R16G16B16A16_SFloat
            GraphicsFormat format;

            if (UseCompositeCamera)
            {
                format = m_CompositeCamera.allowHDR && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render)
                    ? GraphicsFormat.R16G16B16A16_SFloat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            }
            else if (m_TargetCamera)
            {
                format = m_TargetCamera.allowHDR && SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, FormatUsage.Render)
                    ? GraphicsFormat.R16G16B16A16_SFloat : SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            }
            else
            {
                //Debug.Log("No target camera!");
                return;
            }

            // Need to make sure we set the graphics format to our valid format
            // or we will get an out of range value for the render texture format
            // when we try creating the render texture
            m_TargetDescriptor.graphicsFormat = format;
            // Need to enable depth buffer if the target camera did not already have it.
            if (m_TargetDescriptor.depthBufferBits < 24)
                m_TargetDescriptor.depthBufferBits = 24;

            // Using a target texture can interfere with game view camera stack
            if (UseCompositeCamera && UseCameraStackInFallback && m_CompositeViewType == CompositeViewType.GameView)
            {
                if (m_CompositeCamera.targetTexture != null)
                {
                    m_CompositeCamera.targetTexture.Release();
                    m_CompositeCamera.targetTexture = null;
                }

                if (m_CompositeCameraTexture != null)
                {
                    m_CompositeCamera.targetTexture = null;
                    m_CompositeCameraTexture.Release();
                    UnityObjectUtils.Destroy(m_CompositeCameraTexture);
                    m_CompositeCameraTexture = null;
                }

                return;
            }

            if (UseCompositeCamera && m_CompositeCameraTexture != null && m_CompositeCameraTexture.graphicsFormat != format)
            {
                m_CompositeCamera.targetTexture = null;
                m_CompositeCameraTexture.Release();
                UnityObjectUtils.Destroy(m_CompositeCameraTexture);
                m_CompositeCameraTexture = null;
            }

            if (m_CompositeCameraTexture == null)
            {
                var nameString = UseCompositeCamera ? m_CompositeCamera.name : m_TargetCamera.name;
                m_CompositeCameraTexture = new RenderTexture(m_TargetDescriptor)
                {
                    name = $"{nameString} RenderTexture",
                    antiAliasing = 1,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }
            else if (m_CompositeCameraTexture.width != m_TargetDescriptor.width
                || m_CompositeCameraTexture.height != m_TargetDescriptor.height)
            {
                m_CompositeCameraTexture.Release();
                m_CompositeCameraTexture.width = m_TargetDescriptor.width;
                m_CompositeCameraTexture.height = m_TargetDescriptor.height;
            }

            m_CompositeCameraTexture.Create();

            if (UseCompositeCamera)
                m_CompositeCamera.targetTexture = m_CompositeCameraTexture;
        }

        bool EnsureCompositeRenderingSettings(out SimulationRenderSettings simulationRenderSettings)
        {
            simulationRenderSettings = null;
            m_TargetCameraCullingMask = m_TargetCamera.cullingMask;
            m_ShadowDistance = QualitySettings.shadowDistance;

            var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();
            if (IsBaseSceneView)
            {
                var targetCameraScene = m_TargetCamera.scene;
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage == null)
                {
                    m_CompositeViewType = CompositeViewType.NormalSceneView;
                }
                else
                {
                    m_PrefabStageScene = prefabStage.scene;
                    if (!m_PrefabStageScene.IsValid() || targetCameraScene != m_PrefabStageScene)
                    {
                        m_CompositeViewType = CompositeViewType.NormalSceneView;
                    }

                    if (CompositeRenderRuntimeUtils.SceneSimulationRenderSettings.TryGetValue(m_PrefabStageScene,
                        out var prefabRenderSettings))
                    {
                        m_CompositeViewType = CompositeViewType.PrefabIsolation;
                        simulationRenderSettings = prefabRenderSettings;
                    }
                }
            }
            else if (compositeSimulationReady)
            {
                if (UseCompositeCamera)
                {
                    m_TargetCamera.scene = m_BackgroundSceneActive ? simSceneModule.EnvironmentScene : simSceneModule.ContentScene;
                    m_CompositeCamera.scene = m_BackgroundSceneActive ? simSceneModule.ContentScene : simSceneModule.EnvironmentScene;
                }
                else
                {
                    m_TargetCamera.scene = simSceneModule.ContentScene;
                }
            }

            if (IsBaseSceneView && !compositeSimulationReady)
            {
                if (simulationRenderSettings == null)
                    simulationRenderSettings = new SimulationRenderSettings();

                return false;
            }

            UpdateCompositeCamera();

            // If you have multiple game views open at different resolutions only the one that is active
            // is guaranteed to be at the correct resolution for the the window.
            if (m_TargetDescriptor.width != m_TargetCamera.scaledPixelWidth
                || m_TargetDescriptor.height != m_TargetCamera.scaledPixelHeight)
            {
                m_TargetDescriptor.width = m_TargetCamera.scaledPixelWidth;
                m_TargetDescriptor.height = m_TargetCamera.scaledPixelHeight;
                SetRenderTextureDescriptor(m_TargetDescriptor);
            }

            // Need to control the rendering of the background camera here.
            // This is to make sure the render setting are applied correctly
            // and the skybox renders to the correct view.
            if (!IsBaseSceneView)
                CompositeRenderRuntimeUtils.SceneSimulationRenderSettings.TryGetValue(simSceneModule.EnvironmentScene, out simulationRenderSettings);

            if (simulationRenderSettings == null)
                simulationRenderSettings = new SimulationRenderSettings();

            return true;
        }

        internal void PreCompositeCullTargetCamera()
        {
            m_ShadowDistance = QualitySettings.shadowDistance;

            // Need to refresh render setup when switching from legacy to SRP since composite cameras usage can change
            if (m_UsingScriptableRenderPipeline != (GraphicsSettings.currentRenderPipeline != null))
                RefreshCompositeRenderSetup();

            // This is to ensure the a simulation user
            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();

            if (compositeRenderModule == null)
                return;

            if (m_CompositeViewType == CompositeViewType.Undefined || IsBaseSceneView)
                compositeRenderModule.TryRemoveContextUsingSimulation(this);
            else
                compositeRenderModule.TryAddContextUsingSimulation(this);

            // Needs to still be called for x-ray
            compositeRenderModule.OnBeforeBackgroundCameraRender(this);

            if (IsBaseSceneView || !UseCompositeCamera ||!EnsureCompositeRenderingSettings(out var simulationRenderSettings))
                return;

            switch (m_CompositeViewType)
            {
                case CompositeViewType.NormalSceneView:
                {
                    break;
                }
                case CompositeViewType.PrefabIsolation:
                {
                    RenderPrefabIsolation(simulationRenderSettings);
                    break;
                }
                case CompositeViewType.GameView:
                {
                    // Camera subsystem with AR Camera Background will combine the images for the rendered cameras
                    if (s_UseCameraSubsystem)
                    {
                        RenderCompositeAsBackground(simulationRenderSettings);
                        break;
                    }

                    goto case CompositeViewType.OtherView;
                }
                case CompositeViewType.DeviceView:
                case CompositeViewType.SimulationView:
                case CompositeViewType.OtherView:
                {
                    if (!UseFallbackRendering)
                    {
                        if (m_BackgroundSceneActive)
                            RenderCompositeAsForeground(simulationRenderSettings);
                        else
                            RenderCompositeAsBackground(simulationRenderSettings);
                    }

                    if (!UseFallbackRendering)
                    {
                        SetupCommandBuffers(CameraEvents, m_TargetCamera);
                    }

                    break;
                }
            }
        }

        internal void PostCompositeRenderTargetCamera()
        {
            SetupImageEffects();
            QualitySettings.shadowDistance = m_ShadowDistance;

            if (!UseFallbackRendering)
            {
                Unsupported.RestoreOverrideLightingSettings();
            }

            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            if (compositeRenderModule == null)
                return;

            if (m_CompositeViewType == CompositeViewType.PrefabIsolation)
                return;

            if (!UseFallbackRendering)
                TearDownCommandBuffer(CameraEvents, m_TargetCamera);

            if (!compositeSimulationReady)
                return;

            CleanCompositeView();
        }

        void SetupCommandBuffers(CameraEvent[] evts, Camera sceneCamera)
        {
#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                return;
#endif

            if (!UseCompositeCamera || !compositeSimulationReady)
                return;

            // Blit the composite camera output into the target camera's render buffer.
            var buffer = new CommandBuffer { name = k_CompositeBufferName };
            buffer.BeginSample(buffer.name);

            buffer.GetTemporaryRT(k_TemporaryDestTextureID, m_TargetDescriptor);

            buffer.GetTemporaryRT(k_AlphaTexID, m_TargetDescriptor);

            var destTargetID = new RenderTargetIdentifier(k_TemporaryDestTextureID);
            var alphaID = new RenderTargetIdentifier(k_AlphaTexID);

            RenderTargetIdentifier foregroundID;
            RenderTargetIdentifier backgroundID;

            if (!m_BackgroundSceneActive)
            {
                foregroundID = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                backgroundID = new RenderTargetIdentifier(m_CompositeCameraTexture);
                m_CompositeMaterial.DisableKeyword(k_PreMultiplyOverlay);
            }
            else
            {
                foregroundID = new RenderTargetIdentifier(m_CompositeCameraTexture);
                backgroundID = new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget);
                // Alpha needs to be multiplied to foreground if it is being desaturated
                if (m_DesaturateComposited)
                    m_CompositeMaterial.DisableKeyword(k_PreMultiplyOverlay);
                else
                    m_CompositeMaterial.EnableKeyword(k_PreMultiplyOverlay);
            }

            if (m_BackgroundSceneActive)
            {
                buffer.Blit(foregroundID, alphaID);
            }

            // Handle desaturation fade if active
            if (m_DesaturateComposited)
            {
                if (!s_FadeMaterial)
                    s_FadeMaterial = EditorGUIUtility.LoadRequired(k_SceneViewFadeMat) as Material;

                s_FadeMaterial.SetFloat(k_Fade, m_DesaturateComposited ? m_FadeAmount * k_FadeScaleFactor : 0f);

                buffer.GetTemporaryRT(k_Desaturate, m_TargetDescriptor);
                var desaturateID = new RenderTargetIdentifier(k_Desaturate);

                var desaturateDest = !m_BackgroundSceneActive ? backgroundID : foregroundID;

                buffer.Blit(desaturateDest, desaturateID, s_FadeMaterial);
                // This writes over the background ID or foreground ID
                // so that ID and the desaturateDest ID will point to the same RT
                buffer.Blit(desaturateID, desaturateDest);
                buffer.ReleaseTemporaryRT(k_Desaturate);
            }

            // Special composite is only needed when target camera is the background.
            // This is due to needing to use the alpha of the texture rendered on top of the composite.
            if (m_BackgroundSceneActive)
            {
                buffer.SetGlobalTexture(k_OverlayTexID, foregroundID);
                buffer.Blit(backgroundID, destTargetID, m_CompositeMaterial);
            }
            else
            {
                buffer.Blit(backgroundID, destTargetID);
            }

            if (GraphicsSettings.currentRenderPipeline == null && sceneCamera.actualRenderingPath == RenderingPath.DeferredLighting)
            {
                buffer.ClearRenderTarget(true, false, Color.clear);
            }

            // Blit directly to the active camera target to keep depth buffer
            buffer.Blit(destTargetID, BuiltinRenderTextureType.CameraTarget);

            buffer.ReleaseTemporaryRT(k_TemporaryDestTextureID);
            buffer.ReleaseTemporaryRT(k_AlphaTexID);

            buffer.EndSample(buffer.name);
            m_CompositeLayerBuffer = buffer;

            foreach (var evt in evts)
            {
                sceneCamera.AddCommandBuffer(evt, m_CompositeLayerBuffer);
            }
        }

        void TearDownCommandBuffer(CameraEvent[] evts, Camera sceneCamera)
        {
#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                return;
#endif

            if (!UseCompositeCamera || m_CompositeLayerBuffer == null)
                return;

            foreach (var evt in evts)
            {
                sceneCamera.RemoveCommandBuffer(evt, m_CompositeLayerBuffer);
            }

            m_CompositeLayerBuffer.Release();
            m_CompositeLayerBuffer = null;
        }

        internal static void CompositeCameraUpdate(Camera camera)
        {
            var renderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            if (renderModule == null)
                return;

            if (renderModule.TryGetCompositeRenderContext(camera, out var context))
            {
                context.PreCompositeCullTargetCamera();
                context.PostCompositeRenderTargetCamera();
            }
        }

        internal void UpdateCompositeCamera()
        {
            BeforeCompositeCameraUpdate?.Invoke();

            if (UseCompositeCamera)
            {
                var cameraSettings = new CameraSettings(m_CompositeCamera);

                m_CompositeCamera.transform.SetWorldPose(m_TargetCamera.transform.GetWorldPose());

                m_CompositeCamera.CopyFrom(m_TargetCamera);
                cameraSettings.CopySettingToCamera(m_CompositeCamera);

                if (m_CompositeViewType == CompositeViewType.GameView)
                    m_CompositeCamera.depth = m_TargetCamera.depth - 1;
            }

            switch (m_CompositeViewType)
            {
                case CompositeViewType.NormalSceneView:
                case CompositeViewType.PrefabIsolation:
                    m_TargetCamera.cullingMask = Tools.visibleLayers;
                    break;
                case CompositeViewType.SimulationView:
                case CompositeViewType.DeviceView:
                {
                    if (!UseFallbackRendering)
                    {
                        if (m_BackgroundSceneActive)
                        {
                            m_TargetCamera.cullingMask = m_CompositeLayerMask.value;
                            m_CompositeCamera.cullingMask = m_TargetCameraCullingMask & ~m_CompositeLayerMask.value;
                            m_CompositeCamera.clearFlags = CameraClearFlags.SolidColor;
                            m_CompositeCamera.backgroundColor = Color.clear;
                        }
                        else
                        {
                            m_TargetCamera.cullingMask &= ~m_CompositeLayerMask.value;
                            m_CompositeCamera.cullingMask = m_CompositeLayerMask.value;
                            m_CompositeCamera.clearFlags = m_BackgroundClearFlags;
                            m_CompositeCamera.backgroundColor = m_BackgroundColor;
                        }

                        m_TargetCamera.clearFlags = CameraClearFlags.SolidColor;
                        m_TargetCamera.backgroundColor = Color.clear;
                    }
                    else
                    {
                        m_TargetCamera.cullingMask = Tools.visibleLayers;
                    }

                    break;
                }
                case CompositeViewType.GameView:
                case CompositeViewType.OtherView:
                {
                    if (UseCompositeCamera)
                    {
                        m_CompositeCamera.depth = m_TargetCamera.depth - 1;

                        m_TargetCamera.cullingMask &= ~m_CompositeLayerMask.value;
                        m_TargetCamera.clearFlags = UseFallbackRendering && UseCameraStackInFallback ?
                            CameraClearFlags.Depth :
                            m_CompositeViewType == CompositeViewType.OtherView ? CameraClearFlags.SolidColor : CameraClearFlags.Nothing;
                        m_TargetCamera.backgroundColor = Color.clear;

                        m_CompositeCamera.cullingMask = m_CompositeLayerMask.value;
                        m_CompositeCamera.clearFlags = m_BackgroundClearFlags;
                        m_CompositeCamera.backgroundColor = m_BackgroundColor;
                    }
                    else
                    {
                        m_TargetCamera.cullingMask = m_TargetCameraCullingMask;

                        if (m_CompositeViewType == CompositeViewType.GameView)
                        {
                            m_TargetCamera.clearFlags = m_BackgroundClearFlags;
                            m_TargetCamera.backgroundColor = m_BackgroundColor;
                        }
                    }

                    if (UseCompositeCamera)
                    {
                        // Don't try to render the camera the frame it was created
                        if (UseFallbackRendering && !m_CompositeCamera.gameObject.activeSelf)
                            m_CompositeCamera.gameObject.SetActive(true);

                    }

                    break;
                }
                case CompositeViewType.Undefined:
                    break;
            }
        }

        void RenderCompositeAsBackground(SimulationRenderSettings overrideRenderSettingsSettings = null)
        {
            if (!UseCompositeCamera)
                return;

            // Don't try to render the camera the frame it was created
            if (!m_CompositeCamera.gameObject.activeSelf)
            {
                m_CompositeCamera.gameObject.SetActive(true);
                return;
            }

            var targetSceneValid = m_CompositeCamera.scene.IsValid();
            if (targetSceneValid && !UseFallbackRendering)
            {
                Unsupported.SetOverrideLightingSettings(m_CompositeCamera.scene);

                if (overrideRenderSettingsSettings != null)
                    overrideRenderSettingsSettings.ApplyTempRenderSettings();
            }

            RenderCompositeCamera();

            if (targetSceneValid && !UseFallbackRendering)
            {
                Unsupported.RestoreOverrideLightingSettings();
                QualitySettings.shadowDistance = m_ShadowDistance;
            }

            GL.Clear(true, false, Color.black);
        }

        void RenderCompositeAsForeground(SimulationRenderSettings overrideRenderSettingsSettings = null)
        {
            if (!UseCompositeCamera)
                return;

            // Don't try to render the camera the frame it was created
            if (!m_CompositeCamera.gameObject.activeSelf)
            {
                m_CompositeCamera.gameObject.SetActive(true);
                return;
            }

            var simSceneModule = ModuleLoaderCore.instance.GetModule<SimulationSceneModule>();
            if (simSceneModule == null)
                return;

            var targetSceneValid = simSceneModule.EnvironmentScene.IsValid();

            if (targetSceneValid && !UseFallbackRendering)
            {
                Unsupported.RestoreOverrideLightingSettings();
                QualitySettings.shadowDistance = m_ShadowDistance;
            }

            RenderCompositeCamera();

            GL.Clear(true, false, Color.black);

            if (targetSceneValid && !UseFallbackRendering)
            {
                Unsupported.SetOverrideLightingSettings(simSceneModule.EnvironmentScene);

                if (overrideRenderSettingsSettings != null)
                    overrideRenderSettingsSettings.ApplyTempRenderSettings();
            }
        }

        void RenderPrefabIsolation(SimulationRenderSettings overrideRenderSettingsSettings = null)
        {
            if (!UseCompositeCamera)
                return;

            if (m_CompositeCamera.gameObject.activeSelf)
            {
                m_CompositeCamera.gameObject.SetActive(false);
                return;
            }

            if (!UseFallbackRendering)
            {
                Unsupported.SetOverrideLightingSettings(m_PrefabStageScene);
                QualitySettings.shadowDistance = m_ShadowDistance;

                if (overrideRenderSettingsSettings != null)
                    overrideRenderSettingsSettings.ApplyTempRenderSettings();
            }
        }

        /// <summary>
        /// Sets the background color for the composite.
        /// </summary>
        /// <param name="color">Color to use as a background color.</param>
        public void SetBackgroundColor(Color color)
        {
            m_BackgroundColor = color;
        }

        /// <summary>
        /// Sets whether the background scene of the composite should be active.
        /// Used for scene picking in a scene view.
        /// </summary>
        /// <param name="backgroundSceneActive">Should the background be the active scene</param>
        public void SetBackgroundSceneActive(bool backgroundSceneActive)
        {
            m_BackgroundSceneActive = backgroundSceneActive;
            RefreshCompositeRenderSetup();
        }

        /// <summary>
        /// Sets the target camera for the context.
        /// </summary>
        /// <param name="targetCamera">Camera to use as the Target Camera for the context.</param>
        public void SetTargetCamera(Camera targetCamera)
        {
            m_TargetCamera = targetCamera;
            m_TargetCameraCullingMask = m_TargetCamera.cullingMask;
            SetupCompositeCamera();
            RefreshCompositeRenderSetup();
        }

        internal void UpdateCompositeRenderData(CompositeRenderData compositeRenderData)
        {
            SetTargetCamera(compositeRenderData.TargetCamera);
            SetRenderTextureDescriptor(compositeRenderData.CameraTargetDescriptor);
            SetBackgroundColor(compositeRenderData.BackgroundColor);
            CompositeLayerMask = compositeRenderData.CompositeLayerMask;
            ShowImageEffects = compositeRenderData.ShowImageEffects;
            SetBackgroundSceneActive(compositeRenderData.BackgroundSceneActive);
            DesaturateComposited = compositeRenderData.DesaturateComposited;
            UseXRay = compositeRenderData.UseXRay;
        }

        void SetupImageEffects()
        {
#if INCLUDE_POST_PROCESSING
            if (UseFallbackRendering || IsBaseSceneView)
                return;

            if (!m_ShowImageEffects && m_CompositePostProcess != null)
            {
                m_CompositePostProcess.enabled = false;
                return;
            }

            if (!m_ShowImageEffects || !compositeSimulationReady)
                return;

            var moduleLoader = ModuleLoaderCore.instance;
            var simSceneModule = moduleLoader.GetModule<SimulationSceneModule>();

            if (m_CompositeViewType != CompositeViewType.GameView && !UseFallbackRendering)
            {
                if (simSceneModule != null)
                {
                    m_TargetCamera.scene = m_BackgroundSceneActive ? simSceneModule.EnvironmentScene : simSceneModule.ContentScene;
                }
                else
                {
                    Debug.LogError("SimulationSceneModule not found.  Image effects failed to set up.");
                }
            }

            var compositeRenderModule = moduleLoader.GetModule<CompositeRenderModule>();
            if (compositeRenderModule != null)
            {
                if (m_CompositePostProcess == null)
                    m_CompositePostProcess = m_CompositeCamera.gameObject.AddComponent<PostProcessLayer>();

                m_CompositePostProcess.volumeLayer = m_CompositeLayerMask;
                m_CompositePostProcess.finalBlitToCameraTarget = false;
                m_CompositePostProcess.enabled = m_ShowImageEffects && !m_BackgroundSceneActive;
            }
#endif
        }

        void CleanCompositeView()
        {
            QualitySettings.shadowDistance = m_ShadowDistance;

            if (m_TargetCamera == null)
                return;

            switch (m_CompositeViewType)
            {
                case CompositeViewType.NormalSceneView:
                case CompositeViewType.PrefabIsolation:
                {
                    m_TargetCamera.cullingMask = Tools.visibleLayers;
                    break;
                }
                case CompositeViewType.SimulationView:
                case CompositeViewType.DeviceView:
                {
                    m_TargetCamera.cullingMask = !UseFallbackRendering ? m_TargetCameraCullingMask : Tools.visibleLayers;
                    m_TargetCamera.clearFlags = m_BackgroundClearFlags;
                    m_TargetCamera.backgroundColor = m_BackgroundColor;
                    break;
                }
                case CompositeViewType.GameView:
                case CompositeViewType.OtherView:
                {
                    m_TargetCamera.cullingMask = UseFallbackRendering ? m_TargetCamera.cullingMask : m_TargetCameraCullingMask;
                    break;
                }
                case CompositeViewType.Undefined:
                    break;
            }
        }

        void RenderCompositeCamera()
        {
            if (!UseCompositeCamera)
                return;

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                return;
#endif

            if (compositeSimulationReady && !m_BackgroundSceneActive)
                QualitySettings.shadowDistance *= MarsWorldScaleModule.GetWorldScale();

            m_CompositeCamera.Render();

            if (compositeSimulationReady && !m_BackgroundSceneActive)
                QualitySettings.shadowDistance = m_ShadowDistance;
        }
    }
}
