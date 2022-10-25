using System;
using UnityEngine;

namespace Unity.MARS.Simulation.Rendering
{
    [Serializable]
    enum CompositeViewType
    {
        Undefined = -1,
        SimulationView = 0,
        DeviceView,
        NormalSceneView,
        PrefabIsolation,
        GameView,
        OtherView,
    }

    struct CompositeRenderData
    {
        Camera m_TargetCamera;

        public CompositeViewType ViewType { get; }
        public Camera TargetCamera => m_TargetCamera;
        public RenderTextureDescriptor CameraTargetDescriptor { get; }
        public Color BackgroundColor { get; }
        public LayerMask CompositeLayerMask { get; }
        public bool ShowImageEffects { get; }
        public bool BackgroundSceneActive { get; }
        public bool DesaturateComposited { get; }
        public bool UseXRay { get; }

        internal CompositeRenderData(CompositeViewType contextViewType, Camera targetCamera,
            RenderTextureDescriptor cameraTargetDescriptor, Color backgroundColor, LayerMask compositeLayerMask,
            bool showImageEffects, bool backgroundSceneActive, bool desaturateComposited, bool useXRay)
        {
            ViewType = contextViewType;
            m_TargetCamera = targetCamera;
            CameraTargetDescriptor = cameraTargetDescriptor;
            BackgroundColor = backgroundColor;
            CompositeLayerMask = compositeLayerMask;
            ShowImageEffects = showImageEffects;
            BackgroundSceneActive = backgroundSceneActive;
            DesaturateComposited = desaturateComposited;
            UseXRay = useXRay;
        }

        internal void SetTargetCamera(Camera targetCamera)
        {
            m_TargetCamera = targetCamera;
        }
    }
}
