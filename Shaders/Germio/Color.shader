// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

Shader "Germio/Color"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha 
        Pass
        {
            Name "COLOR"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct appdata
            {
                float4 vertex : POSITION;

            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 _Color;

            fixed4 frag(v2f i) : SV_Target
            {
                float4 final_color = _Color;
                final_color.a = _Color.a;
                return final_color;
            }
            ENDCG
        }
    }

    Fallback "Standard"
}