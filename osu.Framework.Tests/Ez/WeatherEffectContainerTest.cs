// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Tests.Ez
{
    [TestFixture]
    public class WeatherEffectContainerTest
    {
        private WeatherEffectContainer weather = null!;

        [SetUp]
        public void SetUp() => weather = new WeatherEffectContainer();

        [TearDown]
        public void TearDown() => weather.Dispose();

        [Test]
        public void TestDefaults()
        {
            Assert.Multiple(() =>
            {
                Assert.That(weather.Effect, Is.EqualTo(WeatherEffect.None));
                Assert.That(weather.Scope, Is.EqualTo(WeatherEffectScope.Global));
                Assert.That(weather.IsGameplay, Is.False);
                Assert.That(weather.IsEffectActive, Is.False);
                Assert.That(weather.Intensity, Is.EqualTo(1));
                Assert.That(weather.AnimationSpeed, Is.EqualTo(1));
                Assert.That(weather.PreviewTime, Is.Null);
                Assert.That(weather.DesertHeat, Is.EqualTo(DesertHeatSettings.Default));
                Assert.That(weather.Underwater, Is.EqualTo(UnderwaterSettings.Default));
                Assert.That(weather.OvercastRain, Is.EqualTo(OvercastRainSettings.Default));
            });
        }

        [Test]
        public void TestScopeMatrix([Values] WeatherEffect effect, [Values] WeatherEffectScope scope, [Values] bool gameplay)
        {
            weather.Effect = effect;
            weather.Scope = scope;
            weather.IsGameplay = gameplay;
            bool expected = effect != WeatherEffect.None
                            && (scope == WeatherEffectScope.Global || (scope == WeatherEffectScope.Gameplay && gameplay) || (scope == WeatherEffectScope.NonGameplay && !gameplay));
            Assert.That(weather.IsEffectActive, Is.EqualTo(expected));
            weather.Intensity = 0;
            Assert.That(weather.IsEffectActive, Is.False);
            weather.Intensity = 1;
            Assert.That(weather.IsEffectActive, Is.EqualTo(expected));
        }

        [Test]
        public void TestInvalidEnumsLeaveStateUnchanged()
        {
            weather.Effect = WeatherEffect.Underwater;
            weather.Scope = WeatherEffectScope.Gameplay;
            Assert.That(() => weather.Effect = (WeatherEffect)999, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => weather.Scope = (WeatherEffectScope)(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(weather.Effect, Is.EqualTo(WeatherEffect.Underwater));
            Assert.That(weather.Scope, Is.EqualTo(WeatherEffectScope.Gameplay));
        }

        private static IEnumerable<TestCaseData> parameterRanges()
        {
            yield return range("Intensity", (w, v) => w.Intensity = v, 0, 1);
            yield return range("AnimationSpeed", (w, v) => w.AnimationSpeed = v, 0, 10);
            yield return range("HeatAmplitude", (w, v) => w.DesertHeat = w.DesertHeat with { Amplitude = v }, 0, 32);
            yield return range("HeatScale", (w, v) => w.DesertHeat = w.DesertHeat with { Scale = v }, 0.1f, 8);
            yield return range("HeatTint", (w, v) => w.DesertHeat = w.DesertHeat with { Tint = v }, 0, 1);
            yield return range("WaterScale", (w, v) => w.Underwater = w.Underwater with { Scale = v }, 0.1f, 8);
            yield return range("WaterBrightness", (w, v) => w.Underwater = w.Underwater with { Brightness = v }, 0, 2);
            yield return range("WaterTint", (w, v) => w.Underwater = w.Underwater with { Tint = v }, 0, 1);
            yield return range("RainDensity", (w, v) => w.OvercastRain = w.OvercastRain with { Density = v }, 0, 1);
            yield return range("RainWind", (w, v) => w.OvercastRain = w.OvercastRain with { Wind = v }, -2, 2);
            yield return range("Cloudiness", (w, v) => w.OvercastRain = w.OvercastRain with { Cloudiness = v }, 0, 1);
        }

        private static TestCaseData range(string name, Action<WeatherEffectContainer, float> setter, float min, float max)
            => new TestCaseData(setter, min, max).SetName($"TestParameterRange({name})");

        [TestCaseSource(nameof(parameterRanges))]
        public void TestParameterRange(Action<WeatherEffectContainer, float> setter, float min, float max)
        {
            Assert.That(() => setter(weather, min), Throws.Nothing);
            Assert.That(() => setter(weather, max), Throws.Nothing);
            Assert.That(() => setter(weather, (min + max) / 2), Throws.Nothing);

            var heat = weather.DesertHeat;
            var water = weather.Underwater;
            var rain = weather.OvercastRain;
            float intensity = weather.Intensity;
            float speed = weather.AnimationSpeed;

            foreach (float invalid in new[] { min - 1, max + 1, float.NaN, float.NegativeInfinity, float.PositiveInfinity })
                Assert.That(() => setter(weather, invalid), Throws.TypeOf<ArgumentOutOfRangeException>());

            Assert.Multiple(() =>
            {
                Assert.That(weather.DesertHeat, Is.EqualTo(heat));
                Assert.That(weather.Underwater, Is.EqualTo(water));
                Assert.That(weather.OvercastRain, Is.EqualTo(rain));
                Assert.That(weather.Intensity, Is.EqualTo(intensity));
                Assert.That(weather.AnimationSpeed, Is.EqualTo(speed));
            });
        }

        [TestCase(double.NaN)]
        [TestCase(double.PositiveInfinity)]
        [TestCase(double.NegativeInfinity)]
        [TestCase(-1)]
        [TestCase(86401)]
        public void TestInvalidPreviewTime(double value)
        {
            weather.PreviewTime = 12.5;
            Assert.That(() => weather.PreviewTime = value, Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(weather.AnimationTime, Is.EqualTo(12.5));
        }

        [Test]
        public void TestIndependentInstances()
        {
            weather.Effect = WeatherEffect.DesertHeat;
            weather.PreviewTime = 86400;
            using var second = new WeatherEffectContainer();
            second.Effect = WeatherEffect.Underwater;
            weather.DesertHeat = weather.DesertHeat with { Amplitude = 32 };
            weather.Scope = WeatherEffectScope.Gameplay;
            Assert.That(second.DesertHeat, Is.EqualTo(DesertHeatSettings.Default));
            Assert.That(second.IsEffectActive, Is.True);
            Assert.That(second.PreviewTime, Is.Null);
            Assert.That(weather.AnimationTime, Is.EqualTo(86400));
            weather.PreviewTime = 0;
            Assert.That(weather.AnimationTime, Is.Zero);
            weather.PreviewTime = null;
            Assert.That(weather.AnimationTime, Is.Zero);
        }

        [Test]
        public void TestUniformLayoutMatchesStd140()
        {
            Assert.That(Marshal.SizeOf<WeatherEffectContainer.WeatherParameters>(), Is.EqualTo(64));
            Assert.That(Marshal.OffsetOf<WeatherEffectContainer.WeatherParameters>("Heat").ToInt32(), Is.EqualTo(16));
            Assert.That(Marshal.OffsetOf<WeatherEffectContainer.WeatherParameters>("Water").ToInt32(), Is.EqualTo(32));
            Assert.That(Marshal.OffsetOf<WeatherEffectContainer.WeatherParameters>("Rain").ToInt32(), Is.EqualTo(48));
        }
    }
}
