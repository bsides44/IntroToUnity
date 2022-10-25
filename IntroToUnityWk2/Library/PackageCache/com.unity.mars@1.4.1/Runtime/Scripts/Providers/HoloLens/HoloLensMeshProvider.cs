#if UNITY_WSA && INCLUDE_MSFT_MR_OPENXR && INCLUDE_MSFT_MR_SCENEUNDERSTANDING
using Microsoft.MixedReality.SceneUnderstanding;
using Microsoft.MixedReality.OpenXR;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.MARS.Data;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using System.Threading;

namespace Unity.MARS.Providers.HoloLens
{
    [ProviderSelectionOptions(excludedPlatforms: new[]{
        RuntimePlatform.WindowsEditor,
        RuntimePlatform.OSXEditor,
        RuntimePlatform.LinuxEditor,
        RuntimePlatform.WindowsPlayer,
        RuntimePlatform.OSXPlayer,
        RuntimePlatform.LinuxPlayer,
    },
    priority: ProviderPriorities.PreferredPriority)]
    class HoloLensMeshProvider : IProvidesMeshes
    {
        struct SceneUnderstandingMesh
        {
            public Pose pose;
            public Vector3[] vertices;
            public int[] triangles;
            public SceneObjectKind kind;
            public bool unchanged;
        }

        public event Action<MRMesh> MeshAdded;
        public event Action<MRMesh> MeshUpdated;
        public event Action<MarsTrackableId> MeshRemoved;

        readonly Dictionary<Guid, MRMesh> m_Meshes = new Dictionary<Guid, MRMesh>();
        readonly Dictionary<Guid, int> m_MeshHashes = new Dictionary<Guid, int>();
        CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

        void IFunctionalityProvider.LoadProvider()
        {
            StartDetectingMeshes();
        }

        void IFunctionalityProvider.ConnectSubscriber(object obj)
        {
            this.TryConnectSubscriber<IProvidesMeshes>(obj);
        }

        void IFunctionalityProvider.UnloadProvider() { }

        public void GetMeshes(List<MRMesh> meshes)
        {
            meshes.AddRange(m_Meshes.Values);
        }

        public void StartDetectingMeshes()
        {
            ProcessMeshes();
        }

        public void StopDetectingMeshes()
        {
            m_CancellationTokenSource.Cancel();
        }

        async void ProcessMeshes()
        {
            var querySettings = new SceneQuerySettings {
                EnableSceneObjectMeshes = HoloLensMeshProviderOptions.instance.UseSceneUnderstanding,
                EnableSceneObjectQuads = false,
                EnableWorldMesh = !HoloLensMeshProviderOptions.instance.UseSceneUnderstanding,
                EnableOnlyObservedSceneObjects = false,
                RequestedMeshLevelOfDetail = (SceneMeshLevelOfDetail)HoloLensMeshProviderOptions.instance.LevelOfDetail,
            };

            var boundingRadius = HoloLensMeshProviderOptions.instance.BoundingRadius;
            var refreshInterval = (int)(HoloLensMeshProviderOptions.instance.RefreshInterval * 1000f);
            var token = m_CancellationTokenSource.Token;
            var status = await SceneObserver.RequestAccessAsync();
            if (status != SceneObserverAccessStatus.Allowed)
            {
                Debug.LogWarningFormat("Scene Understanding permission denied: {0}", status.ToString());
                return;
            }

            Scene scene = null;
            while (!token.IsCancellationRequested)
            {
                // Run Scene Understanding and copying from a threadpool task
                if (scene != null)
                    scene = await Task.Run(() => SceneObserver.ComputeAsync(querySettings, boundingRadius, scene));
                else
                    scene = await Task.Run(() => SceneObserver.ComputeAsync(querySettings, boundingRadius));
                var meshes = await Task.Run(() => ProcessSceneUnderstandingMeshes(scene));

                // Do Unity stuff
                foreach (var mesh in meshes)
                {
                    if (mesh.Value.unchanged)
                        continue;

                    var newMesh = false;
                    if (!m_Meshes.TryGetValue(mesh.Key, out var mrMesh))
                    {
                        newMesh = true;
                        mrMesh = new MRMesh {
                            id = MarsTrackableId.Create(),
                            pose = mesh.Value.pose,
                            meshType = mesh.Value.kind.ToString().ToLower(),
                            mesh = new Mesh(),
                        };
                        m_Meshes.Add(mesh.Key, mrMesh);
                    }
                    mrMesh.mesh.SetVertices(mesh.Value.vertices);
                    mrMesh.mesh.SetTriangles(mesh.Value.triangles, 0);
                    mrMesh.mesh.RecalculateNormals();

                    if (newMesh)
                        MeshAdded?.Invoke(mrMesh);
                    else
                        MeshUpdated?.Invoke(mrMesh);
                }

                var removed = m_Meshes.Keys.Except(meshes.Keys).ToList();
                foreach (var guid in removed)
                {
                    MeshRemoved?.Invoke(m_Meshes[guid].id);
                    m_Meshes.Remove(guid);
                }

                if (refreshInterval < 0f)
                    break;

                await Task.Delay(refreshInterval);
            }
        }

        // Do as much as we can to convert from SceneUnderstanding Scene to Unity friendly data while running off
        // main Unity thread.
        Dictionary<Guid, SceneUnderstandingMesh> ProcessSceneUnderstandingMeshes(Scene scene)
        {
            var sceneMatrix = System.Numerics.Matrix4x4.Identity;
            SpatialGraphNode sgn = SpatialGraphNode.FromStaticNodeId(scene.OriginSpatialGraphNodeId);
            if (sgn.TryLocate(FrameTime.OnUpdate, out var scenePose))
                sceneMatrix = Matrix4x4.TRS(scenePose.position, scenePose.rotation, Vector3.one).ToSystemNumerics();

            var result = new Dictionary<Guid, SceneUnderstandingMesh>();

            foreach (var sceneObject in scene.SceneObjects)
            {
                if (sceneObject.Meshes.Count == 0)
                    continue;

                var sceneUnderstandingMesh = new SceneUnderstandingMesh();

                var matrix = sceneObject.GetLocationAsMatrix() * sceneMatrix;
                System.Numerics.Matrix4x4.Decompose(matrix, out _, out var rotation, out var position);
                sceneUnderstandingMesh.pose = new Pose(new Vector3(position.X, position.Y, -position.Z),
                    new Quaternion(-rotation.X, -rotation.Y, rotation.Z, rotation.W));
                sceneUnderstandingMesh.kind = sceneObject.Kind;

                foreach (var mesh in sceneObject.Meshes)
                {
                    var hash = mesh.GetHashCode();
                    if (m_MeshHashes.TryGetValue(mesh.Id, out var oldHash) && oldHash == hash)
                    {
                        // Mesh is unchanged
                        result.Add(mesh.Id, new SceneUnderstandingMesh { unchanged = true });
                        continue;
                    }
                    m_MeshHashes[mesh.Id] = hash;

                    var vertices = new System.Numerics.Vector3[mesh.VertexCount];
                    mesh.GetVertexPositions(vertices);

                    var unityVertices = new Vector3[mesh.VertexCount];
                    for (int i = 0; i < mesh.VertexCount; i++)
                    {
                        var v = vertices[i];
                        unityVertices[i] = new Vector3(v.X, v.Y, -v.Z);
                    }
                    sceneUnderstandingMesh.vertices = unityVertices;

                    var indices = new uint[mesh.TriangleIndexCount];
                    mesh.GetTriangleIndices(indices);
                    var unityIndices = new int[mesh.TriangleIndexCount];
                    for (int i = 0; i < mesh.TriangleIndexCount; i++)
                    {
                        unityIndices[i] = (int)indices[i];
                    }
                    sceneUnderstandingMesh.triangles = unityIndices;

                    result.Add(mesh.Id, sceneUnderstandingMesh);
                }
            }

            return result;
        }
    }

    static class MatrixExtensions
    {
        public static System.Numerics.Matrix4x4 ToSystemNumerics(this Matrix4x4 m) => new System.Numerics.Matrix4x4(
             m.m00, m.m10, -m.m20, m.m30,
             m.m01, m.m11, -m.m21, m.m31,
            -m.m02, -m.m12, m.m22, -m.m32,
             m.m03, m.m13, -m.m23, m.m33);
    }
}
#endif
