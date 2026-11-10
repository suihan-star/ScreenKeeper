using OLED_Sleeper.Features.ColorTemperature;
using OLED_Sleeper.Native;
using System.Runtime.InteropServices;

namespace OLED_Sleeper.Tests.Features.ColorTemperature
{
    public class AdvancedColorEligibilityTests
    {
        [Theory]
        [InlineData(0u, 0u)]
        [InlineData(1u, 1u)]
        [InlineData(4u, 4u)]
        [InlineData(5u, 69u)]
        [InlineData(1u, 81u)]
        [InlineData(5u, 85u)]
        public void DetailedSdrDistinguishesSupportedAndReservedFlagsFromActiveColor(uint legacyFlags, uint detailedFlags)
        {
            // The 5/69 pair is the observed SDR state on both affected monitors.
            Assert.False(WindowsGammaBackend.EvaluateAdvancedColor(0, legacyFlags, 0, detailedFlags, 0));
        }

        [Theory]
        [InlineData(0, 0u, 1u)]
        [InlineData(0, 0u, 2u)]
        [InlineData(0, 2u, 0u)]
        [InlineData(0, 3u, 0u)]
        [InlineData(0, 6u, 0u)]
        [InlineData(0, 69u, 1u)]
        [InlineData(0, 69u, 2u)]
        [InlineData(5, 0u, 1u)]
        [InlineData(5, 0u, 2u)]
        [InlineData(5, 2u, 0u)]
        public void DetailedWcgHdrOrActiveFlagNeverQualifiesAsSdr(int legacyResult, uint detailedFlags, uint activeMode)
        {
            Assert.True(WindowsGammaBackend.EvaluateAdvancedColor(legacyResult, 0, 0, detailedFlags, activeMode));
        }

        [Theory]
        [InlineData(2u, 0u)]
        [InlineData(3u, 69u)]
        [InlineData(7u, 85u)]
        public void ConflictingLegacyEnabledFlagKeepsProtection(uint legacyFlags, uint detailedFlags)
        {
            Assert.True(WindowsGammaBackend.EvaluateAdvancedColor(0, legacyFlags, 0, detailedFlags, 0));
        }

        [Theory]
        [InlineData(0u, 3u)]
        [InlineData(2u, 3u)]
        [InlineData(69u, uint.MaxValue)]
        public void UnknownDetailedModeCannotEstablishSdr(uint detailedFlags, uint activeMode)
        {
            Assert.Null(WindowsGammaBackend.EvaluateAdvancedColor(0, 0, 0, detailedFlags, activeMode));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(87)]
        [InlineData(1167)]
        public void DetailedSdrStillRequiresSuccessfulLegacyRead(int legacyResult)
        {
            Assert.Null(WindowsGammaBackend.EvaluateAdvancedColor(legacyResult, 5, 0, 69, 0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(87)]
        [InlineData(120)]
        public void OlderSystemsKeepConservativeLegacyEligibility(int unsupportedResult)
        {
            Assert.False(WindowsGammaBackend.EvaluateAdvancedColor(0, 0, unsupportedResult, 0, 0));
            Assert.False(WindowsGammaBackend.EvaluateAdvancedColor(0, 1, unsupportedResult, 0, 0));
            Assert.True(WindowsGammaBackend.EvaluateAdvancedColor(0, 2, unsupportedResult, 0, 0));
            Assert.True(WindowsGammaBackend.EvaluateAdvancedColor(0, 4, unsupportedResult, 0, 0));
            Assert.True(WindowsGammaBackend.EvaluateAdvancedColor(0, 5, unsupportedResult, 0, 0));
            Assert.True(WindowsGammaBackend.EvaluateAdvancedColor(0, 7, unsupportedResult, 0, 0));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(87)]
        [InlineData(120)]
        public void UnsupportedDetailedQueryCannotCompensateForFailedLegacyRead(int unsupportedResult)
        {
            Assert.Null(WindowsGammaBackend.EvaluateAdvancedColor(5, 0, unsupportedResult, 0, 0));
            Assert.Null(WindowsGammaBackend.EvaluateAdvancedColor(5, 7, unsupportedResult, 0, 0));
        }

        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(122)]
        [InlineData(1167)]
        public void UnexpectedDetailedReadFailureCannotFallBackToAnApparentSdrState(int detailedResult)
        {
            Assert.Null(WindowsGammaBackend.EvaluateAdvancedColor(0, 0, detailedResult, 0, 0));
            Assert.Null(WindowsGammaBackend.EvaluateAdvancedColor(0, 7, detailedResult, 0, 0));
        }

        [Fact]
        public void ConfirmedSdrKeepsExistingRecoveryAndValidationScope()
        {
            bool? advanced = WindowsGammaBackend.EvaluateAdvancedColor(0, 5, 0, 69, 0);
            Assert.False(advanced);
            Assert.Empty(WindowsGammaBackend.AdvancedColorModeSuffix(0, 69, 0, advanced));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(50)]
        [InlineData(87)]
        [InlineData(120)]
        public void LegacySdrFallbackKeepsExistingModeIdentity(int unsupportedResult)
        {
            foreach (uint legacyFlags in new uint[] { 0, 1 })
            {
                bool? advanced = WindowsGammaBackend.EvaluateAdvancedColor(0, legacyFlags, unsupportedResult, 0, 0);
                Assert.Empty(WindowsGammaBackend.AdvancedColorModeSuffix(unsupportedResult, 0, 0, advanced));
            }
        }

        [Theory]
        [InlineData(0, 5u, 0, 69u, 1u)]
        [InlineData(0, 5u, 0, 69u, 2u)]
        [InlineData(0, 5u, 0, 71u, 0u)]
        [InlineData(0, 7u, 0, 69u, 0u)]
        [InlineData(0, 5u, 0, 69u, 3u)]
        [InlineData(5, 5u, 0, 69u, 0u)]
        [InlineData(0, 5u, 5, 0u, 0u)]
        [InlineData(0, 5u, 1167, 0u, 0u)]
        [InlineData(0, 5u, 1, 0u, 0u)]
        [InlineData(0, 5u, 50, 0u, 0u)]
        [InlineData(0, 5u, 87, 0u, 0u)]
        [InlineData(0, 5u, 120, 0u, 0u)]
        [InlineData(5, 0u, 87, 0u, 0u)]
        public void NonSdrOrUncertainDetailedStateCannotReuseConfirmedSdrScope(int legacyResult, uint legacyFlags,
            int detailedResult, uint detailedFlags, uint activeMode)
        {
            bool? advanced = WindowsGammaBackend.EvaluateAdvancedColor(legacyResult, legacyFlags, detailedResult, detailedFlags, activeMode);
            string suffix = WindowsGammaBackend.AdvancedColorModeSuffix(detailedResult, detailedFlags, activeMode, advanced);
            Assert.NotEmpty(suffix);
            Assert.NotEqual(WindowsGammaBackend.AdvancedColorModeSuffix(0, 69, 0, false), suffix);
        }

        [Fact]
        public void DifferentDetailedModesAndReadFailuresHaveDistinctScopeMarkers()
        {
            var markers = new[]
            {
                WindowsGammaBackend.AdvancedColorModeSuffix(0, 69, 1, true),
                WindowsGammaBackend.AdvancedColorModeSuffix(0, 69, 2, true),
                WindowsGammaBackend.AdvancedColorModeSuffix(0, 69, 3, null),
                WindowsGammaBackend.AdvancedColorModeSuffix(5, 0, 0, null),
                WindowsGammaBackend.AdvancedColorModeSuffix(1167, 0, 0, null)
            };
            Assert.Equal(markers.Length, markers.Distinct().Count());
        }

        [Fact]
        public void NativeAdvancedColorStructuresMatchWindowsLayoutWithoutCallingHardware()
        {
            Assert.Equal(20, Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER>());
            Assert.Equal(32, Marshal.SizeOf<ColorNative.AdvancedColorInfo>());
            Assert.Equal(36, Marshal.SizeOf<ColorNative.AdvancedColorInfo2>());
            Assert.Equal(0, Marshal.OffsetOf<ColorNative.AdvancedColorInfo>(nameof(ColorNative.AdvancedColorInfo.Header)).ToInt32());
            Assert.Equal(20, Marshal.OffsetOf<ColorNative.AdvancedColorInfo>(nameof(ColorNative.AdvancedColorInfo.Flags)).ToInt32());
            Assert.Equal(0, Marshal.OffsetOf<ColorNative.AdvancedColorInfo2>(nameof(ColorNative.AdvancedColorInfo2.Header)).ToInt32());
            Assert.Equal(20, Marshal.OffsetOf<ColorNative.AdvancedColorInfo2>(nameof(ColorNative.AdvancedColorInfo2.Flags)).ToInt32());
            Assert.Equal(24, Marshal.OffsetOf<ColorNative.AdvancedColorInfo2>(nameof(ColorNative.AdvancedColorInfo2.ColorEncoding)).ToInt32());
            Assert.Equal(28, Marshal.OffsetOf<ColorNative.AdvancedColorInfo2>(nameof(ColorNative.AdvancedColorInfo2.BitsPerColorChannel)).ToInt32());
            Assert.Equal(32, Marshal.OffsetOf<ColorNative.AdvancedColorInfo2>(nameof(ColorNative.AdvancedColorInfo2.ActiveColorMode)).ToInt32());
        }
    }
}
