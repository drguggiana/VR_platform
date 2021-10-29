using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;


[ExecuteInEditMode]
public class DemoARCricket : DemoRecorderBase
{

    // Variables for real cricket position
    public GameObject realCricket;
    private Vector3 realCricketPosition;
    
    // Start is called before the first frame update
    // void Start()
    // {
    //    // Notice here that Start is called from the DemoRecorderBase class, and since we do not need to modify it in 
    //    // any way, it doesn't even need to be explicitly called in this class
    //     base.Start();
    // }
    
    
    // Update is called once per frame
    protected override void Update()
    {
        // Notice here that we are doing more with the Update function than is present in the DemoRecorderBase class, 
        // therefore we must override the DemoRecorderBase.Update() function, call it explicitly to update the tracking 
        // square and get the current mouse position, but we also add functionality to track a cricket in the scene.
        base.Update();
        realCricketPosition = GetRealCricketPosition();
        realCricket.transform.localPosition = realCricketPosition;
    }
    
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
}