using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;

public class Load_camParameters : MonoBehaviour
{

    public Camera cam;
    //    private string load_path;
    // Use this for initialization
    void Start()
    {

        // define the loading path
        string path = Paths.cam_parameters_path;
        // create the streamreader element 
        StreamReader reader = new StreamReader(path);
        // read the entire document
        string param_info = reader.ReadToEnd();
        // close the file
        reader.Close();
        // split the text from the files into lines
        string[] lines = Regex.Split(param_info, "\r\n");

        // parse the lines one by one
        Vector3 cam_position = new Vector3(float.Parse(lines[0]), float.Parse(lines[1]), float.Parse(lines[2]));
        Vector3 cam_rotation = new Vector3(float.Parse(lines[3]), float.Parse(lines[4]), float.Parse(lines[5]));

        Matrix4x4 proj_matrix = new Matrix4x4();

        proj_matrix[0, 0] = float.Parse(lines[6]);
        proj_matrix[0, 1] = float.Parse(lines[7]);
        proj_matrix[0, 2] = float.Parse(lines[8]);
        proj_matrix[0, 3] = float.Parse(lines[9]);

        proj_matrix[1, 0] = float.Parse(lines[10]);
        proj_matrix[1, 1] = float.Parse(lines[11]);
        proj_matrix[1, 2] = float.Parse(lines[12]);
        proj_matrix[1, 3] = float.Parse(lines[13]);

        proj_matrix[2, 0] = float.Parse(lines[14]);
        proj_matrix[2, 1] = float.Parse(lines[15]);
        proj_matrix[2, 2] = float.Parse(lines[16]);
        proj_matrix[2, 3] = float.Parse(lines[17]);

        proj_matrix[3, 0] = float.Parse(lines[18]);
        proj_matrix[3, 1] = float.Parse(lines[19]);
        proj_matrix[3, 2] = float.Parse(lines[20]);
        proj_matrix[3, 3] = float.Parse(lines[21]);


        cam.projectionMatrix = proj_matrix;
        cam.transform.position = cam_position;
        cam.transform.eulerAngles = cam_rotation;

    }


    void Update()
    {

    }

}