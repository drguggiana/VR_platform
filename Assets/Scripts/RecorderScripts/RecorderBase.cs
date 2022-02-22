using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

/*
 This class is the base class for all recorder functions. Functionality common to all
 scenes requiring a recorder should be built here, as all recorder scripts derive from this 
 class. 
 
 This class is responsible for tracking and logging the mouse position from the Optitrack system, 
 updating the tracking square color every frame, setting up data saving files, and setting up 
 the OSC communication and listening for the kill signal. 
*/


//[ExecuteInEditMode]
public class RecorderBase : MonoBehaviour
{
    // Streaming client
    public OptitrackStreamingClient streamingClient;
    public Int32 rigidBodyID;
    
    // OSC communication client
    public OSC osc;
    
    // Variables for tracking square
    public GameObject trackingSquare;
    private Material _trackingSquareMaterial;
    private Color _newColor;
    protected float ColorFactor = 0.0f;

    // Variables for mouse position
    public GameObject mouse;
    protected Vector3 MousePosition;
    protected Vector3 MouseOrientation;
    
    // Timer
    private OptitrackHiResTimer.Timestamp _timerReference;
    protected float TimeStamp;
    
    // Writer for saving data
    protected StreamWriter Writer;

    // Variables to handle the startup sequence
    protected bool InSession = false;
    
    // Waiting variable
    protected bool Release = false;

    // Start is called before the first frame update
    protected virtual void Start()
    {
        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        _timerReference = OptitrackHiResTimer.Now();

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;
        
        // set the OSC communication
        osc.SetAddressHandler("/SessionStart", OnReceiveSessionStart);
        osc.SetAddressHandler("/Close", OnReceiveStop);
        osc.SetAddressHandler("/ReleaseWait", OnReleaseWait);

        // Get the tracking square material and set it to black to start
        _trackingSquareMaterial = trackingSquare.GetComponent<Renderer>().material;
        SetTrackingSquare();
        
        // Set the writer
        Paths.CheckFileExistence(Paths.recording_path);
        Writer = new StreamWriter(Paths.recording_path, true);

        // Write initial parameters and header to file
        LogSceneParams();
        
    }

    // Update is called once per frame
    protected virtual void Update()
    {        
        // For debugging only
        #if (UNITY_EDITOR)
            InSession = true;
            Release = true;
        #endif
        
        SetTrackingSquare();
        GetMousePosition();

    }
    
    // Changes the tracking square color on each frame
    private void SetTrackingSquare()
    {
        // create the color for the square
        // _newColor = new Color(ColorFactor, ColorFactor, ColorFactor, 1f);
        // _newColor = new Color(0.0f, 0.0f, ColorFactor, 1.0f);
        _newColor = new Color(0.0f, 0.0f, ColorFactor, 255f)/255f;
        
        // put it on the square   
        _trackingSquareMaterial.SetColor("_Color", _newColor);
        
        // Define the color for the next iteration (switch it)
        if (ColorFactor > 80f)
        {
            ColorFactor = 0.0f;
        }
        else if (ColorFactor < 16f)
        {
            ColorFactor = 17f;
        }
        else if (ColorFactor < 63f && ColorFactor > 16f)
        {
            ColorFactor = 65f;
        }
        else
        {
            ColorFactor = 81f;
        }
    }

    // Gets the mouse position from Motive on each frame and updates the timestamp in Motive time
    private void GetMousePosition()
    {
        OptitrackRigidBodyState rbState = streamingClient.GetLatestRigidBodyState(rigidBodyID);
        if (rbState != null)
        {
            // get the position of the mouse Rigid Body
            MousePosition = rbState.Pose.Position;
            // change the transform position of the game object
            this.transform.localPosition = MousePosition;
            // also change its rotation
            this.transform.localRotation = rbState.Pose.Orientation;
            // turn the angles into Euler (for later printing)
            MouseOrientation = this.transform.eulerAngles;
            // get the timestamp 
            TimeStamp = rbState.DeliveryTimestamp.SecondsSince(_timerReference);
        }
        else
        {
            MousePosition = mouse.transform.position;
            MouseOrientation = mouse.transform.rotation.eulerAngles;
        }
    }
    
    // Logs the positions of the corners of the arena and any obstacles in the arena
    void LogSceneParams()
    {
        // handle arena corners
        string[] corners = new string[4];

        int i = 0;
        foreach (GameObject corner in HelperFunctions.FindObsWithTag("Corner"))
        {
            Vector3 cornerPosition = corner.transform.position;
            object[] cornerCoord = { cornerPosition.x, cornerPosition.z };
            string arenaCorner = "[" + string.Join(",", cornerCoord) + "]";
            corners[i] = arenaCorner;
            i++;
        }

        string arenaCornersString = string.Concat("arena_corners:", "[", string.Join(",", corners), "]");
        Writer.WriteLine(arenaCornersString);

        // handle any obstacles in the arena. This only logs the centroid of the obstacle
        foreach (GameObject obstacle in HelperFunctions.FindObsWithTag("Obstacle"))
        {
            string thisObstacle = obstacle.name.ToLower();
            Vector3 obstaclePosition = obstacle.transform.position;
            object[] obstacleCoords = { obstaclePosition.x, obstaclePosition.y, obstaclePosition.z };
            thisObstacle = string.Concat(thisObstacle + "obs:", " [", string.Join(",", obstacleCoords), "]");
            Writer.WriteLine(thisObstacle);
        }

        // once done, write a blank line
        Writer.WriteLine(string.Empty);
    }
    
    protected void AssembleHeader(int numRealCrickets=0, int numVRCrickets=0, bool logTrialNumber=false)
    {
        // Define recording templates for commonly used parameters
        string[] mouseTemplate = {"mouse_x_m", "mouse_y_m", "mouse_z_m",
                                  "mouse_xrot_m", "mouse_yrot_m", "mouse_zrot_m"};

        string[] realCricketTemplate = {"_x_m", "_y_m", "_z_m"};
            
        string[] vrCricketTemplate = {"_x", "_y", "_z",
                                      "_xrot", "_yrot", "_zrot",
                                      "_speed", "_state", "_motion", "_encounter"};
        string[] header;
        
        // Assemble string for mouse
        string mouseString = string.Join(", ", mouseTemplate);
        
        // Assemble string for real cricket(s)
        List<string> realCricketCols = new List<string>();
        
        for (int i=0; i < numRealCrickets; i++)
        {
            string cricket = "cricket";
            foreach (string rct in realCricketTemplate)
            {
                realCricketCols.Add(cricket + rct);
            }
        }
        string realCricketString = string.Join(", ", realCricketCols);

        // Assemble the VR cricket string depending on how many VR crickets there are
        List<string> vrCricketCols = new List<string>();
        
        for (int i=0; i < numVRCrickets; i++)
        {
            string vcricket = "vrcricket_" + i;
            foreach (string vct in vrCricketTemplate)
            {
                vrCricketCols.Add(vcricket + vct);
            }
        }
        string vrCricketString = string.Join(", ", vrCricketCols);

        // Add in trial number column if using
        if (logTrialNumber)
        {
            string[] tempHeader = {"time_m", "trial_num", mouseString, realCricketString, vrCricketString, "color_factor"};
            header = tempHeader;
        }
        else
        {
            string[] tempHeader = {"time_m", mouseString, realCricketString, vrCricketString, "color_factor"};
            header = tempHeader;
        }
        
        // Remove any empty strings (if there are no real or vr crickets) and write the line to the file
        header = header.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Writer.WriteLine(string.Join(", ", header));
    }

    // --- Handle OSC Communication --- //
    
    protected virtual void OnReceiveSessionStart(OscMessage message)
    {
        InSession = true;
    }

    void OnReceiveStop(OscMessage message)
    {
        // Close the writer
        Writer.Close();
        // Kill the application
        Application.Quit();
    }

    protected void OnReleaseWait(OscMessage message)
    {
        // Release the waiting to start recording from the loop
        Release = true;
    }
    
    protected void PythonPrint(int msg)
    {
        //For OSC debugging
        OscMessage handshake = new OscMessage
        {
            address = "/Print"
        };
    
        handshake.values.Add(msg);
        osc.Send(handshake);
    }

}
