using UnityEngine;

/*
 This class is a child of the RecorderBase class. It inherits most of its functionality from there.
 See RecorderBase for detailed explanation of what goes on under the hood.
 
 This class is responsible for logging the mouse position from the Optitrack system and the current 
 time and color of the tracking square.
*/

[ExecuteInEditMode]
public class Recorder_VR_Empty : RecorderBase
{

    // Use this for initialization
    protected override void Start()
    {
       base.Start();

       // Write header to file. This function is inherited from the RecorderBase class
       AssembleHeader(1, 0, false);
    }

    // Update is called once per frame
    protected override void Update()
    {
        // Call the base update function to get mouse position from OptiTrack and
        // update the tracking square color
        base.Update();
        
        // --- Handle mouse data --- //
        object[] mouseData = { MousePosition.x, MousePosition.y, MousePosition.z, 
                               MouseOrientation.x, MouseOrientation.y, MouseOrientation.z };
        string mouseString = string.Join(", ", mouseData);

        // --- Data Saving --- //
        string[] allData = { TimeStamp.ToString(), mouseString, ColorFactor.ToString() };
        Writer.WriteLine(string.Join(", ", allData));
    }

}