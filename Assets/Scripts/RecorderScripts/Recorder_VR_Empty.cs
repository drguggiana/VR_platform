using System.Linq;
using UnityEngine;

/*
 This class is a child of the RecorderBase class. It inherits most of its functionality from there.
 See RecorderBase for detailed explanation of what goes on under the hood.
 
 This class is responsible for logging the mouse position from the Optitrack system and the current 
 time and color of the tracking square.
*/

//[ExecuteInEditMode]
public class Recorder_VR_Empty : RecorderBase
{
    // Use this for initialization
    protected override void Start()
    {
        // -- Inherit setup from base class
        base.Start();

        // -------------------------------------------
        // Write header to file. This function is inherited from the RecorderBase class
       AssembleHeader(0, 0, false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        // Get mouse position from OptiTrack and update the tracking square color
        // Note that these functions are defined in the RecorderBase class
        base.Update();
        
        // --- Handle mouse data --- //
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
                               MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);

        // --- Data Saving --- //
        string[] allData = { TimeStamp.ToString(), mouseString, ColorFactor.ToString() };
        allData = allData.Where(x => !string.IsNullOrEmpty(x)).ToArray();
        Writer.WriteLine(string.Join(", ", allData));
    }
    

}