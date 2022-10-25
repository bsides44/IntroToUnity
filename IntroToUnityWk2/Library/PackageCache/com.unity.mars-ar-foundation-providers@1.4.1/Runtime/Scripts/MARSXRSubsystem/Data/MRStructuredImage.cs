using System;
using System.Collections.Generic;
using UnityEngine;

namespace Unity.MARS.XRSubsystem.Data
{
    /// <summary>
    /// Represents the current frame from a simulated device camera. Provides access to the raw image plane textures.
    /// This is used to provide the raw textures to create <c>UnityEngine.XR.ARSubsystems.XRCpuImage</c> in the
    /// <c>Unity.MARS.XRSubsystem.CameraSubsystem</c>.
    /// </summary>
    struct MRStructuredImage
    {
        /// <summary>
        /// Formats used by the raw <see cref="XRCpuImage"/> data. See <see cref="XRCpuImage.format"/>.
        /// </summary>
        internal enum ImageFormat
        {
            /// <summary>
            /// Single plane RGBA 16bit ImageFormat used for maximum compatibility in simulation.
            /// This ImageFormat is equivalent to TextureFormat.ARGB32.
            /// </summary>
            CompatibilityARGB32 = -1,

            /// <summary>
            /// The ImageFormat is unknown or could not be determined.
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// <para>Three-Plane YUV 420 ImageFormat commonly used by Android. See
            /// <a href="https://developer.android.com/ndk/reference/group/media#group___media_1gga9c3dace30485a0f28163a882a5d65a19aea9797f9b5db5d26a2055a43d8491890">
            /// AIMAGE_FORMAT_YUV_420_888</a>.</para>
            /// <para>This ImageFormat consists of three image planes. The first is the Y (luminocity) plane, with 8 bits per
            /// pixel. The second and third are the U and V (chromaticity) planes, respectively. Each 2x2 block of pixels
            /// share the same chromaticity value, so a given (x, y) pixel's chromaticity value is given by
            /// <code>
            /// u = UPlane[(y / 2) * rowStride + (x / 2) * pixelStride];
            /// v = VPlane[(y / 2) * rowStride + (x / 2) * pixelStride];
            /// </code></para>
            /// </summary>
            AndroidYuv420_888 = 1,

            /// <summary>
            /// <para>Bi-Planar Component Y'CbCr 8-bit 4:2:0, full-range (luma=[0,255] chroma=[1,255]) commonly used by
            /// iOS. See
            /// <a href="https://developer.apple.com/documentation/corevideo/1563591-pixel_format_identifiers/kcvpixelformattype_420ypcbcr8biplanarfullrange">
            /// kCVPixelFormatType_420YpCbCr8BiPlanarFullRange</a>.</para>
            /// <para>This ImageFormat consists of two image planes. The first is the Y (luminocity) plane, with 8 bits per
            /// pixel. The second plane is the UV (chromaticity) plane. The U and V chromaticity values are interleaved
            /// (u0, v0, u1, v1, etc.). Each 2x2 block of pixels share the same chromaticity values, so a given (x, y)
            /// pixel's chromaticity value is given by
            /// <code>
            /// u = UvPlane[(y / 2) * rowStride + (x / 2) * pixelStride];
            /// v = UvPlane[(y / 2) * rowStride + (x / 2) * pixelStride + 1];
            /// </code>
            /// pixelStride is always 2 for this ImageFormat, so this can be optimized to
            /// <code>
            /// u = UvPlane[(y >> 1) * rowStride + x &amp; ~1];
            /// v = UvPlane[(y >> 1) * rowStride + x | 1];
            /// </code></para>
            /// </summary>
            IosYpCbCr420_8BiPlanarFullRange = 2,

            /// <summary>
            /// A single channel image ImageFormat with 8 bits per pixel.
            /// </summary>
            OneComponent8 = 3,

            /// <summary>
            /// IEEE754-2008 binary32 float, describing the depth (distance to an object) in meters
            /// </summary>
            DepthFloat32 = 4,

            /// <summary>
            /// 16-bit unsigned integer, describing the depth (distance to an object) in millimeters.
            /// </summary>
            DepthUint16 = 5,
        }

        int m_NativeHandle;
        List<Texture2D> m_TexturePlanes;
        Vector2Int m_Dimensions;
        int m_PlaneCount;
        double m_Timestamp;
        ImageFormat m_Format;

        /// <summary>
        /// The source image's native pointer cast to a 32bit int.
        /// </summary>
        internal int NativeHandle => m_NativeHandle;

        /// <summary>
        /// The textures that make up the individual image planes.
        /// </summary>
        /// <value>
        /// The textures that make up the individual image planes.
        /// </value>
        internal List<Texture2D> TexturePlanes => m_TexturePlanes;

        /// <summary>
        /// The dimensions of the camera image.
        /// </summary>
        /// <value>
        /// The dimensions of the camera image.
        /// </value>
        internal Vector2Int Dimensions => m_Dimensions;

        /// <summary>
        /// The number of video planes in the camera image.
        /// </summary>
        /// <value>
        /// The number of video planes in the camera image.
        /// </value>
        internal int PlaneCount => m_PlaneCount;

        /// <summary>
        /// The timestamp for when the camera image was captured.
        /// </summary>
        /// <value>
        /// The timestamp for when the camera image was captured.
        /// </value>
        internal double TimeStamp => m_Timestamp;

        /// <summary>
        /// The ImageFormat of the camera image. <see cref="ImageFormat"/>
        /// </summary>
        /// <value>
        /// The ImageFormat of the camera image.
        /// </value>
        internal ImageFormat Format => m_Format;

        /// <summary>
        /// Creates a <see cref="MRStructuredImage"/> that represents the current frame from a simulated device camera.
        /// </summary>
        /// <param name="nativeHandle">The source image's native pointer cast to a 32bit int.</param>
        /// <param name="texturePlanes">The textures that make up the individual image planes.</param>
        /// <param name="dimensions">The dimensions of the camera image.</param>
        /// <param name="timestamp">The timestamp for when the camera image was captured.</param>
        /// <param name="format"> Image format of the camera image.</param>
        internal MRStructuredImage(int nativeHandle, List<Texture2D> texturePlanes, Vector2Int dimensions, double timestamp, ImageFormat format)
        {
            m_NativeHandle = nativeHandle;
            m_TexturePlanes = texturePlanes;
            m_Dimensions = dimensions;
            m_PlaneCount = texturePlanes.Count;
            m_Timestamp = timestamp;
            m_Format = format;
        }

        /// <summary>
        /// Attempts to convert an <see cref="XRCpuImage.Format"/> to a `UnityEngine.TextureFormat`.
        /// </summary>
        /// <param name="format">The <see cref="XRCpuImage.Format"/> being extended.</param>
        /// <returns>Returns a `TextureFormat` that matches <paramref name="format"/> if possible. Returns 0 if there
        ///     is no matching `TextureFormat`.</returns>
        internal static TextureFormat AsTextureFormat(ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.OneComponent8: return TextureFormat.R8;
                case ImageFormat.DepthFloat32: return TextureFormat.RFloat;
                case ImageFormat.DepthUint16: return TextureFormat.RFloat;
                case ImageFormat.CompatibilityARGB32: return TextureFormat.ARGB32;
                default: return 0;
            }
        }
    }
}
