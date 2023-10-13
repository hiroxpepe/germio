// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the GPL v2.0 license. See LICENSE text in the project root for license information.

Shader "Germio/TextureShadow"
{
    Properties
    {
        _Color("Color", Color) = (1, 1, 1, 0.5)
        [NoScaleOffset] _MainTex("Main Texture", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Background" }
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            Name "TEXTURE_SHADOW"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                SHADOW_COORDS(1)
                fixed3 ambient : COLOR1;
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.ambient = ShadeSH9(half4(worldNormal, 0));
                TRANSFER_SHADOW(o)
                return o;
            }

            sampler2D _MainTex;
            float4 _Color;

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texture_color = tex2D(_MainTex, i.uv);
                fixed shadow = SHADOW_ATTENUATION(i);
                fixed3 lighting = shadow + i.ambient;
                texture_color.rgb *= lighting;
                float4 final_color = texture_color * _Color;
                final_color.a = _Color.a;
                return final_color;
            }
            ENDCG
        }

        UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }

    Fallback "Standard"
}