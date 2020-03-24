/*
*   Contains all the functionality for Clear depth shaders
*   
*   v1.1 11/2019
*   Written by Fabian Kober
*   fabian-kober@gmx.net
*/
#ifndef QUANTUM_VR_CLEAR_DEPTH_CGINC
#define QUANTUM_VR_CLEAR_DEPTH_CGINC

#include "UnityCG.cginc"

 struct appdata
{
    float4 vertex : POSITION;
};

struct v2f
{
    float4 vertex : SV_POSITION;
};

sampler2D _MainTex;
float4 _MainTex_ST;

v2f vert (appdata v)
{
    v2f o;
    o.vertex = float4(2.0 * (v.vertex.x + 0.5) - 1.0, _ProjectionParams.x * (2.0 * (v.vertex.y + 0.5) - 1.0), 0.0, 1.0);
    
    #if defined(UNITY_REVERSED_Z)
    // far plane is at depth 0, near at w
    o.vertex.z = 0;
    #else
    // far plane is at depth w, near is either 0 or -w
    o.vertex.z = o.vertex.w;
    #endif
    return o;
}

fixed4 frag (v2f i) : SV_Target
{
    return float4(1,0,0,1);
}

#endif