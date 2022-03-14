using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/*
 This class is a child of the RecorderBase class. It inherits most of its functionality from there.
 See RecorderBase for detailed explanation of what goes on under the hood.
*/

//[ExecuteInEditMode]
public class Recorder_VR_MultiCricket : RecorderBase
{
    // Variables for virtual cricket transforms and states
    private GameObject[] _vrCricketObjects;
    // GameObject _vrCricketInstanceGameObject;
    private Vector3 _vrCricketPosition;
    private Vector3 _vrCricketOrientation;
    private int _state;
    private float _speed;
    private int _encounter;
    private int _motion;

    // Use this for initialization
    protected override void Start()
    {
        // -- Inherit setup from base class
        base.Start();

        // -------------------------------------------
        // Get cricket object array sorted by name/number
        _vrCricketObjects = HelperFunctions.FindObsWithTag("vrCricket");

        // Write header to file
        // There may be or not be a real cricket in this scene
        AssembleHeader(0, _vrCricketObjects.Length, false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        // Get mouse position from OptiTrack and update the tracking square color
        // Note that these functions are defined in the RecorderBase class
        base.Update();

        // Write the mouse data to a string
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
            MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);

        string vrCricketString = GetVRCricketData();
        
        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        string[] allData = { TimeStamp.ToString(), mouseString, vrCricketString, ColorFactor.ToString() };
        allData = allData.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Writer.WriteLine(string.Join(", ", allData));

    }

    string GetVRCricketData()
    {
        string vrCricketString = "";
        
        foreach (GameObject vrCricketObj in _vrCricketObjects)
        {
            // Get the VR cricket position and orientation
            _vrCricketPosition = vrCricketObj.transform.position;
            _vrCricketOrientation = vrCricketObj.transform.rotation.eulerAngles;

            // Get the VR cricket speed and current motion state
            _speed = vrCricketObj.GetComponent<Animator>().GetFloat("speed"); ;
            _state = vrCricketObj.GetComponent<Animator>().GetInteger("state_selector");
            _motion = vrCricketObj.GetComponent<Animator>().GetInteger("motion_selector");
            _encounter = vrCricketObj.GetComponent<Animator>().GetInteger("in_encounter");

            // Concatenate strings for this cricket object, and add to all cricket string
            object[] cricket_data = {
                _vrCricketPosition.x, _vrCricketPosition.y, _vrCricketPosition.z,
                _vrCricketOrientation.x, _vrCricketOrientation.y, _vrCricketOrientation.z,
                _speed, _state, _motion, _encounter 
            };

            string thisVRCricket = string.Join(", ", cricket_data);
            List<string> strArray = new List<string> { vrCricketString, thisVRCricket };

            // Remove leading comma
            vrCricketString = string.Join(", ", strArray.Where(s => !string.IsNullOrEmpty(s)));

        }

        return vrCricketString;
    }
    
}
