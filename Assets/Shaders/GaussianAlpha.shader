Shader "Custom/GaussianAlpha" 
    {
 
     Properties
     {
         _MaskColor ("Mask Color", Color) = (0.5, 0.5, 0.5, 1)
         _MaskTex ("Mask", 2D) = "white" {}
         _Sigma ("Alpha Sigma", Range(0, 1)) = 0.2
         _Gain ("Alpha Gain", Range(0, 2)) = 1
         [IntRange] _Invert ("Invert", Range(0, 1)) = 0
     }
 
     SubShader
     {
         Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}
         LOD 300
         
         CGPROGRAM
         #pragma surface surf Lambert alpha:fade

         float4 _MaskColor;
         float _Sigma;
         float _Gain;
         sampler2D _MaskTex;
         int _Invert;

         #define PI 3.141592653589793
 
         struct Input
         {
             float2 uv_MaskTex;
             float3 viewDir;
         };
 
         void surf (Input IN, inout SurfaceOutput o)
         {
             // o.Normal = half3(0, 0, 1);
             // float3 n = WorldNormalVector(IN, o.Normal);
             // o.Emission = dot(IN.viewDir, o.Normal); 
             o.Emission = _MaskColor.rgb;   
             o.Alpha = clamp(_Gain * exp(-0.5 * pow(tex2D(_MaskTex, IN.uv_MaskTex).a / _Sigma, 2)) + _Invert, 0, 1) ;

             
         }
         ENDCG
     }
}

