// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

Shader "Germio/Texture"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 0.5)
        [NoScaleOffset] _MainTex("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "TEXTURE"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldNormal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 _Color;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 texture_color = tex2D(_MainTex, i.uv);
                float4 final_color = texture_color * _Color;
                final_color.a = _Color.a;
                return final_color;
            }
            ENDCG
        }
    }

    Fallback "Standard"
}