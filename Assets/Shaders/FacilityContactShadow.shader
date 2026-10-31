Shader "DarkLord/Sprites/Facility Contact Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Facility silhouette", 2D) = "white" {}
        _Opacity ("Contact darkness", Range(0,1)) = 0.42
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
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half _Opacity;
            CBUFFER_END
            struct Attributes { float3 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = _MainTex_TexelSize.xy;
                half alpha = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv).a * .5h;
                alpha += (SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(p.x,0)).a +
                    SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(p.x,0)).a +
                    SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv+float2(0,p.y)).a +
                    SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv-float2(0,p.y)).a) * .125h;
                half foot = 1 - smoothstep(.16h,.32h,input.uv.y);
                return half4(.01h,.008h,.012h, alpha * foot * _Opacity * input.color.a);
            }
            ENDHLSL
        }
    }
}
