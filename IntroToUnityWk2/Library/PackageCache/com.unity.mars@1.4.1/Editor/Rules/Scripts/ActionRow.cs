using System;
using Unity.MARS.Behaviors;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityObject = UnityEngine.Object;

namespace Unity.MARS.Rules.RulesEditor
{
    [CustomRuleNode(typeof(ContentAction), true)]
    class ActionRow : RuleNode
    {
        const string k_ActionRowPath = k_Directory + "ActionRow.uxml";

        const string k_ActionTitleName = "actionTitle";

        ContentAction m_ContentAction;
        Transform m_Transform;
        Label m_ActionTitleLabel;

        UnityObject m_SelectedComponent;

        internal override Transform ContainerObject => m_Transform.parent;
        internal override GameObject BackingObject => m_Transform.gameObject;

        protected override void OnInit()
        {
            m_ContentAction = (ContentAction)Target;
            m_Transform = m_ContentAction.transform;

            SetupUI();
        }

        internal bool HasChanged(ContentAction contentAction)
        {
            return contentAction != m_ContentAction;
        }

        protected sealed override void SetupUI()
        {
            var contentRowAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_ActionRowPath);
            contentRowAsset.CloneTree(this);

            base.SetupUI();

            m_ActionTitleLabel = this.Q<Label>(k_ActionTitleName);
            m_ActionTitleLabel.text = m_Transform.name;
        }

        internal override void Select()
        {
            m_SelectedComponent = m_Transform.GetComponent<ContentAction>();
            if (m_SelectedComponent == null)
                m_SelectedComponent = m_Transform;

            base.Select(m_SelectedComponent, ContainerObject, m_Transform.gameObject);
        }
    }
}
