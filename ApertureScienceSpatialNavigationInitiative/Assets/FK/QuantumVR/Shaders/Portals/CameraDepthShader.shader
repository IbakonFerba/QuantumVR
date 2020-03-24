/*
*   A shader to depthmask a portal so nothing is rendered behind it. This one is meant for the camera quad
*   
*   v2.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "Hidden/QuantumVR/CameraDepth"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ColorMask 0
        
        Pass
        {            
            CGPROGRAM
            #pragma vertex vertBasic
            #pragma fragment fragCamera

            #include "UnityCG.cginc"
            #include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRCamera.cginc"
            ENDCG
        }
    }
}
