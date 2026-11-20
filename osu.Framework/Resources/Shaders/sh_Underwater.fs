#include "sh_Weather.h"

highp float caustic(highp vec2 p, highp float t)
{
    p += vec2(sin(p.y * 0.7 + t), cos(p.x * 0.6 - t)) * 0.8;
    highp float field = sin(p.x + t) + sin(p.y * 1.17 - t * 0.8) + sin((p.x + p.y) * 0.73 + t * 0.6);
    return pow(max(0.0, 1.0 - abs(field) * 0.48), 12.0);
}

void main(void)
{
    highp vec2 uv = weatherUv();
    highp vec2 p = uv * max(w_Size, vec2(1.0)) / 48.0 * w_Water.x;
    highp float t = w_Time * 0.6;
    highp float light = caustic(p, t) * 0.65 + caustic(p * 1.37 + 8.0, -t * 0.7) * 0.35;
    lowp vec4 colour = weatherSample(uv);
    lowp vec3 tinted = colour.rgb * vec3(0.62, 0.94, 1.06);
    colour.rgb = mix(colour.rgb, tinted, w_Water.z * w_Intensity);
    colour.rgb += vec3(0.36, 0.72, 0.8) * light * w_Water.y * w_Intensity;
    o_Colour = getRoundedColor(colour, v_TexCoord);
}
