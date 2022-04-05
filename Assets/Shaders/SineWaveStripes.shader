Shader "Custom/SineWaveStripes"
{
    Properties
    {
		_Color1 ("Color 1", Color) = (0,0,0,1)
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Cycles ("Cycles", Range(0.1, 50)) = 2
		_Orientation ("Orientation", Range(-180, 180)) = 0
		_Offset ("Offset", float) = 0
    }
	
    SubShader
	{

		Pass
		{
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

			#define PI 3.141592653589793

			struct appdata {
               float4 vertex : POSITION;
               float3 normal : NORMAL;
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
				
				// Rotate points about the pivot
				float3 pos_r = rotatePoint3(i.normal, float3(0., 0., 0.), _Orientation * PI / 180.0);

				// Transform to spherical coordinate system
				float2 pos = RadialCoords(pos_r);

				// Apply number of cycles
				pos.y *= _Cycles;

				// Need to subtract instead of add because neuro orientation convention is backwards
				pos -= float2(_Offset, _Offset);

				// Generate the stripes
				return 0.5 + sin(2 * PI * pos.y) / 2;


			}
			ENDCG
		}
	}
}
