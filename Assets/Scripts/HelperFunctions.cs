using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class HelperFunctions : MonoBehaviour
{
    
    private string[] mouse_template = {"mouse_x_m", "mouse_y_m", "mouse_z_m",
                                       "mouse_xrot_m", "mouse_yrot_m", "mouse_zrot_m"};

    private string[] realCricket_template = {"cricket_x_m", "cricket_y_m", "cricket_z_m"};
        
    private string[] vrCricket_template = {"_x", "_y", "_z",
                                           "_xrot", "_yrot", "_zrot",
                                           "_speed", "_state", "_motion", "_encounter"};
    
    public void AssembleHeader(StreamWriter writer, bool useRealCricket, bool useVRCricket, int numVRCrickets=0)
    {
        

        
        
        if (useRealCricket)
        {
            string realCricket_string = string.Join(", ", realCricket_template);
        }
        else if (useVRCricket)
        {
            List<string> vrCricket_cols = new List<string>();
            
            // Assemble the VR cricket string depending on how many VR crickets there are
            for (int i=0; i < numVRCrickets; i++)
            {
                string vcricket = "vrcricket_" + i;
                foreach (string ct in vrCricket_template)
                {
                    vrCricket_cols.Add(vcricket + ct);
                }
            }
            
            string vrCricket_string = string.Join(", ", vrCricket_cols);
            
        }
        else
        {
            
        }
        

        
        // Assemble strings for all animals in scene
        string mouse_string = string.Join(", ", mouse_template);
        // string realCricket_string = string.Join(", ", realCricket_template);
        // string vrCricket_string = string.Join(", ", vrCricket_cols);
        //
        // object[] header = {"time_m", mouse_string, realCricket_string, vrCricket_string, "color_factor"};
        
        object[] header = {"time_m", mouse_string, "color_factor"};
        writer.WriteLine(string.Join(", ", header));
    }
    
    public static void LogSceneParams (StreamWriter writer)
    {
        // handle arena corners
        string[] corners = new string[4];

        int i = 0;
        foreach (GameObject corner in FindObsWithTag("Corner"))
        {
            Vector3 corner_position = corner.transform.position;
            object[] corner_coord = {corner_position.x, corner_position.z};
            string arena_corner = "[" + string.Join(",", corner_coord) + "]";
            corners[i] = arena_corner;
            i++;
        }

        string arena_corners_string = string.Concat("arena_corners: ", "[", string.Join(",", corners), "]");
        writer.WriteLine(arena_corners_string);

        // handle any obstacles in the arena. This only logs the centroid of the obstacle
        foreach (GameObject obstacle in HelperFunctions.FindObsWithTag("Obstacle"))
        {
            string this_obstacle = obstacle.name.ToLower();
            Vector3 obstacle_position = obstacle.transform.position;
            object[] obstacle_coords = {obstacle_position.x, obstacle_position.y, obstacle_position.z};
            this_obstacle = string.Concat(this_obstacle + "obs: ", " [", string.Join(",", obstacle_coords), "]");
            writer.WriteLine(this_obstacle);
        }

        // once done, write a blank line
        writer.WriteLine(string.Empty);
    }
    
    
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
            float.Parse(sArray[2])
            );

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