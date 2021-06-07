using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class Trajectories : MonoBehaviour {

    public void Linear2D(Transform start, Transform end)
    {
        // This is handled by the NavMesh Agent. Just pass here
    }

    public void Sine2D(float time, Transform start, Transform end)
    {
        // Get distance between points. They should be at the same height, so this is really
        // a 2D lienar distance
        float dist = Vector3.Distance(end.position, start.position);

        // Sine function takes the form A*sin(B(x+C))+D. We have no translation, 
        // so we have A*sin(B(x)), where the period here is 2pi/B. If we want one 
        // period with the distance calculated above, then our sine function takes 
        // the form A*sin(2pi/dist (x)). Say we want to scale the period so that over 
        // the distance there are y periods, then this takes the form 
        // A*sin(y*2pi / dist (x))



        float slope = (end.position.y - start.position.y) / (end.position.x - start.position.x);
        double theta = Math.Atan(slope);
        double c = Math.Cos(theta);
        double s = Math.Sin(theta);
    }

    // --- For movement in 3D --- //


    public void Linear3D(Transform start, Transform end)
    {
        // This is also handled by the NavMesh Agent. Just pass here
    }

    public void SineVert3D(Transform start, Transform end, float height)
    {

    }

    public void SineHorz3D(Transform start, Transform end, float height)
    {

    }


    // -- Helper Functions -- //
    Transform[] StartEndPoints(Vector3 currentPosition, GameObject[] StartEndPositions)
    {
        // Find the start and end points for motion. Adapted from 
        // https://answers.unity.com/questions/1236558/finding-nearest-game-object.html

        Transform[] StartEnd = new Transform[2];

        Transform StartPoint = null;
        Transform EndPoint = null;
        float closestDistanceSqr = 0.0f;

        // Get current player position
        // currentPosition = player.transform.position;
        //currentPosition = new Vector3(-0.5f, 0.0f, -0.5f);

        // Find the farthest corner from the player
        foreach (GameObject start in StartEndPositions)
        {
            Transform start_loc = start.transform;
            Vector3 directionToTarget = start_loc.position - currentPosition;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget > closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                StartPoint = start_loc;
            }
        }

        // Find the end point by getting the corner with the most similar
        // x coordinate (since we only go along long walls)
        foreach (GameObject start in StartEndPositions)
        {
            Transform start_loc = start.transform;
            float x_diff = Mathf.Abs(StartPoint.transform.position.x - start_loc.position.x);
            float z_diff = Mathf.Abs(StartPoint.transform.position.z - start_loc.position.z);
            if (x_diff < 0.1 && z_diff > 0.5)
            {
                EndPoint = start_loc;
            }
        }

        // Need to shift the transform to compensate for radius, plus whatever
        // vertical distance we want
        //StartPoint.transform.position = StartPoint.transform.position + new Vector3(0, size, size);
        //EndPoint.transform.position = EndPoint.transform.position + new Vector3(0, size, size);

        //Debug.Log(StartPoint.transform.position);
        //Debug.Log(EndPoint.transform.position);

        // Build and return array
        StartEnd[0] = StartPoint;
        StartEnd[1] = EndPoint;

        return StartEnd;
    }

}
