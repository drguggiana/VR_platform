// Adapted from the tutorial here: https://andreashackel.de/tech-art/stripes-shader-1/

Shader "Custom/Stripes"
{
	Properties {
		[IntRange] _NumColors ("Number of colors", Range(2, 4)) = 2
		_Color1 ("Color 1", Color) = (0,0,0,1)
		_Color2 ("Color 2", Color) = (1,1,1,1)
		_Color3 ("Color 3", Color) = (1,0,1,1)
		_Color4 ("Color 4", Color) = (0,0,1,1)
		_Cycles ("Cycles", Range(0.1, 50)) = 2
		_WidthShift ("Width Shift", Range(-1, 1)) = 0
		_Orientation ("Orientation", Range(-180, 180)) = 0
		_WarpScale ("Warp Scale", Range(0, 1)) = 0
		_WarpTiling ("Warp Tiling", Range(1, 10)) = 1
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

			int _NumColors;
			fixed4 _Color1;
			fixed4 _Color2;
			fixed4 _Color3;
			fixed4 _Color4;
			
			float _Cycles;
			float _WidthShift;
			float _Orientation;
			float _WarpScale;
			float _WarpTiling;
			float _Offset;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				
			};
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float2 rotatePoint(float2 pt, float2 center, float angle) {
				float sinAngle = sin(angle);
				float cosAngle = cos(angle);
				pt -= center;
				float2 r;
				r.x = pt.x * cosAngle - pt.y * sinAngle;
				r.y = pt.x * sinAngle + pt.y * cosAngle;
				r += center;
				return r;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				const float PI = 3.14159;

				// Rotate points about the pivot
				float2 pos = rotatePoint(i.uv.xy, float2(0.5, 0.5), _Orientation * PI / 180.0);

				// Apply nonlinear warping
				pos.y += sin(pos.x * _WarpTiling * PI * 2) * _WarpScale;

				// Apply number of cycles
				pos.y *= _Cycles;

				pos += float2(_Offset, _Offset);

				// Generate the correct number of stripes
				int value = floor(frac(pos.y) * _NumColors  + _WidthShift);
				value = clamp(value, 0, _NumColors - 1);
				switch (value) {
					case 3: return _Color4;
					case 2: return _Color3;
					case 1: return _Color2;
					default: return _Color1;
				}
			}
			ENDCG
		}
	}
}
