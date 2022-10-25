using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Simulation.Rendering
{
    [Obsolete("CompositeCameraRenderer has been deprecated. Use CompositeRenderBackground instead.", false)]
    [ExecuteAlways]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [MovedFrom("Unity.MARS")]
    public class CompositeCameraRenderer : MonoBehaviour
    {
        [Obsolete]
        public event Action PreCullCamera;

        [Obsolete]
        public event Action PostRenderCamera;

        void OnPreCull()
        {
            PreCullCamera?.Invoke();
        }

        void OnPostRender()
        {
            PostRenderCamera?.Invoke();
        }
    }
}
