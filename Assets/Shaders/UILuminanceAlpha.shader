Shader "UI/LuminanceAlpha"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _CutoffLow ("Cutoff Low", Range(0, 0.5)) = 0.06
        _CutoffHigh ("Cutoff High", Range(0, 0.8)) = 0.22
        _AlphaScale ("Alpha Scale", Range(0.5, 3)) = 1.4
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            sampler2D _MainTex;
            fixed4 _Color;
            float _CutoffLow;
            float _CutoffHigh;
            float _AlphaScale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                float lum = c.r * 0.299 + c.g * 0.587 + c.b * 0.114;
                float a = smoothstep(_CutoffLow, _CutoffHigh, lum) * _AlphaScale;
                a = saturate(a);
                return fixed4(c.rgb * i.color.rgb, a * i.color.a);
            }
            ENDCG
        }
    }
}
