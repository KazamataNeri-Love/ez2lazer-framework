// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Shaders;
using osu.Framework.Graphics.Shaders;
using osu.Framework.IO.Stores;
using osuTK.Graphics.ES30;
using Veldrid.SPIRV;

namespace osu.Framework.Tests.Shaders
{
    [TestFixture]
    public class WeatherShaderTest
    {
        [Test]
        public void TestCompileWeatherShader(
            [Values(FragmentShaderDescriptor.DESERT_HEAT, FragmentShaderDescriptor.UNDERWATER, FragmentShaderDescriptor.OVERCAST_RAIN,
                FragmentShaderDescriptor.UNDERWATER_LIGHT_CURTAIN)]
            string fragment,
            [Values(CrossCompileTarget.GLSL, CrossCompileTarget.ESSL, CrossCompileTarget.HLSL, CrossCompileTarget.MSL)]
            CrossCompileTarget target)
        {
            using var store = new NamespacedResourceStore<byte[]>(new DllResourceStore(typeof(Game).Assembly), "Resources/Shaders");
            var renderer = new GLRenderer();
            using var manager = new ShaderManager(renderer, store);
            using var vertexPart = new GLShaderPart(renderer, "Texture2D", store.Get("sh_Texture2D.vs"), ShaderType.VertexShader, manager);
            using var fragmentPart = new GLShaderPart(renderer, fragment, store.Get($"sh_{fragment}.fs"), ShaderType.FragmentShader, manager);
            var compilationStore = new ShaderCompilationStore();
            var result = compilationStore.CompileVertexFragment(vertexPart.GetRawText(), fragmentPart.GetRawText(), target);

            Assert.Multiple(() =>
            {
                Assert.That(result.VertexBytes, Is.Not.Empty);
                Assert.That(result.FragmentBytes, Is.Not.Empty);
                Assert.That(result.Reflection.ResourceLayouts.SelectMany(l => l.Elements).Select(e => e.Name), Does.Contain("m_WeatherParameters"));
            });

            if (target == CrossCompileTarget.HLSL && System.OperatingSystem.IsWindows())
                Assert.That(compilationStore.CompileHlslBytecode(result.FragmentText, "main", "ps_5_0"), Is.Not.Empty);
        }
    }
}
