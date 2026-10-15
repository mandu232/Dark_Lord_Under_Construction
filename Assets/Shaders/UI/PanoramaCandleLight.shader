Shader "DarkLord/UI/Panorama Candle Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Flame Tint", Color) = (1,0.15,0.06,1)
        _Intensity ("Intensity", Range(0,2)) = 0.75
        _UnscaledTime ("Unscaled Time", Float) = 0
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                // Each small quad follows one candle; vertex red supplies its unique phase.
                float seed=i.color.r*31;
                float t=_UnscaledTime;
                float flicker=.80+.13*sin(t*7.3+seed)+.07*sin(t*13.1+seed*3);
                float2 d=(i.uv-.5)*float2(48,64);
                d.x+=sin(t*4.7+seed+d.y*.2)*.8*saturate(d.y*.12+.5);
                float2 core=d/float2(1.25,4*flicker);
                float2 halo=d/float2(11,16);
                float fire=exp(-dot(core,core))*flicker;
                float glow=exp(-dot(halo,halo))*.12*flicker;
                fixed4 color=fixed4(lerp(_Color.rgb,float3(1,.65,.25),saturate(fire)),
                    saturate((fire+glow)*_Intensity)*_Color.a*i.color.a);
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
