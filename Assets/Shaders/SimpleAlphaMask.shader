Shader "Custom/SimpleAlphaMask"
 {
     Properties
     {
         _MaskColor ("Mask Color", Color) = (0.5, 0.5, 0.5, 1)
         _MaskTex ("Mask", 2D) = "white" {}
         _Gain ("Alpha Gain", Range(0, 10)) = 1
         [IntRange] _Invert ("Invert", Range(0, 1)) = 0
     }
 
     SubShader
     {
         Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}
         LOD 300
         
         CGPROGRAM
         #pragma surface surf Lambert alpha:fade

         float4 _MaskColor;
         float _Gain;
         sampler2D _MaskTex;
         int _Invert;
 
         struct Input
         {
             float2 uv_MaskTex;
         };
 
         void surf (Input IN, inout SurfaceOutput o)
         {
             o.Emission = _MaskColor.rgb;
             // o.Alpha = pow(1 - tex2D(_MaskTex, IN.uv_MaskTex), _Gain);
             o.Alpha = clamp(_Gain * (1 - tex2D(_MaskTex, IN.uv_MaskTex).a) , 0, 1);
         }
         ENDCG
     }
}