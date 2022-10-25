using System;
using UnityEditor;
using UnityEditor.MARS.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MARS.Rules.RulesEditor
{
    [CustomRuleNode(typeof(Proxy))]
    class ProxyRow : EntityRow
    {
        const string k_ProxyRowPath = k_Directory + "ProxyRow.uxml";

        const string k_SimMatchCountName = "simMatchCount";
        const string k_ContentContainerName = "contentContainer";

        const string k_AddButtonProxyAnalyticsLabel = "Margin add button (Proxy row)";

        Transform m_Transform;
        Transform m_SimulatedObject;

        Label m_SimMatchCount;

        internal override Transform ContainerObject => m_Transform;
        internal override GameObject BackingObject => Entity.gameObject;

        protected override void OnInit()
        {
            Entity = (Proxy)Target;
            m_Transform = Entity.transform;
            m_SimulatedObject = SimulatedObjectsManager.instance.GetCopiedTransform(m_Transform);

            SetupUI();
        }

        protected sealed override void SetupUI()
        {
            var proxyRowAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_ProxyRowPath);
            proxyRowAsset.CloneTree(this);

            base.SetupUI();

            SetupProxyPresetUI(Entity);
            ProxyField.value = Entity;

            m_SimMatchCount = this.Q<Label>(k_SimMatchCountName);
            if (m_SimulatedObject == null)
                m_SimMatchCount.style.display = DisplayStyle.None;

            ContentContainer = this.Q<VisualElement>(k_ContentContainerName);

            SetupContentRows();

            CreateAddContentButton();
        }

        internal sealed override bool HasChanged(MARSEntity entity)
        {
            var entityTransform = entity.transform;
            if (base.HasChanged(entity) || entityTransform.childCount != ContentRows.Length)
                return true;

            for (int i = 0; i < entityTransform.childCount; i++)
            {
                var contentRow = ContentRows[i];
                var content = entityTransform.GetChild(i);
                if (contentRow.HasChanged(content))
                    return true;
            }

            return false;
        }

        internal override void Select()
        {
            base.Select(Entity, m_Transform, m_Transform.gameObject);
        }

        protected override void OnAddButton()
        {
            RulesModule.AddContent(Entity.transform);

            EditorEvents.RulesUiUsed.Send(new RuleUiArgs
            {
                label = k_AddButtonProxyAnalyticsLabel
            });
        }
    }
}
