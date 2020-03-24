/*
*   A shader to render a a quad at the far clip plane to clear the z buffer
*   
*   v1.0 11/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
Shader "Hidden/ClearDepthShader"
{
    Properties
    {
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        ZTest Always
        Colormask 0
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRClearDepth.cginc"
            ENDCG
        }
    }
}
