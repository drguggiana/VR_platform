// Taken from the tutorial here: https://andreashackel.de/tech-art/stripes-shader-1/

Shader "Custom/Stripes"
{
    Properties {
    _Color1 ("Color 1", Color) = (0,0,0,1)
	_Color2 ("Color 2", Color) = (1,1,1,1)
	_Cycles ("Cycles", Range(0.01, 180)) = 1
    _Orientation ("Orientation", Range(0, 180)) = 0
    _WarpScale ("Warp Scale", Range(0, 1)) = 0
	_WarpTiling ("Warp Tiling", Range(1, 10)) = 1
    _WidthShift ("Width Shift", Range(0, 1)) = 0.5
    _Offset("deltaVertex", float) = 0
    	
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color1;
            fixed4 _Color2;
            float _Cycles;       // How much tiling is done
            float _Orientation;    // Angle of rotation from 0 to 90 degrees
            float _WarpScale;
			float _WarpTiling;
            float _WidthShift;
            float _Offset;

            const float PI = 3.14159;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
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

            	// Set the motion of the grating
            	r += float2(_Offset, _Offset);
            	
		        return r;
            }

            // This is where the magic happens
            fixed4 frag (v2f i) : SV_Target
            {
                //float2 pos;
                //pos.x = lerp(i.uv.x, i.uv.y, _Direction);
                //pos.y = lerp(i.uv.y, 1 - i.uv.x, _Direction);
                // float pos = lerp(i.uv.x, i.uv.y, _Direction) * _Tiling;

            	// Rotate the grating
				float2 pos = rotatePoint(i.uv.xy, float2(0.5, 0.5), _Orientation * PI / 180.0f);

            	// warp the grating
				pos.x += sin(pos.y * _WarpTiling * PI * 2) * _WarpScale;

            	// set the number of cycles
				pos.x *= _Cycles;

            	// set the width of individual colors
                fixed value = floor(frac(pos.x) + _WidthShift);
            	
	            return lerp(_Color1, _Color2, value);

            }
            
            ENDCG
        }
    }
}
