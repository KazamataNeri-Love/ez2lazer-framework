#include "sh_Weather.h"

highp vec3 lightSheet(highp vec2 p, highp float t, highp float depth, highp float origin)
{
    // Project a soft fan of light. Its folds turn through the water independently of the other sheets.
    highp float phase = t + depth * 2.1;
    highp float twist = sin(phase + p.y * 2.7);
    highp float centre = origin + (p.y - 0.25) * (0.17 - depth * 0.16);
    centre += w_Curtain.w * (0.48 * sin(phase) + 0.34 * p.y * twist);
    highp float spread = (0.32 - depth * 0.045) * (1.0 + p.y * 0.55);
    spread *= 1.0 + w_Curtain.w * 0.52 * twist;
    highp float across = (p.x - centre) / spread;
    highp float envelope = exp(-across * across);

    // Broad, low contrast folds give the translucent sheet shape without outlining its edge.
    highp float fold = (across - twist * 0.45) / 0.58;
    highp float density = envelope * (0.25 + 0.75 * exp(-fold * fold));
    highp vec3 tint = mix(vec3(0.13, 0.73, 0.83), vec3(0.06, 0.29, 0.69), depth * 0.42);
    return tint * density;
}

void main(void)
{
    highp vec2 uv = weatherUv();
    highp float aspect = max(w_Size.x / max(w_Size.y, 1.0), 1.0);
    highp vec2 p = vec2((uv.x - 0.5) * aspect * w_Curtain.x, uv.y);
    highp vec3 light = lightSheet(p, w_Time * 0.92, 0.0, -0.29) * 0.38
        + lightSheet(p, w_Time * 0.67, 1.0, 0.32) * 0.32
        + lightSheet(p, w_Time * 0.51, 2.0, 0.02) * 0.23;

    // Fade the light through the whole water column without a lower rim.
    highp float falloff = pow(clamp(1.0 - uv.y * uv.y, 0.0, 1.0), w_Curtain.z);
    lowp vec4 colour = weatherSample(uv);
    colour.rgb += light * falloff * w_Curtain.y * w_Intensity * 1.6;
    o_Colour = getRoundedColor(colour, v_TexCoord);
}
