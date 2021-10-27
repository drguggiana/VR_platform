using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class DemoRecorderBase : MonoBehaviour
{
    // Streaming client
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    
    // Variables for tracking square
    public GameObject trackingSquare;
    private Material trackingSqaureMaterial;
    private float color_factor = 0.0f;

    // Variables for mouse position
    public GameObject Mouse;
    private Vector3 mousePosition;
    private Vector3 mouseOrientation;
    
    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float timeStamp;
    
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        reference = OptitrackHiResTimer.Now();

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;

        // Get the tracking square material
        trackingSqaureMaterial = trackingSquare.GetComponent<Renderer>().sharedMaterial;

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        SetTrackingSqaure();
        GetMousePosition();
    }
    
    public void SetTrackingSqaure()
    {
        // create the color for the square
        Color new_color = new Color(color_factor, color_factor, color_factor, 1f);
        // put it on the square 
        trackingSqaureMaterial.SetColor("_Color", new_color);
        // Define the color for the next iteration (switch it)
        if (color_factor > 0.0f)
        {
            color_factor = 0.0f;
        }
        else
        {
            color_factor = 1.0f;
        }
    }
    
    public void GetMousePosition()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {
            // get the position of the mouse Rigid Body
            mousePosition = rbState.Pose.Position;
            // change the transform position of the game object
            this.transform.localPosition = mousePosition;
            // also change its rotation
            this.transform.localRotation = rbState.Pose.Orientation;
            // turn the angles into Euler (for later printing)
            mouseOrientation = this.transform.eulerAngles;
            // get the timestamp 
            timeStamp = rbState.DeliveryTimestamp.SecondsSince(reference);
        }
        else
        {
            mousePosition = Mouse.transform.position;
            mouseOrientation = Mouse.transform.rotation.eulerAngles;
        }
    }
}
