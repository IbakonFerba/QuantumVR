/*
*   A shader to depthmask a portal so nothing is rendered behind it. It is masked and clipped by another portal
*   
*   v2.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "Hidden/QuantumVR/PortalSurface"
{
    Properties
    {
        _MaskingBit("Masking Bit", Range(0, 128)) = 0
        _PortalClipOffset("Portal Clip Offset", Float) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ColorMask 0
        
        Stencil 
        {
            Ref 255
            ReadMask [_MaskingBit]
            Comp equal
        }
        Pass
        {            
            CGPROGRAM
            #pragma vertex vertBasic
            #pragma fragment fragPortal

            #include "UnityCG.cginc"
            #include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRPortals.cginc"
            ENDCG
        }
    }
}
