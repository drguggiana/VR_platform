  Shader "Custom/RotateUVs3" {
        Properties {
			//add the cubemap texture to the inspector properties (here is where the script will send the cubemap)
			_Cube ("Reflection Cubemap", Cube) = "_Skybox" {}
			//this is where the rotation is sent from the script if active (not needed now)
            //_Rotation ("Rotation Speed", Vector) = (2,2,2,1)
			//this is the albedo of the output, using default normally
			_ReflectColor ("Reflection Color", Color) = (1,1,1,0.5)
			//here is where the script sends the player position
			_Player ("Player position", Vector) = (0, 0, 0, 0)

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
			fixed4 _Player;
			//define the input structure to the surface shader
            struct Input {
			//load only the world position variable of each pixel, which is the key to the entire process, since this is what allows the cubemap to the properly mapped
				float3 worldPos;
            };
		//define the rotation variable here, again, not used so commented
           //fixed4 _Rotation;

		   //actual surface shader code
			void surf (Input IN, inout SurfaceOutput o) { 
			
			//below is the rotation code (i.e. rotate the texture with the players position, not really used now)
				//float sx = sin ( _Rotation.x  );
				//float cx = cos ( _Rotation.x  );

				//float3x3 rotationMatrixX = float3x3( 1, 0, 0, 0, cx, -sx, 0, sx, cx);

				//float sy = sin ( _Rotation.y  );
				//float cy = cos ( _Rotation.y  );

				//float3x3 rotationMatrixY = float3x3( cy, 0, sy, 0, 1, 0, -sy, 0, cy);

				//float sz = sin ( _Rotation.z  );
				//float cz = cos ( _Rotation.z  );

				//float3x3 rotationMatrixZ = float3x3( cz, -sz, 0, sz, cz, 0, 0, 0, 1);

				//IN.worldRefl = mul(IN.worldRefl, rotationMatrixX);
				//IN.worldRefl = mul(IN.worldRefl, rotationMatrixY);
				//IN.worldRefl = mul(IN.worldRefl, rotationMatrixZ); 

				//correct the world position with the position of the player, allowing for the rendering to look as if seen by the player
				IN.worldPos -= _Player.xyz;
				//render the cube texture based on the corrected world position coordinates
				fixed4 reflcol = texCUBE (_Cube, IN.worldPos);
				//load the colors as the emission value of the output
				o.Emission = reflcol.rgb * _ReflectColor.rgb;
				//and the corresponding alpha, which doesn't really to much here
				o.Alpha = reflcol.a * _ReflectColor.a;

               
            }
            ENDCG
        }
        //FallBack "Diffuse"
    }