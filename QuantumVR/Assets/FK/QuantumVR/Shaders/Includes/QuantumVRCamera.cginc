/*
*   contains all the functionality for camera related QuantumVR shaders
*   
*   v1.1 11/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_CAMERA_CGINC
#define QUANTUM_VR_CAMERA_CGINC
        
#include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRMasking.cginc"

v2f vertCamera (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.vertex.z = _ProjectionParams.y+0.0000001;
    o.worldPos = mul(UNITY_MATRIX_M, v.vertex);
    return o;
}

fixed4 fragCamera (v2f i) : SV_Target
{
    doPortalClipping(_WorldToPortal, (i.worldPos/i.worldPos.w).xyz, 0);
        
    return fixed4(0,1,1,1);
}
            
#endif