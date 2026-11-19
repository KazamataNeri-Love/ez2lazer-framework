// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shaders;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// Applies animated weather to its children. Place around a background, a playfield, or the whole scene.
    /// Inactive effects draw children directly without allocating or rendering a framebuffer.
    /// </summary>
    /// <remarks>
    /// The caller supplies <see cref="IsGameplay"/>; the framework does not infer game-specific screen types.
    /// Input and child lifetimes are unchanged when weather is switched. Set properties on the update thread.
    /// </remarks>
    public partial class WeatherEffectContainer : Container, IBufferedDrawable
    {
        private readonly BufferedDrawNodeSharedData sharedData = new BufferedDrawNodeSharedData(clipToRootNode: false);
        private IShader textureShader = null!;
        private IShader heatShader = null!;
        private IShader underwaterShader = null!;
        private IShader rainShader = null!;

        private WeatherEffect effect;

        public WeatherEffect Effect
        {
            get => effect;
            set
            {
                if (!Enum.IsDefined(value))
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (effect == value)
                    return;

                effect = value;
                invalidateWeather();
            }
        }

        private WeatherEffectScope scope;

        public WeatherEffectScope Scope
        {
            get => scope;
            set
            {
                if (!Enum.IsDefined(value))
                    throw new ArgumentOutOfRangeException(nameof(value));

                if (scope == value)
                    return;

                scope = value;
                invalidateWeather();
            }
        }

        private bool isGameplay;

        /// <summary>
        /// Whether the caller's current scene is gameplay. Defaults to false.
        /// </summary>
        public bool IsGameplay
        {
            get => isGameplay;
            set
            {
                if (isGameplay == value)
                    return;

                isGameplay = value;
                invalidateWeather();
            }
        }

        private float intensity = 1;

        /// <summary>
        /// Overall strength (0–1). Zero bypasses weather completely.
        /// </summary>
        public float Intensity
        {
            get => intensity;
            set
            {
                ValidateRange(value, 0, 1);
                if (intensity == value)
                    return;

                intensity = value;
                invalidateWeather();
            }
        }

        private float animationSpeed = 1;

        /// <summary>
        /// Animation rate (0–10). Zero freezes at the current animation time.
        /// </summary>
        public float AnimationSpeed
        {
            get => animationSpeed;
            set
            {
                ValidateRange(value, 0, 10);
                animationSpeed = value;
            }
        }

        private double? previewTime;

        /// <summary>
        /// Optional fixed time in seconds (0–86400) for reproducible previews. Null resumes animation.
        /// </summary>
        public double? PreviewTime
        {
            get => previewTime;
            set
            {
                if (value.HasValue && (!double.IsFinite(value.Value) || value.Value < 0 || value.Value > 86400))
                    throw new ArgumentOutOfRangeException(nameof(value));

                previewTime = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private DesertHeatSettings desertHeat = DesertHeatSettings.Default;

        public DesertHeatSettings DesertHeat
        {
            get => desertHeat;
            set
            {
                ValidateRange(value.Amplitude, 0, 32);
                ValidateRange(value.Scale, 0.1f, 8);
                ValidateRange(value.Tint, 0, 1);
                desertHeat = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private UnderwaterSettings underwater = UnderwaterSettings.Default;

        public UnderwaterSettings Underwater
        {
            get => underwater;
            set
            {
                ValidateRange(value.Scale, 0.1f, 8);
                ValidateRange(value.Brightness, 0, 2);
                ValidateRange(value.Tint, 0, 1);
                underwater = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private OvercastRainSettings overcastRain = OvercastRainSettings.Default;

        public OvercastRainSettings OvercastRain
        {
            get => overcastRain;
            set
            {
                ValidateRange(value.Density, 0, 1);
                ValidateRange(value.Wind, -2, 2);
                ValidateRange(value.Cloudiness, 0, 1);
                overcastRain = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        public bool IsEffectActive => Effect != WeatherEffect.None && Intensity > 0
                                                                   && (Scope == WeatherEffectScope.Global || (Scope == WeatherEffectScope.Gameplay) == IsGameplay);

        private double elapsedSeconds;
        internal double AnimationTime => PreviewTime ?? elapsedSeconds;

        IShader ITexturedShaderDrawable.TextureShader => textureShader;
        Color4 IBufferedDrawable.BackgroundColour => Color4.Transparent;
        DrawColourInfo? IBufferedDrawable.FrameBufferDrawColour => base.DrawColourInfo;
        Vector2 IBufferedDrawable.FrameBufferScale => Vector2.One;

        // When buffered, apply the container colour once at composition, rather than once per child and again at composition.
        public override DrawColourInfo DrawColourInfo => IsEffectActive ? new DrawColourInfo(Color4.White, base.DrawColourInfo.Blending) : base.DrawColourInfo;

        [BackgroundDependencyLoader]
        private void load(ShaderManager shaders)
        {
            textureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
            heatShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.DESERT_HEAT);
            underwaterShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.UNDERWATER);
            rainShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.OVERCAST_RAIN);
        }

        protected override void Update()
        {
            base.Update();

            if (!IsEffectActive)
                return;

            if (!PreviewTime.HasValue)
                elapsedSeconds += Math.Max(0, Time.Elapsed) / 1000 * AnimationSpeed;

            // Children may animate even when the weather's own clock is paused.
            Invalidate(Invalidation.DrawNode);
        }

        private void invalidateWeather() => Invalidate(Invalidation.DrawNode | Invalidation.Colour);

        internal static void ValidateRange(float value, float min, float max)
        {
            if (!float.IsFinite(value) || value < min || value > max)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Expected a finite value between {min} and {max}.");
        }

        protected override DrawNode CreateDrawNode() => new WeatherEffectDrawNode(this, sharedData);

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            sharedData.Dispose();
        }
    }
}
