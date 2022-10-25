using Unity.MARS;
using Unity.XRTools.ModuleLoader;
using UnityEditor.MARS.Simulation;
using UnityEngine.UIElements;

namespace UnityEditor.MARS.UIElements
{
    class EnvironmentControls : VisualElement
    {
        const string k_Directory = "Packages/com.unity.mars/Editor/Scripts/Simulation/UI/";
        const string k_XMLPath = k_Directory + "EnvironmentControls.uxml";
        const string k_StylePath = k_Directory + "EnvironmentControls.uss";

        const string k_TimelineButtonName = "openTimeline";

        Button m_TimelineButton;

        internal EnvironmentControls()
        {
            SetupUI();
        }

        protected override void ExecuteDefaultAction(EventBase evt)
        {
            base.ExecuteDefaultAction(evt);

            if (evt is AttachToPanelEvent)
            {
                QuerySimulationModule.onTemporalSimulationStart += RefreshUI;
                QuerySimulationModule.onTemporalSimulationStop += RefreshUI;
                QuerySimulationModule.OnOneShotSimulationStart += RefreshUI;
                var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                if (environmentManager != null)
                    environmentManager.EnvironmentChanged += RefreshUI;

                RefreshUI();
            }
            else if (evt is DetachFromPanelEvent)
            {
                QuerySimulationModule.onTemporalSimulationStart -= RefreshUI;
                QuerySimulationModule.onTemporalSimulationStop -= RefreshUI;
                QuerySimulationModule.OnOneShotSimulationStart -= RefreshUI;
                var environmentManager = ModuleLoaderCore.instance.GetModule<MARSEnvironmentManager>();
                if (environmentManager != null)
                    environmentManager.EnvironmentChanged -= RefreshUI;
            }
        }

        void SetupUI()
        {
            var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_XMLPath);
            treeAsset.CloneTree(this);

            var windowStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath);
            styleSheets.Add(windowStyle);

            m_TimelineButton = this.Q<Button>(k_TimelineButtonName);
            m_TimelineButton.clicked += OpenRecordingTimeline;
        }

        void RefreshUI()
        {
            var recordingPlaybackModule = ModuleLoaderCore.instance.GetModule<MarsRecordingPlaybackModule>();
            m_TimelineButton.SetEnabled(recordingPlaybackModule != null && recordingPlaybackModule.IsRecordingAvailable);
        }

        static void OpenRecordingTimeline()
        {
           var recordingPlaybackModule = ModuleLoaderCore.instance.GetModule<MarsRecordingPlaybackModule>();
            if (recordingPlaybackModule == null)
                return;

            EditorEvents.RecordingTimelineOpened.Send(new RecordingTimelineOpenedArgs());
            recordingPlaybackModule.OpenRecordingTimeline();
        }
    }
}
