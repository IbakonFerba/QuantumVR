/*
*   A shader to render a stencil portal that is masked and clipped by another portal, meant for the camera
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "Hidden/QuantumVR/CameraView"
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
       
        ZWrite On
        ZTest always
        
        Stencil 
        {
            Ref 255
            WriteMask [_WriteBit]
            ZFail Zero
            Comp always
            Pass keep
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
