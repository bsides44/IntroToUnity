using System;
using UnityEngine;

namespace Unity.MARS.Data
{
    /// <summary>
    /// Provides a template for tracked mesh data
    /// </summary>
    [Serializable]
    public struct MRMesh : IMRTrackable
    {
        [SerializeField]
        [Tooltip("The id of this mesh as determined by the provider")]
        MarsTrackableId m_ID;

        [SerializeField]
        [Tooltip("The classification type of this mesh")]
        string m_MeshType;

        [SerializeField]
        [Tooltip("The pose of this mesh")]
        Pose m_Pose;

        Mesh m_Mesh;

        /// <summary>
        /// The id of this mesh as determined by the provider
        /// </summary>
        public MarsTrackableId id
        {
            get { return m_ID; }
            set { m_ID = value; }
        }

        /// <summary>
        /// The classification type of this mesh
        /// </summary>
        public string meshType
        {
            get { return m_MeshType; }
            set { m_MeshType = value; }
        }

        /// <summary>
        /// The pose of this mesh
        /// </summary>
        public Pose pose
        {
            get { return m_Pose; }
            set { m_Pose = value; }
        }

        /// <summary>
        /// Mesh object with the vertex and triangle data for the mesh
        /// </summary>
        public Mesh mesh
        {
            get { return m_Mesh; }
            set { m_Mesh = value; }
        }
    }
}
