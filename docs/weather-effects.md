# 天气效果

`WeatherEffectContainer` 位于 `osu.Framework.Graphics.Containers`，将其子树作为天气的作用对象。默认 `Effect = None`
，不改变画面。全局效果可以包住整个场景；只影响菜单背景或玩法背景时，只包住相应层。容器外的音符、HUD、按钮不受影响。

```csharp
var weather = new WeatherEffectContainer
{
    RelativeSizeAxes = Axes.Both,
    Effect = WeatherEffect.DesertHeat,
    Scope = WeatherEffectScope.NonGameplay,
    Intensity = 0.8f,
    Child = background,
};

// 在调用方的场景变化事件里更新；框架不会猜测哪个 Screen 属于游玩。
weather.IsGameplay = isGameplay;

// 可由菜单随机天气系统、玩法 Mod 或动画控制器随时切换和调参。
weather.Effect = WeatherEffect.OvercastRain;
weather.OvercastRain = weather.OvercastRain with { Density = 0.6f, Wind = -0.4f };
```

## 效果和参数

- `DesertHeat`：真实采样场景像素产生热浪折射。`DesertHeatSettings` 包含屏幕像素幅度 `Amplitude`（0–32，默认 4）、波纹尺度
  `Scale`（0.1–8，默认 1）、暖色强度 `Tint`（0–1，默认 0.15）。
- `Underwater`：两层流动焦散和蓝绿色调。`UnderwaterSettings` 包含波光尺度 `Scale`（0.1–8，默认 1）、亮度 `Brightness`（0–2，默认
  0.65）、色调强度 `Tint`（0–1，默认 0.3）。
- `OvercastRain`：分层细雨、风向偏移、阴天去饱和与压暗。`OvercastRainSettings` 包含密度 `Density`（0–1，默认 0.45）、风向 `Wind`
  （−2–2，默认 0.2）、阴天程度 `Cloudiness`（0–1，默认 0.4）。
- `Intensity`：0–1，默认 1；为 0 时完全直通。`AnimationSpeed`：0–10，默认 1，为 0 时冻结天气动画。
- `PreviewTime`：可空的固定预览时间，单位秒，范围 0–86400。设置后暂停天气时间推进，便于复现截图；设为 `null`
  后从原动画进度继续。天气暂停不会停止子树动画或输入。
- `Scope`：`Global`、`Gameplay`、`NonGameplay`。`IsGameplay` 默认 `false`；`IsEffectActive` 可查询当前是否生效。

在 update 线程修改属性。越界值、NaN、Infinity、未定义枚举会抛出 `ArgumentOutOfRangeException`，并保留原参数。每组参数都是值类型，通过
`with` 修改不会影响其他实例。

## 绘制行为

生效时复用一个场景帧缓冲，再用对应着色器合成；不依赖后端特定的 backbuffer 抓取。透明区域保留透明，热浪不会采样容器之外的其他场景。父容器裁剪继续生效。

`Effect = None`、`Intensity = 0` 或范围不匹配时，直接绘制现有子树，不分配帧缓冲，已分配的缓冲在 draw
线程延迟释放。切换效果不会移除、重新加载或重设子节点。生效时容器自身的颜色和透明度在合成阶段应用一次；与其他离屏容器一样，半透明的重叠子节点按整体分组混合。

本组件不负责随机天气调度、游戏设置持久化或玩法判定；这些逻辑由调用方控制。同一场景可放置多个独立实例，也可以嵌套使用。

## 可视化调试

运行 `dotnet run --project osu.Framework.Tests`，在 Test Browser 搜索 `WeatherEffects`，打开 `TestTune`。

左侧为原图，右侧为天气效果。测试步骤面板提供所有天气参数、强度、场景范围、模拟游玩状态、动画速度、固定时间、透明度、圆角和恢复默认。切换天气后相应参数即时生效；网格用于观察真实折射，动态方块用于确认天气暂停时内容仍会更新。底部按钮演示容器内输入和独立前景。

`TestSwitchingPreservesChildrenAndInput` 和 `TestAnimationClock` 同时作为可交互测试和 NUnit 自动化回归用例。

## 自动验证

```powershell
dotnet test osu.Framework.Tests --filter 'FullyQualifiedName~WeatherEffectContainerTest|FullyQualifiedName~WeatherShaderTest|FullyQualifiedName~TestSceneWeatherEffects'
```

普通测试覆盖场景矩阵、参数与枚举校验、实例隔离、uniform 布局、交互、时钟，以及 GLSL / ESSL / HLSL / MSL 实际交叉编译；Windows
下还验证 HLSL 字节码编译。

GPU 测试是显式用例，需要桌面和相应图形后端。每次命令启动独立测试进程，因为后端环境变量在进程初始化时读取。

```powershell
$env:OSU_GRAPHICS_RENDERER = 'gl'
$env:OSU_GRAPHICS_SURFACE = 'OpenGL'
dotnet test osu.Framework.Tests --filter 'FullyQualifiedName=osu.Framework.Tests.Ez.WeatherEffectRenderingTest.TestRenderWeather'

$env:OSU_GRAPHICS_RENDERER = 'veldrid'
$env:OSU_GRAPHICS_SURFACE = 'Direct3D11'
dotnet test osu.Framework.Tests --no-build --filter 'FullyQualifiedName=osu.Framework.Tests.Ez.WeatherEffectRenderingTest.TestRenderWeather'
```

GPU 用例验证三种效果改变画面、不同时间有动画、固定时间可复现、零强度直通、场景切换、透明区域、颜色与透明度、父级圆角、子树外画面和前景不受影响，以及改变尺寸与旋转。截图保存在测试工作目录的
`weather-screenshots/<backend>/` 并附加到测试结果；测试不会自动在无 GPU 的 CI 中执行。
