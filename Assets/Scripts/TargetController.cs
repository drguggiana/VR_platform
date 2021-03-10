﻿using System.Collections;
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
    private bool startTrial = false;

    public int targetIndex = 2;    // Index of the child target to be tested
    public float speed = 0.5f;
    public float acceleration = 0.1f;
    public int trajectory = 0;
    public int start_delta_heading = 45;
    public Vector3 scale = new Vector3(0.025f, 0f, 0.025f);

    // Public variables for the NavMesh Groups to be manipulated
    // public GameObject VScreens;
    // public GameObject NavMeshes_VScreen;

    // private variables for handling motion
    private bool atDestination = false;
    private Vector3 currentPosition;
    private Vector3 localUp;
    private Vector3 globalUp = Vector3.up;

    private Transform targetObj;
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform[] StartEnd;
    private GameObject[] StartPosWall;
    private GameObject[] StartPosFloor;
    private float sinePeriod;
    private float distanceRemaining;

    private int counter = 0;

    // Use this for initialization
    void Start()
    {
        // Set defauly screen material color
        screenMaterial.SetColor("_Color", screenColor);

        // get the navigation agent of the target (independent of target shape)
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Find allocated starting poitns for paths. This is sorted according to 
        // the position conventions established for DLC
        StartPosWall = FindObsWithTag("StartWall");
        StartPosFloor = FindObsWithTag("StartFloor");
    }

    // Update is called once per frame
    void Update () {

        // For debugging only
        #if (UNITY_EDITOR)
        counter++;
        if ((counter % 500 == 0))
        {
            SetupNewTrial();
        }
        #endif

        if (!startTrial)
        {
            // Wait until the mouse is facing the target starting point to begin the trial
            CheckPlayerTargetAngle();
        }

        if (inTrial)
        {

            // Check if at destination
            CheckDestinationReach();

            // If at destination, handle trial end parameters
            if (atDestination)
            {
                TrialEnd();
            }

            // Switch structure for trajectories
            switch (trajectory)
            {
                case 0:
                    // Linear motion along wall. Handled by NavMeshAgent, so just pass
                    break;
                case 1:
                    // Sinusoidal motion along wall
                    SineWall();
                    break;
                case 2:
                    // Linear motion along arena diagonal. Handled by NavMeshAgent, so just pass
                    break;
                case 3:
                    // Horizontal sinusoidal motion along arena diagonal
                    SineHorzDiag();
                    break;
                case 4:
                    // Vertical sinusoidal motion along arena diagonal
                    SineVertDiag();
                    break;
                default:
                    break;
            }

            // Update position
            transform.position = agent.nextPosition;
            
        }
    }

    // -- Trial Setup Functions -- //
    public void SetupNewTrial()
    {

        trialDone = false;
        startTrial = false;

        // For this trial, find start and end points of the target trajectory
        StartEnd = SelectStartEndPoints(trajectory);

        // Set background appearance
        SetupBackgroundAppearance();

        // Set kinematic variables for target
        SetupTargetKinematics();
        
        // --- Set trajectory variables --- //
        // If the agent is operating on a sinusoidal path, make acceleration 0 so we can manually control the motion
        if (trajectory == 1 || trajectory >= 3)
        {
            agent.acceleration = 0;
            agent.updatePosition = false;
        }

        // If we end up using a sinusoidal path, find the temporal period of the motion
        sinePeriod = 2.0f * (float)Math.PI / (Vector3.Distance(StartEnd[1].position, StartEnd[0].position) / speed);

        // --- Set target appearance variables --- //
        SetupTargetAppearance(targetIndex);

    }

    void SetupBackgroundAppearance()
    {
        screenMaterial.SetColor("_Color", (Color)screenColor);
    }

    void SetupTargetKinematics()
    {
        // Move the agent to the starting position and set end point
        agent.Warp(StartEnd[0].position);
        agent.SetDestination(StartEnd[1].position);

        // Get local normal vector based on agent and make the agent look there
        localUp = agent.transform.up;
        transform.LookAt(agent.destination, globalUp);

        // Set speed and acceleration
        agent.speed = speed;
        agent.acceleration = acceleration;
        agent.updatePosition = true;
    }

    void SetupTargetAppearance(int target_index)
    {
        // set target object active and deactivate all other children
        SetTargetActive(target_index);

        // set target color
        try
        {
            targetObj.GetComponent<Renderer>().material.SetColor("_Color", (Color)targetColor);
        }
        catch
        {
            // Handle the coloration of a VR cricket
            Transform current = targetObj.transform.Find("Body");
            current = current.Find("grille_geo");
            current.GetComponent<Renderer>().material.SetColor("_Color", (Color)targetColor);
        }

        // Scale gets set depending on what object is being shown
        if ((targetIndex == 0 || targetIndex == 3) & (scale.x == scale.z))
        {
            // If we are passing an ellipse or ellipsoid, we need to scale the x and y axes by 0.5
            // This preserves a 2:1 major:minor axis ratio, with the major (z) axis being the length set by the scaling factor 
            targetObj.transform.localScale = (Vector3.Scale(scale, new Vector3(0.5f, 0.5f, 1f)));
        }
        else
        {
            // Otherwise, we just use the given scale
            targetObj.transform.localScale = scale;
        }
    }

    void SetTargetActive(int targetIdx)
    {
        // Activate selected target
        targetObj = transform.GetChild(targetIdx);
        targetObj.gameObject.SetActive(true);
    }

    // -- Trial Control Flow Functions -- //

    void CheckPlayerTargetAngle()
    {
        // Get the angle between the target starting position and the current player position

        Vector3 dir = (StartEnd[0].position - player.transform.position).normalized;

        // Dot takes a value between [-1, 1]. 
        // 1 means parallel, facing same direction, 0 is facing 90 deg from target, -1 is facing away
        float dot = Vector3.Dot(dir, transform.forward);

        // Since the dot product is on normalized vectors, acos will get us the absolute angle between 
        // target start and player. Convert to degrees for convenience.
        float angle = (180f / (float)Math.PI) * (float)Math.Acos(dot);

        // If the mouse is sufficiently facing the target, begin the trial.
        if (angle <= start_delta_heading)
        {
            startTrial = true;
        }
    }

    void TrialEnd()
    {
        // What to do when a trial is over
        trialDone = true;
        inTrial = false;
        startTrial = false;
        atDestination = false;
        targetObj.gameObject.SetActive(false);
        targetObj = null;
    }

    //void SetNavMeshActive(int trajectory_type)
    //{
    //    if (trajectory_type < 2)
    //    {
    //        // VScreens.SetActive(true);
    //        NavMeshes_VScreen.SetActive(false);
    //    }
    //    else
    //    {
    //        // VScreens.SetActive(false);
    //        NavMeshes_VScreen.SetActive(true);
    //    }
    //}

    // -- Pathfinding functions -- //

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

    Transform[] SelectStartEndPoints(int trajectory)
    {
        if (trajectory >= 2)
        {
            return FindStartEndPoints(StartPosFloor);
        }
        else
        {
            return FindStartEndPoints(StartPosWall);
        }
    }

    Transform[] FindStartEndPoints(GameObject[] StartPos)
    {
        // Find the start and end points for motion. Adapted from 
        // https://answers.unity.com/questions/1236558/finding-nearest-game-object.html

        Transform[] StartEnd = new Transform[2];

        Transform StartPoint = null;
        Transform EndPoint = null;
        float closestDistanceSqr = 0.0f;

        // Get current player position
        currentPosition = player.transform.position;

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

        // Find the end point. This depends on if we are moving along the wall or along the arena diagonal
        if (trajectory >= 2)

        {   // Here we are moving along the floor. Look for opposite corner.
            closestDistanceSqr = 0.0f;
            foreach (GameObject start in StartPos)
            {
                Transform start_loc = start.transform;
                float dSqrToTarget = Vector3.Distance(start_loc.position, StartPoint.position);
                if (dSqrToTarget > closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    EndPoint = start_loc;
                }
            }
        }
        else
        {   // Here we are moving along the wall
            // Find the corner with the most similar x coordinate (since we only go along long walls)
            foreach (GameObject start in StartPos)
                {
                    Transform start_loc = start.transform;
                    float x_diff = Mathf.Abs(StartPoint.position.x - start_loc.position.x);
                    float z_diff = Mathf.Abs(StartPoint.position.z - start_loc.position.z);
                    if (x_diff < 0.1 && z_diff > 0.5)
                    {
                        EndPoint = start_loc;
                    }
                }
        }

        // Need to shift the transform to compensate for radius, plus whatever
        // vertical distance we want
        //StartPoint.transform.position = StartPoint.transform.position + new Vector3(0, size, size);
        //EndPoint.transform.position = EndPoint.transform.position + new Vector3(0, size, size);

        // Build and return array
        StartEnd[0] = StartPoint;
        StartEnd[1] = EndPoint;

        return StartEnd;
    }
    
    void SineWall(int frequency=3, float amplitude=0.002f)
    {
        // Get current position
        Vector3 pos = agent.transform.position;

        // Move the 
        pos += agent.transform.forward * Time.deltaTime * speed;
        pos -= agent.transform.right * Mathf.Sin(Time.time * frequency * sinePeriod) * amplitude;

        // Check that we aren't overshooting the end point
        distanceRemaining = StartEnd[1].position.z - pos.z;
        if (distanceRemaining < 0.003)
        {
            // Done
            atDestination = true;
        }
        else
        {
            // assign this as the agent's next position
            agent.nextPosition = pos;
        }

    }

    void SineHorzDiag(int frequency = 3, float amplitude = 0.003f)
    {
        // Get current position
        Vector3 pos = agent.transform.position;

        // Move the 
        pos += agent.transform.forward * Time.deltaTime * speed;
        pos += agent.transform.right * Mathf.Sin(Time.time * frequency * sinePeriod) * amplitude;

        // Check that we aren't overshooting the end point
        Vector3 diff = StartEnd[1].position - pos;
        distanceRemaining = (float)Math.Sqrt((diff.x*diff.x) + (diff.z*diff.z));

        if (distanceRemaining < 0.001)
        {
            // Done
            atDestination = true;
        }
        else
        {
            // assign this as the agent's next position
            agent.nextPosition = pos;
        }
    }

    void SineVertDiag(int frequency = 3, float amplitude = 0.003f)
    {
        // Get current position
        Vector3 pos = agent.transform.position;

        // Move the 
        pos += agent.transform.forward * Time.deltaTime * speed;
        pos += agent.transform.right * Mathf.Sin(Time.time * frequency * sinePeriod) * amplitude;

        // Check that we aren't overshooting the end point
        Vector3 diff = StartEnd[1].position - pos;
        distanceRemaining = (float)Math.Sqrt((diff.x * diff.x) + (diff.z * diff.z));

        if (distanceRemaining < 0.001)
        {
            // Done
            atDestination = true;
        }
        else
        {
            // assign this as the agent's next position
            agent.nextPosition = pos;
        }
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
