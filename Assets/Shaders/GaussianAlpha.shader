// Taken from the example at 
// https://forum.unity.com/threads/solved-how-to-add-an-alpha-mask-over-the-existing-transparent-cutout-soft-edge-unlit-shader.289856/    
Shader "Custom/GaussianAlpha" ////////// Shader renamed
    {
       Properties
       {
          _Color ("Mask Color", Color) = (1, 1, 1, 1) ////////// _Color property added to this shader
          _MainTex ("Base (RGB)", 2D) = "white" {}
          _Mask ("Culling Mask", 2D) = "white" {}
          _Cutoff ("Alpha cutoff", Range (0,1)) = 0.1
       }
       SubShader
       {
          Tags {"Queue"="Transparent"}
          Cull Off ////////// I added this line to have the both sides of my plane mesh visible
          Lighting Off
          ZWrite Off
          Blend SrcAlpha OneMinusSrcAlpha
          AlphaTest GEqual [_Cutoff]
          Pass
          {
             SetTexture [_Mask] {
                 constantColor [_Color]
                 Combine texture * constant ////////// The alpha component of the _Color property defines the Culling Mask opacity (depending on the distance between the player and the grid)
                }
             SetTexture [_MainTex] {combine texture, previous}
          }
       }
    }
