Shader "DarkLord/UI/Archive Candle Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Flame Tint", Color) = (1,0.25,0.10,1)
        _Intensity ("Intensity", Range(0,2)) = 0.85
        _UnscaledTime ("Unscaled Time", Float) = 0
        _SpriteSize ("Artwork Size", Vector) = (405,242,0,0)
        _Candle0 ("Left Tall Wick (pixels from top left)", Vector) = (10,160,0,0)
        _Candle1 ("Left Middle Wick", Vector) = (17,174,0,0)
        _Candle2 ("Left Short Wick", Vector) = (25,183,0,0)
        _Candle3 ("Right Tall Wick", Vector) = (395,160,0,0)
        _Candle4 ("Right Middle Wick", Vector) = (388,174,0,0)
        _Candle5 ("Right Short Wick", Vector) = (380,183,0,0)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; float4 local:TEXCOORD1; };
            fixed4 _Color;
            float _Intensity, _UnscaledTime;
            float4 _SpriteSize, _Candle0, _Candle1, _Candle2, _Candle3, _Candle4, _Candle5, _ClipRect;
            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color; return o;
            }
            float2 candle(float2 pixel, float2 wick, float seed)
            {
                float t=_UnscaledTime;
                float flicker=.82+.12*sin(t*7.3+seed)+.06*sin(t*13.1+seed*3);
                float2 d=pixel-(wick-float2(0,1.5));
                d.x+=sin(t*4.7+seed-d.y*.35)*.55*saturate(-d.y*.3+.5);
                float2 core=d/float2(.8,2.1*flicker);
                float2 halo=d/float2(6,8);
                return float2(exp(-dot(core,core)),exp(-dot(halo,halo))*.16)*flicker;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Snap to the artwork's pixel grid so the flame fits the painted candles.
                float2 pixel=floor(float2(i.uv.x,1-i.uv.y)*_SpriteSize.xy)+.5;
                float2 light=candle(pixel,_Candle0.xy,1)+candle(pixel,_Candle1.xy,3)
                    +candle(pixel,_Candle2.xy,5)+candle(pixel,_Candle3.xy,7)
                    +candle(pixel,_Candle4.xy,9)+candle(pixel,_Candle5.xy,11);
                fixed4 color=fixed4(lerp(_Color.rgb,float3(1,.78,.38),saturate(light.x)),
                    saturate((light.x+light.y)*_Intensity)*_Color.a*i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
