// Adapted from xray mouse pos shader test v2.0 – mgear – http://unitycoder.com/blog

Shader "Custom/XRay2017"
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
		_Color("Color", Color) = (0.5,0.5,0.5,1)
		_ObjPos ("ObjPos", Vector) = (0,0,0,0)
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
		_Radius ("HoleRadius", Range(0.1,5)) = 0.5
	}
	SubShader 
	{
		Tags {"Queue"="AlphaTest" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
		LOD 100

		CGPROGRAM
		#pragma surface surf Lambert alphatest:_Cutoff

		struct Input 
		{
			float2 uv_MainTex;
			float3 worldPos;
		};

		fixed4 _Color;
		uniform float4 _ObjPos;
		uniform float _Radius;


		
		void surf (Input IN, inout SurfaceOutput o) 
		{
			// float4 localPos = IN.worldPos -  mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

			float dx = length(_ObjPos.x-IN.worldPos.x);
			float dy = length(_ObjPos.y-IN.worldPos.y);
			float dz = length(_ObjPos.z-IN.worldPos.z);

			
			float dist = (dx*dx + dy*dy + dz*dz) * _Radius;
			dist = clamp(dist, 0, 1);
			
			o.Emission = _Color.rgb;
			o.Alpha = dist;  // alpha is from distance to the mouse
		}
		ENDCG
	} 
	FallBack "Diffuse"
}