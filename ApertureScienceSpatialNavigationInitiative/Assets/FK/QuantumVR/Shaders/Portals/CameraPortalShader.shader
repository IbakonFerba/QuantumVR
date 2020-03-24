/*
*   A shader to render a stencil portal that is masked and clipped itself. This one is meant for the camera quad
*   
*   v2.1 11/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "Hidden/QuantumVR/CameraPortalStencil"
{
    Properties
    {
        _WriteBit("Stencil Write Bit", Range(0, 128)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ColorMask 0
        ZWrite Off
        
        Stencil 
        {
            Ref 255
            WriteMask [_WriteBit]
            Comp always
            Pass replace
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vertCamera
            #pragma fragment fragCamera

            #include "UnityCG.cginc"
            #include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRCamera.cginc"
            ENDCG
        }
    }
}
