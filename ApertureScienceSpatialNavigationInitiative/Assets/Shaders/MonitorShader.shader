Shader "Custom/MonitorShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _OverlayTex("Overlay Texture (RGB)", 2D) = "white" {}
        _OverlayStrength("Overlay Strength", Range(0,1)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        
        // portal rendering
        [HideInInspector] _MaskingBit("Masking Bit", Range(0, 128)) = 0
        [HideInInspector] _DoPortalClipping("Do Portal Clipping", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Stencil 
        {
            Ref 255
            ReadMask [_MaskingBit]
            Comp equal
        }
        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        
        #include "Assets/FK/QuantumVR/Shaders/Includes/QuantumVRObjects.cginc"

        sampler2D _MainTex;
        sampler2D _OverlayTex;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_OverlayTex;
            float3 worldPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        float _OverlayStrength;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
			clip(IN.worldPos.y);
            performPortalClipping(IN.worldPos);
            // Albedo comes from a texture tinted by color
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
            
            float overlay1 = tex2D(_OverlayTex, float2(IN.uv_OverlayTex.x+_Time.x, IN.uv_OverlayTex.y)).r;
            float overlay2 = tex2D(_OverlayTex, float2(IN.uv_OverlayTex.x-_Time.y, IN.uv_OverlayTex.y)).g;
            float overlay3 = tex2D(_OverlayTex, float2(IN.uv_OverlayTex.x+_Time.z, IN.uv_OverlayTex.y)).b;
            o.Albedo = fixed4(0,0,0,1);
            // Metallic and smoothness come from slider variables
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Emission = c.rgb*(1-(overlay1+overlay2+overlay3)*_OverlayStrength);
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
