Shader "Custom/LoopingVFX"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _Speed ("Animation Speed", Float) = 1.0
        _Intensity ("Intensity", Float) = 1.0
        _ScrollX ("Scroll X", Float) = 0.0
        _ScrollY ("Scroll Y", Float) = 0.5
        _Scale ("Scale", Float) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Speed;
            float _Intensity;
            float _ScrollX;
            float _ScrollY;
            float _Scale;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _Color * _Intensity;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Tạo animation scroll
                float2 scrolledUV = i.uv;
                scrolledUV.x += _ScrollX * _Speed * _Time.y;
                scrolledUV.y += _ScrollY * _Speed * _Time.y;
                
                // Scale UV
                scrolledUV = frac(scrolledUV * _Scale);
                
                // Sample texture
                fixed4 texColor = tex2D(_MainTex, scrolledUV);
                
                // Apply color and intensity
                fixed4 finalColor = texColor * i.color;
                
                return finalColor;
            }
            ENDCG
        }
    }

    Fallback "Sprites/Default"
}
