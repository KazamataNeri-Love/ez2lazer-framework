// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Framework.Testing.Drawables.Steps;

namespace osu.Framework.Tests.Visual.Ez
{
    [HeadlessTest]
    public partial class TestSceneWeatherEffectsLoading : FrameworkTestScene
    {
        [Test]
        public void TestBrowserLoadingAndReloading()
        {
            TestBrowser browser = null!;
            TestSceneWeatherEffects scene = null!;
            WeatherEffectContainer weather = null!;

            AddStep("create weather test browser", () =>
            {
                Clear();
                browser = new TestBrowser();
                browser.TestTypes.Clear();
                browser.TestTypes.Add(typeof(TestSceneWeatherEffects));
                Add(browser);
            });

            for (int i = 0; i < 2; i++)
            {
                if (i > 0)
                    AddStep("reload weather scene", () => browser.LoadTest(typeof(TestSceneWeatherEffects), isHotReload: true));

                AddUntilStep("all parameter sliders are loaded", () =>
                    browser.CurrentTest is TestSceneWeatherEffects loaded
                    && loaded.StepsContainer.OfType<StepSlider<float>>().Count(s => s.IsLoaded) == 13);
                AddStep("pause automatic test steps", () =>
                {
                    scene = (TestSceneWeatherEffects)browser.CurrentTest;
                    scene.RunAllSteps(stopCondition: _ => true);
                });
                AddAssert("weather exists before parameter interaction", () => scene.Children.OfType<WeatherEffectContainer>().Count() == 1);

                // Sliders initialise before the browser runs [SetUp], and must also target the replacement created by [SetUp].
                for (int reset = 0; reset < 2; reset++)
                {
                    if (reset > 0)
                        AddStep("reset weather scene", () => scene.SetUpWeather());

                    AddStep("set every slider to its minimum", () =>
                    {
                        weather = scene.Children.OfType<WeatherEffectContainer>().Single();
                        foreach (var slider in scene.StepsContainer.OfType<StepSlider<float>>())
                            slider.Current.Value = ((BindableNumber<float>)slider.Current).MinValue;
                    });
                    AddAssert("sliders update the current weather container", () =>
                        weather.Intensity == 0 && weather.AnimationSpeed == 0 && weather.Alpha == 0.1f
                        && weather.DesertHeat == new DesertHeatSettings(0, 0.1f, 0)
                        && weather.Underwater == new UnderwaterSettings(0.1f, 0, 0)
                        && weather.OvercastRain == new OvercastRainSettings(0, -2, 0));
                    AddStep("restore slider defaults", () =>
                    {
                        foreach (var slider in scene.StepsContainer.OfType<StepSlider<float>>())
                            slider.Current.SetDefault();
                    });
                    AddAssert("sliders restore default weather parameters", () =>
                        weather.Intensity == 1 && weather.AnimationSpeed == 1 && weather.Alpha == 1
                        && weather.DesertHeat == DesertHeatSettings.Default
                        && weather.Underwater == UnderwaterSettings.Default
                        && weather.OvercastRain == OvercastRainSettings.Default);
                }
            }

            AddStep("dispose browser", Clear);
        }
    }
}
