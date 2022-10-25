using System;
using System.Collections.Generic;
using Unity.MARS;
using Unity.MARS.Simulation;
using Unity.MARS.Simulation.Rendering;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;
#if UNITY_EDITOR_OSX
using UnityEngine.Rendering;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.Scripting.APIUpdating;

#if INCLUDE_POST_PROCESSING
using UnityEngine.Rendering.PostProcessing;
#endif

namespace UnityEditor.MARS.Simulation.Rendering
{
    /// <summary>
    /// Module that manages the composite render context for all active views.
    /// This allows the composting of multiple scenes with different render settings in a view.
    /// </summary>
    [ModuleOrder(ModuleOrders.CompositeRenderLoadOrder)]
    [ModuleUnloadOrder(ModuleOrders.CompositeRenderUnloadOrder)]
    [MovedFrom("Unity.MARS")]
    public partial class CompositeRenderModule : IModuleDependency<SimulationSceneModule>
    {
        SimulationSceneModule m_SimulationSceneModule;
        GameObject m_ImageEffectsVolume;

        readonly HashSet<CompositeRenderContext> m_ContextsUsingSimulation = new HashSet<CompositeRenderContext>();
        readonly Dictionary<Camera, CompositeRenderContext> m_CompositeViewRenderContexts =
            new Dictionary<Camera, CompositeRenderContext>();

        internal bool SimulationSceneReady => !BuildPipeline.isBuildingPlayer && m_SimulationSceneModule != null
            && m_SimulationSceneModule.IsSimulationReady;

        /// <summary>
        /// Event callback for before composite cameras render in the Composite Render Context
        /// </summary>
        public event Action<CompositeRenderContext> BeforeBackgroundCameraRender;

        void IModuleDependency<SimulationSceneModule>.ConnectDependency(SimulationSceneModule dependency)
        {
            m_SimulationSceneModule = dependency;
        }

        void IModule.LoadModule()
        {
            CompositeRenderRuntimeUtils.OnImageEffectSettingsSet += SetImageEffectsProfile;
            CompositeRenderRuntimeUtils.OnImageEffectSettingsUnset += TearDownImageEffectsProfile;

            if (CompositeRenderRuntimeUtils.ImageEffectSettings != null)
                SetImageEffectsProfile(CompositeRenderRuntimeUtils.ImageEffectSettings);

            m_CompositeViewRenderContexts.Clear();
            m_ContextsUsingSimulation.Clear();

            SceneView.beforeSceneGui += OnBeforeSceneGui;
            SceneView.duringSceneGui += OnDuringSceneGui;

            CompositeRenderBackground.BackgroundAwake += CreateCompositeContext;
            CompositeRenderBackground.BackgroundDestroyed += DisposeCompositeContext;

            CompositeRenderEditorUtils.SetupRenderShaderGlobals();
        }

        void IModule.UnloadModule()
        {
            m_SimulationSceneModule?.UnregisterSimulationUser(SimulationSceneUsers.instance);

            CompositeRenderRuntimeUtils.OnImageEffectSettingsSet -= SetImageEffectsProfile;
            CompositeRenderRuntimeUtils.OnImageEffectSettingsUnset -= TearDownImageEffectsProfile;
            SceneView.beforeSceneGui -= OnBeforeSceneGui;
            SceneView.duringSceneGui -= OnDuringSceneGui;

            foreach (var compositeViews in m_CompositeViewRenderContexts)
            {
                compositeViews.Value?.Dispose();
            }

            CompositeRenderBackground.BackgroundAwake -= CreateCompositeContext;
            CompositeRenderBackground.BackgroundDestroyed -= DisposeCompositeContext;

            m_SimulationSceneModule = null;
            m_CompositeViewRenderContexts.Clear();
            m_ContextsUsingSimulation.Clear();
        }

        /// <summary>
        /// Try to get a Composite Render Context from the camera that is rendering the context.
        /// </summary>
        /// <param name="camera">Camera rendering the context we are trying to find.</param>
        /// <param name="context">The Composite Render Context associated with the camera.</param>
        /// <returns>Returns True if there is a context associated with the camera.</returns>
        public bool TryGetCompositeRenderContext(Camera camera, out CompositeRenderContext context)
        {
            context = null;

            return SimulationSceneReady && m_CompositeViewRenderContexts.TryGetValue(camera, out context);
        }

        internal void OnBeforeBackgroundCameraRender(CompositeRenderContext compositeRenderContext)
        {
            BeforeBackgroundCameraRender?.Invoke(compositeRenderContext);
        }

        void OnBeforeSceneGui(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var camera = sceneView.camera;

            if (camera == null)
                return;

            if (m_CompositeViewRenderContexts.TryGetValue(camera, out var context))
                context.PreCompositeCullTargetCamera();
        }

        void OnDuringSceneGui(SceneView sceneView)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            var camera = sceneView.camera;

            if (camera == null)
                return;

            if (m_CompositeViewRenderContexts.TryGetValue(camera, out var context))
            {
                context.ShowImageEffects = sceneView.sceneViewState.showImageEffects;
                context.BackgroundClearFlag = sceneView.sceneViewState.showSkybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;

                context.PostCompositeRenderTargetCamera();
            }
            // Add the the SceneView after it has been able to render once
            else if (!m_CompositeViewRenderContexts.ContainsKey(camera))
            {
                var compositeRenderBackground = camera.GetComponent<CompositeRenderBackground>();
                if (compositeRenderBackground == null)
                    compositeRenderBackground = camera.gameObject.AddComponent<CompositeRenderBackground>();

                var isSimView = sceneView is SimulationView;
                var simView = sceneView as SimulationView;
                var compositeData = new CompositeRenderData(isSimView ? CompositeViewType.SimulationView : CompositeViewType.NormalSceneView,
                    null, sceneView.GetSceneTargetTexture() != null ? sceneView.GetSceneTargetTexture().descriptor : new RenderTextureDescriptor(),
                    SimulationView.EditorBackgroundColor, SimulationConstants.SimulatedEnvironmentLayerMask,
                    isSimView && sceneView.sceneViewState.showImageEffects, isSimView && simView.EnvironmentSceneActive,
                    false, !isSimView || simView.UseXRay);

                compositeRenderBackground.InitializeCompositeBackground(compositeData);

                if (isSimView && m_CompositeViewRenderContexts.TryGetValue(camera, out var compositeRenderContext))
                    compositeRenderContext.BeforeCompositeCameraUpdate += simView.UpdateCamera;
            }
        }

        /// <summary>
        /// Creates and adds a CompositeRenderContext to be used in the composite render module
        /// from a camera.
        /// </summary>
        /// <param name="camera">The camera rendering the composite context.</param>
        internal void CreateCompositeContext(Camera camera)
        {
            var compositeRenderBackground = camera.GetComponent<CompositeRenderBackground>();
            if (compositeRenderBackground == null)
                return;

            if (m_CompositeViewRenderContexts.TryGetValue(camera, out var compositeRenderContext))
            {
                compositeRenderContext.UpdateCompositeRenderData(compositeRenderBackground.CompositeData);
                return;
            }

            compositeRenderContext = new CompositeRenderContext(compositeRenderBackground.CompositeData);

            // This is to fix an issue with the camera texture flipping in the composite of the game view.
            // The issue is only present on OSX using Metal rendering
            // and may not be needed in a later version of the editor.
#if UNITY_EDITOR_OSX
            if (compositeRenderBackground.CompositeData.ViewType == CompositeViewType.GameView && SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                compositeRenderBackground.CompositeData.TargetCamera.forceIntoRenderTexture = true;
#endif

            m_CompositeViewRenderContexts.Add(camera, compositeRenderContext);
        }

        /// <summary>
        /// Removes a view from being used in the composite render module
        /// and disposes of the CompositeRenderContext.
        /// </summary>
        /// <param name="camera">The camera rendering the composite context.</param>
        internal void DisposeCompositeContext(Camera camera)
        {
            if (m_CompositeViewRenderContexts.TryGetValue(camera, out var context))
            {
                m_ContextsUsingSimulation.Remove(context);
                m_CompositeViewRenderContexts.Remove(camera);
                context.Dispose();
            }

            if (m_ContextsUsingSimulation.Count == 0)
                m_SimulationSceneModule?.UnregisterSimulationUser(SimulationSceneUsers.instance);
        }

        void SetImageEffectsProfile(ScriptableObject imageEffectProfile)
        {
            if ((m_SimulationSceneModule==null) || !m_SimulationSceneModule.IsSimulationReady)
                return;

            if (m_ImageEffectsVolume != null)
            {
                UnityObjectUtils.Destroy(m_ImageEffectsVolume);
            }

            m_ImageEffectsVolume = new GameObject("Image Effects Volume")
            {
                layer = SimulationConstants.SimulatedEnvironmentLayerIndex,
                hideFlags = HideFlags.HideAndDontSave
            };

            SceneManager.MoveGameObjectToScene(m_ImageEffectsVolume, m_SimulationSceneModule.EnvironmentScene);

#if INCLUDE_POST_PROCESSING
            if (imageEffectProfile is PostProcessProfile postProcessProfile)
            {
                var volume = m_ImageEffectsVolume.AddComponent<PostProcessVolume>();
                volume.hideFlags = HideFlags.HideAndDontSave;

                volume.isGlobal = true;
                volume.profile = postProcessProfile;
            }
#endif
        }

        void TearDownImageEffectsProfile()
        {
            if (m_ImageEffectsVolume != null)
                UnityObjectUtils.Destroy(m_ImageEffectsVolume);
        }

        /// <summary>
        /// Adds a context that is holding the simulation open with a simulation user.
        /// </summary>
        /// <param name="context">Context that is holding the simulation user</param>
        internal void TryAddContextUsingSimulation(CompositeRenderContext context)
        {
            // Simulation views should not start sim in play mode
            if (context == null || Application.isPlaying && (context.CompositeViewType != CompositeViewType.GameView
                || context.CompositeViewType != CompositeViewType.OtherView))
                return;

            if (!m_ContextsUsingSimulation.Contains(context))
            {
                m_ContextsUsingSimulation.Add(context);
                m_SimulationSceneModule?.RegisterSimulationUser(SimulationSceneUsers.instance);
            }
        }

        /// <summary>
        /// Removes a context from the collection of contexts holding the simulation users.
        /// If no context are holding the user the simulation user is unregistered.
        /// </summary>
        /// <param name="context">Context that is no longer holding the simulation user</param>
        internal void TryRemoveContextUsingSimulation(CompositeRenderContext context)
        {
            m_ContextsUsingSimulation.Remove(context);

            if(m_ContextsUsingSimulation.Count == 0)
                m_SimulationSceneModule?.UnregisterSimulationUser(SimulationSceneUsers.instance);
        }
    }
}
