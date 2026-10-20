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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
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
            // Coordinates follow the full 3:1 panorama, so menu panning and zoom stay aligned.
            float shaft(float2 uv, float center, float spread, float t, float phase)
            {
                float depth=saturate((.73-uv.y)/.53);
                // Keep the source at the window; let the lower ends sweep slowly.
                float sway=sin(t*.65)*.006+sin(t*.39+phase)*.002;
                float axis=center+depth*spread+sway*depth;
                float width=lerp(.009,.068,depth)*(1+.03*sin(t*.71+phase));
                float across=(uv.x-axis)/width;
                float envelope=exp(-across*across*2.0);
                float ribbons=.53+.12*sin(across*9+depth*8-t*1.25+phase)
                    +.06*sin(across*17-depth*11+t*.77);
                return envelope*ribbons*smoothstep(.15,.25,uv.y)*(1-smoothstep(.70,.76,uv.y));
            }
            fixed4 frag(v2f i):SV_Target
            {
                float2 uv=i.uv;
                float t=_UnscaledTime*.55;
                float breath=.78+.05*sin(t*.63)+.03*sin(t*1.13+2);
                // Animate inside the painted beam, never move its outer silhouette.
                float beamDepth=saturate((.74-uv.y)/.54);
                float leftEdge=lerp(.445,.140,beamDepth);
                float rightEdge=lerp(.445,.575,beamDepth);
                float beamMask=smoothstep(0,.008,uv.x-leftEdge)
                    *smoothstep(0,.008,rightEdge-uv.x)
                    *smoothstep(.20,.23,uv.y)*(1-smoothstep(.72,.74,uv.y));
                float rays=shaft(uv,.430,-.22,t,0)*.65
                    +shaft(uv,.440,-.14,t,1)*.70
                    +shaft(uv,.449,-.05,t,2)*.75
                    +shaft(uv,.458,.025,t,3)*.70
                    +shaft(uv,.468,.085,t,4)*.65;
                // A restrained pulse inside the central stained glass.
                float2 w=(uv-float2(.449,.53))/float2(.043,.28);
                float window=exp(-dot(w,w)*2)*(.65+.12*sin(t*.47+uv.y*7));
                // Widen the reflected streaks towards the viewer, confined to the floor.
                float floorDepth=saturate((.22-uv.y)/.22);
                float reflectionAxis=.45+floorDepth*.14+sin(t*.65)*.006*floorDepth;
                float reflection=exp(-pow((uv.x-reflectionAxis)/(.045+floorDepth*.14),2)*2);
                reflection*=1-smoothstep(.19,.235,uv.y);
                // Follow only the original diagonal pool of light on the floor.
                float floorLeft=.28+floorDepth*.20;
                float floorRight=.55+floorDepth*.23;
                reflection*=smoothstep(0,.012,uv.x-floorLeft)
                    *smoothstep(0,.012,floorRight-uv.x);
                reflection*=.55+.10*sin(uv.x*82+uv.y*24-t*.90)+.06*sin(uv.x*141-uv.y*37+t*.62);
                float light=((rays*.29+window*.075)*beamMask+reflection*.16)*breath;
                return fixed4(_Color.rgb,saturate(light*_Intensity)*i.color.a*_Color.a);
            }
            ENDCG
        }
    }
}
