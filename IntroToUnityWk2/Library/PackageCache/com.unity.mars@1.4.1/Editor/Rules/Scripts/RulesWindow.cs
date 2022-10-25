using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.MARS;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.MARS.Rules.RulesEditor
{
    class RulesWindow : EditorWindow, ISerializationCallbackReceiver
    {
        const string k_RulesWindowName = "Proxy Rule Set";

        internal static Action GUIDispatch;

        static readonly Vector2 k_MinSize = new Vector2(450, 200);

        [SerializeField]
        List<GameObject> m_ExpandedNodes = new List<GameObject>();

        // Serialized MonoBehaviours don't survive playMode state change; its instanceID is stored instead
        [SerializeField]
        int m_SerializedActiveRuleSetID;

        ProxyRuleSet SerializedActiveRuleSet
        {
            get => m_SerializedActiveRuleSetID != 0
                ? EditorUtility.InstanceIDToObject(m_SerializedActiveRuleSetID) as ProxyRuleSet
                : null;
            set => m_SerializedActiveRuleSetID = value != null ? value.GetInstanceID() : 0;
        }

        [MenuItem(MenuConstants.MenuPrefix + k_RulesWindowName, priority = MenuConstants.RulesWindowPriority)]
        public static void Init()
        {
            var win = GetWindow<RulesWindow>();
            win.titleContent = new GUIContent(k_RulesWindowName);
        }

        void OnEnable()
        {
            minSize = k_MinSize;

            RulesModule.RuleSetInstance = SerializedActiveRuleSet;
            RulesDrawer.Init(rootVisualElement);

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = k_RulesWindowName, active = true});

            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        void OnDisable()
        {
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;

            EditorEvents.WindowUsed.Send(new UiComponentArgs { label = k_RulesWindowName, active = false});
        }

        void OnGUI()
        {
            if (!Entitlements.EntitlementsCheckGUI(position.width))
                return;

            if (Event.current.commandName == "ObjectSelectorClosed" && !RulesModule.WasPicked)
                RulesModule.PickedObject = EditorGUIUtility.GetObjectPickerObject();

            RulesDrawer.OnGUI();

            if (GUIDispatch != null)
            {
                GUIDispatch();
                GUIDispatch = null;
            }

            SerializedActiveRuleSet = RulesModule.RuleSetInstanceExisting;
        }

        void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            SetRuleSetInstanceAndRefreshUI();
        }

        void OnSceneClosed(Scene scene)
        {
            SetRuleSetInstanceAndRefreshUI();
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetRuleSetInstanceAndRefreshUI();
        }

        void OnSceneUnloaded(Scene scene)
        {
            SetRuleSetInstanceAndRefreshUI();
        }

        void OnPlayModeStateChanged(PlayModeStateChange playModeState)
        {
            switch (playModeState)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    SetRuleSetInstanceAndRefreshUI();
                    break;
            }
        }

        void OnHierarchyChanged()
        {
            SetRuleSetInstanceAndRefreshUI();
        }

        void SetRuleSetInstanceAndRefreshUI()
        {
            RulesModule.RuleSetInstance = SerializedActiveRuleSet;
            RulesDrawer.BuildReplicatorsList();
        }

        public void OnBeforeSerialize()
        {
            var expandedNodes = RulesModule.CollapsedNodes;
            m_ExpandedNodes.Clear();
            foreach (var node in expandedNodes)
            {
                m_ExpandedNodes.Add(node);
            }
        }

        public void OnAfterDeserialize()
        {
            var expandedNodes = RulesModule.CollapsedNodes;
            expandedNodes.Clear();
            foreach (var node in m_ExpandedNodes)
            {
                expandedNodes.Add(node);
            }
        }
    }
}
