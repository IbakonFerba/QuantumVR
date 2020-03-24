/*
*   Contains all the functionality for QuantumVR portal shaders
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_PORTALS_CGINC
#define QUANTUM_VR_PORTALS_CGINC

#include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRMasking.cginc"

int _MaskingBit;
float _PortalClipOffset;

fixed4 fragPortal (v2f i) : SV_Target
{
    // if the masking bit is 0 we don't want to do the clipping because we are in the active cell. 
    // 0 = false here, so we can just use the value directly to check whether to do clipping or not
    if(_MaskingBit)
        doPortalClipping(_WorldToPortal, (i.worldPos/i.worldPos.w).xyz, _PortalClipOffset);
        
    return fixed4(0,1,1,1);
}

#endif