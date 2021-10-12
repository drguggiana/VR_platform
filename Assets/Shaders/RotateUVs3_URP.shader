Shader "Custom/RotateUVs3_URP" {
  	
	Properties {
		//add the cubemap texture to the inspector properties (here is where the script will send the cubemap)
		_Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
		
		//this is the albedo of the output, using default normally
		_ReflectColor ("Reflection Color", Color) = (1, 1, 1, 0.5)
		
		//here is where the script sends the player position
		_Player ("Player position", Vector) = (0, 0, 0)
	}
  	
    SubShader {
		//define the type of render and the Level Of Detail
        Tags { "RenderType"="Opaque" }
        LOD 200
       
        CGPROGRAM
        
		//define the surface shader
        #pragma surface surf Lambert
        
		//need to declare the variables from above here also, so they are used for the actual Shader
		samplerCUBE _Cube;
		fixed4 _ReflectColor;
		fixed3 _Player;
        
		//define the input structure to the surface shader
        struct Input {
			//load only the world position variable of each pixel, which is the key to the entire process,
        	//since this is what allows the cubemap to the properly mapped
			float3 worldPos;
        };

		void surf (Input IN, inout SurfaceOutput o) { 

			//correct the world position with the position of the player, allowing for the rendering to look as if seen by the player
			IN.worldPos -= _Player.xyz;
			//render the cube texture based on the corrected world position coordinates
			fixed4 reflcol = texCUBE (_Cube, IN.worldPos);
			//load the colors as the emission value of the output
			o.Emission = reflcol.rgb * _ReflectColor.rgb;
			//and the corresponding alpha, which doesn't really do much here
			o.Alpha = reflcol.a * _ReflectColor.a;
        }
        ENDCG
    }
}