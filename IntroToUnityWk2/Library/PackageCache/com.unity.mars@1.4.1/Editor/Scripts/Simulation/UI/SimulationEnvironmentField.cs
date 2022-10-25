using System.Collections.Generic;
using Unity.MARS.Simulation;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.MARS.UIElements
{
    class SimulationEnvironmentField : MarsPopupField<SimulationEnvironmentField.Choice>
    {
        const string k_VideoRecordingsPrefix = "Video/";
        const string k_DataRecordingsPrefix = "Data/";
        const string k_SyntheticRecordingsPrefix = "Synthetic/";

        internal new class UxmlFactory : UxmlFactory<SimulationEnvironmentField, UxmlTraits> { }

        internal new class UxmlTraits : BaseField<Choice>.UxmlTraits { }

        // Local method use only -- created here to reduce garbage collection. Collections must be cleared before use
        static readonly List<GameObject> k_TempGameObjects = new List<GameObject>();

        internal class Choice
        {
            internal string DisplayName { get; }

            internal Choice(string displayName)
            {
                DisplayName = displayName;
            }
        }

        class IndexedChoice : Choice
        {
            internal int Index { get; }

            internal IndexedChoice(string displayName, int index) : base(displayName)
            {
                Index = index;
            }
        }

        abstract class State
        {
            internal abstract void OnEnter(SimulationEnvironmentField dropdown);
            internal abstract void OnExit(SimulationEnvironmentField dropdown);
            internal abstract void OnValueChange(SimulationEnvironmentField dropdown, ChangeEvent<Choice> evt);
        }

        class InactiveState : State
        {
            internal override void OnEnter(SimulationEnvironmentField dropdown)
            {
                dropdown.choices.Clear();
                dropdown.choices.Add(new Choice( ""));
                dropdown.SetIndexWithoutNotify(0);
                dropdown.SetEnabled(false);
            }

            internal override void OnExit(SimulationEnvironmentField dropdown)
            {
                dropdown.SetEnabled(true);
            }

            internal override void OnValueChange(SimulationEnvironmentField dropdown, ChangeEvent<Choice> evt)
            { }
        }

        class SyntheticState : State
        {
            static string GetDirectoryAndFile(string path)
            {
                if (string.IsNullOrEmpty(path))
                    return "";

                var length = path.Length;
                var startIndex = length - 1;
                var lastIndex = length;
                var slashCount = 0;
                for (; startIndex >= 0 && slashCount <= 1; --startIndex)
                {
                    var c = path[startIndex];

                    if (c == '/')
                        slashCount++;
                    else if (lastIndex == length && c == '.')
                        lastIndex = startIndex;
                }

                if (startIndex < 0)
                {
                    startIndex = 0;
                }
                else
                {
                    // +1 to ignore last for loop iteration and +1 to ignore last '/'
                    startIndex += 2;
                }

                return path.Substring(startIndex, lastIndex - startIndex);
            }

            internal override void OnEnter(SimulationEnvironmentField dropdown)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                var simulationSettings = SimulationSettings.instance;

                if (environmentManager == null || simulationSettings == null)
                    return;

                Choice selectedChoice = null;
                var choices = dropdown.choices;
                choices.Clear();

                var i = 0;
                for (; i < environmentManager.EnvironmentPrefabPaths.Count; i++)
                {
                    var displayName = GetDirectoryAndFile(environmentManager.EnvironmentPrefabPaths[i]);
                    var choice = new IndexedChoice(displayName, i);
                    choices.Add(choice);

                    if (environmentManager.CurrentSyntheticEnvironmentIndex == i)
                        selectedChoice = choice;
                }

                choices.Add(new IndexedChoice("Add Environments...", i));

                dropdown.SetValueWithoutNotify(selectedChoice);
            }

            internal override void OnExit(SimulationEnvironmentField dropdown)
            { }

            internal override void OnValueChange(SimulationEnvironmentField dropdown, ChangeEvent<Choice> evt)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
                var simulationSettings = SimulationSettings.instance;

                if (environmentManager == null || querySimulationModule == null || simulationSettings == null
                    || !(evt.newValue is IndexedChoice syntheticChoice))
                    return;

                var newIndex = syntheticChoice.Index;
                if (newIndex < 0)
                    return;

                // 'Add Environments' option at the end of the list
                if (newIndex == environmentManager.EnvironmentPrefabPaths.Count)
                {
                    MarsContentManagerUtils.TryOpenContentManagerWithMars();
                    return;
                }

                simulationSettings.UseSyntheticRecording = false;
                querySimulationModule.RequestSimulationModeSelection(SimulationModeSelection.SingleFrameMode);

                if (newIndex == environmentManager.CurrentSyntheticEnvironmentIndex)
                    querySimulationModule.RestartSimulationIfNeeded(true);
                else
                    environmentManager.TrySetupEnvironmentAndRestartSimulation(newIndex);
            }
        }

        class LiveState : State
        {
            internal override void OnEnter(SimulationEnvironmentField dropdown)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var videoContextManager = moduleLoader.GetModule<SimulationVideoContextManager>();
                var simulationSettings = SimulationSettings.instance;

                if (videoContextManager == null || simulationSettings == null)
                    return;

                var choices = dropdown.choices;
                choices.Clear();
                Choice selectedChoice = null;

                var webCamDeviceContents = videoContextManager.WebCamDeviceContents;
                if (webCamDeviceContents.Length == 0)
                {
                    var defaultChoice = new Choice("No web cam devices available");
                    choices.Add(defaultChoice);
                    selectedChoice = defaultChoice;
                }
                else
                {
                    for (var i = 0; i < webCamDeviceContents.Length; i++)
                    {
                        var content = webCamDeviceContents[i];
                        var newChoice = new IndexedChoice(content.text, i);
                        choices.Add(newChoice);

                        if (i == simulationSettings.WebCamDeviceIndex)
                            selectedChoice = newChoice;
                    }
                }

                if (selectedChoice != null)
                    dropdown.SetValueWithoutNotify(selectedChoice);
            }

            internal override void OnExit(SimulationEnvironmentField dropdown)
            { }

            internal override void OnValueChange(SimulationEnvironmentField dropdown, ChangeEvent<Choice> evt)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
                var simulationSettings = SimulationSettings.instance;

                if (environmentManager == null || querySimulationModule == null || simulationSettings == null
                    || !(evt.newValue is IndexedChoice indexedChoice))
                    return;

                if (querySimulationModule.simulatingTemporal)
                    querySimulationModule.RequestSimulationModeSelection(SimulationModeSelection.TemporalMode);

                environmentManager.TrySetupEnvironmentAndRestartSimulation(indexedChoice.Index);
            }
        }

        class RecordedState : State
        {
            internal override void OnEnter(SimulationEnvironmentField dropdown)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var simulationRecordingManager = moduleLoader.GetModule<SimulationRecordingManager>();

                if (simulationRecordingManager == null)
                    return;

                var choices = dropdown.choices;
                choices.Clear();

                simulationRecordingManager.ValidateIndependentRecordings();
                foreach (var recording in SimulationRecordingManager.IndependentRecordings)
                {
                    string prefix;
                    if (recording.HasSyntheticEnvironments)
                        prefix = k_SyntheticRecordingsPrefix;
                    else if (recording.HasVideo)
                        prefix = k_VideoRecordingsPrefix;
                    else
                        prefix = k_DataRecordingsPrefix;

                    var choice = new Choice(prefix + recording.DisplayName);
                    choices.Add(choice);
                }

                var index = simulationRecordingManager.CurrentIndependentRecordingIndex;
                if (index >= 0 && index < choices.Count)
                {
                    dropdown.SetIndexWithoutNotify(index);
                }
                else if (choices.Count == 0)
                {
                    var defaultChoice = new Choice("No recordings available");
                    choices.Add(defaultChoice);
                    dropdown.SetValueWithoutNotify(defaultChoice);
                }
            }

            internal override void OnExit(SimulationEnvironmentField dropdown)
            { }

            internal override void OnValueChange(SimulationEnvironmentField dropdown, ChangeEvent<Choice> evt)
            {
                var moduleLoader = ModuleLoaderCore.instance;
                var environmentManager = moduleLoader.GetModule<MARSEnvironmentManager>();
                var querySimulationModule = moduleLoader.GetModule<QuerySimulationModule>();
                var simulationRecordingManager = moduleLoader.GetModule<SimulationRecordingManager>();
                var newIndex = dropdown.choices.IndexOf(evt.newValue);

                if (environmentManager == null || querySimulationModule == null || simulationRecordingManager == null
                    || newIndex == simulationRecordingManager.CurrentIndependentRecordingIndex)
                    return;

                if (querySimulationModule.simulatingTemporal)
                    querySimulationModule.RequestSimulationModeSelection(SimulationModeSelection.TemporalMode);

                environmentManager.TrySetupEnvironmentAndRestartSimulation(newIndex);
            }
        }

        State m_CurrentState;

        public SimulationEnvironmentField()
            : base(null)
        {
            choices = new List<Choice>();
            formatSelectedValueCallback = GetListItemToDisplay;
            formatListItemCallback = GetListItemToDisplay;
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
            else if (evt is ChangeEvent<Choice> changeEvent)
            {
                m_CurrentState?.OnValueChange(this, changeEvent);
            }
        }

        static string GetListItemToDisplay(Choice item)
        {
            return item != null ? item.DisplayName : "";
        }

        void RefreshUI()
        {
            var simulationSettings = SimulationSettings.instance;
            var environmentMode = simulationSettings.EnvironmentMode;

            if (simulationSettings == null)
                return;

            switch (environmentMode)
            {
                case EnvironmentMode.Synthetic:
                    if (m_CurrentState is SyntheticState)
                        return;

                    SetState(new SyntheticState());
                    break;

                case EnvironmentMode.Live:
                    if (m_CurrentState is LiveState)
                        return;

                    SetState(new LiveState());
                    break;

                case EnvironmentMode.Recorded:
                    if (m_CurrentState is RecordedState)
                        return;

                    SetState(new RecordedState());
                    break;

                default:
                    if (m_CurrentState is InactiveState)
                        return;

                    SetState(new InactiveState());
                    break;
            }
        }

        void SetState(State newState)
        {
            m_CurrentState?.OnExit(this);
            m_CurrentState = newState;
            newState?.OnEnter(this);
        }
    }
}
