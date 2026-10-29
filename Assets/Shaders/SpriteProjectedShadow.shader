Shader "DarkLord/Sprites/Projected Shadow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _ShadowColor ("Shadow color", Color) = (0.025,0.018,0.045,0.46)
        _GroundY ("Feet anchor", Float) = -0.84375
        [PerRendererData] _GroundWorldY ("World feet anchor", Float) = -0.84375
        [PerRendererData] _ExcludeSlashEffect ("Exclude slash effect", Float) = 0
        _Shear ("Light projection X", Float) = 0.85
        _Flatten ("Ground projection Y", Float) = 0.42
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True" "DisableBatching"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"
            fixed4 _ShadowColor;
            float _GroundY, _GroundWorldY, _Shear, _Flatten, _ExcludeSlashEffect;
            struct shadowOutput
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float height : TEXCOORD1;
                fixed alpha : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            shadowOutput vert(appdata_t v)
            {
                shadowOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float4 p = mul(unity_ObjectToWorld, UnityFlipSprite(v.vertex, _Flip));
                float h = max(0, p.y - _GroundWorldY);
                p.x += h * _Shear;
                p.y = _GroundWorldY + (p.y - _GroundWorldY) * _Flatten;
                o.vertex = mul(UNITY_MATRIX_VP, p);
                o.uv = v.texcoord;
                o.height = h;
                o.alpha = v.color.a * _RendererColor.a;
                return o;
            }
            fixed4 frag(shadowOutput i) : SV_Target
            {
                fixed4 sprite = SampleSpriteTexture(i.uv);
                // Both slash sheets use #FF4A50 for the impact arc. Preserve the
                // darker red clothing and opaque body while discarding that arc
                // and the second sheet's faint baked ground shadow.
                float3 effectColor = float3(1, 74.0 / 255.0, 80.0 / 255.0);
                #ifndef UNITY_COLORSPACE_GAMMA
                    effectColor = GammaToLinearSpace(effectColor);
                #endif
                float isEffect = 1 - step(0.01, distance(sprite.rgb, effectColor));
                float alpha = sprite.a * lerp(1, (1 - isEffect) * step(0.5, sprite.a), saturate(_ExcludeSlashEffect));
                float fade = lerp(1, 0.48, saturate(i.height / 1.5));
                return fixed4(_ShadowColor.rgb, alpha * _ShadowColor.a * fade * i.alpha);
            }
            ENDCG
        }
    }
}


