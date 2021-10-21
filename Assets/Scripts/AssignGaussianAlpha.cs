using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AssignGaussianAlpha : MonoBehaviour
{
 public enum surfaces
    {
        Sphere,
        Plane
    };

    public surfaces surfaceType;
    public Transform referencePoint;
    public float gaborSize;     // units: deg

    private Material maskMaterial;
    private RenderTexture maskTexture;
    
    private float _GaborSize;
    private int _Invert;


    // Start is called before the first frame update
    void Start()
    {
        maskMaterial = GetComponent<Renderer>().material;
        maskTexture = RenderTexture.GetTemporary(64, 64, 16, RenderTextureFormat.R8);
        
        // Get distance between center of object and reference transform
        // If a sphere, assumes reference is inside the sphere
        // If a plane, assumes object is perpendicular to the reference and centered on it 
        float distanceToRef = Vector3.Distance(this.transform.position, referencePoint.transform.position);

        switch (surfaceType)
        {
            case surfaces.Sphere:
                float radius = GetComponent<MeshRenderer>().bounds.size.magnitude / 2.0f;

                break;
            
            case surfaces.Plane:
                float width = GetComponent<MeshRenderer>().bounds.size.x;
                float gamma = subtendedVisualAngle(width, distanceToRef);


                break;
            
            default:
                break;
            
        }
        
        // Assign the number of cycles
        maskMaterial.SetFloat("_GaborSize", _GaborSize);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void SetInvert(int invertVal)
    {
        maskMaterial.SetInt("_Invert", invertVal);
    }
    
    float subtendedVisualAngle(float width, float distance)
    {
        // Get visual angle subtended by the plane
        float gamma = 2 * (float) Math.Atan(width / 2.0f / distance) * (180f / (float)Math.PI);
        return gamma;
    }
}
