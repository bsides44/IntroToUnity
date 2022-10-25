using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Simulation.Rendering
{
    /// <summary>
    /// Interface that contains values and methods needed to set up a composite view on a given object
    /// </summary>
    [MovedFrom("Unity.MARS")]
    partial interface ICompositeView
    {

        /// <summary>
        /// Camera associated with the view. This is the one providing the render for a view.
        /// </summary>
        Camera TargetCamera { get; }

        /// <summary>
        /// Render texture descriptor for the <c>TargetCamera</c>
        /// </summary>
        RenderTextureDescriptor CameraTargetDescriptor { get; }

        /// <summary>
        /// Color to use as a background fill when drawing the composite
        /// </summary>
        Color BackgroundColor { get; }

        /// <summary>
        /// Are image effects active and should be rendered
        /// </summary>
        bool ShowImageEffects { get; }

        /// <summary>
        /// Is the composited scene the active scene
        /// </summary>
        bool BackgroundSceneActive { get; }

        /// <summary>
        /// Desaturate the layer being composited to the view
        /// </summary>
        bool DesaturateComposited { get; }

        /// <summary>
        /// Enable the x-ray rendering for the view
        /// </summary>
        bool UseXRay { get; }
    }
}
