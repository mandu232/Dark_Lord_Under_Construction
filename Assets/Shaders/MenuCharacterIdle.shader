Shader "DarkLord/UI/Menu Character Idle"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UnscaledTime ("Animation clock", Float) = 0
        _ArmDegrees ("Arm sway degrees", Range(0,2)) = 0.9
        _Breath ("Breathing UV distance", Range(0,0.003)) = 0.0012
        _Speed ("Idle speed", Range(0.1,2)) = 1
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
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
            sampler2D _MainTex;
            float4 _MainTex_TexelSize, _ClipRect;
            fixed4 _Color;
            float _UnscaledTime, _ArmDegrees, _Breath, _Speed;
            v2f vert(appdata v)
            {
                v2f o; o.local=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex);
                o.uv=v.uv; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float t=_UnscaledTime*_Speed;
                float2 uv=i.uv;
                // Coordinates match the standalone CrimsonRepose canvas (not an atlas).
                // The soft region encloses the glass, hand and forearm; elbow stays anchored.
                float arm=1-smoothstep(.68,1.0,length((uv-float2(.267,.445))/float2(.053,.115)));
                float angle=radians(_ArmDegrees)*(sin(t*.83)+.18*sin(t*1.37)) * arm;
                float aspect=_MainTex_TexelSize.z/_MainTex_TexelSize.w;
                float2 pivot=float2(.302,.361);
                float2 d=(uv-pivot)*float2(aspect,1);
                float s=sin(angle),c=cos(angle);
                uv=pivot+float2(c*d.x+s*d.y,-s*d.x+c*d.y)/float2(aspect,1);
                // Exclude the chair's right post and the desk below the shoulder.
                float body=(1-smoothstep(.65,1.0,length((i.uv-float2(.226,.465))/float2(.093,.255))))
                    *smoothstep(.151,.182,i.uv.x)*smoothstep(.28,.38,i.uv.y);
                uv.y-=sin(t*1.12)*_Breath*body;
                fixed4 color=tex2D(_MainTex,uv)*i.color;
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
