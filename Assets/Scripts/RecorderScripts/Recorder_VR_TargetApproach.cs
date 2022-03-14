using UnityEngine;

/*
 This class is a child of the RecorderBase class. It inherits most of its functionality from there.
 See RecorderBase for detailed explanation of what goes on under the hood.
 
 This class is responsible for sending and receiving information about the trial parameters from
 Python, communicating with the target controller, handling trial timing, logging the mouse position 
 from the Optitrack system, logging the target position, and logging the current time and color of 
 the tracking square.
*/

[ExecuteInEditMode]
public class Recorder_VR_TargetApproach : RecorderBase {

    // Variables for target object transforms and states
    public GameObject targetObj;
    public TargetController targetController;
    private Vector3 _targetPosition;

    // Booleans for trial state
    private bool _trialDone = true;
    private bool _inTrial = false;

    // Variables for properties of the target that are manipulated
    private int _trialNum = 0;
    private int _shape;
    private Vector3 _scale;    
    private Vector4 _screenColor;
    private Vector4 _targetColor;
    private float _speed;
    private float _acceleration;
    private int _trajectory;
    
    // For debugging
    private int _counter = 0;


    // Use this for initialization
    protected override void Start () 
    {
        // -- Inherit setup from base class
        base.Start();

        // -------------------------------------------
        
        // set the OSC communication
        osc.SetAddressHandler("/TrialStart", OnReceiveTrialStart);
        
        // Write header to file. This 
        AssembleHeader();
    }

    // Update is called once per frame
    protected override void Update() {
        // For debugging only
        #if (UNITY_EDITOR)
            _counter++;

            if ((_counter % 240 == 0) & _trialDone)
            {
                targetController.SetupNewTrial();
                _inTrial = targetController.inTrial;
                _trialDone = targetController.trialDone;
            }
        #endif

        // --- If in trial, check if the trial is done yet --- //
        if (InSession)
        {
            if (_inTrial)
            {
                _trialDone = targetController.trialDone;

                if (_trialDone)
                {
                    Debug.Log("Trial Done");
                    // Tell Python that the trial ended
                    SendTrialEnd();
                    // Reset the booleans
                    _inTrial = false;
                    // Reset trial number to zero
                    _trialNum = 0;
                    _counter = 0;
                }
            }
        }

        
        // Get mouse position from OptiTrack and update the tracking square color
        // Note that these functions are defined in the RecorderBase class
        // SetTrackingSqaure();
        // GetMousePosition();
        base.Update();
        
        // --- Handle mouse and target data --- //
        
        // Write the mouse data to a string
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
                               MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);
        
        // Process the target object position - this only tracks position
        // TODO: Make this handle animation states if present for 3D tracking
        if (_trialNum > 0)
        {
            _targetPosition = targetObj.transform.position;
        }
        else
        {
            _targetPosition = new Vector3(-1.0f, -1.0f, -1.0f);
        }

        // Write the target data to a string
        object[] targetData = {_targetPosition.x, _targetPosition.y, _targetPosition.z};
        string targetString = string.Join(", ", targetData);

        // --- Data Saving --- //
        // Write the mouse and VR cricket info
        object[] allData = { TimeStamp, _trialNum, mouseString, targetString, ColorFactor };
        Writer.WriteLine(string.Join(", ", allData));

    }
    
    // Functions for writing header of data file
    void AssembleHeader ()
    {
        string[] header = {"time_m", "trial_num",
                           "mouse_x_m", "mouse_y_m", "mouse_z_m",
                           "mouse_xrot_m", "mouse_yrot_m", "mouse_zrot_m",
                           "target_x_m", "target_y_m", "target_z_m",
                           "color_factor"};

        Writer.WriteLine(string.Join(", ", header));
    }
    
    // --- Handle OSC Communication --- //
    void OnReceiveTrialStart (OscMessage message)
    {
        // Parse the values for trial setup
        _trialNum = int.Parse(message.values[0].ToString());
        _shape = int.Parse(message.values[1].ToString());          // This is an integer representing the index of the child object of Target obj in scene
        _scale = HelperFunctions.StringToVector3(message.values[2].ToString());    // Vector3 defining the scale of the target
        _screenColor = HelperFunctions.StringToVector4(message.values[3].ToString());    // Vector4 defining screen color in RGBA
        _targetColor = HelperFunctions.StringToVector4(message.values[4].ToString());    // Vector4 defining target color in RGBA
        _speed = float.Parse(message.values[5].ToString());    // Float for target speed
        _acceleration = float.Parse(message.values[6].ToString());    // Float for target acceleration
        _trajectory = int.Parse(message.values[7].ToString());    // Int for trajectory type

        // Set values in TrialHandler for the next trial
        targetController.targetIndex = _shape;
        targetController.scale = _scale;
        targetController.screenColor = _screenColor;
        targetController.targetColor = _targetColor;
        targetController.speed = _speed;
        targetController.acceleration = _acceleration;
        targetController.trajectory = _trajectory;

        // Send handshake message to Python process to confirm receipt of parameters
        OscMessage handshake = new OscMessage
        {
            address = "/TrialHandshake"
        };
        handshake.values.Add(_trialNum);
        osc.Send(handshake);

        // Set up the newest trial
        targetController.SetupNewTrial();

        // Set booleans that tell us we are now in a trial
        _inTrial = targetController.inTrial;
        _trialDone = targetController.trialDone;

    }

    void SendTrialEnd ()
    {
        // Send trial end message to Python process
        OscMessage message = new OscMessage
        {
            address = "/EndTrial"
        };
        message.values.Add(_trialNum);
        osc.Send(message);
    }
    
 }
