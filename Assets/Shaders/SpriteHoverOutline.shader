Shader "DarkLord/SpriteHoverOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _SpriteUVRect ("Sprite UV Rect", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;
            float4 _SpriteUVRect;
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            Varyings Vert(Attributes v)
            {
                Varyings o; o.positionCS=TransformObjectToHClip(v.positionOS.xyz); o.uv=v.uv; o.color=v.color; return o;
            }
            half Alpha(float2 uv)
            {
                if(any(uv < _SpriteUVRect.xy) || any(uv >= _SpriteUVRect.zw)) return 0;
                return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv).a;
            }
            half4 Frag(Varyings i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color;
                float2 stepUV=_MainTex_TexelSize.xy;
                half edge=min(min(Alpha(i.uv+float2(stepUV.x,0)),Alpha(i.uv-float2(stepUV.x,0))),
                              min(Alpha(i.uv+float2(0,stepUV.y)),Alpha(i.uv-float2(0,stepUV.y))));
                if(c.a>0.1 && edge<0.1) c.rgb=half3(1,1,1);
                return c;
            }
            ENDHLSL
        }
    }
}
