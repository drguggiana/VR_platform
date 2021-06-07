using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class TargetController : MonoBehaviour {

    // Public variables for trial structure, and target/screen setup
    public Trajectories trj;
    public GameObject player;
    public Material screenMaterial;
    public Vector4 screenColor = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);     // white
    public Vector4 targetColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);     // black

    public bool trialDone = false;
    public bool inTrial = false;
    private bool startTrial = false;

    public int targetIndex = 0;    // Index of the child target to be tested
    public float speed = 0.5f;
    public float acceleration = 10f;
    public int trajectory = 0;
    public float start_delta_heading = 60f;
    public Vector3 scale = new Vector3(0.025f, 0f, 0.025f);

    // private variables for handling motion
    private bool atDestination = false;
    private Vector3 currentPosition;
    private Vector3 localUp;
    private Vector3 globalUp = Vector3.up;
    private Vector3 globalForward = Vector3.forward;
    private Transform originalTransform;

    private Transform targetObj;
    private UnityEngine.AI.NavMeshAgent agent;
    private Transform[] StartEnd;
    private GameObject[] StartWall;
    private GameObject[] StartFloor;
    private float sinePeriod;
    private float distanceRemaining;

    // Use this for initialization
    void Start()
    {
        // Set defauly screen material color
        screenMaterial.SetColor("_Color", screenColor);

        // get the navigation agent of the target (independent of target shape)
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Find allocated starting points for paths. This is sorted according to 
        // the position conventions established for DLC
        StartWall = HelperFunctions.FindObsWithTag("StartWall");
        StartFloor = HelperFunctions.FindObsWithTag("StartFloor");
    }

    // Update is called once per frame
    void Update () {

        if (inTrial & !startTrial)
        {
            // Wait until the mouse is facing the target starting point to begin the trial
            float angle = CheckPlayerTargetAngle();

            // If the mouse is sufficiently facing the target, begin the trial.
            if (angle <= start_delta_heading)
            {
                Debug.Log("Look to Start");
                targetObj.gameObject.SetActive(true);
                agent.isStopped = false;
                startTrial = true;
            }
        }

        if (inTrial & startTrial)
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
        Debug.Log("Setup Trial");

        // For this trial, find start and end points of the target trajectory
        StartEnd = SelectStartEndPoints(trajectory);

        // Set background appearance
        SetupBackgroundAppearance();

        // Set kinematic variables for target
        SetupAgentKinematics();

        // --- Set trajectory variables --- //
        SetupTrajectories(trajectory);

        // --- Set target appearance variables --- //
        SetupTargetAppearance(targetIndex, trajectory);

        inTrial = true;
        startTrial = false;
        trialDone = false;
    }

    void SetupBackgroundAppearance()
    {
        screenMaterial.SetColor("_Color", (Color)screenColor);
    }

    void SetupAgentKinematics()
    {
        // Move the agent to the starting position and set end point
        agent.Warp(StartEnd[0].position);
        agent.SetDestination(StartEnd[1].position);

        // Set speed and acceleration
        agent.speed = speed;
        agent.acceleration = acceleration;
        agent.updatePosition = true;
        agent.isStopped = true;

        // Get local normal vector based on agent and make the agent look there
        transform.LookAt(agent.destination, agent.transform.up);
        localUp = agent.transform.up;
    }

    void SetupTrajectories(int trajectory)
    {
        // If the agent is operating on a sinusoidal path, make acceleration 0 so we can manually control the motion
        if (trajectory == 1 || trajectory >= 3)
        {
            agent.acceleration = 0;
            agent.updatePosition = false;
        }

        // If we end up using a sinusoidal path, find the temporal period of the motion
        sinePeriod = 2.0f * (float)Math.PI / (Vector3.Distance(StartEnd[1].position, StartEnd[0].position) / speed);
    }

    void SetupTargetAppearance(int targetIdx, int trajectory)
    {
        // set target object active
        SetTargetActive(targetIdx);

        // Scale the target
        ScaleTarget(targetIdx);

        // Align target with the global up if moving along the wall
        if (trajectory < 2)
        {
            float angularOffset = HelperFunctions.AlignGlobalUp(agent.transform, localUp, globalUp);
            targetObj.transform.localEulerAngles = new Vector3(0f, 0f, angularOffset);
            int offsetSign = Math.Sign((double)angularOffset);
            float aligned = TestDirectionAlignment(agent.transform.forward, globalForward);
            int directionSign = Math.Sign((double)aligned);

            // Translate target so that it isn't being clipped by the wall or floor
            // Here the offset sign compensates for the rotation of the target depending on the direction of motion. 
            // The directionSign is positive if thedirection of motion is the same as global forward, and negative if opposite. 
            // This tells us to move the target + or - in the x-direction
            targetObj.transform.position = new Vector3(agent.transform.position.x + directionSign * offsetSign * (targetObj.transform.localScale.x / 2f) * (float)Math.Cos(angularOffset * Math.PI / 180f),
                                                       agent.transform.position.y + offsetSign * (targetObj.transform.localScale.y / 2f) * (float)Math.Sin(angularOffset * Math.PI / 180f),
                                                       agent.transform.position.z);
        }
        
        // Set target color
        if (targetIdx < 6)
        {
            // Geometric targets
            targetObj.GetComponent<Renderer>().material.SetColor("_Color", (Color)targetColor);
        }
        else
        {
            // This is a VR cricket, so it gets handled differently
            Transform current = targetObj.transform.Find("Body");
            current = current.Find("grille_geo");
            current.GetComponent<Renderer>().material.SetColor("_Color", (Color)targetColor);
        }

        // Set inactive until trial starts
        targetObj.gameObject.SetActive(false);
    }

    float TestDirectionAlignment(Vector3 currentDir, Vector3 testDir)
    {
        // +1 is perfectly aligned, 0 is perpendicular, -1 is perfectly opposite
        float dot = Vector2.Dot(new Vector2(currentDir.x, currentDir.z), new Vector2(testDir.x, testDir.z));
        return dot;
    }

    void ScaleTarget(int targetIdx)
    {
        // For 2D objects, scale x-axis to zero
        if (targetIndex <= 2)
        {
            if (targetIdx == 0)
            {
                // If we are passing an ellipse, we need to scale the y-axis by 0.5
                // This preserves a 2:1 major:minor axis ratio, with the major (z) axis 
                // being the length set by the scaling factor 
                targetObj.transform.localScale = (Vector3.Scale(scale, new Vector3(0f, 0.5f, 1f)));
            }
            else
            {
                targetObj.transform.localScale = (Vector3.Scale(scale, new Vector3(0f, 1f, 1f)));
            }
        }
        else
        {
            if (targetIdx == 3)
            {
                // If we are passing an ellipsoid, we need to scale the y-axis by 0.5
                // This preserves a 2:1 major:minor axis ratio, with the major (z) axis 
                // being the length set by the scaling factor 
                targetObj.transform.localScale = (Vector3.Scale(scale, new Vector3(0.5f, 0.5f, 1f)));
            }
            else
            {
                targetObj.transform.localScale = scale;
            }
        }
    }

    void SetTargetActive(int targetIdx)
    {
        // Activate selected target
        targetObj = transform.GetChild(targetIdx);
        targetObj.gameObject.SetActive(true);

        // Get original target position and rotation
        originalTransform = targetObj.transform;
    }

    void ResetAgentTargetTransform()
    {
        agent.Warp(new Vector3(0f, 0f, 0f));
        targetObj.transform.localEulerAngles = new Vector3(0f, 0f, 0f);
        targetObj.transform.position = agent.transform.position;
    }

    // -- Trial Control Flow Functions -- //   

    float CheckPlayerTargetAngle()
    {
        // Get the vector from the player to the target start point
        Vector3 dir = (StartEnd[0].position - player.transform.position).normalized;

        // Dot takes a value between [-1, 1]. 
        // 1 means parallel, facing same direction, 0 is facing 90 deg from target, -1 is facing away
        // Note here that the transform from Motive to Unity has the X direction of the tracker being forward, where Unity
        // uses Z as the forward direction
        float dot = Vector2.Dot(new Vector2(dir.x, dir.z), new Vector2(player.transform.right.x, player.transform.right.z));

        // Since the dot product is on normalized vectors, acos will get us the absolute angle between 
        // target start and player. Convert to degrees for convenience.
        float angle = (180f / (float)Math.PI) * (float)Math.Acos(dot);

        return angle;
        
    }

    void TrialEnd()
    {
        Debug.Log("End Trial");
        ResetAgentTargetTransform();

        targetObj.gameObject.SetActive(false);
        targetObj = null;

        trialDone = true;
        inTrial = false;
        startTrial = false;
        atDestination = false;
    }

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
                    agent.isStopped = true;
                }
            }
        }
    }

    Transform[] SelectStartEndPoints(int trajectory)
    {
        if (trajectory >= 2)
        {
            return FindStartEndPoints(StartFloor);
        }
        else
        {
            return FindStartEndPoints(StartWall);
        }
    }

    Transform[] FindStartEndPoints(GameObject[] StartEndPositions)
    {
        // Find the start and end points for motion. Adapted from 
        // https://answers.unity.com/questions/1236558/finding-nearest-game-object.html

        Transform[] StartEnd = new Transform[2];

        Transform StartPoint = null;
        Transform EndPoint = null;

        // Get current player position
        currentPosition = player.transform.position;

        //// Find the farthest corner from the player
        //foreach (GameObject start in StartPos)
        //{
        //    Transform start_loc = start.transform;
        //    Vector3 directionToTarget = start_loc.position - currentPosition;
        //    float dSqrToTarget = directionToTarget.sqrMagnitude;
        //    if (dSqrToTarget > closestDistanceSqr)
        //    {
        //        closestDistanceSqr = dSqrToTarget;
        //        StartPoint = start_loc;
        //    }
        //}

        // Find the closest corner to the player
        // Find the corner with the most similar z coordinate (search along short walls)
        foreach (GameObject start in StartEndPositions)
        {
            Transform start_loc = start.transform;
            float x_diff = Mathf.Abs(currentPosition.x - start_loc.position.x);
            float z_diff = Mathf.Abs(currentPosition.z - start_loc.position.z);
            if (x_diff > 0.2 && z_diff < 0.5)
            {
                StartPoint = start_loc;
            }
        }

        // Find the end point. This depends on if we are moving along the wall or along the arena diagonal
        if (trajectory >= 2)
        {   
            // Here we are moving along the floor. Look for opposite (farthest) corner.
            float closestDistanceSqr = 0.0f;
            foreach (GameObject end in StartEndPositions)
            {
                Transform end_loc = end.transform;
                float dSqrToTarget = Vector3.Distance(end_loc.position, StartPoint.position);
                if (dSqrToTarget > closestDistanceSqr)
                {
                    closestDistanceSqr = dSqrToTarget;
                    EndPoint = end_loc;
                }
            }
        }
        else
        {   
            // Here we are moving along the wall
            // Find the corner with the most similar x coordinate (since we only go along long walls)
            foreach (GameObject end in StartEndPositions)
                {
                    Transform end_loc = end.transform;
                    float x_diff = Mathf.Abs(StartPoint.position.x - end_loc.position.x);
                    float z_diff = Mathf.Abs(StartPoint.position.z - end_loc.position.z);
                    if (x_diff < 0.1 && z_diff > 0.5)
                    {
                        EndPoint = end_loc;
                    }
                }
        }

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


}
