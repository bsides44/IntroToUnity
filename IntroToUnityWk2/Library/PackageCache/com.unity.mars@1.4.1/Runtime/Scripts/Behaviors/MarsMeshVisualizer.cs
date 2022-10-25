using System;
using System.Collections.Generic;
using System.Linq;
using Unity.MARS.Data;
using Unity.MARS.Providers;
using Unity.MARS.Simulation;
using Unity.XRTools.ModuleLoader;
using Unity.XRTools.Utils;
using UnityEngine;

namespace Unity.MARS.Visualizers
{
    /// <summary>
    /// Creates meshes as you scan your environment.
    /// If your mesh provider also provides mesh classification then different prefabs can be used for different
    /// classes of meshes. By using an occlusion material meshes can be used to occlude other content without
    /// being visible themselves.
    /// </summary>
    public class MarsMeshVisualizer : MonoBehaviour, IUsesMeshes, IUsesCameraOffset, ISimulatable
    {
        [Serializable]
        public struct MeshPrefab
        {
            public string meshType;
            public GameObject prefab;
        }

        IProvidesMeshes IFunctionalitySubscriber<IProvidesMeshes>.provider { get; set; }
        IProvidesCameraOffset IFunctionalitySubscriber<IProvidesCameraOffset>.provider { get; set; }

        [SerializeField]
        [Tooltip("The prefab to be instantiated for each tracked mesh. Must have a Mesh Filter component.")]
        GameObject m_DefaultMeshPrefab;

        [SerializeField]
        [Tooltip("Optional prefabs to be instantiated instead of the default prefab for tracked meshes with specific classification types.")]
        MeshPrefab[] m_ClassifiedMeshPrefabs = new MeshPrefab[0];

        /// <summary>
        /// The prefab to be instantiated for each tracked mesh.
        /// </summary>
        public GameObject DefaultMeshPrefab
        {
            get => m_DefaultMeshPrefab;
            set
            {
                m_DefaultMeshPrefab = value;
                RecreateMeshes();
            }
        }

        /// <summary>
        /// Optional prefabs to be instantiated instead of the default prefab for tracked meshes with specific classification types
        /// </summary>
        public MeshPrefab[] ClassifiedMeshPrefabs
        {
            get => m_ClassifiedMeshPrefabs;
            set
            {
                m_ClassifiedMeshPrefabs = value;
                RecreateMeshes();
            }
        }

        Dictionary<string, GameObject> m_TypePrefabs;
        readonly Dictionary<MarsTrackableId, GameObject> m_Meshes = new Dictionary<MarsTrackableId, GameObject>();

        void OnEnable()
        {
            this.SubscribeMeshAdded(MeshAdded);
            this.SubscribeMeshUpdated(MeshUpdated);
            this.SubscribeMeshRemoved(MeshRemoved);

            CreateMeshes();
        }

        void OnDisable()
        {
            this.UnsubscribeMeshAdded(MeshAdded);
            this.UnsubscribeMeshUpdated(MeshUpdated);
            this.UnsubscribeMeshRemoved(MeshRemoved);

            DestroyMeshes();
        }

        void MeshAdded(MRMesh mesh)
        {
            CreateOrUpdate(mesh);
        }

        void MeshUpdated(MRMesh mesh)
        {
            CreateOrUpdate(mesh);
        }

        void MeshRemoved(MarsTrackableId id)
        {
            if (m_Meshes.TryGetValue(id, out var go))
            {
                UnityObjectUtils.Destroy(go);
                m_Meshes.Remove(id);
            }
        }

        void CreateOrUpdate(MRMesh mesh)
        {
            if (!m_Meshes.TryGetValue(mesh.id, out var go))
            {
                if (!m_TypePrefabs.TryGetValue(mesh.meshType, out var prefab))
                    prefab = m_DefaultMeshPrefab;
                go = GameObjectUtils.Instantiate(prefab, transform);
                m_Meshes.Add(mesh.id, go);
            }

            var goTransform = go.transform;
            var pose = this.ApplyOffsetToPose(mesh.pose);
            goTransform.SetWorldPose(pose);
            goTransform.localScale = Vector3.one * this.GetCameraScale();
            var meshFilter = go.GetComponentInChildren<MeshFilter>();
            meshFilter.sharedMesh = mesh.mesh;
        }

        void CreateMeshes()
        {
            m_TypePrefabs = m_ClassifiedMeshPrefabs.ToDictionary(x => x.meshType, x => x.prefab);

            var meshes = new List<MRMesh>();
            this.GetMeshes(meshes);
            foreach (var mesh in meshes)
            {
                CreateOrUpdate(mesh);
            }
        }

        void DestroyMeshes()
        {
            foreach (var pair in m_Meshes)
            {
                var go = pair.Value;
                UnityObjectUtils.Destroy(go);
            }

            m_Meshes.Clear();
        }

        void RecreateMeshes()
        {
            DestroyMeshes();
            CreateMeshes();
        }
    }
}
