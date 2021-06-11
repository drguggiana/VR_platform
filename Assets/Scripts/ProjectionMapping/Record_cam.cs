using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
//using System.Collections;

public class Record_cam : MonoBehaviour {

	public Camera targetCam;

	//string writePath = "C:/Users/drguggiana/Documents/Motive_test1/etc/test.txt";
	//StreamWriter writer = new StreamWriter("C:/Users/drguggiana/Documents/Motive_test1/etc/test.txt", true);

	// Use this for initialization
	void Start () {

	}

	// Update is called once per frame
	void Update () {

		if (Input.GetKeyDown (KeyCode.E)) {
			StreamWriter writer = new StreamWriter ("C:/Users/setup/Documents/Unity_projects/UnityProjectionMapping/Build/cam_parameters.txt", true);

			writer.WriteLine (string.Concat (targetCam.transform.position.x.ToString (), "\r\n", targetCam.transform.position.y.ToString (), "\r\n",targetCam.transform.position.z.ToString (), "\r\n",
				targetCam.transform.eulerAngles.x.ToString (), "\r\n", targetCam.transform.eulerAngles.y.ToString (), "\r\n", targetCam.transform.eulerAngles.z.ToString (), "\r\n",
				targetCam.projectionMatrix[0, 0].ToString (), "\r\n",
				targetCam.projectionMatrix[0, 1].ToString (), "\r\n",
				targetCam.projectionMatrix[0, 2].ToString (), "\r\n",
				targetCam.projectionMatrix[0, 3].ToString (), "\r\n",
				targetCam.projectionMatrix[1, 0].ToString (), "\r\n",
				targetCam.projectionMatrix[1, 1].ToString (), "\r\n",
				targetCam.projectionMatrix[1, 2].ToString (), "\r\n",
				targetCam.projectionMatrix[1, 3].ToString (), "\r\n",
				targetCam.projectionMatrix[2, 0].ToString (), "\r\n",
				targetCam.projectionMatrix[2, 1].ToString (), "\r\n",
				targetCam.projectionMatrix[2, 2].ToString (), "\r\n",
				targetCam.projectionMatrix[2, 3].ToString (), "\r\n",
				targetCam.projectionMatrix[3, 0].ToString (), "\r\n",
				targetCam.projectionMatrix[3, 1].ToString (), "\r\n",
				targetCam.projectionMatrix[3, 2].ToString (), "\r\n",
				targetCam.projectionMatrix[3, 3].ToString () ));
			writer.Close ();
		}
	}

	void OnApplicationQuit(){
		//writer.Close();
	}	
}
