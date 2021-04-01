using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
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
    public float start_delta_heading = 30f;
    public Vector3 scale = new Vector3(0.025f, 0f, 0.025f);

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

    // Use this for initialization
    void Start()
    {
        // Set defauly screen material color
        screenMaterial.SetColor("_Color", screenColor);

        // get the navigation agent of the target (independent of target shape)
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Find allocated starting points for paths. This is sorted according to 
        // the position conventions established for DLC
        StartPosWall = FindObsWithTag("StartWall");
        StartPosFloor = FindObsWithTag("StartFloor");
    }

    // Update is called once per frame
    void Update () {

        if (inTrial & !startTrial)
        {
            // Wait until the mouse is facing the target starting point to begin the trial
            CheckPlayerTargetAngle();
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
        inTrial = true;
        startTrial = false;
        trialDone = false;

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

        // Get local normal vector based on agent and make the agent look there
        localUp = agent.transform.up;
        transform.LookAt(agent.destination, localUp);

        // Set speed and acceleration
        agent.speed = speed;
        agent.acceleration = acceleration;
        agent.updatePosition = true;
        agent.isStopped = true;
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

    float PosNegAngle(Vector3 a1, Vector3 a2, Vector3 normal)
    {
        float angle = Vector3.Angle(a1, a2);
        float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(a1, a2)));
        return angle * sign;
    }

    RaycastHit FindSurfaceNormal()
    {
        Vector3 theRay = -Vector3.up;
        RaycastHit hit;

        Physics.Raycast(agent.transform.position, theRay, out hit, 1f);
        return hit;
        // return hit.normal;

    }

    void SetupTargetAppearance(int targetIdx, int trajectory)
    {
        // set target object active
        SetTargetActive(targetIdx);

        targetObj.transform.LookAt(agent.destination, localUp);

        if (targetIdx < 6)
        {
            // set target color
            targetObj.GetComponent<Renderer>().material.SetColor("_Color", (Color)targetColor);

            // Scale gets set depending on what object is being shown
            if ((targetIndex == 0 || targetIndex == 3) & (scale.y == scale.z))
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

            //// For all targets, we want the lowest part of the target to scrape the floor, or be just above it
            //targetObj.transform.position = new Vector3(targetObj.transform.position.x,
            //                                           targetObj.transform.position.y + (targetObj.transform.localScale.y / 2f) + 0.005f,
            //                                           targetObj.transform.position.z);

            // If target is 2D and moving along a wall, rotate it so that it is aligned with global up
            if (targetIdx < 3 & trajectory < 2)
            {
                RaycastHit hit = FindSurfaceNormal();
                Debug.Log(hit.normal);
                float zRot = PosNegAngle(hit.normal, agent.transform.up, agent.transform.forward);
                Debug.Log(zRot);
                targetObj.transform.localEulerAngles = new Vector3(0f, 0f, zRot);
                //targetObj.transform.up = Vector3.up;
            }


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

    private void Align()
    {
        Vector3 theRay = -transform.up;
        RaycastHit hit;

        if (Physics.Raycast(new Vector3(transform.position.x, transform.position.y, transform.position.z),
            theRay, out hit, 2f))
        {

            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.parent.rotation;

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

        // If the mouse is sufficiently facing the target, begin the trial.
        if (angle <= start_delta_heading)
        {
            targetObj.gameObject.SetActive(true);
            agent.isStopped = false;
            startTrial = true;
        }
    }

    void TrialEnd()
    {
        // What to do when a trial is over
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
