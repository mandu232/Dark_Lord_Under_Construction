Shader "DarkLord/Sprites/Actor Contact Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Contact quad", 2D) = "white" {}
        _Opacity ("Contact darkness", Range(0,1)) = 0.38
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half _Opacity;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS=TransformObjectToHClip(input.positionOS);
                output.uv=input.uv; output.color=input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half radius=length(input.uv*2-1);
                half alpha=(1-smoothstep(.3h,1.h,radius))*_Opacity*input.color.a;
                return half4(.008h,.007h,.012h,alpha);
            }
            ENDHLSL
        }
    }
}
