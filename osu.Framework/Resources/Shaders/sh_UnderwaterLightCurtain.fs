#include "sh_Weather.h"

highp float lightCurtain(highp vec2 p, highp float t, highp float phase)
{
    // Smooth folds remain continuous down the screen, like a suspended sheet of light.
    highp float bend = sin(p.y * 2.4 + t + phase) * 0.65;
    bend += (weatherNoise(vec2(p.y * 2.0 + phase, t * 0.45)) - 0.5) * 0.9;
    highp float x = p.x / (1.0 + p.y * 0.35) + bend * w_Curtain.w * 2.4;
    highp float fold = 0.5 + 0.5 * sin(x * 3.0 + phase + t * 0.35
        + weatherNoise(vec2(x * 0.55 + phase, t * 0.2)) * 2.4);
    highp float strands = 0.6 + weatherNoise(vec2(x * 9.0 + phase, t * 0.35)) * 0.4;
    return (pow(fold, 6.0) * 0.6 + pow(fold, 18.0) * 0.25 + pow(fold, 2.0) * 0.15) * strands;
}

void main(void)
{
    highp vec2 uv = weatherUv();
    // Keep multiple folds visible in narrow preview panels as well as wide backgrounds.
    highp float aspect = max(w_Size.x / max(w_Size.y, 1.0), 1.0);
    highp vec2 p = vec2((uv.x - 0.5) * aspect * w_Curtain.x * 7.0, uv.y);
    highp float t = w_Time * 0.22;
    highp float veil = lightCurtain(p, t, 0.0) * 0.55
        + lightCurtain(vec2(p.x * 1.31, p.y), t * 0.83, 4.2) * 0.3
        + lightCurtain(vec2(p.x * 0.73, p.y), t * 0.67, 8.7) * 0.15;
    highp float falloff = pow(clamp(1.0 - uv.y, 0.0, 1.0), w_Curtain.z);
    lowp vec3 lightColour = mix(vec3(0.08, 0.32, 0.68), vec3(0.32, 0.85, 1.0),
        pow(falloff, 0.35) * (0.5 + veil * 0.5));
    lowp vec4 colour = weatherSample(uv);
    colour.rgb += lightColour * veil * falloff * w_Curtain.y * w_Intensity * 2.5;
    o_Colour = getRoundedColor(colour, v_TexCoord);
}
