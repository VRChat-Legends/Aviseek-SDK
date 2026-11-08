// the join screen: the aviseek logo fills the view on a dark backdrop, then the view dives into
// the magnifying glass. the backdrop inside the lens fades away first, so the world shows
// through the glass while it grows, the cat inside fades out, and the ring rushes past.
// the intro script drives it each frame on a quad held in front of the player's eyes
Shader "AviSeek/Intro Overlay"
{
    Properties
    {
        _MainTex ("Logo", 2D) = "white" {}
        _Backdrop ("Backdrop", Color) = (0.05, 0.04, 0.09, 1)
        _Aspect ("Logo Width / Height", Float) = 1.3345
        _LogoHeight ("Logo Height On The Quad", Float) = 0.13
        _LensCentre ("Lens Centre (logo uv)", Vector) = (0.4974, 0.6567, 0, 0)
        _LensInner ("Lens Inner Radius (of logo height)", Float) = 0.2876
        _LensOuter ("Lens Outer Radius (of logo height)", Float) = 0.335
        _Anchor ("Point At The Middle (logo uv)", Vector) = (0.5, 0.5, 0, 0)
        _Zoom ("Zoom", Float) = 1
        _Hole ("Backdrop Inside The Lens", Range(0, 1)) = 1
        _Back ("Backdrop Outside The Lens", Range(0, 1)) = 1
        _Cat ("Cat", Range(0, 1)) = 1
        _Fade ("Fade", Range(0, 1)) = 1
        _LogoAlpha ("Logo", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Overlay+1000" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Backdrop;
            float _Aspect, _LogoHeight, _LensInner, _LensOuter, _Zoom, _Hole, _Back, _Cat, _Fade, _LogoAlpha;
            float4 _LensCentre, _Anchor;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // the quad's middle shows _Anchor, everything else scales out from there
                float2 p = i.uv - 0.5;
                float size = _LogoHeight * max(_Zoom, 0.001);
                float2 luv = _Anchor.xy + float2(p.x / (size * _Aspect), p.y / size);

                fixed4 logo = tex2D(_MainTex, luv);
                float inside = step(0.0, luv.x) * step(luv.x, 1.0) * step(0.0, luv.y) * step(luv.y, 1.0);
                logo.a *= inside * _LogoAlpha;

                // distance from the lens middle, measured in logo heights so the circle stays round
                float2 fromLens = (luv - _LensCentre.xy) * float2(_Aspect, 1.0);
                float d = length(fromLens);
                float aa = max(fwidth(d), 0.00001);
                float inLens = 1.0 - smoothstep(_LensInner - aa, _LensInner + aa, d);
                float inGlass = 1.0 - smoothstep(_LensOuter - aa, _LensOuter + aa, d);

                // the cat is the purple part inside the glass, the ring and the name stay
                float purple = smoothstep(0.15, 0.35, max(logo.r, max(logo.g, logo.b)) - min(logo.r, min(logo.g, logo.b)));
                logo.a *= lerp(1.0, _Cat, purple * inGlass);

                float back = _Backdrop.a * lerp(_Back, _Hole, inLens);
                float alpha = logo.a + back * (1.0 - logo.a);
                fixed3 colour = (logo.rgb * logo.a + _Backdrop.rgb * back * (1.0 - logo.a)) / max(alpha, 0.0001);
                return fixed4(colour, alpha * _Fade);
            }
            ENDCG
        }
    }
}
