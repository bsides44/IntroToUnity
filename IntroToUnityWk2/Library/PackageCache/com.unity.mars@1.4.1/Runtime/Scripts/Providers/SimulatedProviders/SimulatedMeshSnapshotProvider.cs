using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.MARS.Simulation;
using Unity.MARS.Simulation.Rendering;
using Unity.XRTools.ModuleLoader;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.MARS.Providers.Synthetic
{
    /// <summary>
    /// Provides a snapshot of mesh data from the entire simulated environment
    /// </summary>
    [AddComponentMenu("")]
    [ProviderSelectionOptions(ProviderPriorities.SimulatedProviderPriority)]
    class SimulatedMeshSnapshotProvider : SimulatedDataProvider, IProvidesMeshes
    {
        /// <summary>
        /// Shader name must include all these keywords to be considered an X-Ray shader
        /// </summary>
        static readonly string[] k_XRayShaderNameKeywords = { "MARS", "Room X-Ray" };

        List<MRMesh> m_Meshes = new List<MRMesh>();

        static readonly List<MeshFilter> k_MeshFilters = new List<MeshFilter>();
        static readonly List<GeneratedPlanesRoot> k_PlanesRoots = new List<GeneratedPlanesRoot>();
        static readonly List<MeshFilter> k_PlaneMeshFilters = new List<MeshFilter>();
        static readonly List<CombineInstance> k_CombineInstances = new List<CombineInstance>();
        static readonly List<CombineInstance> k_XRayCombineInstances = new List<CombineInstance>();
        static readonly Dictionary<string, List<MeshFilter>> k_MeshesByClassification = new Dictionary<string, List<MeshFilter>>();
        static readonly List<MeshFilter> k_UnclassifiedMeshFilters = new List<MeshFilter>();

#pragma warning disable 67
        public event Action<MRMesh> MeshAdded;
        public event Action<MRMesh> MeshUpdated;
        public event Action<MarsTrackableId> MeshRemoved;
#pragma warning restore 67

        void IFunctionalityProvider.LoadProvider()
        {
        }

        void IFunctionalityProvider.ConnectSubscriber(object obj)
        {
            this.TryConnectSubscriber<IProvidesMeshes>(obj);
        }

        void IFunctionalityProvider.UnloadProvider()
        {
        }

        public void GetMeshes(List<MRMesh> meshes)
        {
            meshes.AddRange(m_Meshes);
        }

        public void StartDetectingMeshes()
        {
        }

        public void StopDetectingMeshes()
        {
        }

        protected override void OnEnvironmentReady(GameObject environmentRoot)
        {
            RemoveMeshes();

            // Mesh data can only be accessed in the player loop if the mesh has read/write enabled, but it can
            // always be accessed from Editor code. So in Editor play mode we combine meshes in a delayCall.
#if UNITY_EDITOR
            if (Application.isPlaying)
                EditorApplication.delayCall += () => AddMeshes(environmentRoot);
            else
                AddMeshes(environmentRoot);
#else
            AddMeshes(environmentRoot);
#endif
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            RemoveMeshes();
        }

        void AddMeshes(GameObject environmentRoot)
        {
            m_Meshes.Clear();

            environmentRoot.GetComponentsInChildren(k_MeshFilters);
            environmentRoot.GetComponentsInChildren(k_PlanesRoots);
            foreach (var planesRoot in k_PlanesRoots)
            {
                planesRoot.GetComponentsInChildren(k_PlaneMeshFilters);
                foreach (var planeMeshFilter in k_PlaneMeshFilters)
                {
                    k_MeshFilters.Remove(planeMeshFilter);
                }

                k_PlaneMeshFilters.Clear();
            }

            k_MeshesByClassification.Clear();
            k_UnclassifiedMeshFilters.Clear();
            foreach (var meshFilter in k_MeshFilters)
            {
                var classification = meshFilter.GetComponentInParent<SimulatedMeshClassification>();
                if (classification == null || string.IsNullOrEmpty(classification.ClassificationType))
                {
                    k_UnclassifiedMeshFilters.Add(meshFilter);
                }
                else
                {
                    var classificationType = classification.ClassificationType;
                    if (!k_MeshesByClassification.TryGetValue(classificationType, out var classifiedMeshFilters))
                    {
                        classifiedMeshFilters = new List<MeshFilter>();
                        k_MeshesByClassification[classificationType] = classifiedMeshFilters;
                    }

                    classifiedMeshFilters.Add(meshFilter);
                }
            }

            var environmentWorldToLocal = environmentRoot.transform.worldToLocalMatrix;
            var meshingSettings = SimulatedMeshingSettings.instance;
            XRayRuntimeUtils.XRayRegions.TryGetValue(environmentRoot.scene, out var activeXRay);
            AddMeshesWithClassification(k_UnclassifiedMeshFilters, environmentWorldToLocal, activeXRay, "", meshingSettings);
            foreach (var kvp in k_MeshesByClassification)
            {
                var classification = kvp.Key;
                var meshFilters = kvp.Value;
                AddMeshesWithClassification(meshFilters, environmentWorldToLocal, activeXRay, classification, meshingSettings);
            }
        }

        void AddMeshesWithClassification(List<MeshFilter> meshFilters, Matrix4x4 environmentWorldToLocal, XRayRegion activeXRay,
            string classification, SimulatedMeshingSettings meshingSettings)
        {
            var meshIndexFormat = meshingSettings.IndexFormat;
            var ignoreOutwardXRayGeometry = meshingSettings.ExcludeExteriorXRayGeometry;
            var vertexCap = meshIndexFormat == IndexFormat.UInt16 ? ushort.MaxValue : uint.MaxValue;
            var meshFilterCount = meshFilters.Count;
            k_CombineInstances.Clear();
            k_XRayCombineInstances.Clear();
            var combineVertexCount = 0;
            var xRayCombineVertexCount = 0;
            var checkXRay = ignoreOutwardXRayGeometry && activeXRay != null && k_XRayShaderNameKeywords.Length > 0;
            for (var i = 0; i < meshFilterCount; i++)
            {
                var meshFilter = meshFilters[i];
                var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
                var meshMaterials = meshRenderer != null ? meshRenderer.sharedMaterials : null;
                var mesh = meshFilter.sharedMesh;
                var subMeshCount = mesh.subMeshCount;
                var combineTransform = environmentWorldToLocal * meshFilter.transform.localToWorldMatrix;
                for (var subIndex = 0; subIndex < subMeshCount; subIndex++)
                {
                    var usesXRay = false;
                    if (checkXRay && meshMaterials != null && meshMaterials.Length > subIndex)
                    {
                        var material = meshMaterials[subIndex];
                        var shaderName = material.shader.name;
                        usesXRay = true;
                        foreach (var keyword in k_XRayShaderNameKeywords)
                        {
                            if (!shaderName.Contains(keyword))
                            {
                                usesXRay = false;
                                break;
                            }
                        }
                    }

                    var combineInstances = usesXRay ? k_XRayCombineInstances : k_CombineInstances;
                    var totalVertexCount = usesXRay ? xRayCombineVertexCount : combineVertexCount;
                    var subVertexCount = mesh.GetSubMesh(subIndex).vertexCount;
                    var newVertexCount = totalVertexCount + subVertexCount;
                    if (newVertexCount > vertexCap || newVertexCount < totalVertexCount) // Check for overflow
                    {
                        var combinedMesh = CreateMeshFromCombineList(combineInstances, meshIndexFormat);
                        if (usesXRay)
                            RemoveOuterXRayGeometry(combinedMesh, activeXRay, environmentWorldToLocal);

                        AddMRMesh(combinedMesh, classification);
                        combineInstances.Clear();
                        newVertexCount = subVertexCount;
                    }

                    if (usesXRay)
                        xRayCombineVertexCount = newVertexCount;
                    else
                        combineVertexCount = newVertexCount;

                    combineInstances.Add(new CombineInstance
                    {
                        mesh = mesh,
                        subMeshIndex = subIndex,
                        transform = combineTransform
                    });
                }
            }

            if (k_CombineInstances.Count > 0)
                AddMRMesh(CreateMeshFromCombineList(k_CombineInstances, meshIndexFormat), classification);

            if (checkXRay && k_XRayCombineInstances.Count > 0)
            {
                var xRayMesh = CreateMeshFromCombineList(k_XRayCombineInstances, meshIndexFormat);
                RemoveOuterXRayGeometry(xRayMesh, activeXRay, environmentWorldToLocal);
                AddMRMesh(xRayMesh, classification);
            }
        }

        static Mesh CreateMeshFromCombineList(List<CombineInstance> combineInstancesList, IndexFormat meshIndexFormat)
        {
            var mesh = new Mesh
            {
                indexFormat = meshIndexFormat
            };

            mesh.CombineMeshes(combineInstancesList.ToArray());
            return mesh;
        }

        static void RemoveOuterXRayGeometry(Mesh mesh, XRayRegion xRayRegion, Matrix4x4 environmentWorldToLocal)
        {
            for (var i = 0; i < mesh.subMeshCount; i++)
            {
                Assert.AreEqual(MeshTopology.Triangles, mesh.GetTopology(i), "Simulated mesh is not made from triangles");
            }

            Vector3 xRayRegionCenter = environmentWorldToLocal * xRayRegion.transform.position;
            var clipOffset = xRayRegion.ClipOffset;
            var floorHeight = xRayRegion.FloorHeight;
            var ceilingHeight = xRayRegion.CeilingHeight;
            var interiorMin = new Vector3(xRayRegionCenter.x - clipOffset, xRayRegionCenter.y + floorHeight, xRayRegionCenter.z - clipOffset);
            var interiorMax = new Vector3(xRayRegionCenter.x + clipOffset, xRayRegionCenter.y + ceilingHeight, xRayRegionCenter.z + clipOffset);
            var vertexCount = mesh.vertexCount;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var removeVertices = new bool[vertexCount];
            for (var i = 0; i < vertexCount; i++)
            {
                // Mesh is at the origin of the environment so no need to transform vertex positions
                var vertex = vertices[i];
                var normal = normals[i];

                // Find out which axis the vertex normal is most aligned with
                var maxNormalIndex = 0;
                var maxNormalValue = normal.x;
                if (normal.y > normal.x && normal.y > normal.z)
                {
                    maxNormalIndex = 1;
                    maxNormalValue = normal.y;
                }
                else if (normal.z > normal.x)
                {
                    maxNormalIndex = 2;
                    maxNormalValue = normal.z;
                }

                var minNormalIndex = 0;
                var minNormalValue = normal.x;
                if (normal.y < normal.x && normal.y < normal.z)
                {
                    minNormalIndex = 1;
                    minNormalValue = normal.y;
                }
                else if (normal.z < normal.x)
                {
                    minNormalIndex = 2;
                    minNormalValue = normal.z;
                }

                // Remove vertex if it is outside the interior along the aligned axis
                if (Mathf.Abs(minNormalValue) > Mathf.Abs(maxNormalValue))
                {
                    removeVertices[i] = vertex[minNormalIndex] < interiorMin[minNormalIndex];
                }
                else
                {
                    removeVertices[i] = vertex[maxNormalIndex] > interiorMax[maxNormalIndex];
                }
            }

            var triangles = mesh.triangles;
            var newTrianglesList = new List<int>();
            for (var i = 0; i < triangles.Length; i += 3)
            {
                var index0 = triangles[i];
                var index1 = triangles[i + 1];
                var index2 = triangles[i + 2];
                if (removeVertices[index0] || removeVertices[index1] || removeVertices[index2])
                    continue;

                newTrianglesList.Add(index0);
                newTrianglesList.Add(index1);
                newTrianglesList.Add(index2);
            }

            mesh.triangles = newTrianglesList.ToArray();
        }

        void AddMRMesh(Mesh mesh, string classification)
        {
            var mrMesh = new MRMesh
            {
                id = MarsTrackableId.Create(),
                meshType = classification,
                pose = Pose.identity,
                mesh = mesh
            };

            m_Meshes.Add(mrMesh);
            MeshAdded?.Invoke(mrMesh);
        }

        void RemoveMeshes()
        {
            foreach (var mesh in m_Meshes)
            {
                MeshRemoved?.Invoke(mesh.id);
            }

            m_Meshes.Clear();
        }
    }
}
