using UnityEngine;

[ExecuteInEditMode]
public class Recorder_VR_SpatioTemporalTuning : RecorderBase
{
    // Private variables for the Gabor stimulus
    public GameObject gaborStim;
    private AssignSpatialTempFreq _assignSpatialTempFreq;
    private AssignGaussianAlpha _assignGaussianAlpha;
    private float _spatialFreq = 0.02222f;
    private float _temporalFreq = 0.5f;

    // Private variables for the trial structure
    private float trialTimer = 0;
    private int _trialNum = 0;    // This is a variable that temporarily holds the trial number
    private int trialNum = 0;
    private bool _inTrial = false;
    private bool inSession = false;
    private float trialDuration = 5;         // In seconds
    private float interStimInterval = 5;     // In seconds


    private int _counter = 0;
    
    // Start is called before the first frame update
    protected override void Start()
    {
        // -- Inherit setup from base class
        base.Start();
        
        // -------------------------------------------
        
        // -- These are specific to the particular scene
        osc.SetAddressHandler("/SetupExperiment", OnReceiveExpSetup);
        osc.SetAddressHandler("/TrialSetup", OnReceiveTrialSetup);

        // Get the scripts for the gabor assignments
        _assignSpatialTempFreq = gaborStim.GetComponent<AssignSpatialTempFreq>();
        _assignGaussianAlpha = gaborStim.GetComponentInChildren<AssignGaussianAlpha>();

        // This function is derived from the RecorderBase class
        // We only assign the header once we know what kind of objects are in the scene
        AssembleHeader(0, 0, true);
    }

    // Update is called once per frame
    protected override void Update()
    {
        // For debugging only
        #if (UNITY_EDITOR)

            if ((_counter % 240 == 0) & !inSession)
            {
                inSession = true;
            }
        #endif
        
        
        
        // Call the base update function to get mouse position from OptiTrack and
        // update the tracking square color
        base.Update();
        
        // --- Handle trial structure --- //
        if (inSession)
        {
            trialTimer += Time.deltaTime;
            
            if (_inTrial)
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
        
        // --- Handle mouse data --- //
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
            MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);

        // --- Data Saving --- //
        object[] allData = { TimeStamp, trialNum, mouseString, ColorFactor };
        Writer.WriteLine(string.Join(", ", allData));
    }
    

    // --- Functions for handling the trial structure of the task --- //
    void StartTrial()
    {
        // handle booleans
        _inTrial = true;
        
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
        _inTrial = false;
        
        // Close the Alpha mask
        _assignGaussianAlpha.SetInvert(1);
        
        // Reset the spatial and temporal frequencies and update the stripes material
        _assignSpatialTempFreq.temporalFreq = 0.0f;
        _assignSpatialTempFreq.uvOffset = 0;
        _assignSpatialTempFreq.SetParameters();
        
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
        _temporalFreq = float.Parse(message.values[1].ToString());
        _spatialFreq = float.Parse(message.values[2].ToString());
        
        // Send handshake message to Python process to confirm receipt of parameters
        OscMessage handshake = new OscMessage
        {
            address = "/Handshake"
        };
        handshake.values.Add(_trialNum);
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
    
}

