using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Unity.MARS.Rendering
{
    static class MarsSrpUtilities
    {
        static void GetRenderPipelineAssetsInGraphicsSettings(Type type, ISet<RenderPipelineAsset> renderPipelineAssets)
        {
            if (!type.IsSubclassOf(typeof(RenderPipelineAsset)))
                return;

            // Is the pipeline assigned from GraphicsSettings asset all other pipelines are overrides from another source?
            TryAddPipelineAsset(type, renderPipelineAssets, GraphicsSettings.defaultRenderPipeline);
        }

        static void GetRenderPipelineAssetsInQualitySettings(Type type, ISet<RenderPipelineAsset> renderPipelineAssets)
        {
            if (!type.IsSubclassOf(typeof(RenderPipelineAsset)))
                return;

            var settingsCount = QualitySettings.names.Length;
            for (var i = 0; i < settingsCount; i++)
            {
                TryAddPipelineAsset(type, renderPipelineAssets, QualitySettings.GetRenderPipelineAssetAt(i));
            }
        }

        internal static void GetRenderPipelineAssetsInProjectSettings(Type type, ISet<RenderPipelineAsset> renderPipelineAssets)
        {
            GetRenderPipelineAssetsInGraphicsSettings(type, renderPipelineAssets);
            GetRenderPipelineAssetsInQualitySettings(type, renderPipelineAssets);
        }

        static void TryAddPipelineAsset(Type type, ISet<RenderPipelineAsset> renderPipelineAssets, RenderPipelineAsset pipelineAsset)
        {
            if (pipelineAsset != null && type.IsSubclassOf(typeof(RenderPipelineAsset)))
                renderPipelineAssets.Add(pipelineAsset);
        }
    }
}
