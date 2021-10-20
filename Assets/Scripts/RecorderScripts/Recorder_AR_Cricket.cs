using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

[ExecuteInEditMode]
public class Recorder_AR_Cricket : MonoBehaviour
{

    // Streaming client
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    
    // OSC communication client
    public OSC osc;

    // Variables for tracking square
    public GameObject trackingSquare;
    private Material trackingSqaureMaterial;
    private float color_factor = 0.0f;

    // Variables for mouse position
    public GameObject Mouse;
    private Vector3 mousePosition;
    private Vector3 mouseOrientation;

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

    // Variables for target object transforms and states
    //public GameObject TargetObj;
    //public TargetController targetController;
    //private Vector3 Target_Position;
    
    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float timeStamp;
    
    // Booleans for trial state
    // private bool trialDone = true;
    // private bool inTrial = false;

    // Variables for properties of the target that are manipulated
    // private int trial_num = 0;
    // private int shape;
    // private Vector3 scale;
    // private Vector4 screen_color;
    // private Vector4 target_color;
    // private float speed;
    // private float acceleration;
    // private int trajectory;

    // Writer for saving data
    private StreamWriter writer;
    private string mouseString;
    private string realCricketString;
    private string vrCricketString;
    private string targetString;

    // For debugging
    private int counter = 0;


    // Use this for initialization
    void Start()
    {
        // Set the writer
        Paths.CheckFileExistence(Paths.recording_path);
        writer = new StreamWriter(Paths.recording_path, true);
        
        // Force full screen on projector
        Screen.fullScreen = true;
        
        // set the OSC communication
        //osc.SetAddressHandler("/TrialStart", OnReceiveTrialStart);
        osc.SetAddressHandler("/Close", OnReceiveStop);

        // Get the reference timer
        reference = OptitrackHiResTimer.Now();

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;

        // Get cricket object array sorted by name/number
        vrCricketObjs = HelperFunctions.FindObsWithTag("vrCricket");
        
        // Get the tracking square material
        trackingSqaureMaterial = trackingSquare.GetComponent<Renderer>().sharedMaterial;
        
        // Write initial parameters and header to file
        LogSceneParams();
        AssembleHeader();
    }

    // Update is called once per frame
    void Update()
    {
        // For debugging only
//#if (UNITY_EDITOR)
//        counter++;

//        if ((counter % 240 == 0) & trialDone)
//        {
//            targetController.SetupNewTrial();
//            inTrial = targetController.inTrial;
//            trialDone = targetController.trialDone;
//        }
//#endif

        /* 
         * Below this point, all updates are done on every frame. 
         * They are recorded regardless of if we are in a trial or not.
         */

        // --- Handle the tracking square --- //
        SetTrackingSqaure();
        
        // --- Handle mouse,  real cricket, and/or VR Cricket data --- //

        GetMousePosition();
        object[] mouseData = { mousePosition.x, mousePosition.y, mousePosition.z, 
                               mouseOrientation.x, mouseOrientation.y, mouseOrientation.z };
        mouseString = string.Join(", ", mouseData);
        
        realCricketPosition = GetRealCricketPosition();
        realCricket.transform.localPosition = realCricketPosition;
        object[] realCricketData = { realCricketPosition.x, realCricketPosition.y, realCricketPosition.z };
        realCricketString = string.Join(", ", realCricketData);
        
        vrCricketString = GetVRCricketData();

        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        object[] all_data = { timeStamp, mouseString, realCricketString, vrCricketString, color_factor };
        writer.WriteLine(string.Join(", ", all_data));

    }


    // --- Functions for tracking objects in the scene --- //
    void SetTrackingSqaure()
    {
        // create the color for the square
        Color new_color = new Color(color_factor, color_factor, color_factor, 1f);
        // put it on the square 
        trackingSqaureMaterial.SetColor("_Color", new_color);
        // Define the color for the next iteration (switch it)
        if (color_factor > 0.0f)
        {
            color_factor = 0.0f;
        }
        else
        {
            color_factor = 1.0f;
        }
    }
    
    void GetMousePosition()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {
            // get the position of the mouse Rigid Body
            mousePosition = rbState.Pose.Position;
            // change the transform position of the game object
            this.transform.localPosition = mousePosition;
            // also change its rotation
            this.transform.localRotation = rbState.Pose.Orientation;
            // turn the angles into Euler (for later printing)
            mouseOrientation = this.transform.eulerAngles;
            // get the timestamp 
            timeStamp = rbState.DeliveryTimestamp.SecondsSince(reference);
        }
        else
        {
            mousePosition = Mouse.transform.position;
            mouseOrientation = Mouse.transform.rotation.eulerAngles;
        }
    }
    
    Vector3 GetRealCricketPosition()
    {
        Vector3 outputPosition;
        List<Vector3> nonlabeledMarkers = new List<Vector3>();
        List<OptitrackMarkerState> labeledMarkers = StreamingClient.GetLatestMarkerStates();
        
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
        vrCricketString = "";
        
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
    
    // --- Functions for writing header of data file --- //
    void LogSceneParams()
    {
        // handle arena corners
        string[] corners = new string[4];

        int i = 0;
        foreach (GameObject corner in HelperFunctions.FindObsWithTag("Corner"))
        {
            Vector3 corner_position = corner.transform.position;
            object[] corner_coord = { corner_position.x, corner_position.z };
            string arena_corner = "[" + string.Join(",", corner_coord) + "]";
            corners[i] = arena_corner;
            i++;
        }

        string arena_corners_string = string.Concat("arena_corners: ", "[", string.Join(",", corners), "]");
        writer.WriteLine(arena_corners_string);

        // handle any obstacles in the arena. This only logs the centroid of the obstacle
        foreach (GameObject obstacle in HelperFunctions.FindObsWithTag("Obstacle"))
        {
            string this_obstacle = obstacle.name.ToString().ToLower();
            Vector3 obstacle_position = obstacle.transform.position;
            object[] obstacle_coords = { obstacle_position.x, obstacle_position.y, obstacle_position.z };
            this_obstacle = string.Concat(this_obstacle + "obs: ", " [", string.Join(",", obstacle_coords), "]");
            writer.WriteLine(this_obstacle);
        }

        // once done, write a blank line
        writer.WriteLine(string.Empty);
    }

    void AssembleHeader()
    {
        
        string[] mouse_template = {"mouse_x_m", "mouse_y_m", "mouse_z_m",
                                   "mouse_xrot_m", "mouse_yrot_m", "mouse_zrot_m"};

        string[] realCricket_template = {"cricket_x_m", "cricket_y_m", "cricket_z_m"};
        
        string[] vrCricket_template = {"_x", "_y", "_z",
                                       "_xrot", "_yrot", "_zrot",
                                       "_speed", "_state", "_motion", "_encounter"};
        
        int numCrickets = vrCricketObjs.Length;
        List<string> vrCricket_cols = new List<string>();
        
        // Assemble the VR cricket string depending on how many VR crickets there are
        for (int i=0; i < numCrickets; i++)
        {
            string vcricket = "vrcricket_" + i;
            foreach (string ct in vrCricket_template)
            {
                vrCricket_cols.Add(vcricket + ct);
            }
        }
        
        // Assemble strings for all animals in scene
        string mouse_string = string.Join(", ", mouse_template);
        string realCricket_string = string.Join(", ", realCricket_template);
        string vrCricket_string = string.Join(", ", vrCricket_cols);
        
        object[] header = {"time_m", mouse_string, realCricket_string, vrCricket_string, "color_factor"};

        writer.WriteLine(string.Join(", ", header));
    }


    // --- Handle OSC Communication --- //
    void OnReceiveStop(OscMessage message)
    {
        // Close the writer
        writer.Close();
        // Kill the application
        Application.Quit();
    }
    
    //void OnReceiveTrialStart(OscMessage message)
    //{
    //    // Parse the values for trial setup
    //    trial_num = int.Parse(message.values[0].ToString());
    //    shape = int.Parse(message.values[1].ToString());          // This is an integer representing the index of the child object of Target obj in scene
    //    scale = HelperFunctions.StringToVector3(message.values[2].ToString());    // Vector3 defining the scale of the target
    //    screen_color = HelperFunctions.StringToVector4(message.values[3].ToString());    // Vector4 defining screen color in RGBA
    //    target_color = HelperFunctions.StringToVector4(message.values[4].ToString());    // Vector4 defining target color in RGBA
    //    speed = float.Parse(message.values[5].ToString());    // Float for target speed
    //    acceleration = float.Parse(message.values[6].ToString());    // Float for target acceleration
    //    trajectory = int.Parse(message.values[7].ToString());    // Int for trajectory type

    //    // Set values in TrialHandler for the next trial
    //    targetController.targetIndex = shape;
    //    targetController.scale = scale;
    //    targetController.screenColor = screen_color;
    //    targetController.targetColor = target_color;
    //    targetController.speed = speed;
    //    targetController.acceleration = acceleration;
    //    targetController.trajectory = trajectory;

    //    // Send handshake message to Python process to confirm receipt of parameters
    //    OscMessage handshake = new OscMessage
    //    {
    //        address = "/Handshake"
    //    };
    //    handshake.values.Add(trial_num);
    //    osc.Send(handshake);

    //    // Set up the newest trial
    //    targetController.SetupNewTrial();

    //    // Set booleans that tell us we are now in a trial
    //    inTrial = targetController.inTrial;
    //    trialDone = targetController.trialDone;

    //}

    // void SendTrialEnd()
    // {
    //     // Send trial end message to Python process
    //     OscMessage message = new OscMessage();
    //     message.address = "/EndTrial";
    //     message.values.Add(trial_num);
    //     osc.Send(message);
    // }

}
