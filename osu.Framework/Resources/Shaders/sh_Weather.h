#ifndef WEATHER_H
#define WEATHER_H

#undef HIGH_PRECISION_VERTEX
#define HIGH_PRECISION_VERTEX

#include "sh_Utils.h"
#include "sh_Masking.h"

layout(location = 2) in highp vec2 v_TexCoord;

layout(std140, set = 0, binding = 0) uniform m_WeatherParameters
{
    highp vec2 w_Size;
    highp float w_Time;
    highp float w_Intensity;
    highp vec4 w_Heat;
    highp vec4 w_Water;
    highp vec4 w_Rain;
    highp vec4 w_Curtain;
};

layout(set = 1, binding = 0) uniform lowp texture2D m_Texture;
layout(set = 1, binding = 1) uniform lowp sampler m_Sampler;
layout(location = 0) out vec4 o_Colour;

highp vec2 weatherUv()
{
    // Using the signed texture rectangle also handles framebuffer Y inversion.
    return (v_TexCoord - v_TexRect.xy) / (v_TexRect.zw - v_TexRect.xy);
}

lowp vec4 weatherSample(highp vec2 uv)
{
    highp vec2 halfTexel = 0.5 / vec2(textureSize(sampler2D(m_Texture, m_Sampler), 0));
    highp vec2 coord = mix(v_TexRect.xy, v_TexRect.zw, uv);
    return texture(sampler2D(m_Texture, m_Sampler), clamp(coord, min(v_TexRect.xy, v_TexRect.zw) + halfTexel, max(v_TexRect.xy, v_TexRect.zw) - halfTexel));
}

highp float weatherHash(highp vec2 p)
{
    p = fract(p * vec2(123.34, 345.45));
    p += dot(p, p + 34.345);
    return fract(p.x * p.y);
}

highp float weatherNoise(highp vec2 p)
{
    highp vec2 cell = floor(p);
    highp vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    return mix(mix(weatherHash(cell), weatherHash(cell + vec2(1.0, 0.0)), f.x),
               mix(weatherHash(cell + vec2(0.0, 1.0)), weatherHash(cell + vec2(1.0)), f.x), f.y);
}

#endif
