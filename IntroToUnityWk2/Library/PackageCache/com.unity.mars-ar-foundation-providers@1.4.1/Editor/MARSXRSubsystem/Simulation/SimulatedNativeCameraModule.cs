#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.MARS.MARSUtils;
using Unity.MARS.Providers;
using Unity.MARS.Settings;
using Unity.MARS.Simulation;
using Unity.MARS.XRSubsystem.Data;
using Unity.MARS.XRSubsystem.Simulation;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEditor;
using UnityEditor.MARS.Simulation.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.XR.ARSubsystems;
using UnityObject = UnityEngine.Object;

#if ARFOUNDATION_2_1_OR_NEWER
using UnityEngine.XR.ARFoundation;
#endif

#if MODULE_URP_ENABLED && ARSUBSYSTEMS_3_OR_NEWER
using Unity.MARS.Rendering;
using UnityEngine.Rendering.Universal;
#endif

#if !ARSUBSYSTEMS_4_OR_NEWER
using System.Reflection;
using SimulatedCameraBackground = UnityEngine.XR.ARFoundation.ARCameraBackground;
#endif

namespace Unity.MARS.XRSubsystem
{
    class SimulatedNativeCameraModule : IModuleDependency<MARSSceneModule>, IModuleDependency<CompositeRenderModule>,
        IModuleBehaviorCallbacks, IUsesLightEstimation, IUsesCameraPose
    {
        readonly struct BackgroundCustomData
        {
            internal readonly bool UseCustomMaterial;
            internal readonly Material CustomMaterial;

            internal BackgroundCustomData(bool useCustomMaterial, Material customMaterial)
            {
                UseCustomMaterial = useCustomMaterial;
                CustomMaterial = customMaterial;
            }
        }

        internal event Action<MRStructuredImage> CameraImageUpdated;
#if ARFOUNDATION_2_1_OR_NEWER
        internal event Action<ARCameraFrameEventArgs> CameraFrameReceived;
#endif
        internal event Action<Pose> CameraPoseUpdated;
        internal event Action<MRCameraTrackingState> CameraTrackingStateChanged;
        internal event Action<MRLightEstimation> LightEstimationUpdated;

#pragma warning disable 649
        Func<CompositeRenderContext, bool> m_CustomSetupSimulatedBackground;
        Action<CompositeRenderContext> m_CustomTearDownSimulatedBackground;
        Func<Camera, bool> m_CustomCheckSimulatedBackground;
#pragma warning restore 649

        const int k_DefaultFramerate = 60;
        const float k_FrameStep = (1f / k_DefaultFramerate) - 0.0025f;

        static bool s_RenderTextureCopySupported;

        Texture m_CameraFeedTexture;
        Texture2D m_CameraImage;
        Camera m_SessionCamera;
        double m_FrameTimeStamp;
        Pose m_CamerPose;

#if MODULE_URP_ENABLED && ARSUBSYSTEMS_3_OR_NEWER
        RenderPipelineAsset m_CurrentRenderPipeline;
        ScriptableRenderer m_CurrentScriptableRenderer;
        UniversalAdditionalCameraData m_SessionAdditionalCameraData;
        UniversalRenderPipelineAsset m_ModifiedUrpAsset;
        int m_ModifiedDataIndex;
        ForwardRendererData m_ModifiedForwardRendererData;
        SimulatedBackgroundRenderFeature m_TempBackgroundRenderFeature;
        int m_ForwardDataOpaqueLayerMask;
        int m_ForwardDataTransparentLayerMask;
#endif

        MRStructuredImage m_CameraStructuredImage;
        readonly List<Texture2D> m_CameraImagePlanes = new List<Texture2D>();
        readonly List<int> m_ShaderIDs = new List<int> { 0 };

#if ARFOUNDATION_2_1_OR_NEWER
        ARCameraFrameEventArgs? m_CameraFrameEventArgs;
#endif
        XRCameraConfiguration? m_CameraConfiguration;
        XRCameraIntrinsics? m_CameraIntrinsics;
        Dictionary<GameObject, BackgroundCustomData> m_CameraBackgroundData = new Dictionary<GameObject, BackgroundCustomData>();

        internal XRCameraConfiguration? CurrentCameraConfiguration => m_CameraConfiguration;
#if ARFOUNDATION_2_1_OR_NEWER
        internal ARCameraFrameEventArgs? CameraFrameEventArgs => m_CameraFrameEventArgs;
#endif
        internal XRCameraIntrinsics? CameraIntrinsics => m_CameraIntrinsics;
        internal MRStructuredImage CameraStructuredImage => m_CameraStructuredImage;
        internal Pose CameraPose => m_CamerPose;

        IProvidesLightEstimation IFunctionalitySubscriber<IProvidesLightEstimation>.provider { get; set; }
        IProvidesCameraPose IFunctionalitySubscriber<IProvidesCameraPose>.provider { get; set; }

        void IModuleDependency<CompositeRenderModule>.ConnectDependency(CompositeRenderModule dependency) { }

        void IModuleDependency<MARSSceneModule>.ConnectDependency(MARSSceneModule dependency)
        {
            var subscriber = new []{this};
            dependency.AddRuntimeSceneObjects(subscriber);
        }

        void IModule.LoadModule() { }

        void IModule.UnloadModule() { }

        void IModuleBehaviorCallbacks.OnBehaviorAwake()
        {
            if (!Application.isPlaying)
                return;

            m_CameraBackgroundData.Clear();
#if ARFOUNDATION_2_1_OR_NEWER
            var arCameraBackgrounds = UnityObject.FindObjectsOfType<ARCameraBackground>();
            foreach (var cameraBackground in arCameraBackgrounds)
            {
#if ARSUBSYSTEMS_3_OR_NEWER
                if (cameraBackground is SimulatedCameraBackground)
                    continue;
#endif

                var gameObject = cameraBackground.gameObject;
                var data = new BackgroundCustomData(cameraBackground.useCustomMaterial, cameraBackground.customMaterial);
                UnityObject.Destroy(cameraBackground);
                m_CameraBackgroundData.Add(gameObject, data);
            }
#endif

            // Need to delay adding the new background components due to ARCameraBackground being disallow multiple components.
            // Even though the old component was destroyed the disallow multiple components check still prevents adding
            // our new component the the GameObject.
            EditorApplication.delayCall += ReplaceARCameraBackground;
        }

        void IModuleBehaviorCallbacks.OnBehaviorEnable()
        {
            CompositeRenderContext.UseCameraSubsystem = true;

            if (m_CameraImage == null)
            {
                var defaultSrc = Texture2D.grayTexture;
                m_CameraImage = new Texture2D(defaultSrc.width, defaultSrc.height, defaultSrc.graphicsFormat,
                    TextureCreationFlags.None)
                {
                    name = "Simulated Native Camera Texture",
                    hideFlags = HideFlags.HideAndDontSave
                };

                Graphics.CopyTexture(defaultSrc, m_CameraImage);
                m_CameraImage.Apply();
            }

            this.SubscribePoseUpdated(OnCameraPoseUpdated);
            this.SubscribeTrackingTypeChanged(OnCameraTrackingStateChanged);
            this.SubscribeLightEstimationUpdated(OnLightEstimationUpdated);

            OnCameraPoseUpdated(this.GetPose());

            m_CameraConfiguration = null;
            m_CameraIntrinsics = null;
            m_FrameTimeStamp = MarsTime.Time;
        }

        void IModuleBehaviorCallbacks.OnBehaviorStart() { }

        void IModuleBehaviorCallbacks.OnBehaviorUpdate()
        {
            if (Application.isPlaying)
                UpdateCameraImageTexture();
        }

        void IModuleBehaviorCallbacks.OnBehaviorDisable()
        {
            TearDownCameraBackgroundRendering();

            this.UnsubscribePoseUpdated(OnCameraPoseUpdated);
            this.UnsubscribeTrackingTypeChanged(OnCameraTrackingStateChanged);
            this.UnsubscribeLightEstimationUpdated(OnLightEstimationUpdated);

            m_CameraConfiguration = null;
            m_CameraIntrinsics = null;
            m_CameraFeedTexture = null;
            m_CameraStructuredImage = default;
            m_CameraBackgroundData.Clear();
            CompositeRenderContext.UseCameraSubsystem = false;

            if (m_CameraImage != null)
            {
                UnityObjectUtils.Destroy(m_CameraImage);
                m_CameraImage = null;
            }
        }

        void IModuleBehaviorCallbacks.OnBehaviorDestroy() { }

        static SimulatedNativeCameraModule()
        {
            if ((SystemInfo.copyTextureSupport & CopyTextureSupport.RTToTexture) == 0)
            {
                Debug.LogWarning("This system does not support copying Render Textures to CPU Texture formats." +
                    "\nYou will not be able to use some features of `XRCameraSubsystem` and `ARCameraBackground` will " +
                    "not display in editor.");
                s_RenderTextureCopySupported = false;
                return;
            }

            s_RenderTextureCopySupported = true;
        }

        void SetupFromCameraFeed(Camera camera, Texture cameraFeedTexture)
        {
            m_CameraStructuredImage = default;
            m_CameraFeedTexture = cameraFeedTexture;

            var focalLenghtValue = camera.focalLength;
            var focalLenght = new Vector2(focalLenghtValue, focalLenghtValue);
            var resolution = new Vector2Int(cameraFeedTexture.width, cameraFeedTexture.height);
            var principalPoint = new Vector2(cameraFeedTexture.width * 0.5f, cameraFeedTexture.height * 0.5f);

            m_CameraConfiguration = CreateXRCameraConfiguration(IntPtr.Zero, resolution, k_DefaultFramerate);
            m_CameraIntrinsics = new XRCameraIntrinsics(focalLenght, principalPoint, resolution);
        }

        void UpdateCameraImageTexture(bool force = false)
        {
            if (m_SessionCamera == null)
            {
                m_SessionCamera = MarsRuntimeUtils.GetSessionAssociatedCamera();
                TearDownCameraBackgroundRendering();
                m_CameraConfiguration = null;
                m_CameraIntrinsics = null;
                return;
            }

            if (!m_CameraConfiguration.HasValue)
            {
                SetupCameraBackgroundRendering();
                return;
            }

            if (m_CustomCheckSimulatedBackground != null && !m_CustomCheckSimulatedBackground(m_SessionCamera))
            {
                TearDownCameraBackgroundRendering();
                return;
            }

#if MODULE_URP_ENABLED && ARSUBSYSTEMS_3_OR_NEWER
            if (m_CustomCheckSimulatedBackground == null)
            {
                if (m_CurrentRenderPipeline == null && GraphicsSettings.currentRenderPipeline != null
                || GraphicsSettings.currentRenderPipeline != null && m_CurrentScriptableRenderer == null)
                {
                    SetupCameraBackgroundRendering();
                    return;
                }

                if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                {
                    if (m_CurrentRenderPipeline != GraphicsSettings.currentRenderPipeline)
                    {
                        TearDownCameraBackgroundRendering();
                        return;
                    }
                }
            }
#endif

            var simTime = MarsTime.Time;

            // Camera frames on device have a time stamp associated with a given frame
            // and can be updated out of sync with the engine.
            // For now are we updating the provided texture at 60fps.
            if (!force && m_FrameTimeStamp + k_FrameStep > simTime)
                return;

            m_FrameTimeStamp = simTime;

            Texture srcImag;
            if (s_RenderTextureCopySupported)
            {
                srcImag = m_CameraFeedTexture ? m_CameraFeedTexture : Texture2D.blackTexture;

                if (m_CameraImage.graphicsFormat != srcImag.graphicsFormat)
                {
                    UnityObjectUtils.Destroy(m_CameraImage);
                    m_CameraImage = new Texture2D(srcImag.width, srcImag.height, srcImag.graphicsFormat, 0,
                        TextureCreationFlags.None);
                }
                else if (m_CameraImage.width != srcImag.width || m_CameraImage.height != srcImag.height)
                {
#if UNITY_2021_2_OR_NEWER
                    m_CameraImage.Reinitialize(srcImag.width, srcImag.height);
#else
                    m_CameraImage.Resize(srcImag.width, srcImag.height);
#endif
                    m_CameraImage.Apply();
                }

                Graphics.CopyTexture(srcImag, m_CameraImage);
            }
            else
            {
                srcImag = m_CameraImage;
            }

            m_CameraImagePlanes.Clear();
            m_CameraImagePlanes.Add(m_CameraImage);
            m_CameraStructuredImage = new MRStructuredImage((int)srcImag.GetNativeTexturePtr(), m_CameraImagePlanes,
                new Vector2Int(m_CameraImage.width, m_CameraImage.height), m_FrameTimeStamp,
                MRStructuredImage.ImageFormat.CompatibilityARGB32);

            m_SessionCamera.ResetProjectionMatrix();

#if MODULE_URP_ENABLED && ARSUBSYSTEMS_3_OR_NEWER
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)
                CompositeRenderContext.CompositeCameraUpdate(m_SessionCamera);
#endif

#if ARFOUNDATION_2_1_OR_NEWER
            var frameEventArgs = new ARCameraFrameEventArgs
            {
                timestampNs = (long)m_FrameTimeStamp,
                projectionMatrix = m_SessionCamera.projectionMatrix,
                displayMatrix = null,
                textures = m_CameraImagePlanes,
                propertyNameIds = m_ShaderIDs,
#if ARSUBSYSTEMS_3_OR_NEWER
                exposureDuration = null,
                exposureOffset = null,
#endif // ARSUBSYSTEMS_3_OR_NEWER
            };

            if (this.TryGetLightEstimation(out var mrLightEstimation))
            {
                var lightEstimationData = new ARLightEstimationData
                {
                    averageBrightness = mrLightEstimation.m_AmbientBrightness,
                    averageColorTemperature = mrLightEstimation.m_AmbientColorTemperature,
                    colorCorrection = mrLightEstimation.m_ColorCorrection,
#if ARSUBSYSTEMS_4_OR_NEWER
                    mainLightIntensityLumens = mrLightEstimation.m_MainLightIntensityLumens,
                    averageMainLightBrightness = mrLightEstimation.m_AmbientBrightness,
                    mainLightColor = mrLightEstimation.m_MainLightColor,
                    mainLightDirection = mrLightEstimation.m_MainLightDirection,
                    ambientSphericalHarmonics = mrLightEstimation.m_SphericalHarmonics
#endif // ARSUBSYSTEMS_4_OR_NEWER
                };

                frameEventArgs.lightEstimation = lightEstimationData;
            }

            m_CameraFrameEventArgs = frameEventArgs;
            CameraFrameReceived?.Invoke(frameEventArgs);
#endif // ARFOUNDATION_2_1_OR_NEWER

            CameraImageUpdated?.Invoke(m_CameraStructuredImage);
        }

        void ReplaceARCameraBackground()
        {
#if ARFOUNDATION_2_1_OR_NEWER
            foreach (var cameraData in m_CameraBackgroundData)
            {
                var background =cameraData.Key.GetComponent<SimulatedCameraBackground>();
                if (background == null)
                    background = cameraData.Key.AddComponent<SimulatedCameraBackground>();

                background.useCustomMaterial = cameraData.Value.UseCustomMaterial;
                background.customMaterial = cameraData.Value.CustomMaterial;
            }
#endif

            m_CameraBackgroundData.Clear();
        }

        void SetupCameraBackgroundRendering()
        {
#if ARFOUNDATION_2_1_OR_NEWER
            // If the behavior or ARFoundationCameraProvider changes to not add the ARCameraBackground
            // this should should change to match
            if (m_SessionCamera != null && m_SessionCamera.GetComponent<SimulatedCameraBackground>() == null)
            {
                var arCameraManager = m_SessionCamera.GetComponent<ARCameraManager>();

                if (!arCameraManager)
                {
                    arCameraManager = m_SessionCamera.gameObject.AddComponent<ARCameraManager>();
                    arCameraManager.hideFlags = HideFlags.DontSave;
                }

                var simulatedCameraBackground = m_SessionCamera.gameObject.AddComponent<SimulatedCameraBackground>();
                simulatedCameraBackground.hideFlags = HideFlags.DontSave;
            }
#endif

            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            if (compositeRenderModule == null || !compositeRenderModule.TryGetCompositeRenderContext(m_SessionCamera,
                out var compositeContext))
                return;

            if (m_CustomSetupSimulatedBackground != null)
            {
                if (m_CustomSetupSimulatedBackground(compositeContext))
                {
                    SetupFromCameraFeed(m_SessionCamera, compositeContext.CompositeCameraTexture);
                    UpdateCameraImageTexture(true);
                }

                return;
            }

            if (SetupScriptableSimulatedBackground(compositeContext))
            {
                SetupFromCameraFeed(m_SessionCamera, compositeContext.CompositeCameraTexture);
                UpdateCameraImageTexture(true);
            }
        }

        void TearDownCameraBackgroundRendering()
        {
            CompositeRenderContext context = null;

            if (m_SessionCamera != null)
            {
                var simulatedCameraBackground = m_SessionCamera.GetComponent<SimulatedCameraBackground>();
                if (simulatedCameraBackground != null)
                    UnityObjectUtils.Destroy(simulatedCameraBackground);

                var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
                compositeRenderModule?.TryGetCompositeRenderContext(m_SessionCamera, out context);
            }

            if (m_CustomTearDownSimulatedBackground != null)
            {
                m_CustomTearDownSimulatedBackground.Invoke(context);
                return;
            }

            TearDownScriptableSimulatedBackground();
        }

        bool SetupScriptableSimulatedBackground(CompositeRenderContext compositeContext)
        {
#if MODULE_URP_ENABLED && ARSUBSYSTEMS_3_OR_NEWER
            m_CurrentRenderPipeline = GraphicsSettings.currentRenderPipeline;

            if (m_CurrentRenderPipeline == null)
                return true;

            if (m_CurrentRenderPipeline is UniversalRenderPipelineAsset urpAsset)
            {
                // Setup of ScriptableRenderFeature on ScriptableRenderer on session camera
                m_SessionAdditionalCameraData = m_SessionCamera.GetUniversalAdditionalCameraData();
                if (m_SessionAdditionalCameraData == null || m_SessionAdditionalCameraData.scriptableRenderer == null)
                    return false;

                m_CurrentScriptableRenderer = m_SessionAdditionalCameraData.scriptableRenderer;

                var compositeCamera = compositeContext.CompositeCamera;
                var compositeAdditionalData = compositeCamera.GetUniversalAdditionalCameraData();
                // Early out if the camera additional data is not ready
                if (compositeAdditionalData == null)
                    return false;

#if ARFOUNDATION_3_0_1_OR_NEWER
                // Not setup for background rendering in built application.
                // Should not make it appear as if rendering is setup for background in game view.
                if (!m_CurrentScriptableRenderer.HasRenderFeature(typeof(ARBackgroundRendererFeature)))
                    return false;
#endif

                // Setup of forward render for background camera
                int backgroundIndex;
                if (SimulatedNativeCameraSettings.instance.compositeForwardRendererData is ForwardRendererData rendererData)
                {
                    if (!urpAsset.HasScriptableRendererData(rendererData, out backgroundIndex))
                    {
                        urpAsset.AddScriptableRendererData(rendererData);
                        m_ModifiedUrpAsset = urpAsset;
                        backgroundIndex = urpAsset.GetRendererDataLength() - 1;
                        m_ModifiedDataIndex = backgroundIndex;
                    }

                    rendererData.opaqueLayerMask = compositeContext.CompositeLayerMask.value;
                    rendererData.transparentLayerMask = compositeContext.CompositeLayerMask.value;
                    rendererData.SetDirty();
                    urpAsset.GetRenderer(backgroundIndex);
                }
                else
                {
                    return false;
                }

                // Get the index of the active renderer
                var sessionCameraActiveRendererIndex = 0;
                var renderDataLength = urpAsset.GetRendererDataLength();
                for (var i = 0; i < renderDataLength; i++)
                {
                    if (m_CurrentScriptableRenderer == urpAsset.GetRenderer(i))
                    {
                        sessionCameraActiveRendererIndex = i;
                        break;
                    }
                }

                // Setup of forward render for session camera
                if (urpAsset.GetRendererData(sessionCameraActiveRendererIndex) is ForwardRendererData sessionForwardRendererData)
                {
                    m_ModifiedForwardRendererData = sessionForwardRendererData;
                    m_ForwardDataOpaqueLayerMask = sessionForwardRendererData.opaqueLayerMask;
                    m_ForwardDataTransparentLayerMask = sessionForwardRendererData.transparentLayerMask;
                    sessionForwardRendererData.opaqueLayerMask &= ~compositeContext.CompositeLayerMask.value;
                    sessionForwardRendererData.transparentLayerMask &= ~compositeContext.CompositeLayerMask.value;

                    if (!sessionForwardRendererData.HasRenderFeature(typeof(SimulatedBackgroundRenderFeature)))
                    {
                        var rendererFeatures = sessionForwardRendererData.rendererFeatures;
                        m_TempBackgroundRenderFeature = ScriptableObject.CreateInstance<SimulatedBackgroundRenderFeature>();
                        m_TempBackgroundRenderFeature.name = $"Temp {nameof(SimulatedBackgroundRenderFeature)}";
                        rendererFeatures?.Add(m_TempBackgroundRenderFeature);
                    }

                    sessionForwardRendererData.SetDirty();
                    urpAsset.GetRenderer(sessionCameraActiveRendererIndex);
                }
                else
                {
                    Debug.LogWarning("Only 'Forward Renderer Data' is supported in the current setup render background!" +
                        "\nPlease use 'CustomSetupSimulatedBackground', 'CustomTearDownSimulatedBackground', and " +
                        "'CustomCheckSimulatedBackground' to define your own render background configuration.");
                    TearDownScriptableSimulatedBackground();
                }

                // Set composite background camera to render with background scriptable renderer
                // and update current renderer with reloaded render object
                compositeAdditionalData.SetRenderer(backgroundIndex);
                m_CurrentScriptableRenderer = m_SessionAdditionalCameraData.scriptableRenderer;
            }
#endif
            return true;
        }

        void TearDownScriptableSimulatedBackground()
        {
#if MODULE_URP_ENABLED && ARSUBSYSTEMS_3_OR_NEWER
            m_CurrentRenderPipeline = null;
            m_CurrentScriptableRenderer = null;

            if (m_ModifiedForwardRendererData != null)
            {
                m_ModifiedForwardRendererData.opaqueLayerMask = m_ForwardDataOpaqueLayerMask;
                m_ModifiedForwardRendererData.transparentLayerMask = m_ForwardDataTransparentLayerMask;

                if (m_TempBackgroundRenderFeature != null)
                    m_ModifiedForwardRendererData.rendererFeatures.Remove(m_TempBackgroundRenderFeature);

                m_TempBackgroundRenderFeature = null;
                m_ModifiedForwardRendererData = null;
            }

            if (SimulatedNativeCameraSettings.instance.compositeForwardRendererData is ForwardRendererData rendererData)
            {
                rendererData.opaqueLayerMask = -1;
                rendererData.transparentLayerMask = -1;
            }

            // Change the base URP asset last so the active renders will be reloaded with the changes to the renderer data
            if (m_ModifiedUrpAsset != null)
            {
                m_ModifiedUrpAsset.RemoveScriptableRendererData(m_ModifiedDataIndex);
                // After the layers masks are set the ScriptableRenders need to be rebuilt
                // getting the ScriptableRenders we just removed will do that
                m_ModifiedUrpAsset.GetRenderer(m_ModifiedUrpAsset.GetRendererDataLength() - 1);

                m_ModifiedUrpAsset = null;
            }
#endif
        }

        void OnLightEstimationUpdated(MRLightEstimation lightEstimation)
        {
            LightEstimationUpdated?.Invoke(lightEstimation);
        }

        void OnCameraTrackingStateChanged(MRCameraTrackingState trackingState)
        {
            CameraTrackingStateChanged?.Invoke(trackingState);
        }

        void OnCameraPoseUpdated(Pose cameraPose)
        {
            m_CamerPose = cameraPose;
            CameraPoseUpdated?.Invoke(m_CamerPose);
        }

        static XRCameraConfiguration CreateXRCameraConfiguration(IntPtr ptr, Vector2Int resolution, int framerate)
        {
#if ARSUBSYSTEMS_4_OR_NEWER
            return new XRCameraConfiguration(ptr, resolution, framerate);
#else
            var configType = typeof(XRCameraConfiguration);

            var configObject = configType.Assembly.CreateInstance(configType.FullName ?? string.Empty, false,
                BindingFlags.Default | BindingFlags.Instance | BindingFlags.NonPublic,
                null, new object[] { resolution, k_DefaultFramerate }, null, null);

            if (configObject is XRCameraConfiguration configuration)
                return configuration;

            return default;
#endif
        }
    }
}
#endif // ARSUBSYSTEMS_2_1_OR_NEWER
