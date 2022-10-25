#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.MARS.XRSubsystem
{
    class MarsXRCpuImageApi : XRCpuImage.Api
    {
        readonly MarsCpuImageApi m_MarsCpuImageApi;

        public MarsXRCpuImageApi(MarsCpuImageApi marsCpuImageApi)
        {
            m_MarsCpuImageApi = marsCpuImageApi;
        }

        public override bool TryGetConvertedDataSize(int nativeHandle, Vector2Int dimensions, TextureFormat format, out int size)
        {
            return MarsCpuImageApi.TryGetConvertedDataSize(dimensions, format, out size);
        }

        public override bool NativeHandleValid(int nativeHandle)
        {
            m_MarsCpuImageApi.NativeHandleValid(nativeHandle);
            return base.NativeHandleValid(nativeHandle);
        }

        public override void DisposeImage(int nativeHandle)
        {
            m_MarsCpuImageApi.DisposeImage(nativeHandle);
            base.DisposeImage(nativeHandle);
        }
    }
}
#endif
