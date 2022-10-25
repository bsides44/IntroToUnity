using Unity.MARS;
using Unity.MARS.Visualizers;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine;

namespace UnityEditor.MARS
{
    class MeshVisualizerObjectCreationData : ObjectCreationData
    {
        const string k_DefaultPrefabPropertyName = "m_DefaultMeshPrefab";
        const string k_PrefabsPropertyName = "m_ClassifiedMeshPrefabs";
        const string k_MeshPrefabMeshTypePropertyName = "meshType";
        const string k_MeshPrefabPrefabPropertyName = "prefab";

        [SerializeField]
        GameObject m_DefaultPrefab;

        [SerializeField]
        MarsMeshVisualizer.MeshPrefab[] m_Prefabs = new MarsMeshVisualizer.MeshPrefab[0];

        public override bool CreateGameObject(out GameObject createdObj, Transform parentTransform)
        {
            MARSSession.EnsureRuntimeState();

            createdObj = GenerateInitialGameObject(m_ObjectName, parentTransform);

            var meshVisualizer = createdObj.AddComponent<MarsMeshVisualizer>();
            var meshVisualizerSerializedObj = new SerializedObject(meshVisualizer);
            var defaultPrefabProperty = meshVisualizerSerializedObj.FindProperty(k_DefaultPrefabPropertyName);
            defaultPrefabProperty.objectReferenceValue = m_DefaultPrefab;
            var prefabsProperty = meshVisualizerSerializedObj.FindProperty(k_PrefabsPropertyName);
            prefabsProperty.arraySize = m_Prefabs.Length;
            for (var i = 0; i < m_Prefabs.Length; i++)
            {
                var property = prefabsProperty.GetArrayElementAtIndex(i);
                var prefab = m_Prefabs[i];
                property.FindPropertyRelative(k_MeshPrefabMeshTypePropertyName).stringValue = prefab.meshType;
                property.FindPropertyRelative(k_MeshPrefabPrefabPropertyName).objectReferenceValue = prefab.prefab;
            }

            meshVisualizerSerializedObj.ApplyModifiedPropertiesWithoutUndo();

            Undo.RegisterCreatedObjectUndo(createdObj, $"Create {createdObj.name}");
            Selection.activeGameObject = createdObj;

            var simObjectsManager = ModuleLoaderCore.instance.GetModule<SimulatedObjectsManager>();
            simObjectsManager?.DirtySimulatableScene();

            return true;
        }
    }

}
