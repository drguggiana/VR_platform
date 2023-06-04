using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/*
 This class is a child of the RecorderBase class. It inherits most of its functionality from there.
 See RecorderBas.cs for detailed explanation of what goes on under the hood.
 
 This class is responsible for sending and receiving information about the trial parameters from
 Python, handling trial timing, logging the mouse position from the Optitrack system, and logging 
 the current time and color of the tracking square.
*/

[ExecuteInEditMode]
public class Recorder_VR_SpatioTemporalTuning : RecorderBase
{
    // Private variables for the Gabor stimulus
    public GameObject gaborStim;
    private AssignSpatialTempFreq _assignSpatialTempFreq;
    private AssignGaussianAlpha _assignGaussianAlpha;
    private float _orientation = 60.0f;
    private float _spatialFreq = 0.04444f;
    private float _temporalFreq = 0.5f;

    // Private variables for the trial structure
    private float _trialTimer = 0f;
    private int _trialNumBuffer = 0;    // This is a variable that temporarily holds the trial number
    private int _trialNum = -1; // trial number from Python
    private int _trialNumWrite = -1; // trial number actually saved
    private bool _inTrial = false;
    private float _trialDuration = 5;         // In seconds
    private float _interStimulusInterval = 5;     // In seconds
    
    // Private variables for shadow exclusion
    private float[] _shadowEdgeLeft = new float[2];
    private float[] _shadowEdgeRight = new float[2];
    private int centerGabor;
    private int widthGabor;
    private int overlapThreshold;

    // Start is called before the first frame update
    protected override void Start()
    {
        // -- Inherit setup from base class
        base.Start();
        
        // -------------------------------------------
        // -- These are specific to the particular scene
        
        // Note that OnReceiveExpSetup is overridden from the base class
        osc.SetAddressHandler("/SetupTrial", OnReceiveTrialSetup);

        // Get the scripts for the gabor assignments
        _assignSpatialTempFreq = gaborStim.GetComponent<AssignSpatialTempFreq>();
        _assignGaussianAlpha = gaborStim.GetComponentInChildren<AssignGaussianAlpha>();
        
        // Get the vertices of the shadow for calculation and set overlap variables
        float[] _shadowBoundaries = SetupShadowExclusion();
        Array.Copy(_shadowBoundaries, 0, _shadowEdgeLeft, 0, 2);
        Array.Copy(_shadowBoundaries, 2, _shadowEdgeRight, 0, 2);
        centerGabor = (int) gaborStim.transform.GetChild(0).transform.rotation.z; //Paths.shadow_boundaries[4];
        widthGabor = (int) _assignGaussianAlpha.gaborSizeDeg;   //Paths.shadow_boundaries[5];
        // overlapThreshold = Paths.shadow_overlap_threshold; //Paths.shadow_boundaries[6];

        // This function overrides the one found in the RecorderBase class
        AssembleHeader();
        
        // Get the recorder out of wait
        SendReleaseWait();
    }

    // Update is called once per frame
    protected override void Update()
    {

        // Wait for the recorder to signal
        if (Release == false)
        {
            return;
        }

        // --- Handle trial structure --- //
        if (InSession)
        {
            // if (GateTrial())
            // {
            //     // advance the timer and record the actual trial number
            //     _trialTimer += Time.deltaTime;
            //     _trialNumWrite = _trialNum;
            // }
            // else
            // {
            //     // timer stays and we tag the frames
            //     _trialNumWrite = -2;
            // }
            
            // advance the timer and record the actual trial number
            _trialTimer += Time.deltaTime;
            _trialNumWrite = _trialNum;

            if (_inTrial)
            {
                if (_trialTimer > _trialDuration)
                {
                    EndTrial();
                }
            }
            else
            {
                if (_trialTimer > _interStimulusInterval)
                {
                    StartTrial();
                }
            }
        }
        
        // Get mouse position from OptiTrack and update the tracking square color
        // Note that these functions are defined in the RecorderBase class
        base.Update();
        
        // --- Handle mouse data --- //
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
                               MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);
        
        // --- Data Saving --- //
        string[] allData = { TimeStamp.ToString(), _trialNumWrite.ToString(), mouseString, _assignSpatialTempFreq.uvOffset.ToString(), ColorFactor.ToString() };
        allData = allData.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Writer.WriteLine(string.Join(", ", allData));
    }
    
    // --- Overrides from base --- //
    protected override void OnReceiveSessionStart(OscMessage message)
    {
        base.OnReceiveSessionStart(message);
        _trialNum = 0;

    }
    
    // *** Functions that are unique to this scene *** //
    
    // Handle shadow exclusion
    float[] SetupShadowExclusion()
    {
        // Get the x and z coordinates of the shadow region boundaries
        
        float[] globalTopCorners = new float[4];
        int i = 0;
        foreach (GameObject corner in HelperFunctions.FindObsWithTag("ShadowEdge"))
        {
            Vector3 cornerPosition = corner.transform.localPosition;
            globalTopCorners[i] = cornerPosition.x;
            i++;
            globalTopCorners[i] = cornerPosition.z;
            i++;
        }
        return globalTopCorners;
    }

    bool GateTrial()
    {
        if (_inTrial)
        {
            // Goal is to get the angle subtended by the shadow

            // determine the vectors to the shadow from the head of the mouse (correcting position units to mm)
            float xVectorLeftRelative = (_shadowEdgeLeft[0] - MousePosition.x) * 1000;
            float zVectorLeftRelative = (_shadowEdgeLeft[1] - MousePosition.z) * 1000;
            float xVectorRightRelative = (_shadowEdgeRight[0] - MousePosition.x) * 1000;
            float zVectorRightRelative = (_shadowEdgeRight[1] - MousePosition.z) * 1000;
            
            // based on these vectors, determine the heading angles of the shadow wrt the 0 azimuth of the mouse
            float angleLeftAbsolute = Mathf.Atan2(zVectorLeftRelative, -xVectorLeftRelative) * Mathf.Rad2Deg;
            float angleRightAbsolute = Mathf.Atan2(zVectorRightRelative, -xVectorRightRelative) * Mathf.Rad2Deg;
            
            // get the center angle and width
            float widthShadow = Mathf.Abs(Mathf.DeltaAngle(angleLeftAbsolute, angleRightAbsolute));
            float centerShadowAbsolute = widthShadow / 2 + angleLeftAbsolute;
            
            // convert the mouse orientation to -180-180 coordinates from 0-360
            float mouseCorrectedOri;
            if (MouseOrientation.y > 180)
            {
                mouseCorrectedOri = MouseOrientation.y - 360;
            }
            else
            {
                mouseCorrectedOri = MouseOrientation.y;    
            }
            // convert the center to relative to mouse heading
            float centerShadowRelative =  Mathf.DeltaAngle(mouseCorrectedOri, centerShadowAbsolute);
            
            // quantify the overlap with the visual field
            float overlap = Mathf.Clamp(Mathf.Min(widthShadow, widthGabor) 
                            - Mathf.Abs(Mathf.DeltaAngle(centerGabor, centerShadowRelative))
                            + Mathf.Abs(widthShadow - widthGabor) / 2, 0.0f, 360);
           
            // compare to a threshold and output the boolean result
            if (overlap > overlapThreshold)
            {
                // _assignSpatialTempFreq.uvOffset = 0;
                if (_assignGaussianAlpha.invertState == 0)
                {
                    _assignGaussianAlpha.SetInvert(1);
                } 
                return false;
            }
            
            if (_assignGaussianAlpha.invertState == 1)
            {
                _assignGaussianAlpha.SetInvert(0);
            } 
            return true;
        }

        return true;
    }


    // --- Functions for handling the trial structure of the task --- //
    void StartTrial()
    {
        // handle booleans
        _inTrial = true;
        
        // Assign trial number for recording
        _trialNum = _trialNumBuffer;
        
        // Assign the orientation, spatial and temporal frequencies and update the stripes material
        _assignSpatialTempFreq.orientation = _orientation;
        _assignSpatialTempFreq.spatialFreq = _spatialFreq;
        _assignSpatialTempFreq.temporalFreq = _temporalFreq;
        _assignSpatialTempFreq.uvOffset = 0;
        _assignSpatialTempFreq.SetParameters();
        
        // Open up the alpha mask
        _assignGaussianAlpha.SetInvert(0);

        // Reset trial timer
        _trialTimer = 0;
    }
    
    void EndTrial()
    {
        // Set boolean to tell us we are now in done with a trial
        _inTrial = false;
        
        // Close the Alpha mask
        _assignGaussianAlpha.SetInvert(1);
        
        // Reset the temporal frequencies and update the stripes material
        _assignSpatialTempFreq.temporalFreq = 0.0f;
        _assignSpatialTempFreq.uvOffset = 0;
        _assignSpatialTempFreq.SetParameters();
        
        // Send message to the Python process that the trial is over
        SendTrialEnd();
        
        // Reset trial number
        _trialNum = 0;
        
        // Reset trial timer
        _trialTimer = 0;
    }
    
    void OnReceiveTrialSetup(OscMessage message)
    {
        // Parse the values for trial structure setup
        _trialDuration = float.Parse(message.values[4].ToString());
        _interStimulusInterval = float.Parse(message.values[5].ToString());
        _trialNumBuffer = int.Parse(message.values[0].ToString());
        _orientation = float.Parse(message.values[1].ToString());
        _temporalFreq = float.Parse(message.values[2].ToString());
        _spatialFreq = float.Parse(message.values[3].ToString());
        
        // Send handshake message to Python process to confirm receipt of parameters
        OscMessage handshake = new OscMessage
        {
            address = "/TrialReceived"
        };
        handshake.values.Add(_trialNumBuffer);
        osc.Send(handshake);
    }
    
    void SendTrialEnd()
    {
        // Send trial end message to Python process
        OscMessage message = new OscMessage
        {
            address = "/EndTrial"
        };
        message.values.Add(_trialNum);
        osc.Send(message);
    }

    void SendReleaseWait()
    {
        // Release the recorder from waiting
        OscMessage message = new OscMessage()
        {
            address = "/ReleaseWait"
        };
        message.values.Add("device");
        osc.Send(message);
    }
    
    // Functions for writing header of data file
    void AssembleHeader ()
    {
        string[] header = {"time_m", "trial_num",
                           "mouse_x_m", "mouse_y_m", "mouse_z_m",
                           "mouse_xrot_m", "mouse_yrot_m", "mouse_zrot_m",
                           "grating_phase", "color_factor"};

        Writer.WriteLine(string.Join(",", header));
    }
    
}

