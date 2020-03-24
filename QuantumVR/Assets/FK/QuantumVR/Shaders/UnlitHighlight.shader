/*
*   A unlit Shader that renders in a dither pattern when behind objects. Backface culling is disabled for this shader
*   
*   v1.0 11/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "Unlit/Highlight"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _DitherTexture("Dither Texture (R)", 2D) = "white" {}
        _DitherThreshold("Dither Threshold", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}
        LOD 100

        Cull off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            fixed4 _Color;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _Color;
            }
            ENDCG
        }
        
        Pass
        {
            ZTest Greater
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            fixed4 _Color;
            float _DitherThreshold;
            
            sampler2D _DitherTexture;
            float4 _DitherTexture_ST;
            float4 _DitherTexture_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 screenPos = i.screenPos.xy/i.screenPos.w;
                float2 ditherCoordinate = screenPos * _ScreenParams.xy * _DitherTexture_TexelSize.xy;
                
                float dither = tex2D(_DitherTexture, ditherCoordinate*_DitherTexture_ST.xy).r;
                clip(dither-_DitherThreshold);
                return _Color;
            }
            ENDCG
        }
    }
}
