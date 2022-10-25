using Unity.MARS.Actions;
using Unity.XRTools.ModuleLoader;
using UnityEditor;
using UnityEditor.MARS.Simulation;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.Rules.RulesEditor
{
    [CustomRuleNode(typeof(Transform), "ValidateTarget")]
    class BuildSurfaceRow : ContentRow
    {
        const string k_PaintContent = "Paint";
        const string k_ColliderContent = "Collider";

#pragma warning disable IDE0051 // Remove unused private members
        // ReSharper disable once UnusedMember.Local -- method is used by 'CustomRuleNodeAttribute'
        static bool ValidateTarget(UnityObject target)
        {
            var transform = target as Transform;
            return transform != null && transform.GetComponent<BuildSurfaceAction>();
        }
#pragma warning restore IDE0051

        protected override void OnInit()
        {
            ContentSupportsActions = false;
            base.OnInit();
        }

        protected override void SetupContent()
        {
            SetObjectSetupAreaVisible(false);

            var renderer = ContentTransform.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                SetTitleName(k_PaintContent);

                var material = renderer.sharedMaterial;
                var so = new SerializedObject(material);
                so.Update();

                NodeContainer.Bind(so);
                ContentObjectField.objectType = typeof(Material);
                ContentObjectField.value = material;
                ContentObjectField.RegisterValueChangedCallback(OnMaterialFieldChange);
            }
            else
            {
                var meshCollider = ContentTransform.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    SetTitleName(k_ColliderContent);

                    var so = new SerializedObject(meshCollider);
                    so.Update();

                    NodeContainer.Bind(so);
                    ContentObjectField.objectType = typeof(MeshCollider);
                    ContentObjectField.value = meshCollider;
                    ContentObjectField.SetEnabled(false);
                }
            }

            AddButtonHoverElement.SetEnabled(false);
        }

        void OnMaterialFieldChange(ChangeEvent<UnityObject> evt)
        {
            if (evt == null)
                return;

            var renderer = BackingObject.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                Debug.LogError("Build Surface object must contain a MeshRenderer.");
                return;
            }

            renderer.material = (Material) evt.newValue;

            var moduleLoader = ModuleLoaderCore.instance;
            var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
            if (querySimulationModule.simulatingTemporal)
                querySimulationModule.RequestSimulationModeSelection(SimulationModeSelection.TemporalMode);

            querySimulationModule.RestartSimulationIfNeeded();
        }
    }
}
