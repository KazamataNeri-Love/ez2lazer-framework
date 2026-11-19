#include "sh_Weather.h"

highp float rainLayer(highp vec2 pixel, highp float layer)
{
    highp vec2 p = pixel / vec2(19.0, 110.0) * layer;
    p.x -= p.y * w_Rain.y * 0.28;
    p.y -= w_Time * (2.6 + layer);
    highp vec2 cell = floor(p);
    highp float random = weatherHash(cell + layer * 17.0);
    highp vec2 f = fract(p);
    highp float centre = 0.2 + weatherHash(cell + 3.1) * 0.6;
    highp float line = 1.0 - smoothstep(0.012, 0.055, abs(f.x - centre));
    highp float tail = smoothstep(0.18, 0.48, f.y) * (1.0 - smoothstep(0.64, 0.91, f.y));
    return line * tail * step(random, w_Rain.x) * (0.25 + random * 0.35);
}

void main(void)
{
    highp vec2 uv = weatherUv();
    highp vec2 pixel = uv * max(w_Size, vec2(1.0));
    highp float rain = rainLayer(pixel, 1.0) + rainLayer(pixel + 41.0, 1.7) * 0.55;
    lowp vec4 colour = weatherSample(uv);
    highp float gray = dot(colour.rgb, vec3(0.299, 0.587, 0.114));
    lowp vec3 overcast = mix(colour.rgb, vec3(gray) * vec3(0.88, 0.94, 1.0), w_Rain.z * 0.65);
    overcast *= 1.0 - w_Rain.z * 0.38;
    colour.rgb = mix(colour.rgb, overcast, w_Intensity);
    colour.rgb = mix(colour.rgb, vec3(0.72, 0.82, 0.9), clamp(rain * w_Intensity, 0.0, 1.0));
    o_Colour = getRoundedColor(colour, v_TexCoord);
}
