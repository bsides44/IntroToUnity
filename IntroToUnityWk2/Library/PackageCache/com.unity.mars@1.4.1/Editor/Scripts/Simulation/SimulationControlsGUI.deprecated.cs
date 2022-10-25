 using System;
using UnityEngine;

namespace UnityEditor.MARS.Simulation
{
    /// <summary>
    /// Deprecated GUI for the simulation controls
    /// </summary>
    public static partial class SimulationControlsGUI
    {
        /// <summary>
        /// GUI for selecting the view type
        /// </summary>
        /// <param name="view">View to change type on</param>
        /// <param name="contents">View type contents</param>
        [Obsolete("ViewSelectionElement has been deprecated and will be removed in future versions of MARS")]
        public static void ViewSelectionElement(ISimulationView view, GUIContent[] contents) { }

        /// <summary>
        /// (Obsolete) Draw help dialogs for a simulated device view
        /// </summary>
        /// <param name="sceneType">Current scene scene view type</param>
        /// <returns>True if any help message was displayed; false if no message was displayed.</returns>
        [Obsolete]
        public static bool DrawHelpArea(ViewSceneType sceneType)
        {
            return false;
        }
    }
}
