using System;
using System.Text.RegularExpressions;

namespace UnityEditor.MARS.Build
{
    readonly struct PackageVersion : IEquatable<PackageVersion>, IComparable<PackageVersion>
    {
        const string k_Major = "major";
        const string k_Minor = "minor";
        const string k_Patch = "patch";
        const string k_PreviewTag = "preview";
        const string k_Preview = "previewDigit";

        static readonly Regex k_Regex = new Regex($@"^(?<{k_Major}>\d+)\.(?<{k_Minor}>\d+)\.(?<{k_Patch}>\d+)"
            + $@"(?<{k_PreviewTag}>-[a-z]+(?:\.(?<{k_Preview}>\d+))?)?$");

        readonly string m_Version;

        readonly uint m_MajorVersion;
        readonly uint m_MinorVersion;
        readonly uint m_PatchVersion;
        readonly uint m_PreviewVersion;
        readonly bool m_IsPreview;

        internal uint MajorVersion => m_MajorVersion;
        internal uint MinorVersion => m_MinorVersion;
        internal uint PatchVersion => m_PatchVersion;
        internal uint PreviewVersion => m_PreviewVersion;
        internal bool IsPreview => m_IsPreview;

        internal PackageVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                m_MajorVersion = default;
                m_MinorVersion = default;
                m_PatchVersion = default;
                m_PreviewVersion = default;
                m_IsPreview = default;
                m_Version = default;
                return;
            }

            m_MajorVersion = 0;
            m_MinorVersion = 0;
            m_PatchVersion = 0;
            m_PreviewVersion = 0;
            m_IsPreview = false;
            m_Version = version;

            var match = k_Regex.Match(version);
            if (match.Success)
            {
                var groups = match.Groups;
                m_MajorVersion = uint.Parse(groups[k_Major].Value);
                m_MinorVersion = uint.Parse(groups[k_Minor].Value);
                m_PatchVersion = uint.Parse(groups[k_Patch].Value);
                if (groups[k_PreviewTag].Success)
                {
                    m_IsPreview = true;
                    m_PreviewVersion = groups[k_Preview].Success ? uint.Parse(groups[k_Preview].Value) : 0;
                }
                else
                {
                    m_PreviewVersion = uint.MaxValue;
                }
            }
            else
                throw new FormatException($"Malformed package version string: {version}");
        }

        internal PackageVersion(uint major, uint minor, uint patch, uint preview, bool isPreview)
        {
            m_MajorVersion = major;
            m_MinorVersion = minor;
            m_PatchVersion = patch;
            m_PreviewVersion = isPreview ? preview : uint.MaxValue;
            m_IsPreview = isPreview;
            m_Version = GetValueString(m_MajorVersion, m_MinorVersion, m_PatchVersion, m_PreviewVersion, isPreview);
        }

        static string GetValueString(uint major, uint minor, uint patch, uint preview, bool isPreview)
        {
            var previewString = "";
            if (isPreview)
                previewString = $"-{k_PreviewTag}.{preview}";

            return $"{major}.{minor}.{patch}{previewString}";
        }

        public bool Equals(PackageVersion other)
        {
            return m_MajorVersion == other.m_MajorVersion
                && m_MinorVersion == other.m_MinorVersion
                && m_PatchVersion == other.m_PatchVersion
                && m_PreviewVersion == other.m_PreviewVersion
                && m_IsPreview == other.m_IsPreview;
        }

        public override bool Equals(object obj)
        {
            return obj is PackageVersion other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)m_MajorVersion;
                hashCode = (hashCode * 397) ^ (int)m_MinorVersion;
                hashCode = (hashCode * 397) ^ (int)m_PatchVersion;
                hashCode = (hashCode * 397) ^ (int)m_PreviewVersion;
                hashCode = (hashCode * 397) ^ m_IsPreview.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(PackageVersion left, PackageVersion right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PackageVersion left, PackageVersion right)
        {
            return !left.Equals(right);
        }

        public int CompareTo(PackageVersion other)
        {
            var compare = m_MajorVersion.CompareTo(other.m_MajorVersion);
            if (compare != 0)
                return compare;
            compare = m_MinorVersion.CompareTo(other.m_MinorVersion);
            if (compare != 0)
                return compare;
            compare = m_PatchVersion.CompareTo(other.m_PatchVersion);
            if (compare != 0)
                return compare;
            if (IsPreview)
            {
                if (other.IsPreview)
                    return m_PreviewVersion.CompareTo(other.m_PreviewVersion);
                return -1;
            }

            return other.IsPreview ? 1 : 0;
        }

        public static bool operator >(PackageVersion left, PackageVersion right)
        {
            return left.CompareTo(right) > 0;
        }

        public static bool operator <(PackageVersion left, PackageVersion right)
        {
            return left.CompareTo(right) < 0;
        }

        public static bool operator >=(PackageVersion left, PackageVersion right)
        {
            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(PackageVersion left, PackageVersion right)
        {
            return left.CompareTo(right) <= 0;
        }

        public static implicit operator PackageVersion(string version) => new PackageVersion(version);

        public override string ToString()
        {
            return m_Version;
        }
    }
}
