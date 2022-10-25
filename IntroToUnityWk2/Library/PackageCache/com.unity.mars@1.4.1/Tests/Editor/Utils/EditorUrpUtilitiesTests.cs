using UnityEngine;
using UnityEngine.Rendering;
using NUnit.Framework;

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
using System.Linq;
using System.Collections;
using Unity.MARS.Rendering;
using Unity.XRTools.Utils;
using UnityEngine.TestTools;
using UnityEngine.Rendering.Universal;
#endif

namespace Unity.MARS.Tests
{
    class EditorUrpUtilitiesTests
    {
        readonly struct CachedSrpSettings
        {
            internal readonly RenderPipelineAsset DefaultPipelineAsset;
            internal readonly RenderPipelineAsset[] QualitySettingsPipelineAssets;
            internal readonly int QualitySettingsIndex;

            internal CachedSrpSettings(RenderPipelineAsset defaultPipelineAsset,
                RenderPipelineAsset[] qualitySettingsPipelineAssets, int qualitySettingsIndex)
            {
                DefaultPipelineAsset = defaultPipelineAsset;
                QualitySettingsPipelineAssets = qualitySettingsPipelineAssets;
                QualitySettingsIndex = qualitySettingsIndex;
            }
        }

        CachedSrpSettings m_CachedSrpSettings;

        [SetUp]
        public void Setup()
        {
            var settingsCount = QualitySettings.names.Length;
            var qualitySettingsAssets = new RenderPipelineAsset[settingsCount];
            for (var i = 0; i < settingsCount; i++)
            {
                qualitySettingsAssets[i] = QualitySettings.GetRenderPipelineAssetAt(i);
            }

            m_CachedSrpSettings = new CachedSrpSettings(GraphicsSettings.defaultRenderPipeline, qualitySettingsAssets,
                QualitySettings.GetQualityLevel());
        }

        [TearDown]
        public void TearDown()
        {
            var settingsCount = QualitySettings.names.Length;
            for (var i = 0; i < settingsCount; i++)
            {
                QualitySettings.SetQualityLevel(i);
                QualitySettings.renderPipeline = m_CachedSrpSettings.QualitySettingsPipelineAssets[i];
            }

            QualitySettings.SetQualityLevel(m_CachedSrpSettings.QualitySettingsIndex);
            GraphicsSettings.defaultRenderPipeline = m_CachedSrpSettings.DefaultPipelineAsset;
        }

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
        [Test]
        public void UniversalRenderPipelineFindRendererDataTest()
        {
            var pipelineModificationAsset = UrpUtilitiesTestSettings.instance.PipelineModification;
            GraphicsSettings.defaultRenderPipeline = pipelineModificationAsset;
            var findIndex = -1;
            var rendererDataLenght = pipelineModificationAsset.GetRendererDataLength();
            var findScriptableRendererAsset = UrpUtilitiesTestSettings.instance.FindScriptableRendererAsset;
            for (var i = 0; i < rendererDataLenght; i++)
            {
                var rendererData = pipelineModificationAsset.GetRendererData(i);
                if (rendererData == findScriptableRendererAsset)
                {
                    findIndex = i;
                    break;
                }
            }

            Assert.True(findIndex > -1);
        }

        static UniversalAdditionalCameraData SetupUrpCamera(GameObject testCameraGameObject)
        {
            var testCamera = testCameraGameObject.AddComponent<Camera>();
            Assert.True(testCamera != null);
            var testCameraAdditionalData = testCamera.GetUniversalAdditionalCameraData();
            Assert.True(testCameraAdditionalData != null);

            return testCameraAdditionalData;
        }

        [UnityTest]
        public IEnumerator GetRenderFeatureTest()
        {
            var testCameraGameObject = new GameObject("URP test camera"){hideFlags = HideFlags.HideAndDontSave};
            var testCameraAdditionalData = SetupUrpCamera(testCameraGameObject);

            // ScriptableRenderer does not have render feature
            GraphicsSettings.defaultRenderPipeline = UrpUtilitiesTestSettings.instance.PipelineDoesNotHaveFeature;
            yield return null;
            var cameraScriptableRenderer = testCameraAdditionalData.scriptableRenderer;
            Assert.True(cameraScriptableRenderer != null);
            var renderFeatures = testCameraAdditionalData.scriptableRenderer.GetRenderFeatures();
            Assert.True(renderFeatures != null);
            Assert.True(renderFeatures.Count == 0);

            // ScriptableRenderer has render feature
            GraphicsSettings.defaultRenderPipeline = UrpUtilitiesTestSettings.instance.PipelineHasFeature;
            yield return null;
            cameraScriptableRenderer = testCameraAdditionalData.scriptableRenderer;
            Assert.True(cameraScriptableRenderer != null);
            renderFeatures = testCameraAdditionalData.scriptableRenderer.GetRenderFeatures();
            Assert.True(renderFeatures != null);
            Assert.True(renderFeatures.Count > 0);
            Assert.True(renderFeatures.OfType<TestRenderFeatureA>().Any());

            UnityObjectUtils.Destroy(testCameraGameObject);
            yield return null;
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void FindCameraRendererTest(int findIndex)
        {
            var testCameraGameObject = new GameObject("URP test camera"){hideFlags = HideFlags.HideAndDontSave};
            var testCameraAdditionalData = SetupUrpCamera(testCameraGameObject);

            var pipelineModificationAsset = UrpUtilitiesTestSettings.instance.PipelineModification;

            // Find and cache the default renderer index
            var defaultRenderer = pipelineModificationAsset.scriptableRenderer;
            var cachedDefaultRendererIndex = -1;
            var rendererLenght = pipelineModificationAsset.GetRendererDataLength();

            if (rendererLenght >= findIndex)
                return;

            for (var i = 0; i < rendererLenght; i++)
            {
                var renderer = pipelineModificationAsset.GetRenderer(i);
                if (renderer == defaultRenderer)
                {
                    cachedDefaultRendererIndex = i;
                    break;
                }
            }

            pipelineModificationAsset.SetDefaultRendererIndex(findIndex);
            GraphicsSettings.defaultRenderPipeline = pipelineModificationAsset;

            var cameraRendererIndex = -1;
            var rendererDataLenght = pipelineModificationAsset.GetRendererLength();
            var findRenderer = testCameraAdditionalData.scriptableRenderer;
            for (var i = 0; i < rendererDataLenght; i++)
            {
                var renderer = pipelineModificationAsset.GetRenderer(i);
                if (renderer == findRenderer)
                {
                    cameraRendererIndex = i;
                    break;
                }
            }

            Assert.True(cameraRendererIndex > -1);
            Assert.True(cameraRendererIndex == findIndex);

            pipelineModificationAsset.SetDefaultRendererIndex(cachedDefaultRendererIndex);
            UnityObjectUtils.Destroy(testCameraGameObject);
        }
#endif // INCLUDE_RENDER_PIPELINES_UNIVERSAL
    }
}
