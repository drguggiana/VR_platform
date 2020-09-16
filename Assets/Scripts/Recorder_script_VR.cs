using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


//[ExecuteInEditMode]
public class Recorder_script_VR : MonoBehaviour
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
    public GameObject CricketObj;
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
        writer = new StreamWriter(Paths.recording_path, true);

    }

    // Update is called once per frame
    void Update()
    {
        // --- Handle the tracking square --- //

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


        // --- Handle mouse and cricket data --- //
        // This works for a single VR cricket

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

        // Get the VR cricket position and orientation
        Cricket_Position = CricketObj.transform.position;
        Cricket_Orientation = CricketObj.transform.rotation.eulerAngles;

        // Get the VR cricket speed and current motion state
        speed = CricketObj.GetComponent<Animator>().GetFloat("speed"); ;
        state = CricketObj.GetComponent<Animator>().GetInteger("state_selector");
        motion = CricketObj.GetComponent<Animator>().GetInteger("motion_selector");
        encounter = CricketObj.GetComponent<Animator>().GetInteger("in_encounter");

        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        writer.WriteLine(
            string.Concat(
                Time_stamp.ToString(), ',',
                Mouse_Position.x.ToString(), ',', Mouse_Position.y.ToString(), ',', Mouse_Position.z.ToString(), ',',
                Mouse_Orientation.x.ToString(), ',', Mouse_Orientation.y.ToString(), ',', Mouse_Orientation.z.ToString(), ',',
                Cricket_Position.x.ToString(), ',', Cricket_Position.y.ToString(), ',', Cricket_Position.z.ToString(), ',',
                Cricket_Orientation.x.ToString(), ',', Cricket_Orientation.y.ToString(), ',', Cricket_Orientation.z.ToString(), ',',
                speed.ToString(), ',', state.ToString(), ',', motion.ToString(), ',', encounter.ToString(), ',', color_factor.ToString()
                )
            );

    }

    void OnReceiveStop(OscMessage message)
    {
        // Close the writer
        writer.Close();
        // Kill the application
        Application.Quit();
    }

}
