//======================================================================================================
// Copyright 2016, NaturalPoint Inc.
//======================================================================================================

using System;
using UnityEngine;


public class Optitrack_mod1 : MonoBehaviour
{
    public OptitrackStreamingClient StreamingClient;
    public Int32 RigidBodyId;
    
    private Vector3 _arenaPosition;


    void Start()
    {
        // If the user didn't explicitly associate a client, find a suitable default.
        if (this.StreamingClient == null)
        {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if (this.StreamingClient == null)
            {
                Debug.LogError(GetType().FullName + ": Streaming client not set, and no " + typeof(OptitrackStreamingClient).FullName + " components found in scene; disabling this component.", this);
                this.enabled = false;
                return;
            }
        }

        // MoveParentPivot();
    }


    void Update()
    {
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {
            this.transform.localPosition = rbState.Pose.Position;
            this.transform.localRotation = rbState.Pose.Orientation;
            //Debug.Log(this.transform.localPosition);
        }
    }

    void MoveParentPivot()
    {
        
        
        OptitrackRigidBodyState rbState = StreamingClient.GetLatestRigidBodyState(RigidBodyId);
        if (rbState != null)
        {
            _arenaPosition = new Vector3(-rbState.Pose.Position.x, 0, -rbState.Pose.Position.z);
            
            this.transform.parent.localPosition += _arenaPosition;
        }
    }
}
