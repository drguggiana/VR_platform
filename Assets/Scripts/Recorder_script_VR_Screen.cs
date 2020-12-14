using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


//[ExecuteInEditMode]
public class Recorder_script_VR_Screen : MonoBehaviour {

    // Streaming client
    public OptitrackStreamingClient StreamingClient;

    // OSC communication client
    public OSC osc;

    // Variables for tracking square
    public GameObject tracking_square;
    private float color_factor = 0.0f;
    private Color new_color;

    // Variables for mouse position
    public GameObject MouseObj;
    private Vector3 Mouse_Position;
    private Vector3 Mouse_Orientation;

    // Rigid body ID for mouse tracking
    public Int32 RigidBodyId;    

    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float Time_stamp;

    // Variables for target object transforms and states
    public GameObject TargetObj;
    public TargetController targetController;
    private Vector3 Target_Position;

    // Booleans for trial state
    private bool trialDone = false;
    private bool inTrial = false;

    // Variables for properties of the target that are manipulated
    private int trial_num = 0;
    private int shape;
    private Vector3 scale;    
    private Vector4 screen_color;
    private Vector4 target_color;
    private float speed;
    private float acceleration;
    private int trajectory;

    // Writer for saving data
    private StreamWriter writer;
    private string mouse_string;
    private string target_string;

    // For debugging
    private int counter = 0;


    // Use this for initialization
    void Start () {

        // set the OSC communication
        osc.SetAddressHandler("/TrialStart", OnReceiveTrialStart);
        osc.SetAddressHandler("/Close", OnReceiveStop);

        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        reference = OptitrackHiResTimer.Now();

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        //Camera cam = GetComponentInChildren<Camera>();
        //cam.nearClipPlane = 0.000001f;

        // Set the writer
        writer = new StreamWriter(Paths.recording_path, true);

        // Write initial parameters and header to file
        LogSceneParams();
        AssembleHeader();
    }

    // Update is called once per frame
    void Update () {
        // For debugging only
#if (UNITY_EDITOR)
            counter++;
            if (counter % 600 == 0)
            {
                targetController.SetupNewTrial();
                inTrial = targetController.inTrial;
                trialDone = targetController.trialDone;
            }
#endif

        // --- If in trial, check if the trial is done yet --- //
        if (inTrial)
        {
            trialDone = targetController.trialDone;

            if (trialDone)
            {
                Debug.Log("Trial Done");
                // Tell Python that the trial ended
                SendTrialEnd();
                // Reset the booleans
                inTrial = false;
                // Reset trial number to zero
                trial_num = 0;

                counter = 0;
            }
        }


        /* 
         * Below this point, all updates are done on every frame. 
         * They are recorded regardless of if we are in a trial or not.
         */

        // --- Handle the tracking square --- //
        //counter++;
        //if (counter % 120 == 0)
        //{
            // create the color for the square
            new_color = new Color(color_factor, color_factor, color_factor, 1f);
            // put it on the square 
            tracking_square.GetComponent<Renderer>().material.SetColor("_Color", new_color);
            // Define the color for the next iteration (switch it)
            if (color_factor > 0.0f)
            {
                color_factor = 0.0f;
            }
            else
            {
                color_factor = 1.0f;
            }
        //}


        // --- Handle mouse and target data --- //

        // Process the mouse position as the other scripts
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {
            // get the position of the mouse RB
            Mouse_Position = rbState.Pose.Position;
            // change the transform position of the game object
            this.transform.localPosition = Mouse_Position;
            // also change its rotation
            this.transform.localRotation = rbState.Pose.Orientation;
            // turn the angles into Euler (for later printing)
            Mouse_Orientation = this.transform.eulerAngles;
            // get the timestamp 
            Time_stamp = rbState.DeliveryTimestamp.SecondsSince(reference);
        }
        else
        {
            Mouse_Position = MouseObj.transform.position;
            Mouse_Orientation = MouseObj.transform.rotation.eulerAngles;
        }

        // Write the mouse data to a string
        object[] mouse_data = {Mouse_Position.x, Mouse_Position.y, Mouse_Position.z, Mouse_Orientation.x, Mouse_Orientation.y, Mouse_Orientation.z};
        mouse_string = string.Join(", ", mouse_data);


        // Process the target object position - this only tracks position
        // TODO: Make this handle animation states if present for 3D tracking
        if (trial_num > 0)
        {
            Target_Position = TargetObj.transform.position;
        }
        else
        {
            Target_Position = new Vector3(-1.0f, -1.0f, -1.0f);
        }

        // Write the target data to a string
        object[] target_data = {Target_Position.x, Target_Position.y, Target_Position.z};
        target_string = string.Join(", ", target_data);

        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        object[] all_data = { Time_stamp, trial_num, mouse_string, target_string, color_factor };
        writer.WriteLine(string.Join(", ", all_data));

    }


    // Functions for writing header of data file
    void LogSceneParams ()
    {
        // handle arena corners
        string[] corners = new string[4];

        int i = 0;
        foreach (GameObject corner in FindObsWithTag("Corner"))
        {
            Vector3 corner_position = corner.transform.position;
            object[] corner_coord = {corner_position.z, corner_position.x};
            string arena_corner = "[" + string.Join(", ", corner_coord) + "]";
            corners[i] = arena_corner;
            i++;
        }

        string arena_corners_string = string.Concat("arena_corners_x_y: ", "[", string.Join(",", corners), "]");
        writer.WriteLine(arena_corners_string);

        // handle any obstacles in the arena. This only logs the centroid of the obstacle
        foreach (GameObject obstacle in FindObsWithTag("Obstacle"))
        {
            string this_obstacle = obstacle.name.ToString().ToLower();
            Vector3 obstacle_position = obstacle.transform.position;
            object[] obstacle_coords = {obstacle_position.z, obstacle_position.x, obstacle_position.y};
            this_obstacle = string.Concat(this_obstacle + "obs_x_y_z: ", " [", string.Join(", ", obstacle_coords), "]");
            writer.WriteLine(this_obstacle);
        }

        // once done, write a blank line
        writer.WriteLine(string.Empty);
    }

    void AssembleHeader ()
    {
        string[] header = {"time_m", "trial_num",
                            "mouse_y_m", "mouse_z_m", "mouse_x_m",
                            "mouse_yrot_m", "mouse_zrot_m", "mouse_xrot_m",
                            "target_y_m", "target_z_m", "target_x_m",
                            "color_factor"};
        writer.WriteLine(string.Join(", ", header));
    }


    // --- Handle OSC Communication --- //
    void OnReceiveTrialStart (OscMessage message)
    {
        // Parse the values for trial setup
        trial_num = int.Parse(message.values[0].ToString());
        shape = int.Parse(message.values[1].ToString());          // This is an integer representing the index of the child object of Target obj in scene
        scale = StringToVector3(message.values[2].ToString());
        screen_color = StringToVector4(message.values[3].ToString());
        target_color = StringToVector4(message.values[4].ToString());
        speed = float.Parse(message.values[5].ToString());
        acceleration = float.Parse(message.values[6].ToString());
        trajectory = int.Parse(message.values[7].ToString());

        // Set values in TrialHandler for the next trial
        targetController.targetIndex = shape;
        targetController.scale = scale;
        targetController.screenColor = screen_color;
        targetController.targetColor = target_color;
        targetController.speed = speed;
        targetController.acceleration = acceleration;
        targetController.trajectory = trajectory;

        // Send handshake message to Python process to confirm receipt of parameters
        OscMessage handshake = new OscMessage();
        handshake.address = "/Handshake";
        handshake.values.Add(trial_num);
        osc.Send(handshake);

        // Set up the newest trial
        targetController.SetupNewTrial();

        // Set booleans that tell us we are now in a trial
        inTrial = targetController.inTrial;
        trialDone = targetController.trialDone;

    }

    Vector3 StringToVector3(string sVector)
    {
        // Taken from user Praveen Ramanayake at https://stackoverflow.com/questions/30959419/converting-string-to-vector-in-unity
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    Vector4 StringToVector4(string sVector)
    {
        // Modified from user Praveen Ramanayake at https://stackoverflow.com/questions/30959419/converting-string-to-vector-in-unity
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector4 result = new Vector4(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]),
            float.Parse(sArray[3]));

        return result;
    }

    void SendTrialEnd ()
    {
        // Send trial end message to Python process
        OscMessage message = new OscMessage();
        message.address = "/EndTrial";
        message.values.Add(trial_num);
        osc.Send(message);
    }

    void OnReceiveStop (OscMessage message)
    {
        // Close the writer
        writer.Close();
        // Kill the application
        Application.Quit();
    }

    // --- Helper Functions --- //
    GameObject[] FindObsWithTag (string tag)
    {
        GameObject[] foundObs = GameObject.FindGameObjectsWithTag(tag);
        Array.Sort(foundObs, CompareObNames);
        return foundObs;
    }

    int CompareObNames (GameObject x, GameObject y)
    {
        return x.name.CompareTo(y.name);
    }


}
