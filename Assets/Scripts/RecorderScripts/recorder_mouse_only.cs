using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;

public class recorder_mouse_only : MonoBehaviour
{
    // Streaming client
    public OptitrackStreamingClient streamingClient;

    // OSC communication client
    public OSC osc;

    // Variables for mouse position
    public GameObject mouseObj;
    private Vector3 mousePosition;
    private Vector3 mouseOrientation;

    // Variables for tracking square
    public GameObject trackingSquare;
    private float colorFactor = 0.0f;
    private Color newColor;

    // Rigid body ID for mouse tracking
    public Int32 rigidBodyId;

    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float timeStamp;

    // Writer for saving data
    private StreamWriter writer;
    private string mouseString;
    

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

        // Write initial parameters and header to file
        LogSceneParams();
        AssembleHeader();
    }

    // Update is called once per frame
    void Update()
    {
        // --- Handle the tracking square --- //

        // create the color for the square
        newColor = new Color(colorFactor, colorFactor, colorFactor, 1f);
        // put it on the square
        trackingSquare.GetComponent<Renderer>().material.SetColor("_Color", newColor);

        // Define the color for the next iteration (switch it)
        if (colorFactor > 0.0f)
        {
            colorFactor = 0.0f;
        }
        else
        {
            colorFactor = 1.0f;
        }
        

        // --- Handle mouse data --- //

        // Write the mouse data to a string
        GetMousePosition();
        object[] mouse_data = { mousePosition.x, mousePosition.y, mousePosition.z, mouseOrientation.x, mouseOrientation.y, mouseOrientation.z };
        mouseString = string.Join(", ", mouse_data);
        
        // --- Data Saving --- //

        // Assemble and save all data
        object[] all_data = { timeStamp, mouseString, colorFactor };
        writer.WriteLine(string.Join(", ", all_data));

    }

    // --- Functions for tracking objects in the scene --- //
    void GetMousePosition()
    {
        OptitrackRigidBodyState rbState = streamingClient.GetLatestRigidBodyState(rigidBodyId);
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
            mousePosition = mouseObj.transform.position;
            mouseOrientation = mouseObj.transform.rotation.eulerAngles;
        }
    }
    
    // --- Functions for writing header of data file --- //
    void LogSceneParams()
    {
        // handle arena corners
        string[] corners = new string[4];

        int i = 0;
        foreach (GameObject corner in HelperFunctions.FindObsWithTag("Corner"))
        {
            Vector3 cornerPosition = corner.transform.position;
            object[] cornerCoord = { cornerPosition.z, cornerPosition.x };
            string arenaCorner = "[" + string.Join(", ", cornerCoord) + "]";
            corners[i] = arenaCorner;
            i++;
        }

        string cornersString = string.Concat("arena_corners_x_y: ", "[", string.Join(",", corners), "]");
        writer.WriteLine(cornersString);

        // handle any obstacles in the arena. This only logs the centroid of the obstacle
        foreach (GameObject obstacle in HelperFunctions.FindObsWithTag("Obstacle"))
        {
            string this_obstacle = obstacle.name.ToString().ToLower();
            Vector3 obstacle_position = obstacle.transform.position;
            object[] obstacle_coords = { obstacle_position.x, obstacle_position.y, obstacle_position.z };
            this_obstacle = string.Concat(this_obstacle + "_x_y_z: ", " [", string.Join(", ", obstacle_coords), "]");
            writer.WriteLine(this_obstacle);
        }

        // once done, write a blank line
        writer.WriteLine(string.Empty);
    }

    void AssembleHeader()
    {
        string[] mouseTemplate = {"mouse_x_m", "mouse_y_m", "mouse_z_m",
                                  "mouse_xrot_m", "mouse_yrot_m", "mouse_zrot_m"};

        string[] realCricketTemplate = {"cricket_x_m", "cricket_y_m", "cricket_z_m"};
        
        string[] vrCricketTemplate = {"_x", "_y", "_z",
                                      "_xrot", "_yrot", "_zrot",
                                      "_speed", "_state", "_motion", "_encounter"};

        // int numCrickets = CricketObjs.Length;
        // List<string> cricket_cols = new List<string>(); ;
        //
        // // Assemble the cricket string depending on how many VR crickets there are
        // for (int i=0; i < numCrickets; i++)
        // {
        //     string vcricket = "vrcricket_" + i;
        //     foreach (string ct in vrCricketTemplate)
        //     {
        //         cricket_cols.Add(vcricket + ct);
        //     }
        // }

        string mouse_string = string.Join(", ", mouseTemplate);

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