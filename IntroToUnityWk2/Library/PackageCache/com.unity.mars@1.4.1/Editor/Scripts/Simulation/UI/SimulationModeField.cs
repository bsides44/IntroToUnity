using System.Linq;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine.UIElements;

namespace UnityEditor.MARS.UIElements
{
    class SimulationModeField : MarsPopupField<string>
    {
        internal new class UxmlFactory : UxmlFactory<SimulationModeField, UxmlTraits> { }

        internal new class UxmlTraits : BaseField<string>.UxmlTraits { }

        public SimulationModeField()
            : base(null)
        {
            choices = MARSEnvironmentManager.ModeTypes.Select(m => m.text).ToList();
        }

        protected override void ExecuteDefaultActionAtTarget(EventBase evt)
        {
            base.ExecuteDefaultActionAtTarget(evt);

            if (evt == null)
                return;

            if (evt is AttachToPanelEvent)
            {
                var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                if (environmentManager != null)
                    environmentManager.EnvironmentChanged += RefreshUI;

                RefreshUI();
            }
            else if (evt is DetachFromPanelEvent)
            {
                var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                if (environmentManager != null)
                    environmentManager.EnvironmentChanged -= RefreshUI;
            }
            else if (evt is ChangeEvent<string> changeEvent)
            {
                OnEnvironmentModeChange(changeEvent);
            }
        }

        void RefreshUI()
        {
            var simulationSettings = SimulationSettings.instance;

            if (simulationSettings == null)
            {
                SetEnabled(false);
                return;
            }

            if (!enabledSelf)
                SetEnabled(true);

            var choiceIndex = (int)simulationSettings.EnvironmentMode;
            if (choiceIndex >= 0 && choiceIndex < choices.Count)
                SetIndexWithoutNotify(choiceIndex);
        }

        void OnEnvironmentModeChange(ChangeEvent<string> changeEvent)
        {
            var simulationSettings = SimulationSettings.instance;
            var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();

            if (environmentManager == null || simulationSettings == null)
                return;

            if (!enabledSelf)
                SetEnabled(true);

            var environmentMode = (EnvironmentMode) choices.IndexOf(changeEvent.newValue);
            environmentManager.TrySetModeAndRestartSimulation(environmentMode);
        }
    }
}
