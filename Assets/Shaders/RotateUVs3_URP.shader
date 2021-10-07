Shader "Custom/RotateUVs3URP" {
    Properties {
		//add the cubemap texture to the inspector properties (here is where the script will send the cubemap)
		_Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
        
		//this is where the rotation is sent from the script if active (not needed now)
        //_Rotation ("Rotation Speed", Vector) = (2,2,2,1)
        
		//this is the albedo of the output, using default normally
		_ReflectColor ("Reflection Color", Color) = (1, 1, 1, 0.5)
        
		//here is where the script sends the player position
		_Player ("Player position", Vector) = (0, 0, 0, 0)

    }
	
    SubShader 
  	{
		//define the type of render and the Level Of Detail
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline"}
        LOD 200
        
        HLSLINCLUDE
            // Required by all Universal Render Pipeline shaders.
            // It will include Unity built-in shader variables (except the lighting variables)
            // (https://docs.unity3d.com/Manual/SL-UnityShaderVariables.html
            // It will also include many utilitary functions. 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Include this if you are doing a lit shader. This includes lighting shader variables,
            // lighting and shadow functions
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

		    // Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
		    #pragma exclude_renderers d3d11_9x
		//       #pragma vertex vert
		//       #pragma fragment frag

        //       
        // GPU instancing
		#pragma multi_compile_instancing

        CBUFFER_START(UnityPerMaterial)
				float4 _ReflectColor;
				float4 _Player;
        CBUFFER_END

        SAMPLER(sampler_Cube);

        
        //define the input structure to the surface shader
        struct VertexInput 
        {
			//load only the world position variable of each pixel, which is the key to the entire process, since this is what allows the cubemap to the properly mapped
			float4 world_position : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct VertexOutput
        {
	        float4 position : SV_POSITION;
        	float2 uv : TEXCOORD0;
        };

        ENDHLSL

        
        Pass
        {

			//define the surface shader
            // #pragma surface surf Lambert
			//need to declare the variables from above here also, so they are used for the actual Shader

            
			//samplerCUBE _Cube;
			
            HLSLPROGRAM
			
            VertexOutput vert(VertexInput i)
	        {
		        VertexOutput o;

        		i.world_position -= _Player.xyz;
        	
        		//render the cube texture based on the corrected world position coordinates
				float4 reflcol = texCUBE (_Cube, IN.worldPos);
				//load the colors as the emission value of the output
				o.Emission = reflcol.rgb * _ReflectColor.rgb;
				//and the corresponding alpha, which doesn't really to much here
				o.Alpha = reflcol.a * _ReflectColor.a;
               
	        }

		   //actual surface shader code
			void surf (Input IN, inout SurfaceOutput o) { 
				//correct the world position with the position of the player, allowing for the rendering to look as if seen by the player
				IN.worldPos -= _Player.xyz;
				//render the cube texture based on the corrected world position coordinates
				float4 reflcol = texCUBE (_Cube, IN.worldPos);
				//load the colors as the emission value of the output
				o.Emission = reflcol.rgb * _ReflectColor.rgb;
				//and the corresponding alpha, which doesn't really to much here
				o.Alpha = reflcol.a * _ReflectColor.a;
               
			}

            ENDHLSL
        }
	}
}
           