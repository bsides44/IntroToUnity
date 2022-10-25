using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.MARS;
using Unity.MARS.MARSUtils;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEditor.MARS.Simulation;
using UnityEditor.MARS.Simulation.Rendering;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
using Unity.MARS.Rendering;
using UnityEngine.Rendering.Universal;
#endif

#if INCLUDE_AR_FOUNDATION
using UnityEngine.XR.ARFoundation;
#endif

namespace UnityEditor.MARS.Build
{
    static class MarsRenderingBuildValidationRules
    {
        const string k_ARFoundationPackage = "com.unity.xr.arfoundation";
        const string k_UniversalRPPackage = "com.unity.render-pipelines.universal";
        const string k_Physics2D = "com.unity.modules.physics2d";
        const string k_ProjectQuality = "Project/Quality";
        const string k_ProjectGraphics = "Project/Graphics";

        static readonly PackageVersion k_UrpPhysics2dMinVersion = "7.5.0";

#if INCLUDE_AR_FOUNDATION
        static readonly Type[] k_IsMARSSceneTypes = { typeof(MARSSession) };
        static readonly Type[] k_UsesARCameraBackgroundTypes = { typeof(ARSession) };
        static readonly Type[] k_ARCameraBackgroundTypes = { typeof(ARCameraBackground) };
#endif

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
#if ARFOUNDATION_3_OR_NEWER
        static readonly HashSet<RenderPipelineAsset> k_RenderPipelineAssets = new HashSet<RenderPipelineAsset>();
#endif

        static MethodInfo s_ClearSceneDirtinessMethodInfo;
        static MethodInfo s_RequestCloseAndRelaunchWithCurrentArgumentsMethodInfo;

        static MarsRenderingBuildValidationRules()
        {
            const BindingFlags bindingFlags = BindingFlags.Default | BindingFlags.Static | BindingFlags.NonPublic;
            s_ClearSceneDirtinessMethodInfo = typeof(EditorSceneManager).GetMethod("ClearSceneDirtiness", bindingFlags);
            s_RequestCloseAndRelaunchWithCurrentArgumentsMethodInfo = typeof(EditorApplication).GetMethod("RequestCloseAndRelaunchWithCurrentArguments", bindingFlags);
        }

        [InitializeOnLoadMethod]
        static void AddMarsRules()
        {
            var globalGraphicsRules = new []
            {
                new BuildValidationRule()
                {
                    name = "Universal RP package dependency check",
                    message = "'Universal RP' package requires that Physics 2D Package is installed or updated to the latest version.",
                    checkPredicate = () =>
                    {
                        var urpVersion = PackageVersionUtility.GetPackageVersion(k_UniversalRPPackage);

                        if (urpVersion == default)
                            return true;

                        if (urpVersion.ToMajorMinor() < k_UrpPhysics2dMinVersion)
                            return PackageVersionUtility.GetPackageVersion(k_Physics2D) != default;

                        return true;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the `Physics 2D` package under " +
                        "'Built-in packages' or update the 'Universal RP' Package to the latest version.",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_Physics2D);
                    }
                },
#if UNITY_EDITOR_WIN
                new BuildValidationRule()
                {
                    name = "Editor standalone graphics API settings check",
                    message = "Simulation rendering does not support OpenGL. It is recommended that you use the Auto " +
                              "Graphics API for the Editor.",
                    checkPredicate = () =>
                    {
                        if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows)
                            || PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64))
                        {
                            return true;
                        }

                        var graphicsAPI = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows);

                        if (graphicsAPI.Length > 0 && (graphicsAPI[0] == GraphicsDeviceType.OpenGLCore)
                            || (graphicsAPI[0] == GraphicsDeviceType.OpenGLES2)
                            || (graphicsAPI[0] == GraphicsDeviceType.OpenGLES3))
                        {
                            return false;
                        }

                        var graphicsAPI64 = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
                        if (graphicsAPI64.Length > 0 && (graphicsAPI64[0] == GraphicsDeviceType.OpenGLCore)
                            || (graphicsAPI64[0] == GraphicsDeviceType.OpenGLES2)
                            || (graphicsAPI64[0] == GraphicsDeviceType.OpenGLES3))
                        {
                            return false;
                        }

                        return true;
                    },
                    fixItMessage = "Open Project Settings > Player, under the Stand Alone Player tab (first tab) in " +
                        "Other Settings, enable `Auto Graphics API for Windows`.",
                    fixIt = () =>
                    {
                        if (!CheckApplyGraphicsAPIList(out var restartEditor))
                            return;

                        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows, true);
                        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, true);

                        if (restartEditor)
                            RestartEditor();
                    }
                },
#endif
#if UNITY_EDITOR_OSX
                new BuildValidationRule()
                {
                    name = "Editor standalone graphics API settings check",
                    message = "Simulation rendering does not support OpenGL. It is recommended that you use the Auto " +
                              "Graphics API for the Editor.",
                    checkPredicate = () =>
                    {
                        if (PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX))
                            return true;

                        var graphicsAPIs = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneOSX);

                        if (graphicsAPIs.Length > 0 && (graphicsAPIs[0] == GraphicsDeviceType.OpenGLCore)
                            || (graphicsAPIs[0] == GraphicsDeviceType.OpenGLES2)
                            || (graphicsAPIs[0] == GraphicsDeviceType.OpenGLES3))
                            return false;

                        return true;
                    },
                    fixItMessage = "Open Project Settings > Player, under the Stand Alone Player tab (first tab) in " +
                        "Other Settings, enable `Auto Graphics API for Mac`.",
                    fixIt = () =>
                    {
                        if (!CheckApplyGraphicsAPIList(out var restartEditor))
                            return;

                        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneOSX, true);

                        if (restartEditor)
                            RestartEditor();
                    }
                },
#endif
#if !INCLUDE_RENDER_PIPELINES_UNIVERSAL
                new BuildValidationRule()
                {
                    name = "Graphics settings scriptable render path supported",
                    message = "Only 'Universal RP' is fully supported by Unity MARS. All other " +
                        "pipelines require custom support.",
                    checkPredicate = () => !CheckGraphicsSettingsUsingScriptableRenderPipeline(),
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectGraphics);
                    },
                    fixItMessage = "Open Project Settings > Graphics and remove the 'Scriptable Render Pipeline " +
                        "Settings' or set them to a supported pipeline asset.",
                    fixItAutomatic = false,
                    helpLink = DocumentationConstants.GraphicsSupportedRenderingModes,
                },
                new BuildValidationRule()
                {
                    name = "Quality settings scriptable render path supported",
                    message = "Only 'Universal RP' is fully supported by Unity MARS. All other " +
                        "pipelines require custom support.",
                    checkPredicate = () => !CheckQualitySettingsUsingScriptableRenderPipeline(),
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectQuality);
                    },
                    fixItMessage = "Open Project Settings > Quality and for each Quality Setting remove the " +
                        "'Scriptable Render Pipeline Asset' or set them to a supported pipeline asset.",
                    fixItAutomatic = false,
                    helpLink = DocumentationConstants.GraphicsSupportedRenderingModes,
                },
#else
                new BuildValidationRule()
                {
                    name = "Graphics settings scriptable render path supported",
                    message = "Only 'Universal RP' is fully supported by Unity MARS. All other " +
                        "pipelines require custom support.",
                    checkPredicate = () => !(CheckGraphicsSettingsUsingScriptableRenderPipeline()
                        && !CheckGraphicsSettingsUsingUniversalPipeline()),
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectGraphics);
                    },
                    fixItMessage = "Open Project Settings > Graphics and remove the 'Scriptable Render Pipeline " +
                        "Settings' or set them to a supported pipeline asset.",
                    fixItAutomatic = false,
                    helpLink = DocumentationConstants.GraphicsSupportedRenderingModes,
                },
                new BuildValidationRule()
                {
                    name = "Quality settings scriptable render path supported",
                    message = "Only 'Universal RP' is fully supported by Unity MARS. All other " +
                        "pipelines require custom support.",
                    checkPredicate = () => !(CheckQualitySettingsUsingScriptableRenderPipeline()
                        && !CheckQualitySettingsUsingUniversalPipeline()),
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectQuality);
                    },
                    fixItMessage = "Open Project Settings > Quality and for each Quality Setting only use 'Universal RP'" +
                        " assets for the 'Scriptable Render Pipeline Asset'.",
                    fixItAutomatic = false,
                    helpLink = DocumentationConstants.GraphicsSupportedRenderingModes,
                },
                new BuildValidationRule()
                {
                    name = "Quality settings mixed scriptable render settings",
                    message = "Unity MARS only supports using a single type of render path in the project's Quality Settings.",
                    checkPredicate = () => !(CheckQualitySettingsUsingScriptableRenderPipeline()
                        && !CheckQualitySettingsUsingUniversalPipeline()),
                    fixIt = () =>
                    {
                        SettingsService.OpenProjectSettings(k_ProjectQuality);
                    },
                    fixItMessage = "Open Project Settings > Quality and for each Quality Setting change the " +
                        "'Scriptable Render Pipeline Asset' to only use a single pipeline type or remove all the " +
                        "`Pipeline Assets` to use Legacy rendering.",
                    fixItAutomatic = false
                },
#endif
                new BuildValidationRule
                {
                    name = "Using Fallback Composite Rendering (Editor Only)",
                    message = "Use the correct 'Fallback Composite Rendering' setting for your Graphics Pipeline",
                    checkPredicate = () =>
                    {
                        var useFallback = CompositeRenderModuleOptions.instance.UseFallbackCompositeRendering;
                        var currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
                        return currentRenderPipeline != null && useFallback
                            || currentRenderPipeline == null && !useFallback;
                    },
                    fixIt = () =>
                    {
                        var renderPipelineSet = GraphicsSettings.currentRenderPipeline != null;
                        CompositeRenderModuleOptions.instance.UseFallbackCompositeRendering = renderPipelineSet;
                        ModuleLoaderCore.instance.ReloadModules();
                    },
                    fixItMessage = "Open Project Settings > MARS > Editor Visuals and enable 'Using Fallback Composite " +
                        "Rendering' if a Scriptable Render Pipeline (SRP) is active and disable it if no SRP is active.",
                    helpLink = DocumentationConstants.GraphicsCompositeRenderOptions,
                }
            };

            var arBackgroundRules = new[]
            {
                new BuildValidationRule()
                {
                    name = "AR Foundation AR Camera Background available",
                    message = "'AR Foundation' package is not installed and is needed to display the device camera feed " +
                        "in your scene.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        return version != default;
                    },
                    fixItMessage = "Open Windows > Package Manager and install the 'AR Foundation' package",
                    fixIt = () =>
                    {
                        PackageManager.Client.Add(k_ARFoundationPackage);
                    },
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetup,
                },
                new BuildValidationRule()
                {
                    name = "AR Foundation AR Camera Background available",
                    message = "'AR Foundation' package is not installed and is needed to display the device camera feed " +
                        "in your scene.",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        return version != default && version >= "2.1.0";
                    },
                    fixItMessage = "Open Windows > Package Manager and update the 'AR Foundation' package to version " +
                        "2.1.0 or greater",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_ARFoundationPackage);
                    },
                    fixItAutomatic = false,
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetup,
                },
#if INCLUDE_AR_FOUNDATION
                new BuildValidationRule()
                {
                    name = "Scene has AR Camera Background",
                    message = "An 'AR Camera Background' component on the main camera is required to have the device " +
                        "camera view displayed in your scene.",
                    checkPredicate = () =>
                    {
                        if (Application.isPlaying)
                            return true;

                        // Early out if MARS scene
                        if (BuildValidator.HasTypesInSceneSetup(k_IsMARSSceneTypes))
                            return true;

                        // Early out if not AR scene
                        if (!BuildValidator.HasTypesInSceneSetup(k_UsesARCameraBackgroundTypes))
                            return true;

                        if (k_ARCameraBackgroundTypes.Length == 0)
                            return false;

                        return BuildValidator.HasTypesInSceneSetup(k_ARCameraBackgroundTypes);
                    },
                    fixIt = () =>
                    {
                        var mainCamera = MarsRuntimeUtils.GetSessionAssociatedCamera(true);
                        using (var undo = new UndoBlock("AR Camera Background Setup"))
                        {
                            if (mainCamera == null)
                            {
                                var inScene = false;
                                foreach (var sceneSetup in EditorSceneManager.GetSceneManagerSetup())
                                {
                                    if (!sceneSetup.isLoaded)
                                        continue;

                                    var scene = SceneManager.GetSceneByPath(sceneSetup.path);

                                    if (mainCamera.gameObject.scene == scene)
                                    {
                                        inScene = true;
                                        break;
                                    }
                                }

                                if (!inScene)
                                {
                                    var mainCameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
                                    undo.RegisterCreatedObject(mainCameraGo);
                                    mainCamera = mainCameraGo.AddComponent<Camera>();
                                }
                            }

                            var camGO = mainCamera.gameObject;
                            Selection.activeGameObject = camGO;
                            undo.AddComponent<ARCameraBackground>(camGO);
                        }
                    },
                    fixItMessage = "Add an 'AR Camera Background' component to the main camera of your scene to see the " +
                        "device camera feed.",
                    sceneOnlyValidation = true,
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetup,
                },
#endif
            };

#if !INCLUDE_RENDER_PIPELINES_UNIVERSAL
            var universalPipeLineBackgroundRules = new BuildValidationRule[] { };
#else
            var universalPipeLineBackgroundRules = new []
            {
                new BuildValidationRule()
                {
                    name = "Pipelines all using same type of valid Render Pipeline Asset",
                    message = "Quality Settings and Graphics Settings not mixing types of Render Pipeline Assets, " +
                        "either not set (NULL) or 'Universal Pipeline Asset'",
                    checkPredicate = () => QualitySettingsValidPipelineAssets() && GraphicsSettingsValidPipelineAssets(),
                    fixItMessage = "Check that all the Render Pipeline Asset property fields in " +
                        "'Project Settings > Graphics' under 'Scriptable Render Pipeline Settings' or " +
                        "'Project Settings > Quality' in the each quality setting under 'Rendering'. " +
                        "are all using an 'UniversalRenderPipelineAsset' or are blank (Null).",
                    fixIt = () =>
                    {
                        if (!QualitySettingsValidPipelineAssets())
                        {
                            QualitySettings.SetQualityLevel(CurrentOrFistQualitySettingsNotValidPipelineAsset());
                            SettingsService.OpenProjectSettings(k_ProjectQuality);
                        }
                        else
                        {
                            SettingsService.OpenProjectSettings(k_ProjectGraphics);
                        }
                    },
                    fixItAutomatic = false,
                },
                new BuildValidationRule()
                {
                    name = "AR Background Render Feature available",
                    message = "AR Background in Universal Render Pipeline requires 'AR Foundation' v3.0.1 or newer",
                    checkPredicate = () =>
                    {
                        var version = PackageVersionUtility.GetPackageVersion(k_ARFoundationPackage);
                        return version != default && version >= "3.0.1";
                    },
                    fixItMessage = "Open Windows > Package Manager and update the 'AR Foundation' package to version " +
                        "3.0.1 or greater",
                    fixIt = () =>
                    {
                        PackageManager.UI.Window.Open(k_ARFoundationPackage);
                    },
                    fixItAutomatic = false,
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetupURP,
                },
#if ARFOUNDATION_3_OR_NEWER
                new BuildValidationRule()
                {
                    name = "Assigned Render Pipelines have AR Background Render Feature",
                    message = "All assigned Universal Render Pipeline Assets' default Renderer Data Asset has AR " +
                        "Background Render Feature",
                    checkPredicate = () =>
                    {
                        k_RenderPipelineAssets.Clear();
                        MarsSrpUtilities.GetRenderPipelineAssetsInProjectSettings(typeof(UniversalRenderPipelineAsset),
                            k_RenderPipelineAssets);

                        foreach (var renderPipeline in k_RenderPipelineAssets)
                        {
                            var urpAsset = renderPipeline as UniversalRenderPipelineAsset;
                            if (!urpAsset.HasRenderFeature(typeof(ARBackgroundRendererFeature)))
                                return false;
                        }

                        return true;

                    },
                    fixItMessage = "For each of the render pipeline assets, which can can be found in " +
                        "'Project Settings > Graphics' under 'Scriptable Render Pipeline Settings' and " +
                        "'Project Settings > Quality' in the active quality setting under 'Rendering': " +
                        "make sure the pipeline asset is a 'UniversalRenderPipelineAsset' and add in the pipeline " +
                        "asset under 'Render List'. If you have multiple pipeline data assets and one already has the " +
                        "'ARBackgroundRendererFeature', set that pipeline data asset to default with the 'Set Default' " +
                        "button or add the 'ARBackgroundRendererFeature' to the default pipeline data asset by selecting " +
                        "the asset next to the button that says 'Default'. In the pipeline data asset press " +
                        "'Add RendererFeature' and select 'ARBackgroundRendererFeature'.",
                    fixIt = () =>
                    {
                        k_RenderPipelineAssets.Clear();
                        MarsSrpUtilities.GetRenderPipelineAssetsInProjectSettings(typeof(UniversalRenderPipelineAsset),
                            k_RenderPipelineAssets);

                        foreach (var renderPipeline in k_RenderPipelineAssets)
                        {
                            var urpAsset = renderPipeline as UniversalRenderPipelineAsset;
                            if (!urpAsset.HasRenderFeature(typeof(ARBackgroundRendererFeature)))
                                urpAsset.AddRenderFeatureToDefaultPipelineData<ARBackgroundRendererFeature>();
                        }

                    },
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetupURP,
                },
                new BuildValidationRule()
                {
                    name = "Assigned Render Pipelines' default renderers have AR Background Render Feature",
                    message = "All assigned Universal Render Pipeline Assets that have an AR Background Render Feature " +
                        "are using the Render Pipeline with that feature as their default.",
                    checkPredicate = () =>
                    {
                        k_RenderPipelineAssets.Clear();
                        MarsSrpUtilities.GetRenderPipelineAssetsInProjectSettings(typeof(UniversalRenderPipelineAsset),
                            k_RenderPipelineAssets);

                        foreach (var renderPipeline in k_RenderPipelineAssets)
                        {
                            var urpAsset = renderPipeline as UniversalRenderPipelineAsset;
                            if (!urpAsset.HasRenderFeature(typeof(ARBackgroundRendererFeature))
                                || !urpAsset.GetDefaultRenderer().HasRenderFeature(typeof(ARBackgroundRendererFeature)))
                                return false;
                        }

                        return true;
                    },
                    fixItMessage = "For each of the render pipeline assets, which can can be found in " +
                        "'Project Settings > Graphics' under 'Scriptable Render Pipeline Settings' and " +
                        "'Project Settings > Quality' in the active quality setting under 'Rendering', that are an " +
                        "'UniversalRenderPipelineAsset': in those pipeline assets under 'Render List', find the one " +
                        "that has an 'ARBackgroundRendererFeature' and set that pipeline data asset to default with " +
                        "the 'Set Default' button.",
                    fixIt = () =>
                    {
                        k_RenderPipelineAssets.Clear();
                        MarsSrpUtilities.GetRenderPipelineAssetsInProjectSettings(typeof(UniversalRenderPipelineAsset),
                            k_RenderPipelineAssets);

                        foreach (var renderPipeline in k_RenderPipelineAssets)
                        {
                            var urpAsset = renderPipeline as UniversalRenderPipelineAsset;
                            if (urpAsset.HasRenderFeature(typeof(ARBackgroundRendererFeature))
                                && !urpAsset.GetDefaultRenderer().HasRenderFeature(typeof(ARBackgroundRendererFeature)))
                            {
                                urpAsset.SetDefaultRendererIndex(urpAsset.GetRenderFeatureRendererIndex(typeof(ARBackgroundRendererFeature)));
                            }
                        }
                    },
                    fixItAutomatic = true,
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetupURP,
                },
                new BuildValidationRule()
                {
                    name = "Current Render Pipeline has AR Background Render Feature",
                    message = "The render pipeline returned by 'GraphicsSettings.currentRenderPipeline' is a " +
                        "'UniversalRenderPipelineAsset' with 'ARBackgroundRendererFeature' as part of a pipeline.",
                    checkPredicate = () =>
                    {
                        // Early out if not AR scene
                        if (!BuildValidator.HasTypesInSceneSetup(k_UsesARCameraBackgroundTypes))
                            return true;

                        if (!UsingPipelineOfType(typeof(UniversalRenderPipelineAsset), k_RenderPipelineAssets))
                            return true;

                        return GraphicsSettings.currentRenderPipeline.HasRenderFeature(typeof(ARBackgroundRendererFeature));
                    },
                    fixItMessage = "Find the current render pipeline, it usually can be found in " +
                        "'Project Settings > Graphics' under 'Scriptable Render Pipeline Settings' or " +
                        "'Project Settings > Quality' in the active quality setting under 'Rendering'. " +
                        "Make sure the pipeline asset is an 'UniversalRenderPipelineAsset' and add in the pipeline " +
                        "asset under 'Render List', if you have multiple pipeline data assets and one already has the " +
                        "'ARBackgroundRendererFeature' set that pipeline data asset to default with the 'Set Default' " +
                        "button or add the 'ARBackgroundRendererFeature' to the default pipeline data asset by selecting " +
                        "the asset next to the button that says 'Default'. In the pipeline data asset press " +
                        "'Add RendererFeature' and select 'ARBackgroundRendererFeature'.",
                    sceneOnlyValidation = true,
                    fixIt = () =>
                    {
                        if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline is
                            UniversalRenderPipelineAsset urpAsset)
                        {
                            urpAsset.AddRenderFeatureToDefaultPipelineData<ARBackgroundRendererFeature>();
                        }
                        else
                        {
                            var currentPipelineName = GraphicsSettings.currentRenderPipeline == null ? "NULL"
                                : GraphicsSettings.currentRenderPipeline.name;
                            Debug.LogError($"Unable to add render feature to current pipeline '{currentPipelineName}'! " +
                                $"Try fixing other validation errors first.");
                        }
                    },
                    fixItAutomatic = true,
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetupURP,
                },
                new BuildValidationRule()
                {
                    name = "Current Render Pipeline's default renderer has AR Background Render Feature",
                    message = "The current Universal Render Pipeline Assets that have an AR Background Render Feature " +
                        "are using the Render Pipeline with that feature as their default.",
                    checkPredicate = () =>
                    {
                        // Early out if not AR scene
                        if (!BuildValidator.HasTypesInSceneSetup(k_UsesARCameraBackgroundTypes))
                            return true;

                        if (!UsingPipelineOfType(typeof(UniversalRenderPipelineAsset), k_RenderPipelineAssets))
                            return true;

                        var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

                        return urpAsset.GetDefaultRenderer().HasRenderFeature(typeof(ARBackgroundRendererFeature));
                    },
                    fixItMessage = "Find the current render pipeline, which usually can be found in " +
                        "'Project Settings > Graphics' under 'Scriptable Render Pipeline Settings' and " +
                        "'Project Settings > Quality' in the active quality setting under 'Rendering'. In that pipeline " +
                        "asset under 'Render List', find the one that has an 'ARBackgroundRendererFeature' and set " +
                        "that pipeline data asset to default with the 'Set Default' button.",
                    sceneOnlyValidation = true,
                    fixIt = () =>
                    {
                        if (GraphicsSettings.currentRenderPipeline != null && GraphicsSettings.currentRenderPipeline is
                            UniversalRenderPipelineAsset urpAsset)
                        {
                            urpAsset.SetDefaultRendererIndex(urpAsset.GetRenderFeatureRendererIndex(typeof(ARBackgroundRendererFeature)));
                        }
                        else
                        {
                            var currentPipelineName = GraphicsSettings.currentRenderPipeline == null ? "NULL"
                                : GraphicsSettings.currentRenderPipeline.name;
                            Debug.LogError($"Unable to add render feature to current pipeline '{currentPipelineName}'! " +
                                "Try fixing other validation errors first.");
                        }
                    },
                    helpLink = DocumentationConstants.GraphicsARBackgroundSetupURP,
                },
#endif // ARFOUNDATION_3_OR_NEWER
            };
#endif // INCLUDE_RENDER_PIPELINES_UNIVERSAL

            // Add Global Graphics Rules
            BuildValidator.AddRules(BuildTargetGroup.Standalone, globalGraphicsRules);
            BuildValidator.AddRules(BuildTargetGroup.iOS, globalGraphicsRules);
            BuildValidator.AddRules(BuildTargetGroup.Android, globalGraphicsRules);
            BuildValidator.AddRules(BuildTargetGroup.Lumin, globalGraphicsRules);
            BuildValidator.AddRules(BuildTargetGroup.WSA, globalGraphicsRules);

            // AR Camera Background Rules
            BuildValidator.AddRules(BuildTargetGroup.Standalone, arBackgroundRules);
            BuildValidator.AddRules(BuildTargetGroup.iOS, arBackgroundRules);
            BuildValidator.AddRules(BuildTargetGroup.Android, arBackgroundRules);

            // URP Rules
            BuildValidator.AddRules(BuildTargetGroup.Standalone, universalPipeLineBackgroundRules);
            BuildValidator.AddRules(BuildTargetGroup.iOS, universalPipeLineBackgroundRules);
            BuildValidator.AddRules(BuildTargetGroup.Android, universalPipeLineBackgroundRules);
        }

        static bool CheckGraphicsSettingsUsingScriptableRenderPipeline()
        {
            return GraphicsSettings.defaultRenderPipeline != null || GraphicsSettings.renderPipelineAsset != null;
        }

        static bool CheckQualitySettingsUsingScriptableRenderPipeline()
        {
            if (QualitySettings.renderPipeline != null)
                return true;

            var settingsCount = QualitySettings.names.Length;
            for (var i = 0; i < settingsCount; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) != null)
                    return true;
            }

            return false;
        }

#if INCLUDE_RENDER_PIPELINES_UNIVERSAL
        static bool CheckGraphicsSettingsUsingUniversalPipeline()
        {
            return GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset
                && GraphicsSettings.renderPipelineAsset is UniversalRenderPipelineAsset;
        }

        static bool CheckQualitySettingsUsingUniversalPipeline()
        {
            var settingsCount = QualitySettings.names.Length;
            for (var i = 0; i < settingsCount; i++)
            {
                var pipelineAsset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (pipelineAsset != null && !(pipelineAsset is UniversalRenderPipelineAsset))
                    return false;
            }

            return true;
        }

        static bool QualitySettingsValidPipelineAssets()
        {
            var settingsCount = QualitySettings.names.Length;
            var urpAssets = 0;
            for (var i = 0; i < settingsCount; i++)
            {
                var pipelineAsset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (pipelineAsset != null && pipelineAsset is UniversalRenderPipelineAsset)
                    urpAssets++;
            }

            return urpAssets == 0 || urpAssets == settingsCount;
        }

        static int CurrentOrFistQualitySettingsNotValidPipelineAsset()
        {
            var currentQualityIndex = QualitySettings.GetQualityLevel();
            var currentQualityPipeline = QualitySettings.GetRenderPipelineAssetAt(currentQualityIndex);

            if (currentQualityPipeline != null && !(currentQualityPipeline is UniversalRenderPipelineAsset))
                return currentQualityIndex;

            var nullIndex = currentQualityPipeline == null ? currentQualityIndex : -1;

            var settingsCount = QualitySettings.names.Length;
            for (var i = 0; i < settingsCount; i++)
            {
                var pipelineAsset = QualitySettings.GetRenderPipelineAssetAt(i);
                if (pipelineAsset == null)
                {
                    if (nullIndex != -1)
                        continue;

                    nullIndex = i;
                }
                else if (!(pipelineAsset is UniversalRenderPipelineAsset))
                {
                    return i;
                }
            }

            return  nullIndex;
        }

        static bool GraphicsSettingsValidPipelineAssets()
        {
            var defaultPipeline = GraphicsSettings.defaultRenderPipeline;
            return defaultPipeline == null || defaultPipeline is UniversalRenderPipelineAsset;
        }

#if ARFOUNDATION_3_OR_NEWER
        static bool UsingPipelineOfType(Type type, HashSet<RenderPipelineAsset> pipelines)
        {
            pipelines.Clear();
            var pipelineType = typeof(RenderPipelineAsset);
            if (type != pipelineType && !type.IsSubclassOf(typeof(RenderPipelineAsset)))
                return false;

            MarsSrpUtilities.GetRenderPipelineAssetsInProjectSettings(type, pipelines);
            return pipelines.Count > 0;
        }
#endif // ARFOUNDATION_3_OR_NEWER
#endif // INCLUDE_RENDER_PIPELINES_UNIVERSAL

        static bool CheckApplyGraphicsAPIList(out bool doRestart)
        {
            // If we have dirty scenes we need to save or discard changes before we restart editor.
            // Otherwise user will get a dialog later on where they can click cancel and put editor in a bad device state.
            var dirtyScenes = new List<Scene>();
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.isDirty)
                    dirtyScenes.Add(scene);
            }

            if (dirtyScenes.Count == 0)
            {
                doRestart = EditorUtility.DisplayDialog("Changing Editor graphics API",
                    "You've changed the active graphics API. This requires a restart of the Editor.",
                    "Restart Editor", "Not now");

                return true;
            }

            var result = EditorUtility.DisplayDialogComplex("Changing Editor graphics API",
                "You've changed the active graphics API. This requires a restart of the Editor. Do you want to save the Scene when restarting?",
                "Save and Restart", "Cancel Changing API", "Discard Changes and Restart");

            switch (result)
            {
                // Save and Restart was selected
                case 0:
                {
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        var saved = EditorSceneManager.SaveScene(dirtyScenes[i]);
                        if (saved == false)
                        {
                            doRestart = false;
                            return false;
                        }
                    }

                    doRestart = true;
                    return true;
                }

                // Cancel was selected
                case 1:
                {
                    doRestart = false;
                    return false;
                }

                // Discard Changes and Restart was selected
                case 2:
                {
                    for (var i = 0; i < dirtyScenes.Count; ++i)
                    {
                        s_ClearSceneDirtinessMethodInfo.Invoke(null, new object[]{dirtyScenes[i]});
                    }

                    doRestart = true;
                    return true;
                }

                default:
                    goto case 2;
            }
        }

        static void RestartEditor()
        {
            if (ModuleLoaderCore.instance.ModulesAreLoaded)
            {
                var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                if (environmentManager != null)
                    environmentManager.TrySaveEnvironmentModificationsDialog(false);
            }

            s_RequestCloseAndRelaunchWithCurrentArgumentsMethodInfo.Invoke(null, null);
        }
    }
}
