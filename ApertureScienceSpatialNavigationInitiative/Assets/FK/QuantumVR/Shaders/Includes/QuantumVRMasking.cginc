/*
*   Contains the basic functionality for all QuantumVR portal masking shaders
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_MASKING_CGINC
#define QUANTUM_VR_MASKING_CGINC

#include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRCore.cginc"

struct appdata
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float4 worldPos : TEXCOORD0;
};

v2f vertBasic (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
    return o;
}

#endif