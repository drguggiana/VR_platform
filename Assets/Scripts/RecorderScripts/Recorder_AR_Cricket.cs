using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/*
 This class is a child of the RecorderBase class. It inherits most of its functionality from there.
 See RecorderBase for detailed explanation of what goes on under the hood.
*/


[ExecuteInEditMode]
public class Recorder_AR_Cricket : RecorderBase
{

    // Variables for real cricket position
    private GameObject[] _realCricketObjects;
    private GameObject _realCricket;
    private Vector3 _realCricketPosition;
    
    // Variables for virtual cricket transforms and states
    private GameObject[] _vrCricketObjects;
    // GameObject _vrCricketInstanceGameObject;
    private Vector3 _vrCricketPosition;
    private Vector3 _vrCricketOrientation;
    private int _state;
    private float _speed;
    private int _encounter;
    private int _motion;

    // Use this for initialization
    protected override void Start()
    {
        // -- Inherit setup from base class
        base.Start();

        // -------------------------------------------
        // Get VR cricket object array sorted by name/number
        _vrCricketObjects = HelperFunctions.FindObsWithTag("vrCricket");
        
        // Get the real cricket object - there will only be one per scene
        _realCricketObjects = HelperFunctions.FindObsWithTag("Cricket");
        _realCricket = _realCricketObjects[0];
        
        // Write header to file
        // There is always at least one real cricket in this scene
        AssembleHeader(_realCricketObjects.Length, _vrCricketObjects.Length, false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        // Update the tracking square color and get mouse position from OptiTrack
        // Note that these functions are defined in the RecorderBase class
        // SetTrackingSqaure();
        // GetMousePosition();
        base.Update();
        
        // --- Handle mouse, real cricket, and/or VR Cricket data --- //
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
                               MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);
        
        _realCricketPosition = GetRealCricketPosition();
        _realCricket.transform.localPosition = _realCricketPosition;
        object[] realCricketData = { _realCricketPosition.x, _realCricketPosition.y, _realCricketPosition.z };
        string realCricketString = string.Join(", ", realCricketData);
        
        string vrCricketString = GetVRCricketData();

        // --- Data Saving --- //
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
                distances[i] = Math.Abs(Vector3.Distance(nonlabeledMarkers[i], _realCricketPosition));
            }

            int minIndex = Array.IndexOf(distances, distances.Min());
            outputPosition = nonlabeledMarkers[minIndex];
        }
        else
        {
            // If there is no point found, reuse the current position
            outputPosition = _realCricket.transform.localPosition;
        }
        return outputPosition;
    }

    string GetVRCricketData()
    {
        string vrCricketString = "";
        
        foreach (GameObject vrCricketObj in _vrCricketObjects)
        {
            // Get the VR cricket position and orientation
            _vrCricketPosition = vrCricketObj.transform.position;
            _vrCricketOrientation = vrCricketObj.transform.rotation.eulerAngles;

            // Get the VR cricket speed and current motion state
            _speed = vrCricketObj.GetComponent<Animator>().GetFloat("speed"); ;
            _state = vrCricketObj.GetComponent<Animator>().GetInteger("state_selector");
            _motion = vrCricketObj.GetComponent<Animator>().GetInteger("motion_selector");
            _encounter = vrCricketObj.GetComponent<Animator>().GetInteger("in_encounter");

            // Concatenate strings for this cricket object, and add to all cricket string
            object[] cricket_data = {
                _vrCricketPosition.x, _vrCricketPosition.y, _vrCricketPosition.z,
                _vrCricketOrientation.x, _vrCricketOrientation.y, _vrCricketOrientation.z,
                _speed, _state, _motion, _encounter 
            };

            string thisVRCricket = string.Join(", ", cricket_data);
            List<string> strArray = new List<string> { vrCricketString, thisVRCricket };

            // Remove leading comma
            vrCricketString = string.Join(", ", strArray.Where(s => !string.IsNullOrEmpty(s)));

        }

        return vrCricketString;
    }
    
}
