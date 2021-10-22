using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;


[ExecuteInEditMode]
public class DemoARCricket : MonoBehaviour
{
    
    // Streaming client
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    
    // Variables for mouse position
    public GameObject mouseObj;
    private Vector3 mousePosition;
    private Vector3 mouseOrientation;
    
    // Variables for real cricket position
    public GameObject realCricket;
    private Vector3 realCricketPosition;
    
    // Variables for tracking square
    public GameObject trackingSquare;
    private Material trackingSqaureMaterial;
    private float color_factor = 0.0f;
    
    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float timeStamp;
    
    
    // Start is called before the first frame update
    void Start()
    {
        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        reference = OptitrackHiResTimer.Now();
        
        // Get the tracking square material
        trackingSqaureMaterial = trackingSquare.GetComponent<Renderer>().material;

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;
    }

    // Update is called once per frame
    void Update()
    {
        SetTrackingSqaure();
        // --- Handle mouse,  real cricket, and/or VR Cricket data --- //
        GetMousePosition();
        realCricketPosition = GetRealCricketPosition();

    }
    
    // --- Functions for tracking objects in the scene --- //
    void SetTrackingSqaure()
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
    
    void GetMousePosition()
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
            mousePosition = mouseObj.transform.position;
            mouseOrientation = mouseObj.transform.rotation.eulerAngles;
        }
    }
    
    Vector3 GetRealCricketPosition()
    {
        Vector3 outputPosition;
        List<Vector3> nonlabeledMarkers = new List<Vector3>();
        List<OptitrackMarkerState> labeledMarkers = StreamingClient.GetLatestMarkerStates();
        
        foreach (OptitrackMarkerState marker in labeledMarkers)
        {
            // Only get markers that are not labeled as part of a RigidBody
            if (marker.Labeled == false)
            {
                nonlabeledMarkers.Add(marker.Position);
                //Debug.Log (marker.Position);
            }
        }

        // Check how many unlabeled markers there are. If there is more than one, find the one closest to the previous
        // position and use that as the cricket position. If there are none, use the last cricket position.
        if (nonlabeledMarkers.Count == 1)
        {
            // If there's just a single unlabeled marker, this is our cricket
            outputPosition = nonlabeledMarkers[0];
        }
        else if (nonlabeledMarkers.Count > 1)
        {
            // If there is more than one point detected, use the one that's closest to the previous position
            float[] distances = new float[nonlabeledMarkers.Count];

            for (int i = 0; i < nonlabeledMarkers.Count; i++)
            {
                distances[i] = Math.Abs(Vector3.Distance(nonlabeledMarkers[i], realCricketPosition));
            }

            int minIndex = Array.IndexOf(distances, distances.Min());
            outputPosition = nonlabeledMarkers[minIndex];
        }
        else
        {
            // If there is no point found, reuse the current position
            outputPosition = realCricket.transform.localPosition;
        }
        return outputPosition;
    }
}