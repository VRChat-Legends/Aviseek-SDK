// the card behind an avatar picture while vrchat fetches it: slow diagonal stripes, a shimmer
// that sweeps across and a soft glow that breathes. it is a ui shader, so masks and the
// rounded card corners still clip it. every card runs a little out of step with the others,
// its phase carried in the graphic's alpha
Shader "AviSeek/Loading Card"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Base ("Base", Color) = (0.09, 0.07, 0.16, 1)
        _Stripe ("Stripe", Color) = (0.16, 0.12, 0.3, 1)
        _Glow ("Glow", Color) = (0.53, 0.49, 1, 1)
        _StripeScale ("Stripe size", Float) = 14
        _Speed ("Speed", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 local : TEXCOORD1;
                float phase : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _Base;
            fixed4 _Stripe;
            fixed4 _Glow;
            float _StripeScale;
            float _Speed;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.local = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                // the builder hides each card's phase in its alpha, between 0.9 and 1
                o.phase = saturate((1.0 - v.color.a) * 10.0);
                o.color = fixed4(v.color.rgb, 1) * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _Speed + i.phase * 6.0;
                float2 uv = i.uv;

                // slow diagonal stripes drifting sideways
                float stripes = frac((uv.x + uv.y) * _StripeScale - t * 0.35);
                stripes = smoothstep(0.42, 0.5, stripes) - smoothstep(0.92, 1.0, stripes);
                fixed3 colour = lerp(_Base.rgb, _Stripe.rgb, stripes * 0.55);

                // a soft glow in the middle that breathes
                float2 centre = uv - 0.5;
                float glow = saturate(1.0 - length(centre * float2(1.0, 1.25)) * 1.7);
                colour += _Glow.rgb * glow * glow * (0.16 + 0.1 * sin(t * 2.1));

                // a bright band that sweeps across now and then
                float sweep = frac(t * 0.42);
                float band = uv.x + uv.y * 0.35 - (sweep * 2.2 - 0.6);
                colour += _Glow.rgb * exp(-band * band * 90.0) * 0.35;

                fixed4 sprite = tex2D(_MainTex, i.uv);
                fixed4 result = fixed4(colour, 1) * i.color;
                result.a *= sprite.a;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.local.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
