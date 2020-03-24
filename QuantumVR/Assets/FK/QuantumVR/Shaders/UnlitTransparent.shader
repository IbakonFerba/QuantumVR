/*
*   A transparent unlit Shader masked by stencil and clipped by a portal
*   
*   v1.0 10/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "QuantumVR/Unlit Transparent"
{
    Properties
    {
        _Color("Color (RGB)", Color) = (1,1,1,1)
        _MainTex ("Texture (RGB)", 2D) = "white" {}
        
        // portal rendering
         [HideInInspector] _MaskingBit("Masking Bit", Range(0, 128)) = 0
         [HideInInspector] _DoPortalClipping("Do Portal Clipping", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Stencil 
        {
            Ref 255
            ReadMask [_MaskingBit]
            Comp equal
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex vertUnlit
            #pragma fragment fragUnlitColorTex
            // make fog work
            #pragma multi_compile_fog

            #include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRUnlit.cginc"
            ENDCG
        }
    }
}
