#if ARSUBSYSTEMS_2_1_OR_NEWER || ARFOUNDATION_5_OR_NEWER
using System;
using System.Runtime.InteropServices;
using UnityEngine;
#if ARSUBSYSTEMS_4_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace Unity.MARS.XRSubsystem
{
    /// <summary>
    /// Encapsulates a native texture object and includes various metadata about the texture.
    /// </summary>
    /// <remarks>
    /// Mirror memory layout of UnityEngine.XR.ARSubsystems.XRTextureDescriptor
    /// This is needed since not all versions of subsystems have public constructors for this struct
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MarsTextureDescriptor
    {
        IntPtr m_NativeTexture;
        int m_Width;
        int m_Height;
        int m_MipmapCount;
        TextureFormat m_Format;
        int m_PropertyNameId;

#if ARSUBSYSTEMS_4_OR_NEWER
        int m_Depth;
        TextureDimension m_Dimension;
#endif

        /// <summary>
        /// A pointer to the native texture object.
        /// </summary>
        /// <value>
        /// A pointer to the native texture object.
        /// </value>
        internal IntPtr nativeTexture
        {
            get { return m_NativeTexture; }
            set { m_NativeTexture = value; }
        }

        /// <summary>
        /// Specifies the width dimension of the native texture object.
        /// </summary>
        /// <value>
        /// The width of the native texture object.
        /// </value>
        internal int width
        {
            get { return m_Width; }
            set { m_Width = value; }
        }

        /// <summary>
        /// Specifies the height dimension of the native texture object.
        /// </summary>
        /// <value>
        /// The height of the native texture object.
        /// </value>
        internal int height
        {
            get { return m_Height; }
            set { m_Height = value; }
        }

        /// <summary>
        /// Specifies the number of mipmap levels in the native texture object.
        /// </summary>
        /// <value>
        /// The number of mipmap levels in the native texture object.
        /// </value>
        internal int mipmapCount
        {
            get { return m_MipmapCount; }
            set { m_MipmapCount = value; }
        }

        /// <summary>
        /// Specifies the texture ImageFormat of the native texture object.
        /// </summary>
        /// <value>
        /// The ImageFormat of the native texture object.
        /// </value>
        internal TextureFormat format
        {
            get { return m_Format; }
            set { m_Format = value; }
        }

        /// <summary>
        /// Specifies the unique shader property name ID for the material shader texture.
        /// </summary>
        /// <value>
        /// The unique shader property name ID for the material shader texture.
        /// </value>
        /// <remarks>
        /// Use the static method <c>Shader.PropertyToID(string name)</c> to get the unique identifier.
        /// </remarks>
        internal int propertyNameId
        {
            get { return m_PropertyNameId; }
            set { m_PropertyNameId = value; }
        }

#if ARSUBSYSTEMS_4_OR_NEWER
        /// <summary>
        /// This specifies the depth dimension of the native texture. For a 3D texture, depth would be greater than zero.
        /// For any other kind of valid texture, depth is one.
        /// </summary>
        /// <value>
        /// The depth dimension of the native texture object.
        /// </value>
        internal int depth
        {
            get => m_Depth;
            set => m_Depth = value;
        }

        /// <summary>
        /// Specifies the [texture dimension](https://docs.unity3d.com/ScriptReference/Rendering.TextureDimension.html) of the native texture object.
        /// </summary>
        /// <value>
        /// The texture dimension of the native texture object.
        /// </value>
        internal TextureDimension dimension
        {
            get => m_Dimension;
            set => m_Dimension = value;
        }
#endif

        internal MarsTextureDescriptor(IntPtr nativeTexture, int width, int height, int mipmapCount,
            TextureFormat format, int propertyNameId)
        {
            m_NativeTexture = nativeTexture;
            m_Width = width;
            m_Height = height;
            m_MipmapCount = mipmapCount;
            m_Format = format;
            m_PropertyNameId = propertyNameId;

#if ARSUBSYSTEMS_4_OR_NEWER
            m_Depth = 0;
            m_Dimension = TextureDimension.None;
#endif
        }

#if ARSUBSYSTEMS_4_OR_NEWER
        internal MarsTextureDescriptor(IntPtr nativeTexture, int width, int height, int mipmapCount,
            TextureFormat format, int propertyNameId, int depth, TextureDimension dimension)
        {
            m_NativeTexture = nativeTexture;
            m_Width = width;
            m_Height = height;
            m_MipmapCount = mipmapCount;
            m_Format = format;
            m_PropertyNameId = propertyNameId;
            m_Depth = depth;
            m_Dimension = dimension;
        }
#endif
    }
}
#endif
