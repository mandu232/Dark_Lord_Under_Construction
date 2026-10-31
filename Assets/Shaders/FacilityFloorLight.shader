Shader "DarkLord/Sprites/Facility Floor Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Light quad", 2D) = "white" {}
        _Color ("Crimson floor light", Color) = (1,0.065,0.035,1)
        _Strength ("Strength", Range(0,1)) = 0.035
        [HideInInspector] _PulseTime ("Preview time (-1 is live)", Float) = -1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="False" }
        Cull Off ZWrite Off
        Blend One One
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "FacilityLightPulse.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Strength;
                float _PulseTime;
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
                float2 p = input.uv * 2 - 1;
                half falloff = 1 - smoothstep(0.0, 1.0, length(p));
                return half4(_Color.rgb * (_Strength * falloff * input.color.r * input.color.a * FacilityLightPulse(_PulseTime)), 0);
            }
            ENDHLSL
        }
    }
}
