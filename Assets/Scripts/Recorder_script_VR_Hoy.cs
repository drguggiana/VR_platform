using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


//[ExecuteInEditMode]
public class Recorder_script_VR_Hoy : MonoBehaviour {

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

    // Variables for object transforms and states
    private GameObject TargetObj;
    private Vector3 Target_Position;

    // Public variables for properties that are manipulated
    private int trial_num = 0;
    private bool trialDone = false;
    private float speed;
    private float acceleration;
    private float contrast;
    private float size;    // This is proprtional to radius of a circle
    private float shape;
    private float trajectory;

    // Writer for saving data
    string mouse_data;
    string target_data;
    private StreamWriter writer;

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
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;

        // Set the writer
        writer = new StreamWriter(Paths.recording_path, true);
    }
	
	// Update is called once per frame
	void Update () {

        // --- Check if the trial is done yet --- //
        checkTrialEnd();
        if (trialDone == true)
        {
            // Tell Python that the trial ended
            SendTrialEnd();
            // Disable the script and then 
        }
        
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
        mouse_data = string.Concat(Mouse_Position.x.ToString(), ',', Mouse_Position.y.ToString(), ',', Mouse_Position.z.ToString(), ',',
                                   Mouse_Orientation.x.ToString(), ',', Mouse_Orientation.y.ToString(), ',', Mouse_Orientation.z.ToString());


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

        // Write the mouse data to a string
        target_data = string.Concat(Target_Position.x.ToString(), ',', Target_Position.y.ToString(), ',', Target_Position.z.ToString());

        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        writer.WriteLine(string.Concat(Time_stamp.ToString(), ',', trial_num.ToString(), ',', 
                                       mouse_data, ',', target_data, ',', 
                                       color_factor.ToString()));
    }


    // --- Check for end of trial --- //
    void checkTrialEnd()
    {
        trialDone = Trajectories.trialDone;
    }

    // --- Handle OSC Communication --- //
    void OnReceiveStop(OscMessage message)
    {
        // Close the writer
        writer.Close();
        // Kill the application
        Application.Quit();
    }

    void OnReceiveTrialStart(OscMessage message)
    {
        // Parse the values for trial setup
        trial_num = message.GetInt(0);
        speed = message.GetFloat(1);
        acceleration = message.GetFloat(2);
        contrast = message.GetFloat(3);
        size = message.GetFloat(4);
        shape = message.GetFloat(5);
        //trajectory = message;

        // TODO figure out how to send vectors over OSC

        // Set values for the next trial
        Trajectories.speed = speed;
        Trajectories.acceleration = acceleration;
        Trajectories.contrast = contrast;
        // Trajectories.size = size;
        //Trajectories.shape = shape; 

        // Reset trial Done boolean
        trialDone = false;
    }

    void SendTrialEnd()
    {
        // Reset trial_num to zero - this is our flag for being in/out of a trial
        trial_num = 0;

        // Send trial end message to Python process
        OscMessage message = new OscMessage();
        message.address = "/EndTrial";
        message.values.Add(1);
        osc.Send(message);
    }

    
}
