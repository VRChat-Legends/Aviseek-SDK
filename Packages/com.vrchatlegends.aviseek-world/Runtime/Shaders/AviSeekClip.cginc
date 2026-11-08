// rounded corners for ui clipped by a RectMask2D. a stencil mask only keeps or drops each pixel,
// which left a jagged rim wherever a card or panel cut its picture off. this measures how far
// the pixel is outside the clip rectangle with its corners rounded by _ClipRadius (canvas units)
// and fades the last pixel, so the edge stays smooth at any distance
#ifndef AVISEEK_CLIP_INCLUDED
#define AVISEEK_CLIP_INCLUDED

float _ClipRadius;

float AviSeekRoundClip(float2 p, float4 clipRect)
{
    float2 centre = (clipRect.xy + clipRect.zw) * 0.5;
    float2 half = (clipRect.zw - clipRect.xy) * 0.5;
    float r = clamp(_ClipRadius, 0.0, min(half.x, half.y));
    float2 q = abs(p - centre) - half + r;
    float d = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
    float aa = max(fwidth(d), 0.0001);
    return saturate(0.5 - d / aa);
}

#endif
