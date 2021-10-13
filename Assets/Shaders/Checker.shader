Shader "Unlit/Checker"
{
	Properties
	{
//		_MainTex ("Texture", 2D) = "white" {}
		_Density ("Density", Range(2,50)) = 30
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
//			 make fog work
//			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 pos : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
//				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
			};

			float _Density;

//			sampler2D _MainTex;
//			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.pos);
//				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv = v.uv*_Density;
//				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 c = i.uv;
				c = floor(c)/2;
				float checker = frac(c.x + c.y)*2;
				return checker;
//				// sample the texture
//				fixed4 col = tex2D(_MainTex, i.uv);
//				// apply fog
//				UNITY_APPLY_FOG(i.fogCoord, col);
//				return col;
			}
			ENDCG
		}
	}
}
