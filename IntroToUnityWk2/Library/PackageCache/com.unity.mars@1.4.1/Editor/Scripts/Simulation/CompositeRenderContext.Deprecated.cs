using System;
using Unity.MARS.Simulation.Rendering;
using UnityEngine;

namespace UnityEditor.MARS.Simulation.Rendering
{
    public partial class CompositeRenderContext
    {
        /// <summary>
        /// Type of view the context is being used with
        /// </summary>
        [Obsolete]
        public ContextViewType ContextViewType => (ContextViewType)(int)m_CompositeViewType;

        /// <summary>
        /// Creates a new composite render context for a given view.
        /// </summary>
        /// <param name="contextViewType">What type of view is the target.</param>
        /// <param name="targetCamera">The camera that renders the view</param>
        /// <param name="cameraTargetDescriptor">the render texture descriptor for the target cameras render texture</param>
        /// <param name="backgroundColor">The background color for compositing the view.</param>
        /// <param name="compositeLayerMask">Layer mask for the composite objects.</param>
        /// <param name="showImageEffects">Show image effects in the view.</param>
        /// <param name="backgroundSceneActive">Is the background of the composite the target camera scene</param>
        /// <param name="desaturateComposited">Desaturate the composited cameras output.</param>
        /// <param name="useXRay">Is the x-ray shader in use in the view.</param>
        [Obsolete]
        public CompositeRenderContext(ContextViewType contextViewType, Camera targetCamera,
            RenderTextureDescriptor cameraTargetDescriptor, Color backgroundColor, LayerMask compositeLayerMask,
            bool showImageEffects = false, bool backgroundSceneActive = false, bool desaturateComposited = false,
            bool useXRay = true) : this((CompositeViewType)(int)contextViewType, targetCamera, cameraTargetDescriptor,
            backgroundColor, compositeLayerMask, showImageEffects, backgroundSceneActive, desaturateComposited, useXRay)
        { }
    }
}
