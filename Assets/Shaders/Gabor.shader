Shader "Custom/Gabor"
 {
     Properties
     {
         _MaskColor ("Mask Color", Color) = (0.5, 0.5, 0.5, 1)
         _MaskTex ("Mask", 2D) = "white" {}
         _Gain ("Alpha Gain", Range(0, 5)) = 1
         
         _Color1 ("Color 1", Color) = (0,0,0,1)
		 _Color2 ("Color 2", Color) = (1,1,1,1)
         _Cycles ("Cycles", Range(0.1, 50)) = 2
         _Orientation ("Orientation", Range(-180, 180)) = 0
         _Offset ("Offset", float) = 0
     }
 
     SubShader
     {
         Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="Transparent"}
         LOD 300

         CGPROGRAM
         #pragma surface surf Lambert alpha:fade vertex:vert

         float4 _MaskColor;
         sampler2D _MaskTex;
         float _Gain;
   

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

         
         struct Input
         {
             float2 uv_MaskTex;
		 	 float2 uv : TEXCOORD0;
			 float4 vertex : SV_POSITION;
         };
 
         void surf (Input IN, inout SurfaceOutput o)
         {
             o.Emission = _MaskColor.rgb;
             o.Alpha = clamp(_Gain * (1 - tex2D(_MaskTex, IN.uv_MaskTex).a), 0, 1);
         }
         ENDCG
     }
}