#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.MARS.Data;
using Unity.MARS.Providers;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace Unity.MARS.XRSubsystem
{
    class MeshSubscriber : IUsesMeshes, IMarsXRSubscriber, IDisposable
    {
        // Used locally to avoid reallocation
        static readonly List<ushort> k_Indices = new List<ushort>();

        readonly Dictionary<MarsTrackableId, List<IDisposable>> m_NativeArrays = new Dictionary<MarsTrackableId, List<IDisposable>>();

        readonly XRMeshSubsystem m_Subsystem;

        IProvidesMeshes IFunctionalitySubscriber<IProvidesMeshes>.provider { get; set; }

        public MeshSubscriber(XRMeshSubsystem subsystem) { m_Subsystem = subsystem; }

        public void SubscribeToEvents()
        {
            this.SubscribeMeshAdded(MeshAddedOrUpdated);
            this.SubscribeMeshUpdated(MeshAddedOrUpdated);
            this.SubscribeMeshRemoved(MeshRemoved);
        }

        public void UnsubscribeFromEvents()
        {
            this.UnsubscribeMeshAdded(MeshAddedOrUpdated);
            this.UnsubscribeMeshUpdated(MeshAddedOrUpdated);
            this.UnsubscribeMeshRemoved(MeshRemoved);
        }

        void MeshAddedOrUpdated(MRMesh mesh)
        {
            var vertices = new NativeArray<Vector3>(mesh.mesh.vertices, Allocator.Persistent);

            // Will be zero length if no normals present
            var normals = new NativeArray<Vector3>(mesh.mesh.normals, Allocator.Persistent);
            IDisposable indicesDisposable;

            if (mesh.mesh.indexFormat == IndexFormat.UInt16)
            {
                mesh.mesh.GetIndices(k_Indices, 0);
                var indices = new NativeArray<ushort>(k_Indices.ToArray(), Allocator.Persistent);
                k_Indices.Clear();
                unsafe
                {
                    AddMesh(mesh.id.subId1,
                        mesh.id.subId2,
                        vertices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(vertices),
                        normals.Length > 0 ? NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(normals) : null,
                        indices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(indices),
                        true
                    );
                }

                indicesDisposable = indices;
            }
            else
            {
                var indices = new NativeArray<int>(mesh.mesh.GetIndices(0), Allocator.Persistent);
                unsafe
                {
                    AddMesh(mesh.id.subId1,
                        mesh.id.subId2,
                        vertices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(vertices),
                        normals.Length > 0 ? NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(normals) : null,
                        indices.Length,
                        NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(indices),
                        false
                    );
                }

                indicesDisposable = indices;
            }

            if (m_NativeArrays.TryGetValue(mesh.id, out var nativeArrays))
                nativeArrays.ForEach(x => x.Dispose());

            m_NativeArrays[mesh.id] = new List<IDisposable> {vertices, normals, indicesDisposable};
        }

        void MeshRemoved(MarsTrackableId id)
        {
            if (m_Subsystem.running)
                RemoveMesh(id.subId1, id.subId2);

            if (m_NativeArrays.TryGetValue(id, out var nativeArrays))
            {
                nativeArrays.ForEach(x => x.Dispose());
                m_NativeArrays.Remove(id);
            }
        }

        public void Dispose()
        {
            foreach (var nativeArrays in m_NativeArrays.Values)
            {
                nativeArrays.ForEach(x => x.Dispose());
            }

            m_NativeArrays.Clear();
        }

        [DllImport("MARSXRSubsystem", EntryPoint = "MARSXRSubsystem_AddOrUpdateMesh")]
        static extern unsafe void AddMesh(ulong id1, ulong id2, int numVertices, void* vertices, void* normals, int numTriangles, void* indices, bool shortIndices);

        [DllImport("MARSXRSubsystem", EntryPoint = "MARSXRSubsystem_RemoveMesh")]
        static extern void RemoveMesh(ulong id1, ulong id2);

        [DllImport("MARSXRSubsystem", EntryPoint = "MARSXRSubsystem_ClearMeshes")]
        static extern void ClearMeshes();
    }
}
#endif
