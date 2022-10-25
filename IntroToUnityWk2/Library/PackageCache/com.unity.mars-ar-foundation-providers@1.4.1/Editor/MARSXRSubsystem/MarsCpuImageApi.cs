#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.MARS.XRSubsystem.Data;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

#if ARSUBSYSTEMS_4_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using UnityEngine.Rendering;
using MarsCameraImageCInfo = UnityEngine.XR.ARSubsystems.XRCpuImage.Cinfo;
using CpuImageFormat = UnityEngine.XR.ARSubsystems.XRCpuImage.Format;

// ReSharper disable once IdentifierTypo
using CameraImagePlaneCinfo = UnityEngine.XR.ARSubsystems.XRCpuImage.Plane.Cinfo;
#else
using CpuImageFormat = UnityEngine.XR.ARSubsystems.CameraImageFormat;
#endif

namespace Unity.MARS.XRSubsystem
{
    class MarsCpuImageApi : IDisposable
    {
        public enum ImageType
        {
            Unknown = 0,
            Camera = 1,
        }

        static readonly int k_MaxCpuUImageFormatValue;

        readonly Dictionary<int, MarsCameraImageCInfo> m_CameraImageInfos = new Dictionary<int, MarsCameraImageCInfo>();
        readonly Dictionary<int, MRStructuredImage> m_StructuredImages = new Dictionary<int, MRStructuredImage>();
        readonly Dictionary<ImageType, int> m_LatestImageHandles = new Dictionary<ImageType, int>();

        static MarsCpuImageApi()
        {
            k_MaxCpuUImageFormatValue = ((int[])Enum.GetValues(typeof(CpuImageFormat))).Max();
        }

        public void Dispose()
        {
            ClearAllCpuImages();
        }

        void ClearAllCpuImages()
        {
            m_CameraImageInfos.Clear();
            m_StructuredImages.Clear();
            m_LatestImageHandles.Clear();
        }

        internal void CreateCameraImage(MRStructuredImage cameraImage)
        {
            if (cameraImage.NativeHandle == 0)
                return;

            var structuredImageFormat = (int)cameraImage.Format;

            // Convert from MRStructuredImage.ImageFormat to XRCpuImage.ImageFormat
            var format = structuredImageFormat < 1 || structuredImageFormat > k_MaxCpuUImageFormatValue?
                CpuImageFormat.Unknown : (CpuImageFormat)structuredImageFormat;

            if (m_CameraImageInfos.TryGetValue(cameraImage.NativeHandle, out var cameraImageCInfo))
            {
                cameraImageCInfo = new MarsCameraImageCInfo(cameraImage.NativeHandle, cameraImage.Dimensions,
                cameraImage.PlaneCount, cameraImage.TimeStamp, format);
                m_CameraImageInfos[cameraImage.NativeHandle] = cameraImageCInfo;
                m_StructuredImages[cameraImage.NativeHandle] = cameraImage;
            }
            else
            {
                cameraImageCInfo = new MarsCameraImageCInfo(cameraImage.NativeHandle, cameraImage.Dimensions,
                    cameraImage.PlaneCount, cameraImage.TimeStamp, format);
                m_CameraImageInfos.Add(cameraImage.NativeHandle, cameraImageCInfo);
                m_StructuredImages.Add(cameraImage.NativeHandle, cameraImage);
            }

            if (m_LatestImageHandles.TryGetValue(ImageType.Camera, out _))
                m_LatestImageHandles[ImageType.Camera] = cameraImage.NativeHandle;
            else
                m_LatestImageHandles.Add(ImageType.Camera, cameraImage.NativeHandle);
        }

        internal bool TryAcquireLatestImage(ImageType imageType, out MarsCameraImageCInfo cameraImageCInfo)
        {
            if (m_LatestImageHandles.TryGetValue(imageType, out var handle)
                && m_CameraImageInfos.TryGetValue(handle, out cameraImageCInfo))
                return true;

            cameraImageCInfo = default;
            return false;
        }

        internal static bool TryGetConvertedDataSize(Vector2Int dimensions, TextureFormat format, out int size)
        {
            int bytes;
            switch (format)
            {
                case TextureFormat.RGBA32:
                    bytes = 4;
                    break;
                default:
                    size = 0;
                    return false;
            }
            size = dimensions.x * dimensions.y * bytes;
            return true;
        }

        internal bool NativeHandleValid(int nativeHandle)
        {
            return m_StructuredImages.ContainsKey(nativeHandle);
        }

        internal void DisposeImage(int nativeHandle)
        {
            m_CameraImageInfos.Remove(nativeHandle);
            m_StructuredImages.Remove(nativeHandle);

            if (m_LatestImageHandles.ContainsValue(nativeHandle))
            {
                var imageTypeToRemove = ImageType.Unknown;
                foreach (var latestImage in m_LatestImageHandles)
                {
                    if (latestImage.Value == nativeHandle)
                    {
                        imageTypeToRemove = latestImage.Key;
                        break;
                    }
                }

                m_LatestImageHandles.Remove(imageTypeToRemove);
            }
        }

        internal void TryGetTextureDescriptors(ImageType imageType, out NativeArray<XRTextureDescriptor> planeDescriptors,
            Allocator allocator)
        {
            if (m_LatestImageHandles.TryGetValue(imageType, out var nativeHandle)
                && m_CameraImageInfos.ContainsKey(nativeHandle)
                && m_StructuredImages.TryGetValue(nativeHandle, out var mrStructuredImage))
            {
                planeDescriptors = new NativeArray<XRTextureDescriptor>();

                var descriptorsArray = new MarsTextureDescriptor[mrStructuredImage.PlaneCount];
                for (var i = 0; i < descriptorsArray.Length; i++)
                {
                    var texture = mrStructuredImage.TexturePlanes[i];
                    var propertyNameId = i == 0 ? CameraSubsystem.TextureSinglePropertyNameId : 0;

                    // To get this is work with scene view will need a way to set the shader id
                    // to the texture for the scene view when requested
                    if (propertyNameId != 0)
                        Shader.SetGlobalTexture(propertyNameId, texture);

                    descriptorsArray[i] = new MarsTextureDescriptor(texture.GetNativeTexturePtr(),
                        texture.width, texture.height, texture.mipmapCount, TextureFormat.RGBA32, propertyNameId
#if ARSUBSYSTEMS_4_OR_NEWER
                        , 0, TextureDimension.Tex2D
#endif
                        );
                }

                var marsDescriptors = new NativeArray<MarsTextureDescriptor>(descriptorsArray, allocator);

                planeDescriptors = marsDescriptors.Reinterpret<XRTextureDescriptor>();
                return;
            }

            planeDescriptors = new NativeArray<XRTextureDescriptor>(Array.Empty<XRTextureDescriptor>(), allocator);
        }

        internal bool TryGetLatestImagePtr(ImageType imageType, out IntPtr nativePtr)
        {
            if (m_LatestImageHandles.TryGetValue(imageType, out var nativeHandel)
                && m_StructuredImages.TryGetValue(nativeHandel, out var structuredImage))
            {
                nativePtr = structuredImage.TexturePlanes[0].GetNativeTexturePtr();
                return true;
            }

            nativePtr = IntPtr.Zero;
            return false;
        }
    }
}
#endif
