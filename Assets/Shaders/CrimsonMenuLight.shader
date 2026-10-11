Shader "DarkLord/UI/Crimson Menu Light"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,0.16,0.12,1)
        _Intensity ("Intensity", Range(0,2)) = 0.6
        _UnscaledTime ("Unscaled time", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            fixed4 _Color;
            float _Intensity, _UnscaledTime;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            float flame(float2 uv, float2 center, float seed)
            {
                float t=_UnscaledTime;
                float flicker=0.72+0.16*sin(t*7.3+seed)+0.09*sin(t*13.1+seed*3);
                float2 d=uv-center;
                d.x+=sin(t*4.7+seed+d.y*140)*0.0015;
                float core=exp(-dot(d/float2(.0023,.010*flicker),d/float2(.0023,.010*flicker)));
                float glow=exp(-dot(d/float2(.022,.041),d/float2(.022,.041)))*.18;
                return (core+glow)*flicker;
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float t=_UnscaledTime;
                float fire=flame(uv,float2(.012,.817),1)+flame(uv,float2(.036,.850),3)
                    +flame(uv,float2(.066,.818),5)+flame(uv,float2(.546,.625),7)
                    +flame(uv,float2(.563,.632),9)+flame(uv,float2(.578,.611),11);
                // Soft moving shafts follow the painted window's downward-left light.
                float y=uv.y;
                float axis=.414-(.63-y)*.58;
                float beam=exp(-pow((uv.x-axis)/.087,2))*smoothstep(.02,.3,y)*(1-smoothstep(.61,.83,y));
                float ribbons=.55+.20*sin(uv.x*51+y*19+t*.36)+.12*sin(uv.x*94-y*32-t*.21);
                float window=exp(-dot((uv-float2(.411,.62))/float2(.075,.27),(uv-float2(.411,.62))/float2(.075,.27)));
                float light=fire+beam*ribbons*.11+window*(.06+.018*sin(t*.7));
                return fixed4(_Color.rgb, saturate(light*_Intensity)*i.color.a*_Color.a);
            }
            ENDCG
        }
    }
}
