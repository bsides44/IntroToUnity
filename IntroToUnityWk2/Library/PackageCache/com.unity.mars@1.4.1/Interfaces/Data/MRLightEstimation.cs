using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.MARS.Data
{
    /// <summary>
    /// Provides a template for light estimation data
    /// </summary>
    [Serializable]
    [MovedFrom("Unity.MARS")]
    public struct MRLightEstimation : IEquatable<MRLightEstimation>
    {
        [SerializeField]
        public float? m_AmbientBrightness;

        [SerializeField]
        public float? m_AmbientColorTemperature;

        [SerializeField]
        public float? m_AmbientIntensityInLumens;

        [SerializeField]
        public Color? m_ColorCorrection;

        [SerializeField]
        public float? m_MainLightBrightness;

        [SerializeField]
        public Color? m_MainLightColor;

        [SerializeField]
        public Vector3? m_MainLightDirection;

        [SerializeField]
        public float? m_MainLightIntensityLumens;

        [SerializeField]
        public SphericalHarmonicsL2? m_SphericalHarmonics;

        [Obsolete]
        public bool AreLightsEqual(MRLightEstimation other)
        {
            return Equals(other);
        }

        public bool Equals(MRLightEstimation other)
        {
            return Nullable.Equals(m_AmbientBrightness, other.m_AmbientBrightness)
                && Nullable.Equals(m_AmbientColorTemperature, other.m_AmbientColorTemperature)
                && Nullable.Equals(m_AmbientIntensityInLumens, other.m_AmbientIntensityInLumens)
                && Nullable.Equals(m_ColorCorrection, other.m_ColorCorrection)
                && Nullable.Equals(m_MainLightBrightness, other.m_MainLightBrightness)
                && Nullable.Equals(m_MainLightColor, other.m_MainLightColor)
                && Nullable.Equals(m_MainLightDirection, other.m_MainLightDirection)
                && Nullable.Equals(m_MainLightIntensityLumens, other.m_MainLightIntensityLumens)
                && Nullable.Equals(m_SphericalHarmonics, other.m_SphericalHarmonics);
        }

        public override bool Equals(object obj)
        {
            return obj is MRLightEstimation other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = m_AmbientBrightness.GetHashCode();
                hashCode = (hashCode * 397) ^ m_AmbientColorTemperature.GetHashCode();
                hashCode = (hashCode * 397) ^ m_AmbientIntensityInLumens.GetHashCode();
                hashCode = (hashCode * 397) ^ m_ColorCorrection.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MainLightBrightness.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MainLightColor.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MainLightDirection.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MainLightIntensityLumens.GetHashCode();
                hashCode = (hashCode * 397) ^ m_SphericalHarmonics.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(MRLightEstimation left, MRLightEstimation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MRLightEstimation left, MRLightEstimation right)
        {
            return !left.Equals(right);
        }
    }
}
