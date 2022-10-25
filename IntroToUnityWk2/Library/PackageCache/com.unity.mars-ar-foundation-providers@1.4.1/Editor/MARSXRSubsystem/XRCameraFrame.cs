#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace Unity.MARS.XRSubsystem
{
    [StructLayout(LayoutKind.Sequential)]
    struct MarsCameraFrame
    {
        internal long TimestampNs;
        internal float AverageBrightness;
        internal float AverageColorTemperature;
        internal Color ColorCorrection;
        internal Matrix4x4 ProjectionMatrix;
        internal Matrix4x4 DisplayMatrix;
        internal TrackingState TrackingState;
        internal IntPtr NativePtr;
        internal XRCameraFrameProperties Properties;

#if ARSUBSYSTEMS_3_OR_NEWER || ARFOUNDATION_5_OR_NEWER
        internal float AverageIntensityInLumens;
        internal double ExposureDuration;
        internal float ExposureOffset;
#endif

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
        internal float MainLightIntensityLumens;
        internal Color MainLightColor;
        internal Vector3 MainLightDirection;
        internal SphericalHarmonicsL2 AmbientSphericalHarmonics;
        internal XRTextureDescriptor CameraGrain;
        internal float NoiseIntensity;
#endif
    }
}
#endif
