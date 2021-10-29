using UnityEngine;
using System;

public class DemoRecorderBase : MonoBehaviour
{
    // Streaming client
    public OptitrackStreamingClient streamingClient;
    public Int32 rigidBodyId;
    
    // Variables for tracking square
    public GameObject trackingSquare;
    private Material _trackingSqaureMaterial;
    private float _colorFactor = 0.0f;

    // Variables for mouse position
    public GameObject mouse;
    private Vector3 _mousePosition;
    private Vector3 _mouseOrientation;
    
    // Timer
    private OptitrackHiResTimer.Timestamp _reference;
    private float _timeStamp;
    
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        // Force full screen on projector
        Screen.fullScreen = true;

        // Get the reference timer
        _reference = OptitrackHiResTimer.Now();

        // Set up the camera (so it doesn't clip objects too close to the mouse)
        Camera cam = GetComponentInChildren<Camera>();
        cam.nearClipPlane = 0.000001f;

        // Get the tracking square material
        _trackingSqaureMaterial = trackingSquare.GetComponent<Renderer>().sharedMaterial;

    }

    // Update is called once per frame
    protected virtual void Update()
    {
        SetTrackingSqaure();
        GetMousePosition();
    }
    
    public void SetTrackingSqaure()
    {
        // create the color for the square
        Color new_color = new Color(_colorFactor, _colorFactor, _colorFactor, 1f);
        // put it on the square 
        _trackingSqaureMaterial.SetColor("_Color", new_color);
        // Define the color for the next iteration (switch it)
        if (_colorFactor > 0.0f)
        {
            _colorFactor = 0.0f;
        }
        else
        {
            _colorFactor = 1.0f;
        }
    }
    
    public void GetMousePosition()
    {
        OptitrackRigidBodyState rbState = streamingClient.GetLatestRigidBodyState(rigidBodyId);
        if (rbState != null)
        {
            // get the position of the mouse Rigid Body
            _mousePosition = rbState.Pose.Position;
            // change the transform position of the game object
            this.transform.localPosition = _mousePosition;
            // also change its rotation
            this.transform.localRotation = rbState.Pose.Orientation;
            // turn the angles into Euler (for later printing)
            _mouseOrientation = this.transform.eulerAngles;
            // get the timestamp 
            _timeStamp = rbState.DeliveryTimestamp.SecondsSince(_reference);
        }
        else
        {
            _mousePosition = mouse.transform.position;
            _mouseOrientation = mouse.transform.rotation.eulerAngles;
        }
    }
}
