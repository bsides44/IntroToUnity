#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Runtime.InteropServices;
using UnityEngine.XR.ARSubsystems;

namespace Unity.MARS.XRSubsystem
{
    [StructLayout(LayoutKind.Explicit)]
    struct XRCameraFrameUnion
    {
        [FieldOffset(0)]
        public MarsCameraFrame marsCameraFrame;

        [FieldOffset(0)]
        public XRCameraFrame m_TheirXRCameraFrame;
    }
}
#endif
