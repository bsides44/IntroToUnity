using System;
using Unity.XRTools.ModuleLoader;
using UnityEditor.EditorTools;
using UnityEditor.MARS.Simulation;

#if !UNITY_2020_2_OR_NEWER
using ToolManager = UnityEditor.EditorTools.EditorTools;
#endif

namespace UnityEditor.MARS.Authoring
{
    class MarsEditorToolsModule : IModuleDependency<ScenePlacementModule>
    {
        static readonly Type[] k_ProxyToolTypes = { typeof(MarsCompareTool), typeof(MarsCreateTool) };

        ScenePlacementModule m_ScenePlacementModule;
        Type m_LastNonProxyToolType;

        void IModule.LoadModule()
        {
            ToolManager.activeToolChanging += OnActiveToolChanging;
            ToolManager.activeToolChanged += OnActiveToolChanged;
        }

        void IModule.UnloadModule()
        {
            ToolManager.activeToolChanging -= OnActiveToolChanging;
            ToolManager.activeToolChanged -= OnActiveToolChanged;
        }

        void IModuleDependency<ScenePlacementModule>.ConnectDependency(ScenePlacementModule dependency)
        {
            m_ScenePlacementModule = dependency;
        }

        void OnActiveToolChanging()
        {
            var activeToolType = ToolManager.activeToolType;
            if (!IsProxyToolType(activeToolType))
                m_LastNonProxyToolType = activeToolType;
        }

        void OnActiveToolChanged()
        {
            if (!ActiveToolIsProxyTool())
                return;

            // Require content selection type for proxy-based tools
            foreach (var simulationView in SimulationView.SimulationViews)
            {
                simulationView.EnvironmentSceneActive = false;
            }

            // Current dragged object might be in environment scene when the switch to content selection happens
            m_ScenePlacementModule.EnsureDraggedObjectInSimViewIsInContent();
        }

        internal void EnsureProxyToolIsNotActive()
        {
            if (!ActiveToolIsProxyTool())
                return;

            if (m_LastNonProxyToolType != null)
                ToolManager.SetActiveTool(m_LastNonProxyToolType);
            else
                ToolManager.SetActiveTool((EditorTool)null);
        }

        internal static bool ActiveToolIsProxyTool()
        {
            return IsProxyToolType(ToolManager.activeToolType);
        }

        static bool IsProxyToolType(Type type)
        {
            foreach (var proxyToolType in k_ProxyToolTypes)
            {
                if (type == proxyToolType)
                    return true;
            }

            return false;
        }
    }
}
