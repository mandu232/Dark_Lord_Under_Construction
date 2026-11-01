Shader "DarkLord/Gameplay Effect Original Color"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] _Color ("Tint", Color) = (1,1,1,1)
        _ActorVisibility ("Actor visibility", Range(0,1)) = .85
        [HideInInspector] _SpriteGeometry ("Sprite geometry", Float) = 1
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        ZTest Always
        Pass
        {
            Tags { "LightMode"="DarkLordEffects" }
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            struct Attributes { COMMON_2D_INPUTS half4 color : COLOR; };
            struct Varyings { COMMON_2D_OUTPUTS half4 color : COLOR; };
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/2DCommon.hlsl"
            TEXTURE2D_X(_GameplayActorMask);
            SAMPLER(sampler_GameplayActorMask);
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _ActorVisibility;
                half _SpriteGeometry;
            CBUFFER_END
            Varyings Vertex(Attributes input)
            {
                SetUpSpriteInstanceProperties();
                if (_SpriteGeometry > .5) input.positionOS = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                Varyings output = CommonUnlitVertex(input);
                output.color = input.color * _Color;
                if (_SpriteGeometry > .5) output.color *= unity_SpriteColor;
                return output;
            }
            half4 Fragment(Varyings input) : SV_Target
            {
                half4 color = CommonUnlitFragment(input, input.color);
                // Both attachments share the same raster orientation; post-process UV flips do not apply here.
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                half actor = SAMPLE_TEXTURE2D_X(_GameplayActorMask, sampler_GameplayActorMask, uv).r;
                color.a *= 1 - actor * _ActorVisibility;
                return color;
            }
            ENDHLSL
        }
    }
}
