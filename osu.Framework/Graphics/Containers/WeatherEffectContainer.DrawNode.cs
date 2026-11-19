// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Graphics.Containers
{
    public partial class WeatherEffectContainer
    {
        private class WeatherEffectDrawNode : CompositeDrawableDrawNode
        {
            protected new WeatherEffectContainer Source => (WeatherEffectContainer)base.Source;

            private readonly WeatherBufferedDrawNode buffered;
            private readonly BufferedDrawNodeSharedData sharedData;
            private bool active;

            public WeatherEffectDrawNode(WeatherEffectContainer source, BufferedDrawNodeSharedData sharedData)
                : base(source)
            {
                this.sharedData = sharedData;
                buffered = new WeatherBufferedDrawNode(source, new CompositeDrawableDrawNode(source), sharedData);
            }

            public override void ApplyState()
            {
                base.ApplyState();
                active = Source.IsEffectActive;
                if (active)
                    buffered.ApplyState();
            }

            protected override void Draw(IRenderer renderer)
            {
                if (!active)
                {
                    // Release on the draw thread, after any older active draw nodes have finished.
                    sharedData.ReleaseBuffers();
                    base.Draw(renderer);
                    return;
                }

                buffered.Capture.Children = Children;
                DrawOther(buffered, renderer);
            }

            // Buffered children must never write their opaque interiors into the screen depth buffer.
            protected internal override bool CanDrawOpaqueInterior => true;

            protected override void DrawOpaqueInterior(IRenderer renderer)
            {
                if (active)
                    DrawOtherOpaqueInterior(buffered, renderer);
                else
                    base.DrawOpaqueInterior(renderer);
            }

            protected override void Dispose(bool isDisposing)
            {
                buffered.Dispose();
                base.Dispose(isDisposing);
            }
        }

        private class WeatherBufferedDrawNode : BufferedDrawNode
        {
            protected new WeatherEffectContainer Source => (WeatherEffectContainer)base.Source;
            public CompositeDrawableDrawNode Capture => (CompositeDrawableDrawNode)Child;

            private IShader shader = null!;
            private WeatherParameters parameters;
            private IUniformBuffer<WeatherParameters>? parametersBuffer;

            public WeatherBufferedDrawNode(WeatherEffectContainer source, CompositeDrawableDrawNode capture, BufferedDrawNodeSharedData sharedData)
                : base(source, capture, sharedData)
            {
            }

            public override void ApplyState()
            {
                base.ApplyState();
                shader = Source.Effect switch
                {
                    WeatherEffect.DesertHeat => Source.heatShader,
                    WeatherEffect.Underwater => Source.underwaterShader,
                    WeatherEffect.UnderwaterLightCurtain => Source.lightCurtainShader,
                    _ => Source.rainShader,
                };

                parameters.Time = (float)Source.AnimationTime;
                parameters.Intensity = Source.Intensity;
                parameters.Size = DrawRectangle.Size;
                parameters.Heat = new osuTK.Vector4(Source.DesertHeat.Amplitude, Source.DesertHeat.Scale, Source.DesertHeat.Tint, 0);
                parameters.Water = new osuTK.Vector4(Source.Underwater.Scale, Source.Underwater.Brightness, Source.Underwater.Tint, 0);
                parameters.Rain = new osuTK.Vector4(Source.OvercastRain.Density, Source.OvercastRain.Wind, Source.OvercastRain.Cloudiness, 0);
                parameters.Curtain = new osuTK.Vector4(Source.UnderwaterLightCurtain.Scale, Source.UnderwaterLightCurtain.Brightness,
                    Source.UnderwaterLightCurtain.Falloff, Source.UnderwaterLightCurtain.Sway);
            }

            protected override void DrawContents(IRenderer renderer)
            {
                UnbindTextureShader(renderer);
                parametersBuffer ??= renderer.CreateUniformBuffer<WeatherParameters>();
                parametersBuffer.Data = parameters;
                shader.BindUniformBlock("m_WeatherParameters", parametersBuffer);
                shader.Bind();
                renderer.DrawFrameBuffer(SharedData.MainBuffer, DrawRectangle, DrawColourInfo.Colour);
                shader.Unbind();
                BindTextureShader(renderer);
            }

            protected override void Dispose(bool isDisposing)
            {
                parametersBuffer?.Dispose();
                base.Dispose(isDisposing);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal record struct WeatherParameters
        {
            public UniformVector2 Size;
            public UniformFloat Time;
            public UniformFloat Intensity;
            public UniformVector4 Heat;
            public UniformVector4 Water;
            public UniformVector4 Rain;
            public UniformVector4 Curtain;
        }
    }
}
