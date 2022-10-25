#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

#if ARFOUNDATION_2_1_OR_NEWER
using UnityEngine.XR.ARFoundation;
#endif

namespace Unity.MARS.XRSubsystem
{
    /// <summary>
    /// Camera subsystem implementation for MARS XR Subsystems.
    /// </summary>
    public sealed class CameraSubsystem : XRCameraSubsystem
    {
        /// <summary>
        /// The identifying name for the camera-providing implementation.
        /// </summary>
        /// <value>
        /// The identifying name for the camera-providing implementation.
        /// </value>
        const string k_SubsystemId = "MARS-Camera";

        /// <summary>
        /// The name for the shader for rendering the camera texture.
        /// </summary>
        /// <value>
        /// The name for the shader for rendering the camera texture.
        /// </value>
        const string k_BackgroundShaderName = "Unlit/Mars Background Simple";

        /// <summary>
        /// The shader property name for the simple RGB component of the camera video frame.
        /// </summary>
        /// <value>
        /// The shader property name for the  simple RGB component of the camera video frame.
        /// </value>
        const string k_TextureSinglePropertyName = "_textureSingle";

        /// <summary>
        /// The shader property name identifier for the simple RGB component of the camera video frame.
        /// </summary>
        /// <value>
        /// The shader property name identifier for the simple RGB component of the camera video frame.
        /// </value>
        internal static readonly int TextureSinglePropertyNameId = Shader.PropertyToID(k_TextureSinglePropertyName);

#if !((ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER) && UNITY_2020_2_OR_NEWER)
#if ARSUBSYSTEMS_3_OR_NEWER
        /// <inheritdoc />
        protected override Provider CreateProvider() { return new MarsMRProvider(); }
#else
        /// <inheritdoc />
        protected override IProvider CreateProvider() { return new MarsMRProvider(); }
#endif // ARSUBSYSTEMS_3_OR_NEWER
#endif // !(ARSUBSYSTEMS_4_OR_NEWER && UNITY_2020_2_OR_NEWER)

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
        class MarsMRProvider : Provider
#elif ARSUBSYSTEMS_3_OR_NEWER
        partial class MarsMRProvider : Provider
#else
        partial class MarsMRProvider : IProvider
#endif
        {
#if ARFOUNDATION_2_1_OR_NEWER
            ARCameraFrameEventArgs m_CameraFrameEventArgs;
#endif

            /// <summary>
            /// Get the material used by <c>XRCameraSubsystem</c> to render the camera texture.
            /// </summary>
            /// <returns>
            /// The material to render the camera texture.
            /// </returns>
#if ARSUBSYSTEMS_3_OR_NEWER || ARFOUNDATION_5_OR_NEWER
            public override Material cameraMaterial => m_CameraMaterial;
#else
            internal Material cameraMaterial => m_CameraMaterial;
#endif
            Material m_CameraMaterial;
            bool m_ConnectedToNativeCamera;

            public override bool permissionGranted => true;

#if !(ARSUBSYSTEMS_3_OR_NEWER || ARFOUNDATION_5_OR_NEWER)
            public override string shaderName => k_BackgroundShaderName;
#endif

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
            public override XRCpuImage.Api cpuImageApi => m_CpuImageApi;
            MarsXRCpuImageApi m_CpuImageApi;
#endif

            MarsCpuImageApi m_MarsCpuImageApi = new MarsCpuImageApi();

            public MarsMRProvider()
            {
                var backgroundShader = Shader.Find(k_BackgroundShaderName);

                if (backgroundShader == null)
                {
                    Debug.LogError("Cannot create camera background material compatible with the render pipeline");
                }
                else
                {
                    m_CameraMaterial = CreateCameraMaterial(k_BackgroundShaderName);
                }
            }

            /// <summary>
            /// Method to be implemented by provider to start the camera for the subsystem.
            /// </summary>
            public override void Start()
            {
                base.Start();

                m_MarsCpuImageApi = new MarsCpuImageApi();
#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
                m_CpuImageApi = new MarsXRCpuImageApi(m_MarsCpuImageApi);
#endif

                ConnectToNativeCameraModule();
            }

            public override void Stop()
            {
                DisconnectFromNativeCameraModule();

                if (m_MarsCpuImageApi != null)
                {
                    m_MarsCpuImageApi.Dispose();
                    m_MarsCpuImageApi = null;
                }

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
                m_CpuImageApi = null;
#endif
                base.Stop();
            }

            bool ConnectToNativeCameraModule()
            {
                if (m_MarsCpuImageApi == null)
                {
                    DisconnectFromNativeCameraModule();
                    return false;
                }

                if (m_ConnectedToNativeCamera)
                    return true;

                var nativeCameraModule = ModuleLoaderCore.instance.GetModule<SimulatedNativeCameraModule>();
                if (nativeCameraModule == null)
                    return false;

#if ARFOUNDATION_2_1_OR_NEWER
                if (nativeCameraModule?.CameraFrameEventArgs != null)
                    m_CameraFrameEventArgs = (ARCameraFrameEventArgs)nativeCameraModule.CameraFrameEventArgs;
#endif

#if ARFOUNDATION_2_1_OR_NEWER
                nativeCameraModule.CameraFrameReceived += CameraFrameReceived;
                CameraFrameReceived(m_CameraFrameEventArgs);
#endif
                nativeCameraModule.CameraImageUpdated += m_MarsCpuImageApi.CreateCameraImage;
                m_MarsCpuImageApi.CreateCameraImage(nativeCameraModule.CameraStructuredImage);

                nativeCameraModule.CameraPoseUpdated += PoseUpdated;
                PoseUpdated(nativeCameraModule.CameraPose);

                m_ConnectedToNativeCamera = true;

                return true;
            }

            void DisconnectFromNativeCameraModule()
            {
                if (!m_ConnectedToNativeCamera)
                    return;

                var nativeCameraModule = ModuleLoaderCore.instance.GetModule<SimulatedNativeCameraModule>();
                if (nativeCameraModule != null)
                {
                    if (m_MarsCpuImageApi != null)
                        nativeCameraModule.CameraImageUpdated -= m_MarsCpuImageApi.CreateCameraImage;

#if ARFOUNDATION_2_1_OR_NEWER
                    nativeCameraModule.CameraFrameReceived -= CameraFrameReceived;
#endif
                    nativeCameraModule.CameraPoseUpdated -= PoseUpdated;
                }

                m_ConnectedToNativeCamera = false;
            }

            /// <summary>
            /// Method to be implemented by provider to destroy the camera for the subsystem.
            /// </summary>
            public override void Destroy()
            {
                if (m_MarsCpuImageApi != null)
                {
                    m_MarsCpuImageApi.Dispose();
                    m_MarsCpuImageApi = null;
                }
            }

#if !(ARSUBSYSTEMS_3_OR_NEWER || ARFOUNDATION_5_OR_NEWER)
            /// <summary>
            /// Create the camera material from the given camera shader name.
            /// </summary>
            /// <param name="cameraShaderName">The name of the camera shader.</param>
            /// <returns>
            /// The created camera material shader.
            /// </returns>
            /// <exception cref="System.InvalidOperationException">Thrown if the shader cannot be found or if a
            /// material cannot be created for the shader.</exception>
            protected static Material CreateCameraMaterial(string cameraShaderName)
            {
                var shader = Shader.Find(cameraShaderName);
                if (shader == null)
                {
                    throw new InvalidOperationException($"Could not find shader named '{cameraShaderName}' required "
                        + $"for video overlay on camera subsystem.");
                }

                var material = new Material(shader);
                if (material == null)
                {
                    throw new InvalidOperationException($"Could not create a material for shader named "
                        + $"'{cameraShaderName}' required for video overlay on camera "
                        + $"subsystem.");
                }

                return material;
            }
#endif

            public override XRCameraConfiguration? currentConfiguration
            {
                get
                {
                    if (!ConnectToNativeCameraModule())
                        return null;

                    var nativeCameraModule = ModuleLoaderCore.instance.GetModule<SimulatedNativeCameraModule>();
                    return nativeCameraModule?.CurrentCameraConfiguration;
                }
            }

            public override NativeArray<XRCameraConfiguration> GetConfigurations(XRCameraConfiguration defaultCameraConfiguration, Allocator allocator)
            {
                if (!ConnectToNativeCameraModule())
                    return base.GetConfigurations(defaultCameraConfiguration, allocator);

                var configs = new NativeArray<XRCameraConfiguration>(1, allocator);
                var nativeCameraModule = ModuleLoaderCore.instance.GetModule<SimulatedNativeCameraModule>();
                var configuration = defaultCameraConfiguration;
                if (nativeCameraModule?.CurrentCameraConfiguration != null)
                    configuration = (XRCameraConfiguration)nativeCameraModule.CurrentCameraConfiguration;

                configs[0] = configuration;
                return configs;
            }

            public override bool TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
            {
                if (!ConnectToNativeCameraModule())
                {
                    cameraIntrinsics = default;
                    return false;
                }

                var nativeCameraModule = ModuleLoaderCore.instance.GetModule<SimulatedNativeCameraModule>();
                if (nativeCameraModule?.CameraIntrinsics != null)
                {
                    cameraIntrinsics = (XRCameraIntrinsics)nativeCameraModule.CameraIntrinsics;
                    return true;
                }

                cameraIntrinsics = default;
                return false;
            }

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
            public override bool TryAcquireLatestCpuImage(out XRCpuImage.Cinfo cameraImageCInfo)
            {
                if (m_MarsCpuImageApi != null)
                    return m_MarsCpuImageApi.TryAcquireLatestImage(MarsCpuImageApi.ImageType.Camera, out cameraImageCInfo);

                cameraImageCInfo = new XRCpuImage.Cinfo();
                return false;
            }
#else
            public override bool TryAcquireLatestImage(out CameraImageCinfo cameraImageCInfo)
            {
                if (m_MarsCpuImageApi != null)
                {
                    var imageAcquired = m_MarsCpuImageApi.TryAcquireLatestImage(MarsCpuImageApi.ImageType.Camera, out var marsCameraImageCInfo);
                    cameraImageCInfo = new CameraImageCinfo(marsCameraImageCInfo.nativeHandle, marsCameraImageCInfo.dimensions, marsCameraImageCInfo.planeCount, marsCameraImageCInfo.timestamp, marsCameraImageCInfo.format);
                    return imageAcquired;
                }

                cameraImageCInfo = new CameraImageCinfo();
                return false;
            }
#endif

            public override NativeArray<XRTextureDescriptor> GetTextureDescriptors(XRTextureDescriptor defaultDescriptor,
                Allocator allocator)
            {
                if (m_MarsCpuImageApi != null)
                {
                    m_MarsCpuImageApi.TryGetTextureDescriptors(MarsCpuImageApi.ImageType.Camera, out var descriptors, allocator);
                    return descriptors;
                }

                return base.GetTextureDescriptors(defaultDescriptor, allocator);
            }

            static void PoseUpdated(Pose pose)
            {
                SetCameraPose(pose.position.x, pose.position.y, pose.position.z,
                    pose.rotation.x, pose.rotation.y, pose.rotation.z, pose.rotation.w);
            }

#if ARFOUNDATION_2_1_OR_NEWER
            void CameraFrameReceived(ARCameraFrameEventArgs args)
            {
                m_CameraFrameEventArgs = args;
            }
#endif

            public override bool TryGetFrame(XRCameraParams cameraParams, out XRCameraFrame cameraFrame)
            {
                if (!ConnectToNativeCameraModule())
                {
                    cameraFrame = new XRCameraFrame();
                    return false;
                }

                var frame = new MarsCameraFrame();
                XRCameraFrameProperties properties = 0;
#if ARFOUNDATION_2_1_OR_NEWER
                var lightEstimation = m_CameraFrameEventArgs.lightEstimation;
                if (lightEstimation.averageBrightness.HasValue)
                {
                    frame.AverageBrightness = lightEstimation.averageBrightness.Value;
                    properties |= XRCameraFrameProperties.AverageBrightness;
                }

                if (lightEstimation.averageColorTemperature.HasValue)
                {
                    frame.AverageColorTemperature = lightEstimation.averageColorTemperature.Value;
                    properties |= XRCameraFrameProperties.AverageColorTemperature;
                }

                if (lightEstimation.colorCorrection.HasValue)
                {
                    frame.ColorCorrection = lightEstimation.colorCorrection.Value;
                    properties |= XRCameraFrameProperties.ColorCorrection;
                }

#if ARSUBSYSTEMS_3_OR_NEWER || ARFOUNDATION_5_OR_NEWER
                if (lightEstimation.averageIntensityInLumens.HasValue)
                {
                    frame.AverageIntensityInLumens = lightEstimation.averageIntensityInLumens.Value;
                    properties |= XRCameraFrameProperties.AverageIntensityInLumens;
                }
#endif // ARSUBSYSTEMS_3_OR_NEWER

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
                if (lightEstimation.mainLightColor.HasValue)
                {
                    frame.MainLightColor = lightEstimation.mainLightColor.Value;
                    properties |= XRCameraFrameProperties.MainLightColor;
                }

                if (lightEstimation.mainLightDirection.HasValue)
                {
                    frame.MainLightDirection = lightEstimation.mainLightDirection.Value;
                    properties |= XRCameraFrameProperties.MainLightDirection;
                }

                if (lightEstimation.mainLightIntensityLumens.HasValue)
                {
                    frame.MainLightIntensityLumens = lightEstimation.mainLightIntensityLumens.Value;
                    properties |= XRCameraFrameProperties.MainLightIntensityLumens;
                }

                if (lightEstimation.ambientSphericalHarmonics.HasValue)
                {
                    frame.AmbientSphericalHarmonics = lightEstimation.ambientSphericalHarmonics.Value;
                    properties |= XRCameraFrameProperties.AmbientSphericalHarmonics;
                }
#endif // ARSUBSYSTEMS_4_OR_NEWER

                if (m_CameraFrameEventArgs.timestampNs.HasValue)
                {
                    frame.TimestampNs = (long)m_CameraFrameEventArgs.timestampNs;
                    properties |= XRCameraFrameProperties.Timestamp;
                }

                if (m_CameraFrameEventArgs.projectionMatrix.HasValue)
                {
                    frame.ProjectionMatrix = (Matrix4x4)m_CameraFrameEventArgs.projectionMatrix;
                    properties |= XRCameraFrameProperties.ProjectionMatrix;
                }

                if (m_CameraFrameEventArgs.displayMatrix.HasValue)
                {
                    frame.DisplayMatrix = (Matrix4x4)m_CameraFrameEventArgs.displayMatrix;
                    properties |= XRCameraFrameProperties.DisplayMatrix;
                }
#endif // ARFOUNDATION_2_1_OR_NEWER
                if (m_MarsCpuImageApi != null && m_MarsCpuImageApi.TryGetLatestImagePtr(MarsCpuImageApi.ImageType.Camera, out var nativePtr))
                    frame.NativePtr = nativePtr;

                frame.Properties = properties;

                var union = new XRCameraFrameUnion { marsCameraFrame = frame };
                cameraFrame = union.m_TheirXRCameraFrame;
                return true;
            }

            // ReSharper disable InconsistentNaming
            [DllImport("MARSXRSubsystem", EntryPoint = "MARSXRSubsystem_SetCameraPose")]
            public static extern void SetCameraPose(float pos_x, float pos_y, float pos_z,
                float rot_x, float rot_y, float rot_z, float rot_w);
            // ReSharper restore InconsistentNaming
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Register()
        {
            var cameraSubsystemCInfo = new XRCameraSubsystemCinfo
            {
                id = k_SubsystemId,
#if (ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER) && UNITY_2020_2_OR_NEWER
                providerType = typeof(CameraSubsystem.MarsMRProvider),
                subsystemTypeOverride = typeof(CameraSubsystem),
#else
                implementationType = typeof(CameraSubsystem),
#endif
                supportsCameraConfigurations = true,
                supportsCameraImage = true,
            };

            if (!XRCameraSubsystem.Register(cameraSubsystemCInfo))
            {
                Debug.LogErrorFormat("Cannot register the {0} subsystem", k_SubsystemId);
            }
        }
    }
}
#endif
