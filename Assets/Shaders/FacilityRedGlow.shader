Shader "DarkLord/Sprites/Facility Red Glow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Facility sprite", 2D) = "white" {}
        _GlowColor ("Crimson light", Color) = (1,0.065,0.035,1)
        _GlowStrength ("Emission strength", Range(0,2)) = 0.45
        _HaloStrength ("Pixel halo strength", Range(0,1)) = 0.22
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
            float4 _MainTex_TexelSize;
            CBUFFER_START(UnityPerMaterial)
                half4 _GlowColor;
                half _GlowStrength, _HaloStrength;
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
            half RedMask(float2 uv)
            {
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                // Crimson has red dominance and more blue than green; bronze does not.
                half red = step(c.g * 1.45h, c.r) * step(c.b * 1.25h, c.r) * step(c.g * .95h, c.b);
                return red * c.a * saturate(c.r * 1.8h);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = _MainTex_TexelSize.xy;
                half halo = (RedMask(input.uv + float2(p.x,0)) + RedMask(input.uv - float2(p.x,0)) +
                    RedMask(input.uv + float2(0,p.y)) + RedMask(input.uv - float2(0,p.y))) * .16h;
                halo += (RedMask(input.uv + p) + RedMask(input.uv - p) +
                    RedMask(input.uv + float2(p.x,-p.y)) + RedMask(input.uv + float2(-p.x,p.y))) * .1h;
                halo += (RedMask(input.uv + float2(p.x*2,0)) + RedMask(input.uv - float2(p.x*2,0)) +
                    RedMask(input.uv + float2(0,p.y*2)) + RedMask(input.uv - float2(0,p.y*2))) * .07h;
                half intensity = RedMask(input.uv) * _GlowStrength + halo * _HaloStrength;
                return half4(_GlowColor.rgb * intensity * input.color.r * input.color.a * FacilityLightPulse(_PulseTime), 0);
            }
            ENDHLSL
        }
    }
}
