using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignSpatialTempFreq : MonoBehaviour
{

    public enum surfaces
    {
        Sphere,
        Plane
    };

    public surfaces surfaceType;
    public Transform referencePoint;
    public float orientation;
    public float spatialFreq;     // units: cycles/deg
    public float temporalFreq;    // units: cycles/sec (Hz)
    public float uvOffset = 0;

    private Material stripesMaterial;

    private float _Cycles;
    private float gratingSpeed;
    private float distanceToRef;


    // Start is called before the first frame update
    void Start()
    {
        stripesMaterial = GetComponent<Renderer>().material;
        
        // This is interesting because the temporal frequency is set by the linear motion of the UV, and not the stripes
        // themselves. For a sphere, it is the same on a 
        // gratingSpeed = temporalFreq;
        
        // Get distance between center of object and reference transform
        // If a sphere, assumes reference is inside the sphere
        // If a plane, assumes object is perpendicular to the reference and centered on it 
        distanceToRef = Vector3.Distance(this.transform.position, referencePoint.transform.position);
        SetParameters();
    }

    // Update is called once per frame
    void Update()
    {
        uvOffset += gratingSpeed * Time.deltaTime;
        stripesMaterial.SetFloat("_Offset", uvOffset);
    }
    
    public void SetParameters()
    {
        switch (surfaceType)
        {
            case surfaces.Sphere:
                float radius = GetComponent<MeshRenderer>().bounds.size.magnitude / 2.0f;
                _Cycles = calculateCyclesOnSphere(spatialFreq, radius);
                gratingSpeed = temporalFreq;
                break;
            
            case surfaces.Plane:
                float width = GetComponent<MeshRenderer>().bounds.size.x;
                float gamma = subtendedVisualAngle(width, distanceToRef);
                _Cycles = calculateCyclesOnPlane(spatialFreq, gamma);
                gratingSpeed = temporalFreq;    
                break;
            
            default:
                break;
            
        }
        
        // Assign the number of cycles
        stripesMaterial.SetFloat("_Cycles", _Cycles);
        
        // Assign the orientation of the stripes
        stripesMaterial.SetFloat("_Orientation", orientation);
    }
    
    float calculateCyclesOnSphere(float sf, float radius)
    {
        // Assumes that spatial frequency comes in cycles/deg

        // TODO if not tied to player transform, this isn't valid
        // Get distance between center of sphere and reference transform
        // float distance = Vector3.Distance(this.transform.position, referencePoint.transform.position);
        
        return sf * 180.0f;
    }

    float calculateCyclesOnPlane(float sf, float visAngle)
    {
        // Assumes that spatial frequency comes in cycles/deg
        float cycles = sf * visAngle;
        return cycles;
    }

    float arcLength(float radius, float angle)
    {
        // Units are meters
        return radius * angle * ((float)Math.PI / 180.0f);
    }

    float subtendedVisualAngle(float width, float distance)
    {
        // Get visual angle subtended by the plane
        float gamma = 2 * (float) Math.Atan(width / 2.0f / distance) * (180f / (float)Math.PI);
        return gamma;
    }
    
}
