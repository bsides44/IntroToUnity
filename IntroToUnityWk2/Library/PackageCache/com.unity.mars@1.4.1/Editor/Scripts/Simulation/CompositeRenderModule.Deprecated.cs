using System;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Simulation.Rendering
{
    /// <summary>
    /// Obsolete - Type of composite context view.
    /// </summary>
    [Obsolete]
    [Serializable]
    [MovedFrom("Unity.MARS")]
    public enum ContextViewType
    {
        /// <summary>Undefined view type</summary>
        Undefined = -1,
        /// <summary>Simulation View</summary>
        SimulationView = 0,
        /// <summary>Device View</summary>
        DeviceView,
        /// <summary>Scene view viewing the main scene</summary>
        NormalSceneView,
        /// <summary>Scene view in prefab isolation mode</summary>
        PrefabIsolation,
        /// <summary>Game View</summary>
        GameView,
        /// <summary>Other custom view type</summary>
        OtherView,
    }

    public partial class CompositeRenderModule
    {
        /// <summary>
        /// Get the active Composite Render Module for the Module Loader Core.
        /// </summary>
        /// <returns>True if there is a loaded Composite Render Module.</returns>
        /// <param name="compositeRenderModule">The active Composite Render Module.</param>
        [Obsolete("GetActiveCompositeRenderModule is obsolete, use 'ModuleLoaderCore.instance.GetModule<CompositeRenderModule>()'")]
        public static bool GetActiveCompositeRenderModule(out CompositeRenderModule compositeRenderModule)
        {
            compositeRenderModule = null;
            compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            return compositeRenderModule != null;
        }

        /// <summary>
        /// Obsolete - Try to get a Composite Render Context associated with the Scriptable Object
        /// from the active Composite Render Module.
        /// </summary>
        /// <param name="scriptableObject">Scriptable object we want the context for.</param>
        /// <param name="context">The Composite Render Context associated with the scriptable object.</param>
        /// <returns>Returns True if there is a context associated with the scriptable object.</returns>
        [Obsolete("TryGetCompositeRenderContext is obsolete, first get the 'CompositeRenderModule' instance from " +
            "the 'ModuleLoaderCore' the call 'TryGetCompositeRenderContext(camera, out context)'")]
        public static bool TryGetCompositeRenderContext(ScriptableObject scriptableObject, out CompositeRenderContext context)
        {
            var compositeRenderModule = ModuleLoaderCore.instance.GetModule<CompositeRenderModule>();
            if (scriptableObject is ISimulationView simulationView && compositeRenderModule != null)
                return compositeRenderModule.TryGetCompositeRenderContext(simulationView.camera, out context);

            context = null;
            return false;
        }

        /// <summary>
        /// Obsolete - Adds a view to the composite render module.
        /// </summary>
        /// <param name="scriptableObject">ScriptableObject to add.</param>
        [Obsolete("AddView(scriptableObject) is obsolete, use CreateCompositeContext(camera)")]
        public void AddView(ScriptableObject scriptableObject)
        {
            if (scriptableObject is ISimulationView simulationView)
                CreateCompositeContext(simulationView.camera);
        }

        /// <summary>
        /// Obsolete - Removes a view from being used in the composite render module
        /// and disposes of the CompositeRenderContext for that view.
        /// </summary>
        /// <param name="scriptableObject">ScriptableObject to be removed.</param>
        [Obsolete("RemoveView(scriptableObject) is obsolete, use DisposeCompositeContext(camera)")]
        public void RemoveView(ScriptableObject scriptableObject)
        {
            if (scriptableObject is ISimulationView simulationView)
                DisposeCompositeContext(simulationView.camera);
        }
    }
}
