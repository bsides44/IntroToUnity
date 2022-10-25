#if ARFOUNDATION_2_1_OR_NEWER
using System;
using System.Collections.Generic;
using Unity.MARS.Data;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityObject = UnityEngine.Object;

#if !ARFOUNDATION_4_OR_NEWER
using System.Text.RegularExpressions;
#endif

#if UNITY_IOS
using System.Linq;
using Unity.Collections;
using UnityEngine.XR.ARKit;
#endif

namespace Unity.MARS.Providers.ARFoundation
{
    [ProviderSelectionOptions(excludedPlatforms: new[]
    {
        RuntimePlatform.WindowsEditor,
        RuntimePlatform.OSXEditor,
        RuntimePlatform.LinuxEditor,
        RuntimePlatform.WindowsPlayer,
        RuntimePlatform.OSXPlayer,
        RuntimePlatform.LinuxPlayer,
    })]
    class ARFoundationMeshProvider : IProvidesMeshes
    {
#if !ARFOUNDATION_4_OR_NEWER
        static readonly Regex s_TrackableIdRegex = new Regex(@"^(?<part1>[a-fA-F\d]{16})-(?<part2>[a-fA-F\d]{16})$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
#endif

#if UNITY_IOS
        static readonly int k_NumClassifications = Enum.GetNames(typeof(ARMeshClassification)).Length;
        static readonly string[] k_MeshTypeNames = Enum.GetNames(typeof(ARMeshClassification)).Select(s => s.ToLower()).ToArray();
        readonly int[] m_ClassificationCounts = new int[k_NumClassifications];
#endif

        bool m_MeshSubsystemAvailable;
        ARMeshManager m_ARMeshManager;
        bool m_TempARMeshManager;

        public event Action<MRMesh> MeshAdded;
        public event Action<MRMesh> MeshUpdated;
        public event Action<MarsTrackableId> MeshRemoved;

        readonly Dictionary<TrackableId, Dictionary<int, MRMesh>> m_Meshes = new Dictionary<TrackableId, Dictionary<int, MRMesh>>();

#if UNITY_IOS
        readonly List<int> m_BaseTriangles = new List<int>();
        readonly List<int> m_ClassifiedTriangles = new List<int>();
#endif

        void IFunctionalityProvider.LoadProvider()
        {
#if ARFOUNDATION_4_OR_NEWER
            m_MeshSubsystemAvailable = LoaderUtility.GetActiveLoader().GetLoadedSubsystem<XRMeshSubsystem>() != null;
            if (!m_MeshSubsystemAvailable)
                return;
#endif

            ARFoundationSessionProvider.RequireARSession();

            var currentSession = ARFoundationSessionProvider.currentSession;
            if (currentSession)
            {
                m_ARMeshManager = UnityObject.FindObjectOfType<ARMeshManager>();
                if (!m_ARMeshManager)
                {
                    var origin = UnityObject.FindObjectOfType<ARSessionOrigin>();
                    if (origin == null)
                        origin = currentSession.gameObject.AddComponent<ARSessionOrigin>();
                    var managerGo = new GameObject("MeshManager");
                    managerGo.transform.SetParent(origin.transform);
                    m_ARMeshManager = managerGo.AddComponent<ARMeshManager>();
                    m_ARMeshManager.hideFlags = HideFlags.DontSave;
                    m_TempARMeshManager = true;
                }

#if !ARFOUNDATION_4_OR_NEWER
                m_MeshSubsystemAvailable = m_ARMeshManager.subsystem != null;
                if (!m_MeshSubsystemAvailable)
                    return;
#endif

                var meshFilterGo = new GameObject();
                var meshFilter = meshFilterGo.AddComponent<MeshFilter>();
                m_ARMeshManager.meshPrefab = meshFilter;

                AddExistingTrackables();
                m_ARMeshManager.meshesChanged += ARMeshManagerOnMeshesChanged;
                StartDetectingMeshes();
            }
        }

        void IFunctionalityProvider.ConnectSubscriber(object obj) { this.TryConnectSubscriber<IProvidesMeshes>(obj); }

        void IFunctionalityProvider.UnloadProvider()
        {
            if (!m_MeshSubsystemAvailable)
                return;

            StopDetectingMeshes();
            if (m_ARMeshManager)
            {
                m_ARMeshManager.meshesChanged -= ARMeshManagerOnMeshesChanged;
                if (m_TempARMeshManager)
                    UnityObjectUtils.Destroy(m_ARMeshManager.gameObject);
            }

            ARFoundationSessionProvider.TearDownARSession();
        }

        public void GetMeshes(List<MRMesh> meshes)
        {
            if (!m_MeshSubsystemAvailable)
                return;

#if UNITY_IOS
            var frackedMeshes = new Dictionary<int, MRMesh>();
            foreach (var baseMesh in m_ARMeshManager.meshes)
            {
                var id = ParseTrackableId(baseMesh.mesh.name);
                AddOrUpdateARKitMeshes(id, baseMesh.mesh, frackedMeshes, false);
                meshes.AddRange(frackedMeshes.Values);
                frackedMeshes.Clear();
            }
#else
            foreach (var baseMesh in m_ARMeshManager.meshes)
            {
                var id = ParseTrackableId(baseMesh.mesh.name);
                meshes.Add(new MRMesh {
                    id = new MarsTrackableId(id.subId1, id.subId2),
                    meshType = null,
                    mesh = baseMesh.mesh,
                });
            }
#endif
        }

        public void StartDetectingMeshes()
        {
            if (!m_MeshSubsystemAvailable)
                return;

            m_ARMeshManager.density = ARFoundationMeshProviderOptions.instance.MeshDensity;
#if UNITY_IOS
            m_ARMeshManager.subsystem.SetClassificationEnabled(true);
#endif
            m_ARMeshManager.subsystem.Start();
        }

        public void StopDetectingMeshes()
        {
            if (m_ARMeshManager)
                m_ARMeshManager.subsystem.Stop();
        }

        void AddExistingTrackables()
        {
            if (m_ARMeshManager == null)
                return;

            foreach (var mesh in m_ARMeshManager.meshes)
            {
                TryAddOrUpdateMesh(mesh);
            }
        }

        void ARMeshManagerOnMeshesChanged(ARMeshesChangedEventArgs args)
        {
            args.added.ForEach(TryAddOrUpdateMesh);
            args.updated.ForEach(TryAddOrUpdateMesh);
            args.removed.ForEach(RemoveMesh);
        }

        void TryAddOrUpdateMesh(MeshFilter mesh)
        {
            var id = ParseTrackableId(mesh.sharedMesh.name);
            if (!m_Meshes.TryGetValue(id, out var meshes))
            {
                meshes = new Dictionary<int, MRMesh>();
                m_Meshes.Add(id, meshes);
            }
#if UNITY_IOS
            AddOrUpdateARKitMeshes(id, mesh.sharedMesh, meshes, true);
#else
            AddOrUpdateMeshes(id, mesh.sharedMesh, meshes);
#endif
        }

        void RemoveMesh(MeshFilter meshFilter)
        {
            var id = ParseTrackableId(meshFilter.name);
            if (m_Meshes.TryGetValue(id, out var meshes))
            {
                foreach (var mesh in meshes.Values)
                {
                    MeshRemoved?.Invoke(mesh.id);
                }

                m_Meshes.Remove(id);
            }
        }

#if UNITY_IOS

        // Splits the mesh into multiple meshes by face classifications
        void AddOrUpdateARKitMeshes(TrackableId id, Mesh baseMesh, Dictionary<int, MRMesh> meshes, bool notify)
        {
            Array.Clear(m_ClassificationCounts, 0, k_NumClassifications);
            var classifications = m_ARMeshManager.subsystem.GetFaceClassifications(id, Allocator.Persistent);
            for (var i = 0; i < classifications.Length; i++)
            {
                m_ClassificationCounts[(int) classifications[i]]++;
            }

            baseMesh.GetTriangles(m_BaseTriangles, 0);
            for (var i = 0; i < k_NumClassifications; i++)
            {
                var faceCount = m_ClassificationCounts[i];
                if (faceCount == 0)
                    continue;

                var newMesh = false;
                if (!meshes.TryGetValue(i, out var mesh))
                {
                    newMesh = true;
                    mesh = new MRMesh
                    {
                        id = MarsTrackableId.Create(),
                        pose = Pose.identity,
                        meshType = k_MeshTypeNames[i],
                        mesh = new Mesh(),
                    };
                    meshes.Add(i, mesh);
                }
                else
                    mesh.mesh.Clear();

                m_ClassifiedTriangles.Clear();
                m_ClassifiedTriangles.Capacity = faceCount * 3;
                for (var j = 0; j < classifications.Length; j++)
                {
                    if ((int) classifications[j] == i)
                    {
                        m_ClassifiedTriangles.Add(m_BaseTriangles[j * 3]);
                        m_ClassifiedTriangles.Add(m_BaseTriangles[j * 3 + 1]);
                        m_ClassifiedTriangles.Add(m_BaseTriangles[j * 3 + 2]);
                    }
                }

                mesh.mesh.vertices = baseMesh.vertices;
                mesh.mesh.normals = baseMesh.normals;
                mesh.mesh.SetTriangles(m_ClassifiedTriangles, 0);

                if (notify)
                {
                    if (newMesh)
                        MeshAdded?.Invoke(mesh);
                    else
                        MeshUpdated?.Invoke(mesh);
                }
            }
        }
#else
        void AddOrUpdateMeshes(TrackableId id, Mesh baseMesh, Dictionary<int, MRMesh> meshes)
        {
            // Without classifications each mesh gets type None
            if (!meshes.TryGetValue(0, out var mesh))
            {
                mesh = new MRMesh {
                    id = new MarsTrackableId(id.subId1, id.subId2),
                    pose = Pose.identity,
                    meshType = null,
                    mesh = baseMesh,
                };
                meshes.Add(0, mesh);
                MeshAdded?.Invoke(mesh);
            }
            else
            {
                mesh.mesh = baseMesh;
                MeshUpdated?.Invoke(mesh);
            }
        }
#endif

        static TrackableId ParseTrackableId(string s)
        {
            var idString = s.Split(' ')[1];
#if ARFOUNDATION_4_OR_NEWER
            return new TrackableId(idString);
#else
            var match = s_TrackableIdRegex.Match(idString);
            if (!match.Success)
                throw new FormatException($"trackable ID '{s}' does not match expected format");

            var subId1 = ulong.Parse(match.Groups["part1"].Value, System.Globalization.NumberStyles.HexNumber);
            var subId2 = ulong.Parse(match.Groups["part2"].Value, System.Globalization.NumberStyles.HexNumber);
            return new TrackableId(subId1, subId2);
#endif
        }
    }
}
#endif
