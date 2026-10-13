Shader "DarkLord/UI/Story Paper Burn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Page", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BurnProgress ("Burn Progress", Range(0,1)) = 0
        _AspectRatio ("Page Aspect Ratio", Float) = 1.77778
        _BurnCount ("Burn Circle Count", Float) = 2
        _BurnCenter1 ("Burn Center 1", Vector) = (.25,.45,1,0)
        _BurnCenter2 ("Burn Center 2", Vector) = (.75,.65,1,0)
        _BurnCenter3 ("Burn Center 3", Vector) = (.5,.3,1,0)
        _EmberColor ("Scarlet Ember", Color) = (1,.08,.18,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
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

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            sampler2D _MainTex;
            fixed4 _Color;
            float _BurnProgress, _AspectRatio;
            float _BurnCount;
            float4 _BurnCenter1, _BurnCenter2, _BurnCenter3;
            fixed4 _EmberColor;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float noise(float2 p)
            {
                float2 cell = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(hash(cell), hash(cell + float2(1,0)), f.x),
                    lerp(hash(cell + float2(0,1)), hash(cell + float2(1,1)), f.x), f.y);
            }

            float circleEdge(float2 uv, float4 center)
            {
                float2 scale = float2(_AspectRatio, 1);
                float distanceToCenter = length((uv - center.xy) * scale);
                float maxRadius = length(max(center.xy, 1 - center.xy) * scale);
                // Stagger the circles slightly, but guarantee every one fully opens at the end.
                float progress = pow(saturate((_BurnProgress - center.w) / (1 - center.w)), center.z);
                return distanceToCenter / maxRadius - lerp(-.04, 1.06, progress);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 page = tex2D(_MainTex, input.uv) * input.color;
                float2 centered = (input.uv - .5) * float2(_AspectRatio, 1);
                float edge = min(circleEdge(input.uv, _BurnCenter1), circleEdge(input.uv, _BurnCenter2));
                if (_BurnCount > 2.5) edge = min(edge, circleEdge(input.uv, _BurnCenter3));
                // A nearly round, stationary edge keeps only a faint singed-paper texture.
                edge += (noise(centered * 24) - .5) * .004
                    + (noise(centered * 75) - .5) * .0012;
                float remaining = smoothstep(-.0025, .003, edge);
                float charred = 1 - smoothstep(.009, .025, edge);
                float ember = 1 - smoothstep(.001, .008, edge);
                float3 burnt = lerp(page.rgb, float3(.025, .004, .008), charred);
                float3 glow = lerp(_EmberColor.rgb * .65, _EmberColor.rgb, ember);
                page.rgb = lerp(burnt, glow, ember);
                page.a *= remaining;
                return page;
            }
            ENDCG
        }
    }
}
