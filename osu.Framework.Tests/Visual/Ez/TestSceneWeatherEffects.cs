// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Ez
{
    [System.ComponentModel.Description("Weather effects: original comparison, heat haze, underwater caustics, rain, and live tuning")]
    public partial class TestSceneWeatherEffects : ManualInputManagerTestScene
    {
        public override bool AutomaticallyRunFirstStep => false;

        private WeatherEffectContainer weather = null!;
        private BasicButton childButton = null!;
        private BasicButton foregroundButton = null!;
        private SpriteText? status;
        private int childClicks;
        private int foregroundClicks;

        [SetUp]
        public void SetUpWeather() => Schedule(() =>
        {
            Clear();
            childClicks = foregroundClicks = 0;
            Add(status = new SpriteText { Font = FrameworkFont.Regular.With(size: 15) });
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.48f, 0.9f),
                Y = 25,
                Masking = true,
                CornerRadius = 24,
                Children = new Drawable[]
                {
                    new WeatherTestBackground(),
                    label("ORIGINAL"),
                },
            });
            Add(weather = new WeatherEffectContainer
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.48f, 0.9f),
                RelativePositionAxes = Axes.X,
                X = 0.52f,
                Y = 25,
                Masking = true,
                CornerRadius = 24,
                Effect = WeatherEffect.DesertHeat,
                Children = new Drawable[]
                {
                    new WeatherTestBackground(),
                    label("WEATHER"),
                    childButton = new BasicButton
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Size = new Vector2(200, 36),
                        Y = -70,
                        Text = "Inside weather: click me",
                        Action = () => childClicks++,
                    },
                },
            });
            Add(foregroundButton = new BasicButton
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Y = -12,
                Size = new Vector2(280, 36),
                Text = "Independent foreground / HUD",
                Action = () => foregroundClicks++,
            });
        });

        private static SpriteText label(string text) => new SpriteText
        {
            Text = text,
            Position = new Vector2(18, 16),
            Font = FrameworkFont.Regular.With(size: 24),
        };

        protected override void Update()
        {
            base.Update();
            if (status != null)
                status.Text = $"{weather.Effect} | {weather.Scope} | gameplay: {weather.IsGameplay} | active: {weather.IsEffectActive} | time: {weather.AnimationTime:F2}s";
        }

        [Test]
        public void TestTune()
        {
            double previewSeconds = 5;
            foreach (var effect in Enum.GetValues<WeatherEffect>())
                AddStep($"weather: {effect}", () => weather.Effect = effect);
            AddStep("preview heat", () => weather.Effect = WeatherEffect.DesertHeat);
            foreach (var scope in Enum.GetValues<WeatherEffectScope>())
                AddStep($"scope: {scope}", () => weather.Scope = scope);
            AddStep("scope: global", () => weather.Scope = WeatherEffectScope.Global);
            AddToggleStep("simulate gameplay", value => weather.IsGameplay = value);
            AddSliderStep("intensity", 0f, 1f, 1f, value => weather.Intensity = value);
            AddSliderStep("animation speed / 0 pauses", 0f, 4f, 1f, value => weather.AnimationSpeed = value);
            AddToggleStep("fixed preview time", value => weather.PreviewTime = value ? previewSeconds : null);
            AddSliderStep("preview time (seconds)", 0f, 60f, 5f, value =>
            {
                previewSeconds = value;
                if (weather.PreviewTime.HasValue)
                    weather.PreviewTime = value;
            });
            AddStep("resume live animation", () => weather.PreviewTime = null);
            AddSliderStep("heat: distortion (px)", 0f, 32f, 4f, value => weather.DesertHeat = weather.DesertHeat with { Amplitude = value });
            AddSliderStep("heat: wave scale", 0.1f, 8f, 1f, value => weather.DesertHeat = weather.DesertHeat with { Scale = value });
            AddSliderStep("heat: warm tint", 0f, 1f, 0.15f, value => weather.DesertHeat = weather.DesertHeat with { Tint = value });
            AddSliderStep("water: caustic scale", 0.1f, 8f, 1f, value => weather.Underwater = weather.Underwater with { Scale = value });
            AddSliderStep("water: brightness", 0f, 2f, 0.65f, value => weather.Underwater = weather.Underwater with { Brightness = value });
            AddSliderStep("water: blue-green tint", 0f, 1f, 0.3f, value => weather.Underwater = weather.Underwater with { Tint = value });
            AddSliderStep("rain: density", 0f, 1f, 0.45f, value => weather.OvercastRain = weather.OvercastRain with { Density = value });
            AddSliderStep("rain: wind", -2f, 2f, 0.2f, value => weather.OvercastRain = weather.OvercastRain with { Wind = value });
            AddSliderStep("rain: overcast", 0f, 1f, 0.4f, value => weather.OvercastRain = weather.OvercastRain with { Cloudiness = value });
            AddToggleStep("rounded clipping", value => weather.CornerRadius = value ? 70 : 0);
            AddSliderStep("panel opacity", 0.1f, 1f, 1f, value => weather.Alpha = value);
            AddStep("reset all weather parameters", () =>
            {
                weather.Effect = WeatherEffect.DesertHeat;
                weather.Scope = WeatherEffectScope.Global;
                weather.IsGameplay = false;
                weather.Intensity = weather.AnimationSpeed = weather.Alpha = 1;
                weather.PreviewTime = null;
                weather.DesertHeat = DesertHeatSettings.Default;
                weather.Underwater = UnderwaterSettings.Default;
                weather.OvercastRain = OvercastRainSettings.Default;
                weather.CornerRadius = 24;
            });
        }

        [TestCase(WeatherEffect.DesertHeat)]
        [TestCase(WeatherEffect.Underwater)]
        [TestCase(WeatherEffect.OvercastRain)]
        public void TestSwitchingPreservesChildrenAndInput(WeatherEffect effect)
        {
            Drawable[]? originalChildren = null;
            AddStep("set weather", () =>
            {
                weather.Effect = effect;
                originalChildren = weather.Children.ToArray();
            });
            clickChildAndAssert(1);
            AddStep("zero intensity bypass", () => weather.Intensity = 0);
            AddAssert("inactive", () => !weather.IsEffectActive);
            clickChildAndAssert(2);
            AddStep("gameplay scope outside gameplay", () =>
            {
                weather.Intensity = 1;
                weather.Scope = WeatherEffectScope.Gameplay;
            });
            AddAssert("outside scope", () => !weather.IsEffectActive);
            clickChildAndAssert(3);
            AddStep("enter gameplay", () => weather.IsGameplay = true);
            AddAssert("active in gameplay", () => weather.IsEffectActive);
            clickChildAndAssert(4);
            AddStep("disable effect", () => weather.Effect = WeatherEffect.None);
            clickChildAndAssert(5);
            AddAssert("same child instances and lifetime", () => weather.Children.SequenceEqual(originalChildren!) && childButton.IsAlive && !childButton.IsDisposed);
            AddStep("click independent foreground", () =>
            {
                InputManager.MoveMouseTo(foregroundButton);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("foreground receives input", () => foregroundClicks == 1);
        }

        private void clickChildAndAssert(int count)
        {
            AddStep("click inside weather", () =>
            {
                InputManager.MoveMouseTo(childButton);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert($"child click count = {count}", () => childClicks == count);
        }

        [Test]
        public void TestAnimationClock()
        {
            var manual = new ManualClock();
            double frozen = 0;
            AddStep("use manual clock", () => weather.Clock = new FramedClock(manual));
            AddStep("advance one second", () => manual.CurrentTime = 1000);
            AddStep("remember time and pause", () =>
            {
                frozen = weather.AnimationTime;
                weather.AnimationSpeed = 0;
            });
            AddStep("advance while paused", () => manual.CurrentTime = 2000);
            AddAssert("time remains frozen", () => weather.AnimationTime == frozen);
            AddStep("set deterministic preview", () => weather.PreviewTime = 12.5);
            AddStep("advance while previewing", () => manual.CurrentTime = 3000);
            AddAssert("fixed time is exact", () => weather.AnimationTime == 12.5);
            AddStep("resume at twice speed", () =>
            {
                weather.PreviewTime = null;
                weather.AnimationSpeed = 2;
            });
            AddStep("advance another second", () => manual.CurrentTime = 4000);
            AddAssert("speed applies to elapsed time", () => Math.Abs(weather.AnimationTime - frozen - 2) < 0.001);
        }

        [Test]
        public void TestWeatherDoesNotPauseChildren()
        {
            UpdatingBox counter = null!;
            int previous = 0;
            AddStep("add animated child and freeze weather", () =>
            {
                weather.Add(counter = new UpdatingBox { Size = new Vector2(20) });
                weather.PreviewTime = 5;
                weather.AnimationSpeed = 0;
            });
            AddStep("remember child update count", () => previous = counter.Updates);
            AddAssert("child updates during fixed preview", () => counter.Updates > previous);
            AddStep("disable weather", () =>
            {
                weather.Effect = WeatherEffect.None;
                previous = counter.Updates;
            });
            AddAssert("child updates in bypass", () => counter.Updates > previous);
            AddStep("reactivate weather", () =>
            {
                weather.Effect = WeatherEffect.Underwater;
                previous = counter.Updates;
            });
            AddAssert("child continues after reactivation", () => counter.Updates > previous);
        }

        private partial class UpdatingBox : Box
        {
            public int Updates { get; private set; }

            protected override void Update()
            {
                base.Update();
                Updates++;
            }
        }
    }

    /// <summary>
    /// A generated test image: no external assets, with sharp lines to reveal heat refraction.
    /// </summary>
    internal partial class WeatherTestBackground : Container
    {
        public bool Animate { get; set; } = true;
        private readonly Box moving;

        public WeatherTestBackground()
        {
            RelativeSizeAxes = Axes.Both;
            Add(new Box
            {
                RelativeSizeAxes = Axes.Both,
                Colour = ColourInfo.GradientVertical(new Color4(0.08f, 0.19f, 0.3f, 1), new Color4(0.55f, 0.4f, 0.22f, 1)),
            });

            for (int i = 1; i < 12; i++)
            {
                Add(new Box { RelativeSizeAxes = Axes.Y, RelativePositionAxes = Axes.X, X = i / 12f, Width = 2, Colour = new Color4(1, 1, 1, 0.3f) });
                Add(new Box { RelativeSizeAxes = Axes.X, RelativePositionAxes = Axes.Y, Y = i / 12f, Height = 2, Colour = new Color4(1, 1, 1, 0.3f) });
            }

            Add(new Circle
            {
                RelativePositionAxes = Axes.Both,
                Position = new Vector2(0.7f, 0.25f),
                Origin = Anchor.Centre,
                Size = new Vector2(95),
                Colour = new Color4(1, 0.78f, 0.36f, 1),
            });
            Add(moving = new Box
            {
                RelativePositionAxes = Axes.Both,
                Position = new Vector2(0.4f, 0.55f),
                Origin = Anchor.Centre,
                Size = new Vector2(100, 65),
                Colour = new Color4(0.2f, 0.85f, 0.72f, 1),
                Rotation = 20,
            });
        }

        protected override void Update()
        {
            base.Update();
            if (Animate)
                moving.Rotation = 20 + (float)(Time.Current / 70);
        }
    }
}
