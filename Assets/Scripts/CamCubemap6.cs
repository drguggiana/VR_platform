// Attach this script to an object that uses a Reflective shader.
// Realtime reflective cubemaps!

//Include the pertinent libraries
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//so it runs during edit mode
[ExecuteInEditMode]

//Declare the class
public class CamCubemap6 : MonoBehaviour
{
    //define the cubemap size
    public int cubemapSize = 1024;
    //declare the camera variable (the special one that will render the cubemap)
    private Camera cam;
    //declare the public variable to add the camera transform
    public Transform playerTrans;
    //declare the render texture that will receive the cubemap
    private RenderTexture rtex;
    //declare the variable to send rotation to the shader, not needed now
    //private Vector4 rot;

    void Start()
    {
        // render all six faces at startup
        UpdateCubemap(63);
    }

    void LateUpdate()
    {
       //render the faces this frame
       UpdateCubemap(63); // all six faces
        
    }

    void UpdateCubemap(int faceMask)
    {   
        //if the camera hasn't been created, create it
        if (!cam)
        {   
            //create a temp game object to contain the camera
            GameObject go = new GameObject("CubemapCamera");
            //add the camera to the game object
            go.AddComponent<Camera>();
            //hide the flags from this GO to the inspector
            go.hideFlags = HideFlags.HideAndDontSave;
            //get the handle to the camera
            cam = go.GetComponent<Camera>();
            //update the position of the camera to the position of the camera in the player
            cam.transform.position = playerTrans.position;
            //define the near clip plane of the camera
            cam.nearClipPlane = 0.03f;
            //define the far clip plane
            cam.farClipPlane = 10; // don't render very far into cubemap
            //define the field of view, 90 for cubemapping
            cam.fieldOfView = 90;
            //define the aspect ratio, 1 for cubemapping
            cam.aspect = 1;
            //disable the camera, since it's only used to generate the cubemap
            cam.enabled = false;
        }
        //if the render texture hasn't been created
        if (!rtex)
        {
            //create the render texture with the desired size, make it square and depth needs to be a power of two
            rtex = new RenderTexture(cubemapSize, cubemapSize, 16);
            //set up the dimension as a cube texture, has to be that way for the rendertocubemap function
            rtex.dimension = UnityEngine.Rendering.TextureDimension.Cube;
            //also hide the flags from the inspector
            rtex.hideFlags = HideFlags.HideAndDontSave;
            //include only the "projector" layer, as defined in the inspector
            cam.cullingMask = 1 << 9;
            //set the render texture as the texture to be used by the shader (i.e. in the material in the object containint the script)
            GetComponent<Renderer>().sharedMaterial.SetTexture("_Cube", rtex);
            //allocate memory for the rotation vector to be sent to the shader
            //rot = new Vector4();
            //send the rotation info
            //GetComponent<Renderer>().sharedMaterial.SetVector("_Rotation", rot);
        }
        //update the position of the camera to the player
        cam.transform.position = playerTrans.position;

        //DONT DELETE, ALLOWS FOR ROTATION
        //rot = playerTrans.rotation.eulerAngles;
        //GetComponent<Renderer>().sharedMaterial.SetVector("_Rotation", rot);
        
        //send the player position information to the shader
        GetComponent<Renderer>().sharedMaterial.SetVector("_Player", new Vector4(playerTrans.position.x, playerTrans.position.y, playerTrans.position.z));
        //actually render the cubemap to the texture, all 6 faces
        cam.RenderToCubemap(rtex, faceMask);


    }

    void OnDisable()
    {
        //once done, destroy the cam and texture
        DestroyImmediate(cam);
        DestroyImmediate(rtex);
    }
}