using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//[ExecuteInEditMode]
public class TargetController : MonoBehaviour {

    // Public variables for trial structure, and target/screen setup
    public GameObject player;
    public Material screenMaterial;
    public Vector4 screenColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);     // white
    public Vector4 targetColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);     // black

    public bool trialDone = false;
    public bool inTrial = false;

    public int targetIndex = 2;    // Index of the child target to be tested
    public float speed = 0.5f;
    public float acceleration = 0.1f;
    public int trajectory = 0;
    public Vector3 scale = new Vector3(0.025f, 0f, 0.025f);

    private bool atDestination = false;
    private Vector3 currentPosition;
    private Vector3 localUp;

    private Transform targetObj;
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform[] StartEnd;


    // Use this for initialization
    void Start()
    {
        // Set defauly screen material color
        screenMaterial.SetColor("_Color", screenColor);

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
                TrialEnd();
            }
        }
    }

    public void SetupNewTrial()
    {
        trialDone = false;
        inTrial = true;

        // --- Set VR screen appearance variables --- //
        screenMaterial.SetColor("_Color", (Color)screenColor);

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

        // --- Set target appearance variables --- //

        // set target object active and deactivate all other children
        SetTargetActive(targetIndex);

        // set target color
        targetObj.GetComponent<Renderer>().material.SetColor("_Color", (Color)targetColor);

        // Scale gets set depending on what object is being shown
        if (targetIndex == 0 & scale.x == scale.z)
        {
            // If we are passing an ellipse, we need to scale the z axis by 2
            targetObj.transform.localScale = (Vector3.Scale(scale, new Vector3(1f, 1f, 2f)));
        }
        else
        {
            // Otherwise, we just use the given scale
            targetObj.transform.localScale = scale;
        }
    }


    void TrialEnd()
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
