// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Graphics.Containers
{
    public enum WeatherEffect
    {
        None,
        DesertHeat,
        Underwater,
        OvercastRain,
    }

    public enum WeatherEffectScope
    {
        Global,
        Gameplay,
        NonGameplay,
    }

    /// <summary>
    /// Heat distortion in screen pixels (0–32), wave scale (0.1–8), and warm tint (0–1).
    /// </summary>
    public readonly record struct DesertHeatSettings(float Amplitude, float Scale, float Tint)
    {
        public static DesertHeatSettings Default => new DesertHeatSettings(4, 1, 0.15f);
    }

    /// <summary>
    /// Caustic scale (0.1–8), light brightness (0–2), and underwater tint (0–1).
    /// </summary>
    public readonly record struct UnderwaterSettings(float Scale, float Brightness, float Tint)
    {
        public static UnderwaterSettings Default => new UnderwaterSettings(1, 0.65f, 0.3f);
    }

    /// <summary>
    /// Rain density (0–1), horizontal wind (−2–2), and overcast darkening (0–1).
    /// </summary>
    public readonly record struct OvercastRainSettings(float Density, float Wind, float Cloudiness)
    {
        public static OvercastRainSettings Default => new OvercastRainSettings(0.45f, 0.2f, 0.4f);
    }
}
