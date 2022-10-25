using System;
using UnityEngine;

#if ARFOUNDATION_2_1_OR_NEWER
using UnityEngine.XR.ARFoundation;
#endif

#if ARFOUNDATION_3_0_1_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace Unity.MARS.XRSubsystem.Simulation
{
    /// <summary>
    /// Editor only version of <c>ARCameraBackground</c> that strips avoids calling <c>CommandBuffer.IssuePluginEvent</c>.
    /// Calls to <c>CommandBuffer.IssuePluginEvent</c> causes some versions of the Unity Editor to crash with multiple
    /// invocations.
    /// </summary>
    [AddComponentMenu("")]
    class SimulatedCameraBackground
#if ARFOUNDATION_3_0_1_OR_NEWER
        : ARCameraBackground
    {
        /// <summary>
        /// Name of the main texture parameter for the material
        /// </summary>
        static readonly int k_MainTex = Shader.PropertyToID("_MainTex");

        /// <summary>
        /// Configures the <paramref name="commandBuffer"/> by first clearing it,
        /// and then adding necessary render commands.
        /// </summary>
        /// <param name="commandBuffer">The command buffer to configure.</param>
        protected override void ConfigureLegacyCommandBuffer(CommandBuffer commandBuffer)
        {
            var texture = !material.HasProperty(k_MainTex) ? null : material.GetTexture(k_MainTex);

            commandBuffer.Clear();
            commandBuffer.SetInvertCulling(shouldInvertCulling);
            commandBuffer.ClearRenderTarget(true, false, Color.clear);
            commandBuffer.Blit(texture, BuiltinRenderTextureType.CameraTarget, material);
        }
    }
#elif ARFOUNDATION_2_1_OR_NEWER
        : MonoBehaviour
    {
        const string k_DisplayTransformName = "_UnityDisplayTransform";
        static readonly int k_DisplayTransformId = Shader.PropertyToID(k_DisplayTransformName);

        [SerializeField]
        bool m_UseCustomMaterial;

        [SerializeField]
        Material m_CustomMaterial;

        ARFoundationBackgroundRenderer m_BackgroundRenderer;
        bool m_CameraSetupThrewException;
        Camera m_Camera;
        ARCameraManager m_CameraManager;
        Material m_SubsystemMaterial;
        ARFoundationBackgroundRenderer m_LegacyBackgroundRenderer;
        ARRenderMode m_Mode;

        /// <summary>
        /// When <c>false</c>, a material is generated automatically from the shader included in the platform-specific package.
        /// When <c>true</c>, <see cref="customMaterial"/> is used instead, overriding the automatically generated one.
        /// This is not necessary for most AR experiences.
        /// </summary>
        internal bool useCustomMaterial
        {
            get { return m_UseCustomMaterial; }
            set
            {
                m_UseCustomMaterial = value;
                UpdateMaterial();
            }
        }

        /// <summary>
        /// If <see cref="useCustomMaterial"/> is <c>true</c>, this <c>Material</c> will be used
        /// instead of the one included with the platform-specific AR package.
        /// </summary>
        internal Material customMaterial
        {
            get { return m_CustomMaterial; }
            set
            {
                m_CustomMaterial = value;
                UpdateMaterial();
            }
        }

        /// <summary>
        /// The current <c>Material</c> used for background rendering.
        /// </summary>
        internal Material material
        {
            get => m_BackgroundRenderer.backgroundMaterial;
            private set => m_BackgroundRenderer.backgroundMaterial = value;
        }

        Material subsystemMaterial
        {
            get
            {
                if (m_SubsystemMaterial == null)
                    m_SubsystemMaterial = CreateMaterialFromSubsystemShader();

                return m_SubsystemMaterial;
            }
        }

        ARRenderMode mode
        {
            get { return m_Mode; }
            set
            {
                m_Mode = value;

                if (m_LegacyBackgroundRenderer != null)
                    m_LegacyBackgroundRenderer.mode = m_Mode;
            }
        }

        Material CreateMaterialFromSubsystemShader()
        {
            if (m_CameraSetupThrewException)
                return null;

            // Try to create a material from the plugin's provided shader.
            if (string.IsNullOrEmpty(m_CameraManager.shaderName))
                return null;

            var shader = Shader.Find(m_CameraManager.shaderName);
            if (shader == null)
            {
                // If an exception is thrown, then something is irrecoverably wrong.
                // Set this flag so we don't try to do this every frame.
                m_CameraSetupThrewException = true;

                throw new InvalidOperationException(string.Format(
                    "Could not find shader named \"{0}\" required for video overlay on camera subsystem.",
                    m_CameraManager.shaderName));
            }

            return new Material(shader);
        }

        void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            UpdateMaterial();

            var mat = material;
            var count = eventArgs.textures.Count;
            for (int i = 0; i < count; ++i)
            {
                mat.SetTexture(
                    eventArgs.propertyNameIds[i],
                    eventArgs.textures[i]);
            }

            mode = ARRenderMode.MaterialAsBackground;

            if (eventArgs.displayMatrix.HasValue)
                mat.SetMatrix(k_DisplayTransformId, eventArgs.displayMatrix.Value);

            if (eventArgs.projectionMatrix.HasValue)
                m_Camera.projectionMatrix = eventArgs.projectionMatrix.Value;
        }

        void SetupBackgroundRenderer()
        {
            if (m_LegacyBackgroundRenderer == null)
                m_LegacyBackgroundRenderer = new ARFoundationBackgroundRenderer();

            m_BackgroundRenderer = m_LegacyBackgroundRenderer;

            m_BackgroundRenderer.mode = mode;
            m_BackgroundRenderer.camera = m_Camera;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            m_CameraManager = GetComponent<ARCameraManager>();
            if (m_CameraManager == null)
            {
                m_CameraManager = gameObject.AddComponent<ARCameraManager>();
                m_CameraManager.hideFlags = HideFlags.DontSave;
            }

            SetupBackgroundRenderer();
        }

        void OnEnable()
        {
            UpdateMaterial();
            m_CameraManager.frameReceived += OnCameraFrameReceived;
            ARSession.stateChanged += OnSessionStateChanged;
        }

        void OnDisable()
        {
            mode = ARRenderMode.StandardBackground;
            m_CameraManager.frameReceived -= OnCameraFrameReceived;
            ARSession.stateChanged -= OnSessionStateChanged;
            m_CameraSetupThrewException = false;

            // We are no longer setting the projection matrix
            // so tell the camera to resume its normal projection
            // matrix calculations.
            m_Camera.ResetProjectionMatrix();
        }

        void OnSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
        {
            // If the session goes away then return to using standard background mode
            if (eventArgs.state < ARSessionState.SessionInitializing && m_BackgroundRenderer != null)
                mode = ARRenderMode.StandardBackground;
        }

        void UpdateMaterial()
        {
            material = m_UseCustomMaterial ? m_CustomMaterial : subsystemMaterial;
        }
    }
#else
        : MonoBehaviour{ }
#endif
}
