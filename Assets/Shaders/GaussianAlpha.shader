Shader "Custom/GaussianAlpha" 
{
    Properties {
        _MaskColor ("Mask Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTex ("Color (RGB)", 2D) = "white" {}
        _AlphaTex("Alpha (A)", 2D) = "white" {}
        _Sigma ("Alpha Sigma", Range(0, 1)) = 0.2
        _Gain ("Alpha Gain", Range(1, 2)) = 1
        [IntRange] _Invert ("Invert", Range(0, 1)) = 0
    }
    
    SubShader{
        Pass {
            Tags {"Queue"="Transparent" "RenderType"="Transparent"}
            LOD 300
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                sampler2D _AlphaTex;
                float4 _MaskColor;
                float _Sigma;
                float _Gain;
                int _Invert;
            
                #define PI 3.141592653589793
            
                struct appdata {
                    float4 vertex : POSITION;
                    float3 normal : NORMAL;
                    float2 uv : TEXCOORD0;
                };
            
                struct v2f
                {
                    float4 pos : SV_POSITION;
                    float3 normal : TEXCOORD0;
                };
            
                v2f vert (appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.normal = v.normal;
                    return o;
                }
            
                inline float2 RadialCoords(float3 a_coords)
                {
                    float3 a_coords_n = normalize(a_coords);
                    float lon = atan2(a_coords_n.z, a_coords_n.x);
                    float lat = acos(a_coords_n.y);
                    float2 sphereCoords = float2(lon, lat) * (1.0 / PI);
                    return float2(sphereCoords.x * 0.5 + 0.5, 1 - sphereCoords.y);
                }
            
                float4 frag(v2f IN) : SV_Target
                {
                    // Get the UVs in equirectangular coordinates so that the Gaussian isn't distorted
                    float2 equiUV = RadialCoords(IN.normal);

                    // Define the mask color
                    
                    fixed4 c = tex2D(_MainTex, equiUV) * _MaskColor;

                    // Manipulate the alpha by shifting the width of the window
                    fixed4 alpha = 1 - _Gain * exp(-0.5 * pow(tex2D(_AlphaTex, equiUV) / _Sigma, 2));
                    // fixed4 alpha = 1- tex2D(_AlphaTex, equiUV);

                    // Clamp the range so we can close the window
                    alpha = clamp(alpha + _Invert, 0, 1);
                    // fixed4 alpha = clamp((_Gain / (pow(2 * PI, 0.5) * _Sigma)) * exp(-0.5 * pow(tex2D(_AlphaTex, equiUV) / _Sigma, 2)) + _Invert, 0, 1);
                    
                    // Blend the masks
                    return fixed4(c.r, c.g, c.b, alpha.r);
                }
            
            ENDCG
        }
    }
    FallBack "VertexLit"
}
