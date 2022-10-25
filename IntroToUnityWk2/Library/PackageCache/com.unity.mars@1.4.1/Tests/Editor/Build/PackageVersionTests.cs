using System;
using NUnit.Framework;
using UnityEditor.MARS.Build;

namespace Unity.MARS.Tests
{
    class PackageVersionTests
    {
        [Test]
        public void ParseStringPackageVersion()
        {
            // These should not throw
            var v1 = new PackageVersion("1.0.0");
            Assert.That(v1.IsPreview, Is.False);
            var v2 = new PackageVersion("1.0.0-preview");
            Assert.That(v2.IsPreview, Is.True);
            var v3 = new PackageVersion("1.0.0-preview.1");
            Assert.That(v3.IsPreview, Is.True);
            var v4 = new PackageVersion("1.0.0-pre.1");
            Assert.That(v4.IsPreview, Is.True);
            var v5 = new PackageVersion("1.0.0-abc.1");
            Assert.That(v5.IsPreview, Is.True);

            const string maxValue = "4294967295";
            const string maxPlusOneValue = "4294967296";
            Assert.DoesNotThrow(() => new PackageVersion($"{maxValue}.{maxValue}.{maxValue}-pre.{maxValue}"));
            Assert.Throws<OverflowException>(() => new PackageVersion($"{maxPlusOneValue}.0.0"));
            Assert.Throws<OverflowException>(() => new PackageVersion($"0.{maxPlusOneValue}.0"));
            Assert.Throws<OverflowException>(() => new PackageVersion($"0.0.{maxPlusOneValue}"));
            Assert.Throws<OverflowException>(() => new PackageVersion($"0.0.0-pre.{maxPlusOneValue}"));

            Assert.Throws<FormatException>(() => new PackageVersion("-1.0.0"));
            Assert.Throws<FormatException>(() => new PackageVersion("0.-1.0"));
            Assert.Throws<FormatException>(() => new PackageVersion("0.0.-1"));
            Assert.Throws<FormatException>(() => new PackageVersion("0.0.0-pre.-1"));

            Assert.DoesNotThrow(() =>
            {
                var v = new PackageVersion("");
                Assert.That(v == default);
                Assert.That(v == null);
            });

            Assert.Throws<FormatException>(() => new PackageVersion("1.0"));
            Assert.Throws<FormatException>(() => new PackageVersion("1.0.0.0"));
        }

        [Test]
        public void ImplicitCastTest()
        {
            Assert.DoesNotThrow(() =>
            {
                PackageVersion v =  "1.0.0";
                Assert.That(v.IsPreview, Is.False);
            });

            Assert.DoesNotThrow(() =>
            {
                PackageVersion v = string.Empty;
                Assert.That(v == default);
                Assert.That(v == null);
            });
        }

        [Test]
        public void ComparisonTest()
        {
            Assert.That(new PackageVersion("2.0.0") > "1.0.0");
            Assert.That(new PackageVersion("1.1.0") > "1.0.0");
            Assert.That(new PackageVersion("1.0.1") > "1.0.0");
            Assert.That(new PackageVersion("01.00.001") == "1.0.01");

            Assert.That(new PackageVersion("1.0.0") > "1.0.0-preview");
            Assert.That(new PackageVersion("1.0.0-preview.1") > "1.0.0-preview");
            Assert.That(new PackageVersion("1.0.0-preview.2") > "1.0.0-preview.1");

            Assert.That(new PackageVersion("1.0.0-preview.1") == new PackageVersion("1.0.0-pre.1"));
            Assert.That(new PackageVersion("1.0.00-preview.01") == new PackageVersion("1.0.0-pre.1"));

            Assert.That(new PackageVersion("1.0.0") >= "1.0.0");
            Assert.That(new PackageVersion("1.0.0") <= "1.0.0");
        }

        [Test]
        public void ConversionTest()
        {
            Assert.That(new PackageVersion("1.3.1").ToMajor() == new PackageVersion("1.3.4").ToMajor());
            Assert.That(new PackageVersion("1.3.1").ToMajorMinor() > new PackageVersion("1.3.4").ToMajor());
            Assert.That(new PackageVersion("1.3.5").ToMajorMinor() == new PackageVersion("1.3.4").ToMajorMinor());
            Assert.That(new PackageVersion("1.3.5").ToMajorMinorPatch() > new PackageVersion("1.3.4").ToMajorMinor());

            Assert.That(new PackageVersion("1.3.4").ToMajor() == new PackageVersion("1.3.4-pre.1").ToMajor());

            Assert.That(new PackageVersion("1.3.4").ToMajor().IsPreview == false);
            Assert.That(new PackageVersion("1.3.4-pre.1").ToMajor().IsPreview == false);

            Assert.That(new PackageVersion("1.3.1-pre.1").ToMajor() == new PackageVersion("1.3.1-pre.2").ToMajor());
            Assert.That(new PackageVersion("1.3.1-pre.1").ToMajorMinor() > new PackageVersion("1.4.1-pre.2").ToMajor());
            Assert.That(new PackageVersion("1.3.1-pre.1").ToMajorMinor() == new PackageVersion("1.3.1-pre.2").ToMajorMinor());
            Assert.That(new PackageVersion("1.3.1-pre.1").ToMajorMinorPatch() > new PackageVersion("1.3.1-pre.2").ToMajorMinor());
            Assert.That(new PackageVersion("1.3.1-pre.1").ToMajorMinorPatch() == new PackageVersion("1.3.1-pre.2").ToMajorMinorPatch());
        }

        [Test]
        public void GetPackageVersionTest()
        {
            Assert.That(PackageVersionUtility.GetPackageVersion("com.unity.mars") >= "1.0.0");
        }
    }
}
