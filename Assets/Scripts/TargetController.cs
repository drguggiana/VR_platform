using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//[ExecuteInEditMode]
public class TargetController : MonoBehaviour {

    public GameObject player;

    // Variables for testing properties that are manipulated
    public int targetIndex = 2;    // Index of the child target to be tested
    public float speed = 0.5f;
    public float acceleration = 0.1f;
    public float contrast = 1.0f;     // [0-1] 1 is full contrast and 0 is no contrast
    public float scale = 0.025f;     // This is proprtional to radius of a circle
    public int trajectory = 0;

    public bool trialDone = false;
    public bool inTrial = false;

    //public GameObject screen;
    public Material screen_material;

    private bool atDestination = false;
    private Vector3 currentPosition;
    private Vector3 localUp;

    private Transform targetObj;
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform[] StartEnd;

    private Color target_color;
    private Color screen_color;

    // Use this for initialization
    void Start()
    {
        // Get the values for the color of the screens
        //Renderer rend;
        //rend = screen.GetComponent<Renderer>();
        screen_color = screen_material.GetColor("_Color");

        // get the navigation agent of the target (independent of target shape)
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
    }

    // Update is called once per frame
    void Update () {

        if (inTrial == true)
        {
            // Update position
            transform.position = agent.nextPosition;

            // Check if at destination
            CheckDestinationReach();

            // If at destination, handle trial end parameters
            if (atDestination)
            {
                AtTrialEnd();
            }
        }
    }

    public void SetupNewTrial()
    {  
        // Set booleans
        inTrial = true;
        trialDone = false;

        // set target object active and deactivate all other children
        SetTargetActive(targetIndex);

        // --- Set target appearance variables --- //
        target_color = CalculateTargetColor();
        targetObj.GetComponent<Renderer>().material.SetColor("_Color", target_color);

        // Scale gets set depending on what object is being shown
        if (targetIndex == 0)
        {
            targetObj.transform.localScale = new Vector3(scale, 0.0f, 2.0f * scale);
        }
        else
        {
            targetObj.transform.localScale = new Vector3(scale, 0.0f, scale);
        }

        // --- set NavMeshAgent kinematic variables --- //

        // For this trial, find start and end points of the stimulus
        StartEnd = StartEndPoints();
        // Move the agent to the starting position and set end point
        agent.Warp(StartEnd[0].position);
        agent.SetDestination(StartEnd[1].position);
        // Get local normal vector based on agent and make the agent look there
        localUp = agent.transform.up;
        transform.LookAt(agent.destination, localUp);
        // Set speed and acceleration
        agent.speed = speed;
        agent.acceleration = acceleration;
        
        Debug.Log("Trial Start");
    }

    Color CalculateTargetColor()
    {
        // Assumes a typical target is pure black, and on a light background. 

        // Get dynamic range. Alpha component should be 0 after subtraction.
        Color screen_target_diff = screen_color - Color.black;    
        // Multiply the difference by the contrast scaling factor 
        screen_target_diff = screen_target_diff * contrast;
        // Target material color is the screen color minus the scaled difference
        Color color = screen_color - screen_target_diff;
        return color;
    }

    void AtTrialEnd()
    {
        // What to do when a trial is over
        trialDone = true;
        inTrial = false;
        atDestination = false;
        targetObj.gameObject.SetActive(false);
        targetObj = null;
    }

    void SetTargetActive(int targetIdx)
    {
        // Activate selected target
        targetObj = transform.GetChild(targetIdx);
        targetObj.gameObject.SetActive(true);
    }

    void CheckDestinationReach()
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

    Transform[] StartEndPoints()
    {
        // Find the start and end points for motion. Adapted from 
        // https://answers.unity.com/questions/1236558/finding-nearest-game-object.html

        // Find allocated starting poitns for paths. This is sorted according to 
        // the position conventions established for DLC
        GameObject[] StartPos = FindObsWithTag("Starts");

        Transform[] StartEnd = new Transform[2];

        Transform StartPoint = null;
        Transform EndPoint = null;
        float closestDistanceSqr = 0.0f;

        // Get current player position
        currentPosition = player.transform.position;
        //currentPosition = new Vector3(-0.5f, 0.0f, -0.5f);

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

    // -- Helper Functions -- //
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
