using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


[ExecuteInEditMode]
public class OSC_test : MonoBehaviour
{

    public GameObject Cube;

    // Streaming client
    public OptitrackStreamingClient StreamingClient;

    // OSC communication client
    public OSC osc;

    // Variables for mouse position
  
    // Timer
    private OptitrackHiResTimer.Timestamp reference;
    private float Time_stamp;

    // Public variables for properties that are manipulated
    private int trial_num = 0;
    private bool trialDone = false;
    private bool inTrial = false;
    private float speed;
    private float acceleration;
    private float contrast;
    private float size;    // This is proprtional to radius of a circle
    private float shape;
    private float trajectory;

    private int counter = 0;


    // Use this for initialization
    void Start()
    {

        // set the OSC communication
        osc.SetAddressHandler("/TrialStart", OnReceiveTrialStart);
        osc.SetAddressHandler("/Close", OnReceiveStop);

    }

    // Update is called once per frame
    void Update()
    {
        counter++;


        // --- Check if the trial is done yet --- //
        //checkTrialEnd();
        if (inTrial == true & counter % 600 == 0)
        {
            trialDone = true;
            Debug.Log("Trial Done");
            // Tell Python that the trial ended
            SendTrialEnd();
            // Reset trial_num to zero - this is our flag for being in/out of a trial
            trial_num = 0;
            inTrial = false;
        }

    }


    // --- Handle OSC Communication --- //
  

    void OnReceiveTrialStart(OscMessage message)
    {
        trialDone = false;
        inTrial = true;
        counter = 0;

        // Parse the values for trial setup
        trial_num = message.GetInt(0);

        // Send handshake message to Python process
        OscMessage handshake = new OscMessage();
        handshake.address = "/Handshake";
        handshake.values.Add(trial_num);
        osc.Send(handshake);
    }

    void SendTrialEnd()
    {
        // Send trial end message to Python process
        OscMessage message = new OscMessage();
        message.address = "/EndTrial";
        message.values.Add(trial_num);
        osc.Send(message);
    }

    void OnReceiveStop(OscMessage message)
    {
        // Kill the application
        Application.Quit();
    }

}
