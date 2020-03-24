/*
*   Contains all the functionality for Unlit QuantumVR Object shaders
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_UNLIT_CGINC
#define QUANTUM_VR_UNLIT_CGINC

#include "UnityCG.cginc"
#include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRObjects.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    UNITY_FOG_COORDS(1)
    float4 vertex : SV_POSITION;
    float3 posWorld : TEXCOORD1;
};

fixed4 _Color;
sampler2D _MainTex;
float4 _MainTex_ST;

v2f vertUnlit (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    o.posWorld = mul(unity_ObjectToWorld, v.vertex);
    UNITY_TRANSFER_FOG(o,o.vertex);
    return o;
}

fixed4 fragUnlitColorTex (v2f i) : SV_Target
{
    performPortalClipping(i.posWorld);
    // sample the texture
    fixed4 col = tex2D(_MainTex, i.uv)*_Color;
    // apply fog
    UNITY_APPLY_FOG(i.fogCoord, col);
    return col;
}

#endif