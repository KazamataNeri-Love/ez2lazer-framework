// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Framework.Testing.Drawables.Steps;
using osu.Framework.Timing;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Ez
{
    [System.ComponentModel.Description("Weather effects: heat haze, underwater caustics, blue-cyan light curtains, rain, and live tuning")]
    public partial class TestSceneWeatherEffects : ManualInputManagerTestScene
    {
        public override bool AutomaticallyRunFirstStep => false;

        private WeatherEffectContainer weather = null!;
        private Container originalPanel = null!;
        private WeatherTestBackground originalBackground = null!;
        private WeatherTestBackground effectBackground = null!;
        private BasicButton childButton = null!;
        private BasicButton foregroundButton = null!;
        private SpriteText? status;
        private StepSlider<float>? animationSpeedSlider;
        private int childClicks;
        private int foregroundClicks;

        // Slider initialisation invokes callbacks before the browser executes its queued [SetUp] steps.
        [BackgroundDependencyLoader]
        private void load() => createWeather();

        [SetUp]
        public void SetUpWeather() => Schedule(createWeather);

        private void createWeather()
        {
            Clear();
            childClicks = foregroundClicks = 0;
            Add(status = new SpriteText { Font = FrameworkFont.Regular.With(size: 15) });
            Add(originalPanel = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.48f, 0.9f),
                Y = 25,
                Masking = true,
                CornerRadius = 24,
                Children = new Drawable[]
                {
                    originalBackground = new WeatherTestBackground(),
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
                    effectBackground = new WeatherTestBackground(),
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
        }

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
            {
                string playback = weather.PreviewTime.HasValue ? "Fixed" : weather.AnimationSpeed == 0 ? "Paused" : "Live";
                status.Text = $"{weather.Effect} | {weather.Scope} | gameplay: {weather.IsGameplay} | active: {weather.IsEffectActive} | {playback}: {weather.AnimationTime:F2}s";
            }
        }

        [Test]
        public void TestTune()
        {
            double previewSeconds = 5;
            foreach (var effect in Enum.GetValues<WeatherEffect>())
                AddStep($"weather: {effect}", () => weather.Effect = effect);
            AddStep("preview heat", () => weather.Effect = WeatherEffect.DesertHeat);
            AddStep("preview underwater light curtain", ShowLightCurtainPreview);
            AddStep("wide light curtain preview", ShowWideLightCurtainPreview);
            AddStep("use comparison grid", () =>
            {
                restoreComparison();
                originalBackground.SetDeepWater(false);
                effectBackground.SetDeepWater(false);
            });
            foreach (var scope in Enum.GetValues<WeatherEffectScope>())
                AddStep($"scope: {scope}", () => weather.Scope = scope);
            AddStep("scope: global", () => weather.Scope = WeatherEffectScope.Global);
            AddToggleStep("simulate gameplay", value => weather.IsGameplay = value);
            AddSliderStep("intensity", 0f, 1f, 1f, value => weather.Intensity = value);
            Scheduler.Add(() =>
            {
                StepsContainer.Add(animationSpeedSlider = new StepSlider<float>("animation speed / 0 pauses", 0, 4, 1)
                {
                    ValueChanged = value => weather.AnimationSpeed = value,
                });
            }, false);
            AddStep("freeze at preview time", () => weather.PreviewTime = previewSeconds);
            AddSliderStep("preview time (seconds)", 0f, 60f, 5f, value =>
            {
                previewSeconds = value;
                if (weather.PreviewTime.HasValue)
                    weather.PreviewTime = value;
            });
            AddStep("resume live animation", resumeLiveAnimation);
            AddSliderStep("heat: distortion (px)", 0f, 32f, 4f, value => weather.DesertHeat = weather.DesertHeat with { Amplitude = value });
            AddSliderStep("heat: wave scale", 0.1f, 8f, 1f, value => weather.DesertHeat = weather.DesertHeat with { Scale = value });
            AddSliderStep("heat: warm tint", 0f, 1f, 0.15f, value => weather.DesertHeat = weather.DesertHeat with { Tint = value });
            AddSliderStep("water: caustic scale", 0.1f, 8f, 1f, value => weather.Underwater = weather.Underwater with { Scale = value });
            AddSliderStep("water: brightness", 0f, 2f, 0.65f, value => weather.Underwater = weather.Underwater with { Brightness = value });
            AddSliderStep("water: blue-green tint", 0f, 1f, 0.3f, value => weather.Underwater = weather.Underwater with { Tint = value });
            AddSliderStep("curtain: fold scale", 0.1f, 8f, 1f, value => weather.UnderwaterLightCurtain = weather.UnderwaterLightCurtain with { Scale = value });
            AddSliderStep("curtain: brightness", 0f, 2f, 0.5f, value => weather.UnderwaterLightCurtain = weather.UnderwaterLightCurtain with { Brightness = value });
            AddSliderStep("curtain: downward falloff", 0.1f, 8f, 2f, value => weather.UnderwaterLightCurtain = weather.UnderwaterLightCurtain with { Falloff = value });
            AddSliderStep("curtain: sway", 0f, 1f, 0.35f, value => weather.UnderwaterLightCurtain = weather.UnderwaterLightCurtain with { Sway = value });
            AddSliderStep("rain: density", 0f, 1f, 0.45f, value => weather.OvercastRain = weather.OvercastRain with { Density = value });
            AddSliderStep("rain: wind", -2f, 2f, 0.2f, value => weather.OvercastRain = weather.OvercastRain with { Wind = value });
            AddSliderStep("rain: overcast", 0f, 1f, 0.4f, value => weather.OvercastRain = weather.OvercastRain with { Cloudiness = value });
            AddToggleStep("rounded clipping", value => weather.CornerRadius = value ? 70 : 0);
            AddSliderStep("panel opacity", 0.1f, 1f, 1f, value => weather.Alpha = value);
            AddStep("reset all weather parameters", () =>
            {
                restoreComparison();
                weather.Effect = WeatherEffect.DesertHeat;
                weather.Scope = WeatherEffectScope.Global;
                weather.IsGameplay = false;
                weather.Intensity = weather.AnimationSpeed = weather.Alpha = 1;
                animationSpeedSlider?.Current.SetDefault();
                resumeLiveAnimation();
                weather.DesertHeat = DesertHeatSettings.Default;
                weather.Underwater = UnderwaterSettings.Default;
                weather.UnderwaterLightCurtain = UnderwaterLightCurtainSettings.Default;
                weather.OvercastRain = OvercastRainSettings.Default;
                weather.CornerRadius = 24;
                originalBackground.SetDeepWater(false);
                effectBackground.SetDeepWater(false);
            });
        }

        public void ShowLightCurtainPreview()
        {
            restoreComparison();
            weather.Effect = WeatherEffect.UnderwaterLightCurtain;
            resumeLiveAnimation();
            originalBackground.SetDeepWater(true);
            effectBackground.SetDeepWater(true);
        }

        private void resumeLiveAnimation()
        {
            weather.PreviewTime = null;
            if (weather.AnimationSpeed > 0)
                return;

            weather.AnimationSpeed = 1;
            animationSpeedSlider?.Current.SetDefault();
        }

        public void ShowWideLightCurtainPreview()
        {
            ShowLightCurtainPreview();
            originalPanel.Hide();
            weather.Width = 1;
            weather.X = 0;
        }

        private void restoreComparison()
        {
            originalPanel.Show();
            weather.Width = 0.48f;
            weather.X = 0.52f;
        }

        [Test]
        public void TestUnderwaterLightCurtainPreview()
        {
            AddStep("show deep-water light curtain", ShowLightCurtainPreview);
            AddAssert("light curtain is active", () => weather.Effect == WeatherEffect.UnderwaterLightCurtain && weather.IsEffectActive);
            AddStep("freeze the last preview and set animation speed to zero", () =>
            {
                weather.PreviewTime = 5;
                weather.AnimationSpeed = 0;
            });
            AddStep("show wide light curtain", ShowWideLightCurtainPreview);
            AddAssert("wide preview fills the scene", () => weather.Width == 1 && weather.X == 0 && originalPanel.Alpha == 0);
            AddAssert("wide preview resumes live animation", () => weather.PreviewTime == null && weather.AnimationSpeed > 0);
            AddStep("restore comparison preview", ShowLightCurtainPreview);
            AddAssert("comparison layout is restored", () => weather.Width == 0.48f && weather.X == 0.52f && originalPanel.Alpha == 1);
        }

        [TestCase(WeatherEffect.DesertHeat)]
        [TestCase(WeatherEffect.Underwater)]
        [TestCase(WeatherEffect.OvercastRain)]
        [TestCase(WeatherEffect.UnderwaterLightCurtain)]
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
        private readonly Box background;
        private readonly Box moving;

        public WeatherTestBackground()
        {
            RelativeSizeAxes = Axes.Both;
            Add(background = new Box
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

        public void SetDeepWater(bool deepWater)
        {
            background.Colour = deepWater
                ? ColourInfo.GradientVertical(new Color4(0.02f, 0.13f, 0.24f, 1), new Color4(0.005f, 0.02f, 0.065f, 1))
                : ColourInfo.GradientVertical(new Color4(0.08f, 0.19f, 0.3f, 1), new Color4(0.55f, 0.4f, 0.22f, 1));

            foreach (var drawable in Children)
            {
                if (drawable != background)
                    drawable.Alpha = deepWater ? 0 : 1;
            }
        }
    }
}
