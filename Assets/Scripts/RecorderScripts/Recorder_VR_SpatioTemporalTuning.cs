using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;
using UnityEngine.Serialization;

[ExecuteInEditMode]
public class Recorder_VR_SpatioTemporalTuning : MonoBehaviour
{
    
    // Streaming client
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    
    // OSC communication client
    public OSC osc;

    // Variables for mouse position
    public GameObject mouseObj;
    private Vector3 mousePosition;
    private Vector3 mouseOrientation;
    
    // Variables for tracking square
    public GameObject trackingSquare;
    private Material trackingSqaureMaterial;
    private float colorFactor = 0.0f;

    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float timeStamp;
    
    // Writer for saving data
    private StreamWriter writer;
    private string mouseString;
    
    // Private variables for the Gabor stimulus
    public GameObject gaborStim;
    private AssignSpatialTempFreq _assignSpatialTempFreq;
    private AssignGaussianAlpha _assignGaussianAlpha;
    private float _spatialFreq;
    private float _temporalFreq;

    // Private variables for the trial structure
    private float trialTimer = 0;
    private int _trialNum = 0;    // This is a variable that temporarily holds the trial number
    private int trialNum = 0;
    private bool inTrial = false;
    private bool inSession = false;
    private float trialDuration;         // In seconds
    private float interStimInterval;     // In seconds
    
    
    // Start is called before the first frame update
    void Start()
    {
        // -- These get called in every recorder script
        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        reference = OptitrackHiResTimer.Now();

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;
        
        // Get the tracking square material
        trackingSqaureMaterial = trackingSquare.GetComponent<Renderer>().material;

        // set the OSC communication
        osc.SetAddressHandler("/Close", OnReceiveStop);
        
        // Set the writer
        Paths.CheckFileExistence(Paths.recording_path);
        writer = new StreamWriter(Paths.recording_path, true);
        
        // Write initial parameters and header to file
        HelperFunctions.LogSceneParams(writer);
        // -------------------------------------------
        
        // -- These are specific to the particular scene
        osc.SetAddressHandler("/SetupExperiment", OnReceiveExpSetup);
        osc.SetAddressHandler("/TrialSetup", OnReceiveTrialSetup);

        // Get the scripts for the gabor assignments
        _assignSpatialTempFreq = gaborStim.GetComponent<AssignSpatialTempFreq>();
        _assignGaussianAlpha = gaborStim.GetComponentInChildren<AssignGaussianAlpha>();

        AssembleHeader();
        // -------------------------------------------
    }

    // Update is called once per frame
    void Update()
    {
        
        /* 
         * Below this point, all updates are done on every frame. 
         * They are recorded regardless of if we are in a trial or not.
         */

        // --- Handle the tracking square --- //
        SetTrackingSqaure();
        
        // --- Handle mouse data --- //
        GetMousePosition();
        object[] mouseData = { mousePosition.x, mousePosition.y, mousePosition.z, 
                               mouseOrientation.x, mouseOrientation.y, mouseOrientation.z };
        mouseString = string.Join(", ", mouseData);
        
        // --- Handle trial structure --- //
        if (inSession)
        {
            trialTimer += Time.deltaTime;
            
            if (inTrial)
            {
                if (trialTimer > trialDuration)
                {
                    EndTrial();
                }
            }
            else
            {
                if (trialTimer > interStimInterval)
                {
                    StartTrial();
                }
            }
        }

        // --- Data Saving --- //
        object[] allData = { timeStamp, trialNum, mouseString, colorFactor };
        writer.WriteLine(string.Join(", ", allData));
    }
    
    
    // --- Functions for tracking objects in the scene --- //
    void SetTrackingSqaure()
    {
        // create the color for the square
        Color newColor = new Color(colorFactor, colorFactor, colorFactor, 1f);
        // put it on the square 
        trackingSqaureMaterial.SetColor("_Color", newColor);
        // Define the color for the next iteration (switch it)
        if (colorFactor > 0.0f)
        {
            colorFactor = 0.0f;
        }
        else
        {
            colorFactor = 1.0f;
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
            mousePosition = mouseObj.transform.position;
            mouseOrientation = mouseObj.transform.rotation.eulerAngles;
        }
    }
    
    
    // --- Functions for writing header of data file --- //
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
        
        object[] header = {"time_m", "trial_num", mouse_string, "color_factor"};
        writer.WriteLine(string.Join(", ", header));
    }

    // --- Functions for handling the trial structure of the task --- //
    void StartTrial()
    {
        // handle booleans
        inTrial = true;
        
        // Assign trial number for recording
        trialNum = _trialNum;
        
        // Assign the spatial and temporal frequencies and update the stripes material
        _assignSpatialTempFreq.spatialFreq = _spatialFreq;
        _assignSpatialTempFreq.temporalFreq = _temporalFreq;
        _assignSpatialTempFreq.uvOffset = 0;
        _assignSpatialTempFreq.SetParameters();
        
        // Open up the alpha mask
        _assignGaussianAlpha.SetInvert(0);

        // Reset trial timer
        trialTimer = 0;
    }
    
    void EndTrial()
    {
        // Set boolean to tell us we are now in done with a trial
        inTrial = false;
        
        // Close the Alpha mask
        _assignGaussianAlpha.SetInvert(1);
        
        // Send message to the Python process that the trial is over
        SendTrialEnd();
        
        // Reset trial number
        trialNum = 0;
        
        // Reset trial timer
        trialTimer = 0;
    }
    
    // --- Handle OSC Communication --- //
    void OnReceiveExpSetup(OscMessage message)
    {
        // Parse the values for trial structure setup
        trialDuration = float.Parse(message.values[0].ToString());
        interStimInterval = float.Parse(message.values[1].ToString());
        
        // Set boolean that we are now in the experimental session
        inSession = true;
    }
    void OnReceiveTrialSetup(OscMessage message)
    {
        // Parse the values for trial structure setup
        _trialNum = int.Parse(message.values[0].ToString());
        _spatialFreq = float.Parse(message.values[1].ToString());
        _temporalFreq = float.Parse(message.values[2].ToString());
        
        // Send handshake message to Python process to confirm receipt of parameters
        OscMessage handshake = new OscMessage
        {
            address = "/Handshake"
        };
        handshake.values.Add(trialNum);
        osc.Send(handshake);
    }

    void SendTrialEnd ()
    {
        // Send trial end message to Python process
        OscMessage message = new OscMessage
        {
            address = "/EndTrial"
        };
        message.values.Add(trialNum);
        osc.Send(message);
    }
    void OnReceiveStop(OscMessage message)
    {
        // Set the boolean or session end
        inSession = false;
        // Close the writer
        writer.Close();
        // Kill the application
        Application.Quit();
    }
    
    
}

