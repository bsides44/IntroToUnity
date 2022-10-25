using UnityEditor.XRTools.Utils;
using UnityEngine;

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
using srpAsset = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
using rendererData = UnityEngine.Rendering.Universal.ScriptableRendererData;
#else
using srpAsset = UnityEngine.Rendering.RenderPipelineAsset;
using rendererData = UnityEngine.ScriptableObject;
#endif

namespace Unity.MARS.Tests
{
    class UrpUtilitiesTestSettings : EditorScriptableSettings<UrpUtilitiesTestSettings>
    {
        [SerializeField]
        srpAsset m_PipelineDoesNotHaveFeature;

        [SerializeField]
        srpAsset m_PipelineHasFeature;

        [SerializeField]
        srpAsset m_PipelineModification;

        [SerializeField]
        rendererData m_FindScriptableRendererAsset;

        internal srpAsset PipelineDoesNotHaveFeature => m_PipelineDoesNotHaveFeature;
        internal srpAsset PipelineHasFeature => m_PipelineHasFeature;
        internal srpAsset PipelineModification => m_PipelineModification;
        internal rendererData FindScriptableRendererAsset => m_FindScriptableRendererAsset;
    }
}
