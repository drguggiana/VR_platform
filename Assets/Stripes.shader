// Taken from the tutorial here: https://andreashackel.de/tech-art/stripes-shader-1/

Shader "Unlit/Stripes"
{
    Properties {
    _Color1 ("Color 1", Color) = (0,0,0,1)
	_Color2 ("Color 2", Color) = (1,1,1,1)
	_Tiling ("Tiling", Range(1, 500)) = 10
    _Direction ("Direction", Range(0, 1)) = 0
    _WarpScale ("Warp Scale", Range(0, 1)) = 0
	_WarpTiling ("Warp Tiling", Range(1, 10)) = 1
    _WidthShift ("Width Shift", Range(0, 1)) = 0.5
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
            int _Tiling;         // How much tiling is done
            float _Direction;    // Angle of rotation from 0 to 90 degrees
            float _WarpScale;
			float _WarpTiling;
            float _WidthShift;
            
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
	        return r;
            }

            // This is where the magic happens
            fixed4 frag (v2f i) : SV_Target
            {
                //float2 pos;
                //pos.x = lerp(i.uv.x, i.uv.y, _Direction);
                //pos.y = lerp(i.uv.y, 1 - i.uv.x, _Direction);
                // float pos = lerp(i.uv.x, i.uv.y, _Direction) * _Tiling;

                const float PI = 3.14159;

				float2 pos = rotatePoint(i.uv.xy, float2(0.5, 0.5), _Direction * 2 * PI);

				pos.x += sin(pos.y * _WarpTiling * PI * 2) * _WarpScale;
				pos.x *= _Tiling;

                fixed value = floor(frac(pos.x) + _WidthShift);
	            return  lerp(_Color1, _Color2, value);

            }
            
            ENDCG
        }
    }
}
