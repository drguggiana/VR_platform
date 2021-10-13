using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class AssignGaborParams : MonoBehaviour
{
    public enum surfaces
    {
        Sphere,
        Plane
    };

    public surfaces surfaceType;
    public Transform referencePoint;
    public float spatialFreq;     // units: cycles/deg
    public float temporalFreq;    // units: Hz

    public float gaborRadius;     // units: degrees

    private RenderTexture maskTexture;
    private Material stripesMaterial;
    private Material maskMaterial;
    
    private float _Frequency;
    private float _Offset = 0;
    private float gratingSpeed;
    
    // Start is called before the first frame update
    void Start()
    {
        // Create Mask render texture
        maskTexture = new RenderTexture(64, 64, 16, RenderTextureFormat.RGBAUShort);
        
        stripesMaterial = GetComponent<Renderer>().material;
        maskMaterial = GetComponentInChildren<Renderer>().material;
        
        // Get distance between center of object and reference transform
        // If a sphere, assumes reference is inside the sphere
        // If a plane, assumes object is perpendicular to the reference and centered on it 
        float distanceToRef = Vector3.Distance(this.transform.position, referencePoint.transform.position);

        switch (surfaceType)
        {
            case surfaces.Sphere:
                float radius = GetComponent<MeshRenderer>().bounds.size.magnitude / 2.0f;
                _Frequency = calculateCyclesOnSphere(spatialFreq, radius);
                gratingSpeed = calculateGratingSpeedOnSphere(temporalFreq, spatialFreq, _Frequency, radius);
                break;
            
            case surfaces.Plane:
                float width = GetComponent<MeshRenderer>().bounds.size.x;
                float gamma = subtendedVisualAngle(width, distanceToRef);
                _Frequency = calculateCyclesOnPlane(spatialFreq, gamma);
                gratingSpeed = calculateGratingSpeedOnPlane(temporalFreq, spatialFreq, _Frequency, width, distanceToRef, gamma);
                Debug.Log(gratingSpeed);
                break;
            
            default:
                break;
            
        }
        
        // Assign the number of cycles
        stripesMaterial.SetFloat("_Frequency", _Frequency);
    }

    // Update is called once per frame
    void Update()
    {
        _Offset += gratingSpeed * Time.deltaTime;
        stripesMaterial.SetFloat("_Offset", _Offset);
    }


    void CalculateGaussian(RenderTexture tex, float x0, float y0, float sigma)
    {
        for (int r = 0; r < tex.height; r++)
        {
            for (int c = 0; c < tex.width; c++)
            {
                float x = (float) Math.Pow(c - x0, 2) / (2.0f * sigma);
                float y = (float) Math.Pow(r - y0, 2) / (2.0f * sigma);
                
            }
        }
    }
    
    
    float calculateCyclesOnSphere(float sf, float radius)
    {
        // TODO if not tied to player transform, this isn't valid
        // Get distance between center of sphere and reference transform
        // float distance = Vector3.Distance(this.transform.position, referencePoint.transform.position);
        

        // Assumes that spatial frequency comes in cycles/deg
        float cycles = sf * 180.0f;
        return cycles;
    }
    
    float calculateGratingSpeedOnSphere(float tf, float sf, float numCycles, float radius)
    {
        // Get the unit distance per cycle
        float cycleDist = arcLengthDeg(radius, 1.0f) / (sf * numCycles);
        
        // Multiply by temporal frequency to get the speed of the grating
        return tf * cycleDist;
    }
    
    float calculateCyclesOnPlane(float sf, float visAngle)
    {
        // Assumes that spatial frequency comes in cycles/deg
        float cycles = sf * visAngle;
        return cycles;
    }
    
    float calculateGratingSpeedOnPlane(float tf, float sf, float numCycles, float width, float distance, float visAngle)
    {
        // Get the unit distance per cycle
        float distPerDegree = width / visAngle;
        float cycleDist = width / numCycles;
        Debug.Log(cycleDist);
        
        // Multiply by temporal frequency to get the speed of the grating
        return tf * cycleDist;
    }

    float arcLengthDeg(float radius, float angle)
    {
        return radius * angle * ((float)Math.PI / 180.0f);
    }

    float subtendedVisualAngle(float width, float distance)
    {
        // Get visual angle subtended by the plane
        float gamma = 2 * (float) Math.Atan(width / 2.0f / distance) * (180f / (float)Math.PI);
        return gamma;
    }
}
