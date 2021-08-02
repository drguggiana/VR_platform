using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;


//[ExecuteInEditMode]
public class Recorder_script_VR_empty : MonoBehaviour
{
    // Streaming client
    public OptitrackStreamingClient StreamingClient;

    // OSC communication client
    public OSC osc;

    // Variables for mouse position
    public GameObject MouseObj;
    private Vector3 Mouse_Position;
    private Vector3 Mouse_Orientation;

    // Variables for cricket transforms and states
    private GameObject[] CricketObjs;
    GameObject cricket_obj;
    private Vector3 Cricket_Position;
    private Vector3 Cricket_Orientation;
    private int state;
    private float speed;
    private int encounter;
    private int motion;

    // Variables for tracking square
    public GameObject tracking_square;
    private float color_factor = 0.0f;
    private Color new_color;

    // Rigid body ID for mouse tracking
    public Int32 RigidBodyId;

    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float Time_stamp;

    // Writer for saving data
    private StreamWriter writer;
    private string mouse_string;
    private string cricket_string = "";
    private string this_cricket;

    private int count = 0;


    // Use this for initialization
    void Start()
    {
        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        reference = OptitrackHiResTimer.Now();

        // set the OSC communication
        osc.SetAddressHandler("/Close", OnReceiveStop);

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;

        // Set the writer
        Paths.CheckFileExistence(Paths.recording_path);
        writer = new StreamWriter(Paths.recording_path, true);

        // Get cricket object array sorted by name/number
        CricketObjs = HelperFunctions.FindObsWithTag("Cricket");

        // Write initial parameters and header to file
        LogSceneParams();
        AssembleHeader();
    }

    // Update is called once per frame
    void Update()
    {
        count += 1;
        // --- Handle the tracking square --- //

        // create the color for the square
        new_color = new Color(color_factor, color_factor, color_factor, 1f);
        // put it on the square
        tracking_square.GetComponent<Renderer>().material.SetColor("_Color", new_color);

        // Define the color for the next iteration (switch it)
        //if (count % 120 == 0)
        //{
            if (color_factor > 0.0f)
        {
            color_factor = 0.0f;
        }
        else
        {
            color_factor = 1.0f;
        }
        //}



        // --- Handle mouse and cricket data --- //

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
        object[] mouse_data = { Mouse_Position.x, Mouse_Position.y, Mouse_Position.z, Mouse_Orientation.x, Mouse_Orientation.y, Mouse_Orientation.z };
        mouse_string = string.Join(", ", mouse_data);

        // // Loop through the VR Crickets to get their data
        // foreach (GameObject cricket_obj in CricketObjs)
        // {
        //     // Get the VR cricket position and orientation
        //     Cricket_Position = cricket_obj.transform.position;
        //     Cricket_Orientation = cricket_obj.transform.rotation.eulerAngles;
        //
        //     // Get the VR cricket speed and current motion state
        //     speed = cricket_obj.GetComponent<Animator>().GetFloat("speed"); ;
        //     state = cricket_obj.GetComponent<Animator>().GetInteger("state_selector");
        //     motion = cricket_obj.GetComponent<Animator>().GetInteger("motion_selector");
        //     encounter = cricket_obj.GetComponent<Animator>().GetInteger("in_encounter");
        //
        //     // Concatenate strings for this cricket object, and add to all cricket string
        //     object[] cricket_data = {Cricket_Position.x, Cricket_Position.y, Cricket_Position.z,
        //                              Cricket_Orientation.x, Cricket_Orientation.y, Cricket_Orientation.z,
        //                              speed, state, motion, encounter };
        //
        //     this_cricket = string.Join(", ", cricket_data);
        //
        //     List<string> strArray = new List<string> { cricket_string, this_cricket };
        //
        //     // Remove leading comma
        //     cricket_string = string.Join(", ", strArray.Where(s => !string.IsNullOrEmpty(s)));
        //
        // }


        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        object[] all_data = { Time_stamp, mouse_string, color_factor };
        writer.WriteLine(string.Join(", ", all_data));

        cricket_string = "";

    }

    // Functions for writing header of data file
    void LogSceneParams()
    {
        // handle arena corners
        string[] corners = new string[4];

        int i = 0;
        foreach (GameObject corner in HelperFunctions.FindObsWithTag("Corner"))
        {
            Vector3 corner_position = corner.transform.position;
            object[] corner_coord = { corner_position.z, corner_position.x };
            string arena_corner = "[" + string.Join(", ", corner_coord) + "]";
            corners[i] = arena_corner;
            i++;
        }

        string arena_corners_string = string.Concat("arena_corners_x_y: ", "[", string.Join(",", corners), "]");
        writer.WriteLine(arena_corners_string);

        // handle any obstacles in the arena. This only logs the centroid of the obstacle
        foreach (GameObject obstacle in HelperFunctions.FindObsWithTag("Obstacle"))
        {
            string this_obstacle = obstacle.name.ToString().ToLower();
            Vector3 obstacle_position = obstacle.transform.position;
            object[] obstacle_coords = { obstacle_position.z, obstacle_position.x, obstacle_position.y };
            this_obstacle = string.Concat(this_obstacle + "_obs_x_y_z: ", " [", string.Join(", ", obstacle_coords), "]");
            writer.WriteLine(this_obstacle);
        }

        // once done, write a blank line
        writer.WriteLine(string.Empty);
    }

    void AssembleHeader()
    {
        string[] mouse_template = {"mouse_y_m", "mouse_z_m", "mouse_x_m",
                                   "mouse_yrot_m", "mouse_zrot_m", "mouse_xrot_m"};

        string[] cricket_template = {"_y", "_z", "_x",
                                     "_yrot", "_zrot", "_xrot",
                                     "_speed", "_state", "_motion", "_encounter"};

        // int numCrickets = CricketObjs.Length;
        // List<string> cricket_cols = new List<string>(); ;
        //
        // // Assemble the cricket string depending on how many VR crickets there are
        // for (int i=0; i < numCrickets; i++)
        // {
        //     string vcricket = "vrcricket_" + i;
        //     foreach (string ct in cricket_template)
        //     {
        //         cricket_cols.Add(vcricket + ct);
        //     }
        // }

        string mouse_string = string.Join(", ", mouse_template);
        // string cricket_string = string.Join(", ", cricket_cols);

        object[] header = {"time_m", mouse_string, "color_factor"};
        writer.WriteLine(string.Join(", ", header));
    }

    void OnReceiveStop(OscMessage message)
    {
        // Close the writer
        writer.Close();
        // Kill the application
        Application.Quit();
    }

}