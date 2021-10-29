using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[ExecuteInEditMode]
public class Recorder_AR_Cricket : RecorderBase
{

    // Variables for real cricket position
    public GameObject realCricket;
    private Vector3 realCricketPosition;
    
    // Variables for virtual cricket transforms and states
    private GameObject[] vrCricketObjs;
    GameObject vrCricketInstanceGameObject;
    private Vector3 vrCricketPosition;
    private Vector3 vrCricketOrientation;
    private int state;
    private float speed;
    private int encounter;
    private int motion;

    // Use this for initialization
    protected override void Start()
    {
        base.Start();

        // Get cricket object array sorted by name/number
        vrCricketObjs = HelperFunctions.FindObsWithTag("vrCricket");
        
        // Write header to file
        // There is always at least one real cricket in this scene
        AssembleHeader(1, vrCricketObjs.Length, false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        // Get mouse position from OptiTrack and update the tracking square color
        // Note that these functions are defined in the RecorderBase class
        // SetTrackingSqaure();
        // GetMousePosition();
        base.Update();
        
        // --- Handle mouse, real cricket, and/or VR Cricket data --- //
        
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
                               MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);
        
        realCricketPosition = GetRealCricketPosition();
        realCricket.transform.localPosition = realCricketPosition;
        object[] realCricketData = { realCricketPosition.x, realCricketPosition.y, realCricketPosition.z };
        string realCricketString = string.Join(", ", realCricketData);
        
        string vrCricketString = GetVRCricketData();

        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        string[] allData = { TimeStamp.ToString(), mouseString, realCricketString, vrCricketString, ColorFactor.ToString() };
        allData = allData.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Writer.WriteLine(string.Join(", ", allData));

    }


    // --- Functions for tracking objects in the scene --- //
    Vector3 GetRealCricketPosition()
    {
        Vector3 outputPosition;
        List<Vector3> nonlabeledMarkers = new List<Vector3>();
        List<OptitrackMarkerState> labeledMarkers = streamingClient.GetLatestMarkerStates();
        
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

    string GetVRCricketData()
    {
        string vrCricketString = "";
        
        foreach (GameObject vrCricketObj in vrCricketObjs)
        {
            // Get the VR cricket position and orientation
            vrCricketPosition = vrCricketObj.transform.position;
            vrCricketOrientation = vrCricketObj.transform.rotation.eulerAngles;

            // Get the VR cricket speed and current motion state
            speed = vrCricketObj.GetComponent<Animator>().GetFloat("speed"); ;
            state = vrCricketObj.GetComponent<Animator>().GetInteger("state_selector");
            motion = vrCricketObj.GetComponent<Animator>().GetInteger("motion_selector");
            encounter = vrCricketObj.GetComponent<Animator>().GetInteger("in_encounter");

            // Concatenate strings for this cricket object, and add to all cricket string
            object[] cricket_data = {
                vrCricketPosition.x, vrCricketPosition.y, vrCricketPosition.z,
                vrCricketOrientation.x, vrCricketOrientation.y, vrCricketOrientation.z,
                speed, state, motion, encounter 
            };

            string thisVRCricket = string.Join(", ", cricket_data);
            List<string> strArray = new List<string> { vrCricketString, thisVRCricket };

            // Remove leading comma
            vrCricketString = string.Join(", ", strArray.Where(s => !string.IsNullOrEmpty(s)));

        }

        return vrCricketString;
    }
    
}
