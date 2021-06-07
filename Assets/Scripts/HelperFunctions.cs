using System;
using UnityEngine;

public class HelperFunctions : MonoBehaviour
{
    public static GameObject[] FindObsWithTag(string tag)
    {
        GameObject[] foundObs = GameObject.FindGameObjectsWithTag(tag);
        Array.Sort(foundObs, CompareObNames);
        return foundObs;
    }

    private static int CompareObNames(GameObject x, GameObject y)
    {
        return x.name.CompareTo(y.name);
    }

    public static Vector3 StringToVector3(string sVector)
    {
        // Taken from user Praveen Ramanayake at https://stackoverflow.com/questions/30959419/converting-string-to-vector-in-unity
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector3 result = new Vector3(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]));

        return result;
    }

    public static Vector4 StringToVector4(string sVector)
    {
        // Modified from user Praveen Ramanayake at https://stackoverflow.com/questions/30959419/converting-string-to-vector-in-unity
        // Remove the parentheses
        if (sVector.StartsWith("(") && sVector.EndsWith(")"))
        {
            sVector = sVector.Substring(1, sVector.Length - 2);
        }

        // split the items
        string[] sArray = sVector.Split(',');

        // store as a Vector3
        Vector4 result = new Vector4(
            float.Parse(sArray[0]),
            float.Parse(sArray[1]),
            float.Parse(sArray[2]),
            float.Parse(sArray[3]));

        return result;
    }

    public static RaycastHit FindSurfaceNormal(Vector3 location, Vector3 localUp)
    {
        Vector3 theRay = -localUp;
        RaycastHit hit;
        Physics.Raycast(location, theRay, out hit, 1f);
        return hit;
    }

    public static float AlignGlobalUp(Transform transform, Vector3 localUp, Vector3 globalUp)
    {
        RaycastHit hit = FindSurfaceNormal(transform.position, localUp);
        float zRot = PosNegAngle(hit.normal, globalUp, transform.forward);
        return zRot;
    }

    public static float PosNegAngle(Vector3 a1, Vector3 a2, Vector3 normal)
    {
        float angle = Vector3.Angle(a1, a2);
        float sign = Mathf.Sign(Vector3.Dot(normal, Vector3.Cross(a1, a2)));
        return angle * sign;
    }

}