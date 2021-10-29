using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using System;


//[ExecuteInEditMode]
public class Recorder_VR_MultiCricket : RecorderBase
{
    // Variables for cricket transforms and states
    private GameObject[] vrCricketObjs;
    GameObject cricket_obj;
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
    private string mouse_string;
    private string cricket_string = "";
    private string this_cricket;

    private int count = 0;


    // Use this for initialization
    protected override void Start()
    {
        
        base.Start();

        // Get cricket object array sorted by name/number
        vrCricketObjs = HelperFunctions.FindObsWithTag("vrCricket");

        // Write header to file
        // There is always at least one real cricket in this scene
        AssembleHeader(1, vrCricketObjs.Length, false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        count += 1;
        base.Update();

        // Write the mouse data to a string
        object[] mouse_data = { MousePosition.x, MousePosition.y, MousePosition.z, 
                                MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        mouse_string = string.Join(", ", mouse_data);

        string vrCricketString = GetVRCricketData();


        // --- Data Saving --- //

        // Write the mouse and VR cricket info
        object[] all_data = { Time_stamp, mouse_string, vrCricketString, color_factor };
        writer.WriteLine(string.Join(", ", all_data));

        cricket_string = "";

    }

    string GetVRCricketData()
    {
        string vrCricketString = "";
        
        foreach (GameObject vrCricketObj in vrCricketObjs)
        {
            // Get the VR cricket position and orientation
            vrCricketPosition = vrCricketObj.transform.position;
            vrCricketOrientation = vrCricketObj.transform.rotation.eulerAngles;

            // Get the VR cricket speed and current motion state
            speed = vrCricketObj.GetComponent<Animator>().GetFloat("speed"); ;
            state = vrCricketObj.GetComponent<Animator>().GetInteger("state_selector");
            motion = vrCricketObj.GetComponent<Animator>().GetInteger("motion_selector");
            encounter = vrCricketObj.GetComponent<Animator>().GetInteger("in_encounter");

            // Concatenate strings for this cricket object, and add to all cricket string
            object[] cricket_data = {
                vrCricketPosition.x, vrCricketPosition.y, vrCricketPosition.z,
                vrCricketOrientation.x, vrCricketOrientation.y, vrCricketOrientation.z,
                speed, state, motion, encounter 
            };

            string thisVRCricket = string.Join(", ", cricket_data);
            List<string> strArray = new List<string> { vrCricketString, thisVRCricket };

            // Remove leading comma
            vrCricketString = string.Join(", ", strArray.Where(s => !string.IsNullOrEmpty(s)));

        }

        return vrCricketString;
    }


}
