using UnityEngine;

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.MARS.Tests
{
    class TestRenderFeatureA
#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
        : ScriptableRendererFeature
    {
        public override void Create() { }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
    }
#else
        : ScriptableObject {}
#endif
}
