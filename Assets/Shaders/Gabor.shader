Shader "Custom/Gabor"
{
    Properties
    {
    	// For bars
		_Color1 ("Stripe Color 1", Color) = (0,0,0,1)
		_Color2 ("Stripe Color 2", Color) = (1,1,1,1)
		_Cycles ("Cycles (num stripes)", Range(0.1, 50)) = 2
		_Orientation ("Orientation (degrees)", Range(-180, 180)) = 0
		_Offset ("Phase (a.u.)", float) = 0
    	
    	// For alpha mask
    	_MaskColor ("Mask Color", Color) = (0.5, 0.5, 0.5, 1)
        _MainTex ("Color (RGB)", 2D) = "white" {}
        _AlphaTex("Alpha (A)", 2D) = "white" {}
        _Sigma ("Alpha Sigma", Range(0, 10)) = 0.2
        _Gain ("Alpha Gain", Range(0, 2)) = 1
        [IntRange] _Invert ("Invert", Range(0, 1)) = 0
    }
	
    SubShader
	{

		Pass
		{
			Tags {"Queue"="Transparent" "RenderType"="Transparent"}
            LOD 300
            
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			fixed4 _Color1;
			fixed4 _Color2;
			float _Cycles;
			float _Orientation;
			float _Offset;
			
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

			float3 rotatePoint3(float3 pt, float3 center, float angle) {
				float sinAngle = sin(angle);
				float cosAngle = cos(angle);
				pt -= center;

            	// right hand rule
            	float3x3 rot_mat= float3x3(1., 0., 0., 0., cosAngle, -sinAngle, 0, sinAngle, cosAngle); 
            	float3 r = mul(rot_mat, pt);

				r += center;
            	
				return r;
			}

			float2 RadialCoords(float3 a_coords)
            {
                float3 a_coords_n = normalize(a_coords);
                float lon = atan2(a_coords_n.z, a_coords_n.x);
                float lat = asin(a_coords_n.y);
                float2 sphereCoords = float2(lon, lat) * (1.0 / PI);
                return float2(sphereCoords.x * 0.5 + 0.5, 1 - sphereCoords.y);
            }
			
			fixed4 frag (v2f i) : SV_Target
			{
				// --- For the Sine Waves Stripes --- //
				// Rotate points about the pivot
				float3 pos_r = rotatePoint3(i.normal, float3(0., 0., 0.), _Orientation * PI / 180.0);

				// Transform to spherical coordinate system
				float2 pos = RadialCoords(pos_r);

				// Apply number of cycles
				pos.y *= _Cycles;

				// Need to subtract instead of add because neuro orientation convention is backwards
				pos -= float2(_Offset, _Offset);

				// Generate the stripes
				float sine = 0.5 + sin(2 * PI * pos.y) / 2;

				// --- For the Alpha Mask --- //
				float2 equiUV = RadialCoords(i.normal);
                fixed4 mask = tex2D(_MainTex, equiUV) * _MaskColor;
                fixed4 alpha = clamp(_Gain * exp(-0.5 * pow(tex2D(_AlphaTex, equiUV) / _Sigma, 2)) + _Invert, 0, 1);
				fixed4 full_mask = (mask.r, mask.g, mask.b, alpha.r);
				
				return full_mask;


			}
			ENDCG
		}
	}
}
