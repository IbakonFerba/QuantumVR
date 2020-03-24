/*
*   Contains functionality needed in all QuantumVR shaders
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_CORE_CGINC
#define QUANTUM_VR_CORE_CGINC

float4x4 _WorldToPortal;
        
/*  discards fragments that are in the negative z space of a portal (which is where a portal comes from)
*
*   worldToPortal - world to local matrix of the portal used for clipping
*   worldSpaceFragPos - world space position of the fragment that should be checked
*/
void doPortalClipping(float4x4 worldToPortal, float3 worldSpaceFragPos, float portalClipOffset) 
{
   clip(mul(worldToPortal, float4(worldSpaceFragPos, 1.0)).z-portalClipOffset);
}

#endif