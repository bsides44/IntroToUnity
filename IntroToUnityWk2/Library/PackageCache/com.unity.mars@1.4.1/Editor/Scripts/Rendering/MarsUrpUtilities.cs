#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Unity.MARS.Rendering
{
    static class MarsUrpUtilities
    {
        const string k_RenderFeaturesListName = "rendererFeatures";
        const string k_RenderersArrayName = "m_Renderers";
        const string k_DefaultRendererIndexName = "m_DefaultRendererIndex";
        const string k_RendererDataListName = "m_RendererDataList";
        const string k_RendererFeaturesName = "m_RendererFeatures";
        const string k_RendererFeatureMapName = "m_RendererFeatureMap";
        const BindingFlags k_BindingFlags = BindingFlags.Default | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.Instance;

        static PropertyInfo s_RenderFeaturesListInfo;
        static FieldInfo s_RenderersArrayInfo;

        static MarsUrpUtilities()
        {
            var scriptableRendererType = typeof(ScriptableRenderer);
            s_RenderFeaturesListInfo = scriptableRendererType.GetProperty(k_RenderFeaturesListName, k_BindingFlags);

            if (s_RenderFeaturesListInfo == null)
                throw new Exception($"Universal Render Pipeline `ScriptableRenderer.{k_RendererFeaturesName}` not found!");

            var urpAssetType = typeof(UniversalRenderPipelineAsset);
            s_RenderersArrayInfo = urpAssetType.GetField(k_RenderersArrayName, k_BindingFlags);

            if (s_RenderersArrayInfo == null)
                throw new Exception($"Universal Render Pipeline 'UniversalRenderPipelineAsset.{k_RenderersArrayName}' not found!");
        }

        internal static List<ScriptableRendererFeature> GetRenderFeatures(this ScriptableRenderer scriptableRenderer)
        {
            if (scriptableRenderer != null && s_RenderFeaturesListInfo.GetValue(scriptableRenderer)
                is List<ScriptableRendererFeature> rendererFeatures)
            {
                return rendererFeatures;
            }

            return null;
        }

        internal static int GetRendererLength(this UniversalRenderPipelineAsset pipelineAsset)
        {
            if (pipelineAsset != null && s_RenderersArrayInfo.GetValue(pipelineAsset)
                is ScriptableRenderer[] scriptableRenderers)
            {
                return scriptableRenderers.Length;
            }

            return 0;
        }

        internal static ScriptableRenderer GetDefaultRenderer(this UniversalRenderPipelineAsset pipelineAsset)
        {
            // -1 always returns the default renderer
            return pipelineAsset.GetRenderer(-1);
        }

        internal static bool HasRenderFeature(this RenderPipelineAsset pipelineAsset, Type featureType)
        {
            if (pipelineAsset == null || !(pipelineAsset is UniversalRenderPipelineAsset urpAsset)
                || !featureType.IsSubclassOf(typeof(ScriptableRendererFeature)))
                return false;

            var renderersLength = urpAsset.GetRendererLength();
            for (var i = 0; i < renderersLength; i++)
            {
                if (urpAsset.GetRenderer(i).HasRenderFeature(featureType))
                    return true;
            }

            return false;
        }

        internal static bool HasRenderFeature(this ScriptableRenderer renderer, Type featureType)
        {
            if (renderer == null || !featureType.IsSubclassOf(typeof(ScriptableRendererFeature)))
                return false;

            foreach (var rendererFeature in renderer.GetRenderFeatures())
            {
                if (rendererFeature.GetType() == featureType || rendererFeature.GetType().IsSubclassOf(featureType))
                    return true;
            }

            return false;
        }

        internal static int GetRenderFeatureRendererIndex(this RenderPipelineAsset pipelineAsset, Type featureType)
        {
            if (pipelineAsset == null || !(pipelineAsset is UniversalRenderPipelineAsset urpAsset)
                                      || !featureType.IsSubclassOf(typeof(ScriptableRendererFeature)))
            {
                return -1;
            }

            var renderersLength = urpAsset.GetRendererLength();
            for (var i = 0; i < renderersLength; i++)
            {
                if (urpAsset.GetRenderer(i).HasRenderFeature(featureType))
                    return i;
            }

            return -1;
        }

        internal static void AddRenderFeatureToDefaultPipelineData<T>(this UniversalRenderPipelineAsset
            universalRenderPipelineAsset) where T : ScriptableRendererFeature
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var defaultRendererIndexProp = urpAssetSerializedObject.FindProperty(k_DefaultRendererIndexName);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);
            var defaultRendererData = rendererDataProp.GetArrayElementAtIndex(defaultRendererIndexProp.intValue)
                .objectReferenceValue as ScriptableRendererData;

            var renderDataSerializedObject = new SerializedObject(defaultRendererData);

            var rendererFeatures = renderDataSerializedObject.FindProperty(k_RendererFeaturesName);
            var rendererFeaturesMap = renderDataSerializedObject.FindProperty(k_RendererFeatureMapName);

            renderDataSerializedObject.Update();

            ScriptableObject component = ScriptableObject.CreateInstance<T>();
            component.name = $"New{typeof(T).Name}";
            Undo.RegisterCreatedObjectUndo(component, "Add Renderer Feature");

            // Store this new effect as a sub-asset so we can reference it safely afterwards
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(defaultRendererData))
            {
                AssetDatabase.AddObjectToAsset(component, defaultRendererData);
            }

            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(component, out _, out long localId);

            // Grow the list first, then add - that's how serialized lists work in Unity
            rendererFeatures.arraySize++;
            var componentProp = rendererFeatures.GetArrayElementAtIndex(rendererFeatures.arraySize - 1);
            componentProp.objectReferenceValue = component;

            // Update GUID Map
            rendererFeaturesMap.arraySize++;
            var guidProp = rendererFeaturesMap.GetArrayElementAtIndex(rendererFeaturesMap.arraySize - 1);
            guidProp.longValue = localId;
            renderDataSerializedObject.ApplyModifiedProperties();

            // Force save / refresh
            if (EditorUtility.IsPersistent(defaultRendererData))
                EditorUtility.SetDirty(defaultRendererData);

            renderDataSerializedObject.ApplyModifiedProperties();
        }

        internal static bool HasScriptableRendererData(this UniversalRenderPipelineAsset universalRenderPipelineAsset,
            ScriptableRendererData scriptableRendererData, out int index)
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);
            for (var i = 0; i < rendererDataProp.arraySize; i++)
            {
                var arrayElement = rendererDataProp.GetArrayElementAtIndex(i);
                if (arrayElement.objectReferenceValue == scriptableRendererData)
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        internal static void AddScriptableRendererData(this UniversalRenderPipelineAsset universalRenderPipelineAsset,
            ScriptableRendererData scriptableRendererData)
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);
            rendererDataProp.arraySize++;
            var newArrayElement = rendererDataProp.GetArrayElementAtIndex(rendererDataProp.arraySize - 1);
            newArrayElement.objectReferenceValue = scriptableRendererData;

            // Used for temporary change at runtime
            urpAssetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            urpAssetSerializedObject.UpdateIfRequiredOrScript();
        }

        internal static void RemoveScriptableRendererData(this UniversalRenderPipelineAsset universalRenderPipelineAsset,
            int index)
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);

            if (index >= rendererDataProp.arraySize)
            {
                throw new ArgumentOutOfRangeException();
            }
            else
            {
                var removeProp = rendererDataProp.GetArrayElementAtIndex(index);
                removeProp.objectReferenceValue = null;

                // Resize if last index
                if (index == rendererDataProp.arraySize - 1)
                    rendererDataProp.arraySize--;
            }

            // Used for temporary change at runtime
            urpAssetSerializedObject.ApplyModifiedPropertiesWithoutUndo();
            urpAssetSerializedObject.UpdateIfRequiredOrScript();
        }

        internal static bool HasRenderFeature(this ScriptableRendererData renderData, Type featureType)
        {
            if (renderData == null || !featureType.IsSubclassOf(typeof(ScriptableRendererFeature)))
                return false;

            foreach (var rendererFeature in renderData.rendererFeatures)
            {
                if (rendererFeature.GetType() == featureType || rendererFeature.GetType().IsSubclassOf(featureType))
                    return true;
            }

            return false;
        }

        internal static int GetRendererDataLength(this UniversalRenderPipelineAsset universalRenderPipelineAsset)
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);

            return rendererDataProp.arraySize;
        }

        internal static ScriptableRendererData GetRendererData(this UniversalRenderPipelineAsset universalRenderPipelineAsset,
            int index)
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);
            var renderData = rendererDataProp.GetArrayElementAtIndex(index).objectReferenceValue as ScriptableRendererData;
            return renderData ? renderData : null;
        }

        internal static void SetDefaultRendererIndex(this UniversalRenderPipelineAsset universalRenderPipelineAsset, int index)
        {
            var urpAssetSerializedObject = new SerializedObject(universalRenderPipelineAsset);
            var rendererDataProp = urpAssetSerializedObject.FindProperty(k_RendererDataListName);

            var maxIndex = rendererDataProp.arraySize - 1;

            if (index > maxIndex || index < 0)
                return;

            var defaultRendererIndexProp = urpAssetSerializedObject.FindProperty(k_DefaultRendererIndexName);
            defaultRendererIndexProp.intValue = index;

            // Force save / refresh
            if (EditorUtility.IsPersistent(universalRenderPipelineAsset))
                EditorUtility.SetDirty(universalRenderPipelineAsset);

            urpAssetSerializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
