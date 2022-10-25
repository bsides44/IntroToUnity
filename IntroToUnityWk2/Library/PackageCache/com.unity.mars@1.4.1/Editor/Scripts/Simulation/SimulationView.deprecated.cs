using System;

namespace UnityEditor.MARS.Simulation
{
    public partial class SimulationView
    {
        /// <summary>
        /// (Obsolete) Desaturate the inactive composite layer
        /// </summary>
        [Obsolete]
        public bool DesaturateInactive
        {
            get => false;
            set { }
        }

#if UNITY_2021_2_OR_NEWER
        /// <inheritdoc />
        [Obsolete]
        protected override void OnGUI(){}
#endif
    }
}
