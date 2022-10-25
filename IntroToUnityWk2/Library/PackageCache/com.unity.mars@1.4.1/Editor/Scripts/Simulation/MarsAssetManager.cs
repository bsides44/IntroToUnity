using System;
using System.Collections.Generic;
using Unity.MARS.Data.Recorded;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace UnityEditor.MARS.Simulation
{
    class MarsAssetManager : ScriptableObject
    {
        [Serializable]
        class AssetList<T> where T : UnityObject
        {
            [SerializeField]
            protected List<T> m_Assets = new List<T>();

            internal List<T> assets => m_Assets;

            internal virtual void Clear()
            {
                m_Assets.Clear();
            }

            internal void Load()
            {
                Clear();

                var guids = AssetDatabase.FindAssets(GetAssetFilter());
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                    if (!Filter(asset))
                        m_Assets.Add(asset);
                }

                OnLoaded();
            }

            internal bool Validate()
            {
                if (IsValid())
                    return true;

                Load();
                return false;
            }

            protected virtual bool Filter(T asset)
            {
                return asset == null;
            }

            protected virtual void OnLoaded()
            { }

            bool IsValid()
            {
                if (m_Assets == null || m_Assets.Count <= 0)
                    return false;

                foreach (var asset in m_Assets)
                {
                    if (asset == null)
                        return false;
                }

                return true;
            }

            string GetAssetFilter()
            {
                return "t:" + typeof(T).Name;
            }
        }

        [Serializable]
        class RecordingSortedList : AssetList<SessionRecordingInfo>
        {
            internal void Sort()
            {
                m_Assets.Sort((a, b) => a.DisplayName.CompareTo(b.DisplayName));
            }

            protected override void OnLoaded()
            {
                Sort();
            }
        }

        [Serializable]
        class IndependentRecordingList : RecordingSortedList
        {
            protected override void OnLoaded()
            {
                Sort();

                var targetIndex = 0;

                // Move synthetic environment recordings
                for (var i = targetIndex; i < m_Assets.Count; i++)
                {
                    var sessionInfo = m_Assets[i];
                    if (!sessionInfo.HasSyntheticEnvironments)
                        continue;

                    m_Assets.RemoveAt(i);
                    m_Assets.Insert(targetIndex, sessionInfo);
                    targetIndex++;
                }

                // Move video recordings
                for (var i = targetIndex; i < m_Assets.Count; i++)
                {
                    var sessionInfo = m_Assets[i];
                    if (!sessionInfo.HasVideo)
                        continue;

                    m_Assets.RemoveAt(i);
                    m_Assets.Insert(targetIndex, sessionInfo);
                    targetIndex++;
                }
            }
        }

        [Serializable]
        class SyntheticRecordingListMap : RecordingSortedList, ISerializationCallbackReceiver
        {
            // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
            static readonly List<GameObject> k_EnvironmentPrefabs = new List<GameObject>();

            [SerializeField]
            List<GameObject> m_SyntheticPrefabs = new List<GameObject>();

            [SerializeField]
            List<RecordingSortedList> m_SyntheticRecordings = new List<RecordingSortedList>();

            readonly Dictionary<GameObject, RecordingSortedList> m_SyntheticRecordingsMap =
                new Dictionary<GameObject, RecordingSortedList>();

            internal override void Clear()
            {
                base.Clear();

                m_SyntheticPrefabs.Clear();
                m_SyntheticRecordings.Clear();
                m_SyntheticRecordingsMap.Clear();
            }

            internal bool TryGetRecordings(GameObject environmentPrefab, out RecordingSortedList recordings)
            {
                return m_SyntheticRecordingsMap.TryGetValue(environmentPrefab, out recordings);
            }

            protected override bool Filter(SessionRecordingInfo asset)
            {
                return base.Filter(asset) || !asset.HasSyntheticEnvironments;
            }

            protected override void OnLoaded()
            {
                base.OnLoaded();

                foreach (var recording in m_Assets)
                {
                    k_EnvironmentPrefabs.Clear();
                    recording.GetSyntheticEnvironments(k_EnvironmentPrefabs);
                    foreach (var environmentPrefab in k_EnvironmentPrefabs)
                    {
                        if (m_SyntheticRecordingsMap.TryGetValue(environmentPrefab, out var recordingList))
                        {
                            recordingList.assets.Add(recording);
                        }
                        else
                        {
                            recordingList = new RecordingSortedList();
                            recordingList.assets.Add(recording);
                            m_SyntheticRecordingsMap[environmentPrefab] = recordingList;
                        }
                    }
                }

                foreach (var kvp in m_SyntheticRecordingsMap)
                    kvp.Value.Sort();

                StoreSyntheticRecordingsMap();
            }

            void ISerializationCallbackReceiver.OnBeforeSerialize()
            {
                StoreSyntheticRecordingsMap();
            }

            void ISerializationCallbackReceiver.OnAfterDeserialize()
            {
                m_SyntheticRecordingsMap.Clear();
                for (var i = 0; i < m_SyntheticPrefabs.Count && i < m_SyntheticRecordings.Count; i++)
                    m_SyntheticRecordingsMap[m_SyntheticPrefabs[i]] = m_SyntheticRecordings[i];
            }

            void StoreSyntheticRecordingsMap()
            {
                m_SyntheticPrefabs.Clear();
                m_SyntheticRecordings.Clear();
                foreach (var kvp in m_SyntheticRecordingsMap)
                {
                    m_SyntheticPrefabs.Add(kvp.Key);
                    m_SyntheticRecordings.Add(kvp.Value);
                }
            }
        }

        static MarsAssetManager s_Instance;

        internal static MarsAssetManager instance
        {
            get
            {
                if (s_Instance == null)
                {
                    var globalSettings = Resources.FindObjectsOfTypeAll<MarsAssetManager>();
                    if (globalSettings.Length > 0)
                        s_Instance = globalSettings[0];
                }

                if (s_Instance == null)
                    s_Instance = CreateInstance<MarsAssetManager>();

                return s_Instance;
            }
        }


#pragma warning disable 649
        [HideInInspector]
        [SerializeField]
        SyntheticRecordingListMap m_SyntheticRecordingListMap = new SyntheticRecordingListMap();

        [HideInInspector]
        [SerializeField]
        IndependentRecordingList m_IndependentRecordingList = new IndependentRecordingList();
#pragma warning restore 649

        internal List<SessionRecordingInfo> SyntheticRecordings => m_SyntheticRecordingListMap.assets;
        internal List<SessionRecordingInfo> IndependentRecordings => m_IndependentRecordingList.assets;

        internal void ReloadRecordings()
        {
            m_SyntheticRecordingListMap.Load();
            m_IndependentRecordingList.Load();
            EditorUtility.SetDirty(this);
        }

        internal void ReloadSyntheticRecordings()
        {
            m_SyntheticRecordingListMap.Load();
            EditorUtility.SetDirty(this);
        }

        internal bool TryGetSyntheticRecordings(GameObject environmentPrefab, out List<SessionRecordingInfo> syntheticRecordings)
        {
            if (m_SyntheticRecordingListMap.TryGetRecordings(environmentPrefab, out var syntheticRecordingList))
            {
                syntheticRecordings = syntheticRecordingList.assets;
                return true;
            }

            syntheticRecordings = null;
            return false;
        }

        internal bool ValidateSyntheticRecordings() => Validate(m_SyntheticRecordingListMap);
        internal bool ValidateIndependentRecordings() => Validate(m_IndependentRecordingList);

        void OnEnable()
        {
            hideFlags = HideFlags.DontSave;
        }

        bool Validate(RecordingSortedList recordingList)
        {
            if (recordingList.Validate())
                return true;

            EditorUtility.SetDirty(this);
            return false;
        }
    }
}
