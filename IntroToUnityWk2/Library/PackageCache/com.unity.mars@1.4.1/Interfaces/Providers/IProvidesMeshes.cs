using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.XRTools.ModuleLoader;

namespace Unity.MARS.Providers
{
    /// <summary>
    /// Defines the API for a Mesh Provider
    /// </summary>
    public interface IProvidesMeshes : IFunctionalityProvider
    {
        /// <summary>
        /// Called when a mesh becomes tracked for the first time
        /// </summary>
        event Action<MRMesh> MeshAdded;

        /// <summary>
        /// Called when a tracked mesh has updated data
        /// </summary>
        event Action<MRMesh> MeshUpdated;

        /// <summary>
        /// Called when a tracked mesh is removed
        /// </summary>
        event Action<MarsTrackableId> MeshRemoved;

        /// <summary>
        /// Get the currently tracked meshes
        /// </summary>
        /// <param name="meshes">A list of MRMesh objects to which the currently tracked meshes will be added</param>
        void GetMeshes(List<MRMesh> meshes);

        /// <summary>
        /// Start detecting meshes.  Mesh detection is enabled on initialization, so this is only necessary after
        /// calling StopDetectingMeshes.
        /// </summary>
        void StartDetectingMeshes();

        /// <summary>
        /// Stop detecting meshes
        /// </summary>
        void StopDetectingMeshes();
    }
}
