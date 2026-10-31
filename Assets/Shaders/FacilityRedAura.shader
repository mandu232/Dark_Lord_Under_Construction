Shader "DarkLord/Sprites/Facility Red Aura"
{
    Properties
    {
        [PerRendererData] _MainTex ("Blurred crimson mask", 2D) = "white" {}
        _Color ("Crimson light", Color) = (1,0.065,0.035,1)
        _Strength ("Aura strength", Range(0,2)) = 0.65
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
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
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
                half mask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv).a;
                return half4(_Color.rgb * (mask * _Strength * FacilityLightPulse(_PulseTime) * input.color.r * input.color.a),0);
            }
            ENDHLSL
        }
    }
}
