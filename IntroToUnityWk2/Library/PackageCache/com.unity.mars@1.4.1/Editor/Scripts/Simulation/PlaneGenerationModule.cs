using System.Collections.Generic;
using Unity.MARS;
using Unity.MARS.Data;
using Unity.MARS.Data.Synthetic;
using Unity.MARS.Providers;
using Unity.MARS.Simulation;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEditor.MARS.Data.Synthetic
{
    /// <summary>
    /// Module used to generate planes for a synthetic environment
    /// </summary>
    [MovedFrom("Unity.MARS")]
    public class PlaneGenerationModule : IModule, IUsesPlaneFinding
    {
        const string k_SavePlanesDialogTitle = "Saving Planes";
        const string k_NoPlanesDialogMessage = "No planes have been discovered in simulation. Cannot save planes to environment.";
        const string k_NoPlanesDialogOk = "OK";
        const string k_ConfirmDestroyDialogMessage = "This action will destroy all previously generated planes. If you want " +
            "to preserve any planes you can move them out of the parent game object that has the component {0}. Do you want to continue?";

        const string k_ConfirmDestroyDialogOk = "Yes";
        const string k_ConfirmDestroyDialogCancel = "No";

        /// <inheritdoc />
        public IProvidesPlaneFinding provider { get; set; }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        // Reference type collections must also be cleared after use
        static readonly List<MRPlane> k_Planes = new List<MRPlane>();
        static readonly List<GeneratedPlanesRoot> k_PlanesRoots = new List<GeneratedPlanesRoot>();

        /// <inheritdoc />
        void IModule.LoadModule() { }

        /// <inheritdoc />
        void IModule.UnloadModule() { }

        /// <summary>
        /// Run planes extraction from the <c>PlanesExtractionManager</c>
        /// </summary>
        /// <param name="settings">Settings to use when running extraction</param>
        public static void ExtractPlanes(PlaneExtractionSettings settings)
        {
            PlanesExtractionManager.ExtractPlanes(settings);
        }

        /// <summary>
        /// Try to destroy game objects in the synthetic environment that have the <c>GeneratedPlanesRoot</c> component
        /// </summary>
        /// <param name="environmentRoot">Synthetic environment root game object</param>
        /// <param name="confirmDestroyDialogTitle">Confirmation dialog title</param>
        /// <param name="undoBlock">Undo block used for editing action</param>
        /// <returns><c>True</c> if planes were destroyed</returns>
        public static bool TryDestroyPreviousPlanes(GameObject environmentRoot, string confirmDestroyDialogTitle, UndoBlock undoBlock)
        {
            // k_PlanesRoots is cleared by GetComponentsInChildren
            environmentRoot.GetComponentsInChildren(k_PlanesRoots);

            var anyChildrenToDestroy = false;
            foreach (var planesRoot in k_PlanesRoots)
            {
                if (planesRoot.transform.childCount > 0)
                {
                    anyChildrenToDestroy = true;
                    break;
                }
            }

            var destroyPlanesRoots = true;
            if (anyChildrenToDestroy)
            {
                destroyPlanesRoots = EditorUtility.DisplayDialog(confirmDestroyDialogTitle,
                    string.Format(k_ConfirmDestroyDialogMessage, nameof(GeneratedPlanesRoot)),
                    k_ConfirmDestroyDialogOk, k_ConfirmDestroyDialogCancel);
            }

            if (destroyPlanesRoots)
            {
                foreach (var planesRoot in k_PlanesRoots)
                {
                    Undo.DestroyObjectImmediate(planesRoot.gameObject);
                }
            }

            k_PlanesRoots.Clear();
            return destroyPlanesRoots;
        }

        /// <summary>
        /// Save all plane data from the current simulation to the synthetic environment
        /// </summary>
        /// <param name="environmentRoot">Root game object for the synthetic environment</param>
        public void SavePlanesFromSimulation(GameObject environmentRoot)
        {
            using (var undoBlock = new UndoBlock("Save Planes From Simulation"))
            {
                k_Planes.Clear();
                this.GetPlanes(k_Planes);
                if (k_Planes.Count == 0)
                {
                    EditorUtility.DisplayDialog(k_SavePlanesDialogTitle, k_NoPlanesDialogMessage, k_NoPlanesDialogOk);
                    return;
                }

                if (!TryDestroyPreviousPlanes(environmentRoot, k_SavePlanesDialogTitle, undoBlock))
                    return;

                CreateGeneratedPlanes(k_Planes, environmentRoot.transform, undoBlock);
            }
        }

        static void CreateGeneratedPlanes(IEnumerable<MRPlane> planes, Transform environmentRoot, UndoBlock undoBlock)
        {
            var planesRoot = CreateGeneratedPlanesRoot(environmentRoot, undoBlock);
            var planesRootTrans = planesRoot.transform;
            var simPlanePrefab = MarsObjectCreationResources.instance.GeneratedSimulatedPlanePrefab;
            foreach (var plane in planes)
            {
                var synthPlane = Object.Instantiate(simPlanePrefab, planesRootTrans);
                synthPlane.transform.SetWorldPose(plane.pose);
                synthPlane.SetMRPlaneData(plane.vertices, plane.center, plane.extents);
            }

            planesRoot.gameObject.SetLayerRecursively(SimulationConstants.SimulatedEnvironmentLayerIndex);
            planesRoot.DisablePlanesScenePicking();
        }

        internal static void CreateGeneratedPlanes(IEnumerable<ExtractedPlaneData> planes, Transform environmentRoot, UndoBlock undoBlock)
        {
            var planesRoot = CreateGeneratedPlanesRoot(environmentRoot, undoBlock);
            var planesRootTrans = planesRoot.transform;
            var simPlanePrefab = MarsObjectCreationResources.instance.GeneratedSimulatedPlanePrefab;
            foreach (var plane in planes)
            {
                var synthPlane = Object.Instantiate(simPlanePrefab, planesRootTrans);
                synthPlane.transform.SetWorldPose(plane.pose);
                synthPlane.SetMRPlaneData(plane.vertices, plane.center, plane.extents);
            }

            planesRoot.gameObject.SetLayerRecursively(SimulationConstants.SimulatedEnvironmentLayerIndex);
            planesRoot.DisablePlanesScenePicking();
        }

        static GeneratedPlanesRoot CreateGeneratedPlanesRoot(Transform parent, UndoBlock undoBlock)
        {
            var planesRootTrans = new GameObject(GeneratedPlanesRoot.PlanesRootName).transform;
            var planesRoot = planesRootTrans.gameObject.AddComponent<GeneratedPlanesRoot>();
            planesRootTrans.SetParent(parent);
            undoBlock.RegisterCreatedObject(planesRootTrans.gameObject);
            return planesRoot;
        }
    }
}
