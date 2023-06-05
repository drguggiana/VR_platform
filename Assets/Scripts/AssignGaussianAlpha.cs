using System;
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
    public float gaborSizeDeg = 50f;     // units: deg
    public int invertState = 1;
    
    private Material maskMaterial;
    private float _Sigma;


    // Start is called before the first frame update
    void Start()
    {
        maskMaterial = GetComponent<Renderer>().material;
        
        // Get distance between center of object and reference transform
        // If a sphere, assumes reference is inside the sphere
        // If a plane, assumes object is perpendicular to the reference and centered on it 
        float distanceToRef = Vector3.Distance(this.transform.position, referencePoint.transform.position);

        switch (surfaceType)
        {
            case surfaces.Sphere:
                float radius = GetComponent<MeshRenderer>().bounds.size.magnitude / 2.0f;
                _Sigma = calculateGaussianSigma(gaborSizeDeg, radius);
                break;
            
            case surfaces.Plane:
                _Sigma = calculateGaussianSigma(gaborSizeDeg, distanceToRef);
                break;
            
            default:
                break;
            
        }   
        
        // Assign the Guassian standard deviation
        maskMaterial.SetFloat("_Sigma", _Sigma);
    }
    
    public void SetInvert(int invertVal)
    {
        invertState = invertVal;
        maskMaterial.SetInt("_Invert", invertVal);
    }

    float calculateGaussianSigma(float visAngle, float distance)
    {
        // Calculates the standard deviation needed for the Gaussian given the desired subtended visual angle.
        // Uses full width at tenth of max (FWTM - the inflection point of the Gaussian) as the window size
        float windowWidth = distance * (float) Math.Tan(visAngle * (float) Math.PI / 180f);
        float sigma = 1 - (windowWidth / 4.29193f);
        
        return sigma;
    }
}
