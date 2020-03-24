/*
*   Contains all the functionality needed by all QuantumVR object shaders
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_CGINC
#define QUANTUM_VR_CGINC

#include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRCore.cginc"

// world to local matrix of the portal the object is going through at the moment
float4x4 _WorldToPortalTraversing;
int _MaskingBit;

// if 1 or 2 special portal traversal clipping needs to be performed
int _DoPortalClipping;

/*  discards fragments that are in the posiive z space of a portal (which is where a portal goes to)
*
*   worldToPortal - world to local matrix of the portal used for clipping
*   worldSpaceFragPos - world space position of the fragment that should be checked
*/
void doPortalClippingInverted(float4x4 worldToPortal, float3 worldSpaceFragPos) 
{
    clip(-mul(worldToPortal, float4(worldSpaceFragPos, 1.0)).z);
}

/*  executes portal clipping if necessary
*
*   worldSpaceFragPos - world space position of the fragment that should be checked
*/
void performPortalClipping(float3 worldSpaceFragPos) 
{
    // if the masking bit is not 0, it means the object is rendered through a portal, so it must be clipped by that portal
    if(_MaskingBit)
        doPortalClipping(_WorldToPortal, worldSpaceFragPos, 0);
                
    // if _DoPortalClipping is 1 it means this material is rendering an object going through a portal on the same spatial layer as the portal is. It needs to be clipped behind the portal
    if(_DoPortalClipping == 1)
        doPortalClippingInverted(_WorldToPortalTraversing, worldSpaceFragPos);
    else if(_DoPortalClipping == 2) // if _DoPortalClipping is 2 it means this material is rendering an object going through a portal on the spatial layer the portal is leading to. we need to perform normal clipping for that portal
        doPortalClipping(_WorldToPortalTraversing, worldSpaceFragPos, 0);
}

#endif