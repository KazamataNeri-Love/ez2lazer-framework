#include "sh_Weather.h"

void main(void)
{
    highp vec2 uv = weatherUv();
    highp vec2 p = uv * max(w_Size, vec2(1.0)) / 90.0 * w_Heat.y;
    highp float t = w_Time * 0.55;
    highp float turbulence = weatherNoise(p + vec2(0.0, -t))
                          + 0.5 * weatherNoise(p * 2.13 + vec2(t * 0.3, -t * 1.7)) - 0.75;
    highp vec2 wave = vec2(sin(p.y * 3.5 - t * 3.0) + turbulence, turbulence * 0.45);
    highp vec2 edge = smoothstep(vec2(0.0), vec2(0.06), uv) * smoothstep(vec2(0.0), vec2(0.06), 1.0 - uv);
    highp vec2 offset = wave * w_Heat.x * w_Intensity * edge.x * edge.y / max(w_Size, vec2(1.0));
    lowp vec4 colour = weatherSample(uv + offset);
    colour.rgb = mix(colour.rgb, colour.rgb * vec3(1.12, 1.02, 0.82), w_Heat.z * w_Intensity);
    o_Colour = getRoundedColor(colour, v_TexCoord);
}
