Shader "Custom/GaussianAlpha" 
    {
 
     Properties
     {
         _MaskColor ("Mask Color", Color) = (0.5, 0.5, 0.5, 1)
         _MainTex ("Mask", 2D) = "white" {}
         _Sigma ("Alpha Sigma", Range(0, 10)) = 0.2
         _Gain ("Alpha Gain", Range(0, 2)) = 1
         [IntRange] _Invert ("Invert", Range(0, 1)) = 0
     }
 
     SubShader
     {
         Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}
         LOD 300
         
         CGPROGRAM
         #pragma surface surf Standard  alpha:fade vertex:vert

         float4 _MaskColor;
         float _Sigma;
         float _Gain;
         sampler2D _MainTex;
         int _Invert;

         #define PI 3.141592653589793
 
         struct Input
         {
             float3 viewDir;
             float3 direction;
         };

         void vert(inout appdata_full v, out Input o) {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.direction = v.normal;
        }
         
         void surf (Input IN, inout SurfaceOutputStandard o)
         {

            // Puff out the direction to compensate for interpolation.
            float3 direction = normalize(IN.direction);

            // Get a longitude wrapping eastward from x-, in the range 0-1.
            float longitude = 0.5 - atan2(direction.z, direction.x) / (2.0f * PI);
            // Get a latitude wrapping northward from y-, in the range 0-1.
            float latitude = 0.5 + asin(direction.y) / PI;

            // Combine these into our own sampling coordinate pair.
            float2 customUV = float2(longitude, latitude);

             // Set color and the alpha 
             o.Emission = _MaskColor.rgb;
             o.Alpha = clamp(tex2D(_MainTex, customUV).a + _Invert, 0, 1);
             // o.Alpha = clamp(_Gain * exp(-0.5 * pow(tex2D(_MainTex, customUV).a / _Sigma, 2)) + _Invert, 0, 1);

             
         }
         ENDCG
     }
}

