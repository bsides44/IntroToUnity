using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.XRTools.ModuleLoader;

namespace Unity.MARS.Providers
{
    /// <summary>
    /// Provides access to meshing features
    /// </summary>
    public interface IUsesMeshes : IFunctionalitySubscriber<IProvidesMeshes>
    {
    }

    public static class UsesMeshesMethods
    {
        /// <summary>
        /// Get the currently tracked meshes
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshes">A list of MRMesh objects to which the currently tracked meshes will be added</param>
        public static void GetMeshes(this IUsesMeshes obj, List<MRMesh> meshes)
        {
            obj.provider.GetMeshes(meshes);
        }

        /// <summary>
        /// Subscribe to the meshAdded event, which is called when a mesh becomes tracked for the first time
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshAdded">The delegate to subscribe</param>
        public static void SubscribeMeshAdded(this IUsesMeshes obj, Action<MRMesh> meshAdded)
        {
            obj.provider.MeshAdded += meshAdded;
        }

        /// <summary>
        /// Unsubscribe from the meshAdded event, which is called when a mesh is added for the first time
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshAdded">The delegate to unsubscribe</param>
        public static void UnsubscribeMeshAdded(this IUsesMeshes obj, Action<MRMesh> meshAdded)
        {
            obj.provider.MeshAdded -= meshAdded;
        }

        /// <summary>
        /// Subscribe to the meshUpdated event, which is called when a tracked mesh has new data
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshUpdated">The delegate to subscribe</param>
        public static void SubscribeMeshUpdated(this IUsesMeshes obj, Action<MRMesh> meshUpdated)
        {
            obj.provider.MeshUpdated += meshUpdated;
        }

        /// <summary>
        /// Unsubscribe from the meshUpdated event, which is called when a tracked mesh has new data
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshUpdated">The delegate to unsubscribe</param>
        public static void UnsubscribeMeshUpdated(this IUsesMeshes obj, Action<MRMesh> meshUpdated)
        {
            obj.provider.MeshUpdated -= meshUpdated;
        }

        /// <summary>
        /// Subscribe to the meshRemoved event, which is called when a tracked mesh is removed
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshRemoved">The delegate to subscribe</param>
        public static void SubscribeMeshRemoved(this IUsesMeshes obj, Action<MarsTrackableId> meshRemoved)
        {
            obj.provider.MeshRemoved += meshRemoved;
        }

        /// <summary>
        /// Unsubscribe from the meshRemoved event, which is called when a tracked mesh is removed
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        /// <param name="meshRemoved">The delegate to unsubscribe</param>
        public static void UnsubscribeMeshRemoved(this IUsesMeshes obj, Action<MarsTrackableId> meshRemoved)
        {
            obj.provider.MeshRemoved -= meshRemoved;
        }

        /// <summary>
        /// Stop detecting meshes.   This will happen automatically on destroying the session. It is only necessary to
        /// call this method to pause mesh detection while maintaining camera tracking
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        public static void StopDetectingMeshes(this IUsesMeshes obj)
        {
            obj.provider.StopDetectingMeshes();
        }

        /// <summary>
        /// Start detecting meshes. Mesh detection is enabled on initialization, so this is only necessary after
        /// calling StopDetecting.
        /// </summary>
        /// <param name="obj">The IUsesMeshes instance</param>
        public static void StartDetectingMeshes(this IUsesMeshes obj)
        {
            obj.provider.StartDetectingMeshes();
        }
    }
}
