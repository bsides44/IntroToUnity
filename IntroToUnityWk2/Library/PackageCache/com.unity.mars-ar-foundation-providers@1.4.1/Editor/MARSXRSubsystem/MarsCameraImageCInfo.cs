#if ARSUBSYSTEMS_2_1_OR_NEWER && !ARSUBSYSTEMS_4_OR_NEWER
using System;
using UnityEngine;
using UnityEngine.XR.ARSubsystems;

namespace Unity.MARS.XRSubsystem
{
    struct MarsCameraImageCInfo : IEquatable<MarsCameraImageCInfo>
    {
        int m_NativeHandle;
        Vector2Int m_Dimensions;
        int m_PlaneCount;
        double m_Timestamp;
        CameraImageFormat m_Format;

        /// <summary>
        /// The handle representing the camera image on the native level.
        /// </summary>
        /// <value>
        /// The handle representing the camera image on the native level.
        /// </value>
        public int nativeHandle => m_NativeHandle;

        /// <summary>
        /// The dimensions of the camera image.
        /// </summary>
        /// <value>
        /// The dimensions of the camera image.
        /// </value>
        public Vector2Int dimensions => m_Dimensions;

        /// <summary>
        /// The number of video planes in the camera image.
        /// </summary>
        /// <value>
        /// The number of video planes in the camera image.
        /// </value>
        public int planeCount => m_PlaneCount;

        /// <summary>
        /// The timestamp for when the camera image was captured.
        /// </summary>
        /// <value>
        /// The timestamp for when the camera image was captured.
        /// </value>
        public double timestamp => m_Timestamp;

        /// <summary>
        /// The format of the camera image.
        /// </summary>
        /// <value>
        /// The format of the camera image.
        /// </value>
        public CameraImageFormat format => m_Format;

        /// <summary>
        /// Constructs the camera image cinfo.
        /// </summary>
        /// <param name="nativeHandle">The handle representing the camera image on the native level.</param>
        /// <param name="dimensions">The dimensions of the camera image.</param>
        /// <param name="planeCount">The number of video planes in the camera image.</param>
        /// <param name="timestamp">The timestamp for when the camera image was captured.</param>
        /// <param name="format">The format of the camera image.</param>
        public MarsCameraImageCInfo(int nativeHandle, Vector2Int dimensions, int planeCount, double timestamp,
            CameraImageFormat format)
        {
            m_NativeHandle = nativeHandle;
            m_Dimensions = dimensions;
            m_PlaneCount = planeCount;
            m_Timestamp = timestamp;
            m_Format = format;
        }

        public bool Equals(MarsCameraImageCInfo other)
        {
            return nativeHandle.Equals(other.nativeHandle) && dimensions.Equals(other.dimensions)
                && planeCount.Equals(other.planeCount) && timestamp.Equals(other.timestamp)
                && format.Equals(other.format);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is MarsCameraImageCInfo info && Equals(info);
        }

        public static bool operator ==(MarsCameraImageCInfo lhs, MarsCameraImageCInfo rhs) => lhs.Equals(rhs);

        public static bool operator !=(MarsCameraImageCInfo lhs, MarsCameraImageCInfo rhs) => !(lhs == rhs);

        public override int GetHashCode()
        {
            var hashCode = 486187739;
            unchecked
            {
                hashCode = hashCode * 486187739 + nativeHandle.GetHashCode();
                hashCode = hashCode * 486187739 + dimensions.GetHashCode();
                hashCode = hashCode * 486187739 + planeCount.GetHashCode();
                hashCode = hashCode * 486187739 + timestamp.GetHashCode();
                hashCode = hashCode * 486187739 + ((int)format).GetHashCode();
            }
            return hashCode;
        }

        public override string ToString()
        {
            return string.Format("nativeHandle: {0} dimensions:{1} planes:{2} timestamp:{3} format:{4}",
                nativeHandle.ToString(), dimensions.ToString(), planeCount.ToString(),
                timestamp.ToString(), format.ToString());
        }
    }
}
#endif
