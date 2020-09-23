using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class Trajectories : MonoBehaviour {

    // Public variables for properties that are manipulated
    public static string targetType = "Square";
    public static float speed = 0.5f;
    public static float acceleration = 0.1f;
    public static float contrast = 0.0f;     // [0-1]
    public static Vector3 size = new Vector3(0.025f, 0.0f, 0.025f);    // This is proprtional to radius of a circle
    public static bool trialDone = false;

    private Transform player;
    private Transform targetObj;
    private UnityEngine.AI.NavMeshAgent agent;
    private GameObject[] StartPos;
    private Transform[] StartEnd;

    private Color target_color;
    private bool atDestination = false;

    // Use this for initialization
    void OnEnable() {

        // get the transform of the player
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // get the navigation agent of the target (independent of shape)
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // set target object active and deactivate all other children
        targetObj = SetTargetActive(targetType);

        // --- set target appearance variables --- //
        target_color = new Color(contrast, contrast, contrast, 1f);
        targetObj.GetComponent<Renderer>().material.SetColor("_Color", target_color);
        targetObj.transform.localScale = size;

        // --- set NavMeshAgent kinematic variables --- //
       
        // For this trial, find start and end points of the stimulus
        StartEnd = StartEndPoints();
        // Move the agent to the starting position
        agent.Warp(StartEnd[0].position);
        // Orient the target shape correctly
        targetObj.up = Vector3.up;
        // Set the destination for the agent
        agent.SetDestination(StartEnd[1].position);
        // Set speed and acceleration
        agent.speed = speed;
        agent.acceleration = acceleration;

    }

    // Update is called once per frame
    void Update () {
        // Update position
        transform.position = agent.nextPosition;
        // Check if at destination
        CheckDestReach();
        // If at destination, change trial end flag to true and disable target
        if (atDestination == true)
        {
            trialDone = true;
            targetObj.gameObject.SetActive(false);
        }
        

    }


    void CheckDestReach()
    {
        // Check if we've reached the destination
        if (!agent.pathPending)
        {
            if (agent.remainingDistance <= agent.stoppingDistance)
            {
                if (!agent.hasPath || agent.velocity.sqrMagnitude == 0f)
                {
                    // Done
                    atDestination = true;
                }
            }
        }
    }

    // Set target shape active and deactivate all others
    Transform SetTargetActive(string targetType)
    {
        Transform thisTarget = null;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == targetType)
            {
                child.gameObject.SetActive(true);
                thisTarget = child;
            }
        }

        return thisTarget;
    }

    // Find the start and end points for motion. Adapted from 
    // https://answers.unity.com/questions/1236558/finding-nearest-game-object.html
    Transform[] StartEndPoints()
    {
        // Find allocated starting poitns for paths. This is sorted according to 
        // the position conventions established for DLC
        StartPos = FindObsWithTag("Starts");

        Transform[] StartEnd = new Transform[2];

        Vector3 currentPosition = player.position;
        Transform StartPoint = null;
        Transform EndPoint = null;
        float closestDistanceSqr = 0.0f;
        
        // Find the farthest corner from the player
        foreach (GameObject start in StartPos)
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
        foreach (GameObject start in StartPos)
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

    GameObject[] FindObsWithTag(string tag)
    {
        GameObject[] foundObs = GameObject.FindGameObjectsWithTag(tag);
        Array.Sort(foundObs, CompareObNames);
        return foundObs;
    }

    int CompareObNames(GameObject x, GameObject y)
    {
        return x.name.CompareTo(y.name);
    }


}
