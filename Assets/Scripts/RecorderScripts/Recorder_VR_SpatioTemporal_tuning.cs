using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class Recorder_VR_SpatioTemporal_tuning : MonoBehaviour
{
    
    // Streaming client
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    
    // OSC communication client
    public OSC osc;

    // Variables for tracking square
    public GameObject trackingSquare;
    private float color_factor = 0.0f;
    private Color new_color;

    // Variables for mouse position
    public GameObject mouseObj;
    private Vector3 mousePosition;
    private Vector3 mouseOrientation;
    
    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float timeStamp;
    
    // Writer for saving data
    private StreamWriter writer;
    private string mouseString;


    // Start is called before the first frame update
    void Start()
    {
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

        // Set the writer
        Paths.CheckFileExistence(Paths.recording_path);
        writer = new StreamWriter(Paths.recording_path, true);
        
        
        // Write initial parameters and header to file
        LogSceneParams();
        AssembleHeader();
    }

    // Update is called once per frame
    void Update()
    {
        
        /* 
         * Below this point, all updates are done on every frame. 
         * They are recorded regardless of if we are in a trial or not.
         */

        // --- Handle the tracking square --- //

        // create the color for the square
        new_color = new Color(color_factor, color_factor, color_factor, 1f);
        // put it on the square 
        trackingSquare.GetComponent<Renderer>().sharedMaterial.SetColor("_Color", new_color);
        // Define the color for the next iteration (switch it)
        if (color_factor > 0.0f)
        {
            color_factor = 0.0f;
        }
        else
        {
            color_factor = 1.0f;
        }
        
        // --- Handle mouse, cricket, and target data --- //

        // Process the mouse position as the other scripts
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {
            // get the position of the mouse RB
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
            mousePosition = mouseObj.transform.position;
            mouseOrientation = mouseObj.transform.rotation.eulerAngles;
        }

        // Write the mouse data to a string
        object[] mouse_data = { mousePosition.x, mousePosition.y, mousePosition.z, 
                                mouseOrientation.x, mouseOrientation.y, mouseOrientation.z };
        mouseString = string.Join(", ", mouse_data);
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
        
        // int numCrickets = vrCricketObjs.Length;
        // List<string> vrCricket_cols = new List<string>();
        //
        // // Assemble the VR cricket string depending on how many VR crickets there are
        // for (int i=0; i < numCrickets; i++)
        // {
        //     string vcricket = "vrcricket_" + i;
        //     foreach (string ct in vrCricket_template)
        //     {
        //         vrCricket_cols.Add(vcricket + ct);
        //     }
        // }
        
        // Assemble strings for all animals in scene
        string mouse_string = string.Join(", ", mouse_template);
        // string realCricket_string = string.Join(", ", realCricket_template);
        // string vrCricket_string = string.Join(", ", vrCricket_cols);
        //
        // object[] header = {"time_m", mouse_string, realCricket_string, vrCricket_string, "color_factor"};
        
        object[] header = {"time_m", mouse_string, "color_factor"};
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
    
    
}

