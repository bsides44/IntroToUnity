using System;
using System.Collections.Generic;
using Unity.MARS.Behaviors;
using Unity.MARS.Forces;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.Rules.RulesEditor
{
    [CustomRuleNode(typeof(Transform))]
    class ContentRow : RuleNode
    {
        const string k_ContentRowPath = k_Directory + "ContentRow.uxml";

        const string k_ContentObjectFieldName = "contentObjectField";
        const string k_LandmarkButtonName = "landmarkButton";
        const string k_ContentTitleName = "contentTitle";
        const string k_ObjectSetupAreaName = "objectSetupArea";

        const string k_AddButtonContentAnalyticsLabel = "Margin add button (Content row)";
        const string k_AddLandmarkButtonAnalyticsLabel = "Add landmark button";

        ActionRow[] m_ActionRows;
        Transform m_ContainerTransform;

        protected ObjectField ContentObjectField;
        protected Transform ContentTransform;

        internal override Transform ContainerObject => ContentTransform.parent;
        internal override GameObject BackingObject => ContentTransform.gameObject;
        internal bool ContentSupportsActions { get; set; }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<ContentAction> k_TempActions = new List<ContentAction>();

        protected override void OnInit()
        {
            ContentTransform = (Transform)Target;
            m_ContainerTransform = ContentTransform.parent;
            ContentSupportsActions = true;

            SetupUI();
        }

        public static bool IsApplicableToTransform(Transform t)
        {
            return !ProxyForces.IsForceRegion(t);
        }

        internal bool HasChanged(Transform content)
        {
            if (content != ContentTransform)
                return true;

            k_TempActions.Clear();
            GetActionCandidates(k_TempActions);
            if (m_ActionRows.Length != k_TempActions.Count)
                return true;

            for (var i = 0; i < m_ActionRows.Length; i++)
            {
                var actionRow = m_ActionRows[i];
                var child = k_TempActions[i];
                if (actionRow.HasChanged(child))
                    return true;
            }

            return false;
        }

        protected sealed override void SetupUI()
        {
            var contentRowAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_ContentRowPath);
            contentRowAsset.CloneTree(this);

            base.SetupUI();

            SetupChildren();

            ContentObjectField = this.Q<ObjectField>(k_ContentObjectFieldName);

            SetupContent();

            ContentObjectField.RegisterCallback<ExecuteCommandEvent>(RulesModule.PickObject);
        }

        protected virtual void SetupContent()
        {
            ContentObjectField.objectType = typeof(Transform);
            ContentObjectField.RegisterValueChangedCallback(OnContentFieldChange);

            var content = ContentTransform;
            var isLandmark = RulesModule.GetMarsObjectType(ContentTransform) == MarsObjectType.Landmark;
            if (isLandmark)
                content = content.childCount > 0 ? content.GetChild(0) : null;

            if (content != null)
            {
                var so = new SerializedObject(content);
                so.Update();

                NodeContainer.Bind(so);
                ContentObjectField.value = content;
            }

            var landmarkButton = this.Q<Button>(k_LandmarkButtonName);
            landmarkButton.clicked += () =>
            {
                RulesModule.DoAddLandmark(m_ContainerTransform, ContentTransform,
                    () => { EditorApplication.delayCall += RulesDrawer.BuildReplicatorsList; });

                EditorEvents.RulesUiUsed.Send(new RuleUiArgs
                {
                    label = k_AddLandmarkButtonAnalyticsLabel
                });
            };

            if (isLandmark)
            {
                landmarkButton.text = RulesModule.GetLandmarkName(ContentTransform);
            }
        }

        void OnContentFieldChange(ChangeEvent<UnityObject> evt)
        {
            if (evt == null)
                return;

            if (evt.previousValue == null)
            {
                var newObject = (Transform) evt.newValue;
                var isPrefab = PrefabUtility.IsPartOfPrefabAsset(newObject);
                if (isPrefab)
                    newObject = UnityObject.Instantiate(newObject.gameObject).transform;

                newObject.parent = BackingObject.transform;
                newObject.localPosition = Vector3.zero;
            }
            else
            {
                GameObject newObject = null;
                if (evt.newValue != null)
                    newObject = ((Transform) evt.newValue).gameObject;

                RulesModule.ReplaceGameObject(newObject, (Transform) evt.previousValue, false);
            }
        }

        void GetActionCandidates(List<ContentAction> actionObjects)
        {
            foreach (Transform child in ContentTransform)
            {
                var contentAction = child.GetComponentsInChildren<ContentAction>();
                if (contentAction == null)
                    continue;

                actionObjects.AddRange(contentAction);
            }
        }

        void SetupChildren()
        {
            k_TempActions.Clear();
            GetActionCandidates(k_TempActions);

            m_ActionRows = new ActionRow[k_TempActions.Count];
            for (int i = 0; i < k_TempActions.Count; i++)
            {
                var actionRow = (ActionRow)CreateRow(k_TempActions[i]);
                Add(actionRow);
                m_ActionRows[i] = actionRow;
            }
        }

        internal override void Select()
        {
            var frameTarget = ContentTransform;
            if (!ContentSupportsActions)
                frameTarget = ContentTransform.parent;

            base.Select(ContentTransform, frameTarget, ContentTransform.gameObject);
        }

        protected override void OnAddButton()
        {
            RulesModule.AddAction(ContentTransform);

            EditorEvents.RulesUiUsed.Send(new RuleUiArgs
            {
                label = k_AddButtonContentAnalyticsLabel
            });
        }

        protected void SetTitleName(string value)
        {
            var contentTitle = this.Q<Label>(k_ContentTitleName);
            contentTitle.text = value;
        }

        protected void SetObjectSetupAreaVisible(bool value)
        {
            var objectSetupArea = this.Q<VisualElement>(k_ObjectSetupAreaName);
            objectSetupArea.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
