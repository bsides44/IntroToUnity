using System;
using UnityEngine;

namespace Unity.MARS.Simulation.Rendering
{
    [AddComponentMenu("")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    class CompositeRenderBackground : MonoBehaviour
    {
#if UNITY_EDITOR
        internal static Action<Camera> BackgroundAwake;
        internal static Action<Camera> BackgroundDestroyed;

        internal CompositeRenderData CompositeData => m_CompositeData;

        internal Func<bool> UseFallbackRendering;
        internal Func<bool> UseCameraCallbacks;

        internal event Action PreCullCamera;
        internal event Action PostRenderCamera;

        bool m_IsInitialized;
        CompositeRenderData m_CompositeData;

        void Awake()
        {
            if (!m_IsInitialized)
                return;

            m_CompositeData.SetTargetCamera(GetComponent<Camera>());
            BackgroundAwake?.Invoke(CompositeData.TargetCamera);
        }

        void OnDestroy()
        {
            BackgroundDestroyed?.Invoke(CompositeData.TargetCamera);
        }

        internal void InitializeCompositeBackground(CompositeRenderData compositeRenderData)
        {
            m_IsInitialized = true;
            m_CompositeData = compositeRenderData;
            m_CompositeData.SetTargetCamera(GetComponent<Camera>());
            BackgroundAwake?.Invoke(CompositeData.TargetCamera);
        }

        void OnPreCull()
        {
            if (!UseFallbackRendering() && UseCameraCallbacks())
                PreCullCamera?.Invoke();
        }

        void OnPostRender()
        {
            if (!UseFallbackRendering() && UseCameraCallbacks())
                PostRenderCamera?.Invoke();
        }

        void Update()
        {
            if (CompositeData.ViewType != CompositeViewType.GameView || !UseFallbackRendering())
                return;

            PreCullCamera?.Invoke();
            PostRenderCamera?.Invoke();
        }
#endif
    }
}
