// the profile menu breaking apart when it is thrown. the mesh is the menu cut into triangles,
// textured with one picture of the menu taken the frame it was let go. every piece flies along
// the throw, spreads out from the middle, tumbles, falls and fades, with its edges lit up in the
// accent colour like cracked glass. all of it happens here, the script only moves _Progress
Shader "AviSeek/Shatter"
{
    Properties
    {
        _MainTex ("Snapshot", 2D) = "black" {}
        _Accent ("Edge glow", Color) = (0.52, 0.48, 0.99, 1)
        _Progress ("Progress", Range(0, 1)) = 0
        _Seconds ("Seconds", Float) = 1.1
        _Throw ("Throw velocity (local units a second)", Vector) = (0, 0, 0, 0)
        _Gravity ("Gravity (local units a second squared)", Vector) = (0, -3000, 0, 0)
        _Spread ("Spread", Float) = 520
        _Size ("Width, height, corner radius", Vector) = (1200, 820, 36, 0)
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Accent;
            float _Progress;
            float _Seconds;
            float4 _Throw;
            float4 _Gravity;
            float _Spread;
            float4 _Size;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 piece : TEXCOORD1;
                float4 weights : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 weights : TEXCOORD1;
                float2 random : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // turns p around axis by angle (rodrigues)
            float3 Turn(float3 p, float3 axis, float angle)
            {
                float c = cos(angle);
                float s = sin(angle);
                return p * c + cross(axis, p) * s + axis * dot(axis, p) * (1 - c);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float t = _Progress * _Seconds;
                float2 middle = v.piece.xy;
                float r1 = v.piece.z;
                float r2 = v.piece.w;

                // the piece around its own middle: it shrinks a little and tumbles
                float3 local = v.vertex.xyz - float3(middle, 0);
                local *= 1 - 0.35 * _Progress;
                float3 axis = normalize(float3(r2 - 0.5, r1 - 0.5, 0.35));
                local = Turn(local, axis, (r1 - 0.5) * 9 * t + (r2 - 0.5) * 3 * t * t);

                // where the middle goes: out from the centre, towards the player, along the throw, down
                float2 away = middle / max(length(middle), 1);
                float3 drift = float3(away * _Spread * (0.4 + r2), -_Spread * 0.6 * r1) * t;
                drift += _Throw.xyz * t * (0.7 + 0.6 * r1);
                drift += 0.5 * _Gravity.xyz * t * t * (0.8 + 0.4 * r2);

                o.pos = UnityObjectToClipPos(float4(float3(middle, 0) + local + drift, 1));
                o.uv = v.uv;
                o.weights = v.weights.rgb;
                o.random = float2(r1, r2);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // the menu's rounded outline, the picture's own alpha is not to be trusted
                float2 p = (i.uv - 0.5) * _Size.xy;
                float2 q = abs(p) - (_Size.xy * 0.5 - _Size.z);
                float outside = length(max(q, 0)) + min(max(q.x, q.y), 0) - _Size.z;
                float shape = saturate(0.5 - outside);
                if (shape <= 0) discard;

                fixed4 color = tex2D(_MainTex, i.uv);
                color.a = 1;
                // cracks: bright along each piece's edges, flaring as it breaks
                float edge = min(i.weights.r, min(i.weights.g, i.weights.b));
                float crack = 1 - smoothstep(0.0, 0.018 + 0.012 * i.random.x, edge);
                float flare = 1 - saturate(_Progress * 4);
                color.rgb += _Accent.rgb * crack * (0.7 + 1.1 * flare);
                color.rgb += _Accent.rgb * flare * 0.08;
                // each piece fades on its own time
                float fade = 1 - smoothstep(0.35 + 0.3 * i.random.y, 1, _Progress);
                color.a = shape * fade;
                return color;
            }
            ENDCG
        }
    }
}
