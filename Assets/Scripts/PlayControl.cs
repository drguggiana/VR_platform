using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayControl : MonoBehaviour
{

    public float movementSpeed = 5.0f;
    public float mouseSensitivity = 3.0f;
    public Transform camObj;
    public float udRange = 60.0f;
    public float jumpSpeed = 20;

    float rotUD = 0;

    float vertVel = 0;

    CharacterController cc;


    // Use this for initialization
    void Start()
    {
        Cursor.visible = false;
        cc = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {

        // Rotation
        float rotLR = Input.GetAxis("Mouse X") * mouseSensitivity;

        transform.Rotate(0, rotLR, 0);

        rotUD -= Input.GetAxis("Mouse Y");
        rotUD = Mathf.Clamp(rotUD, -udRange, udRange);

        camObj.localRotation = Quaternion.Euler(rotUD, 0, 0);

        // Movement

        float forwardSpeed = Input.GetAxis("Vertical") * movementSpeed;
        float sideSpeed = Input.GetAxis("Horizontal") * movementSpeed;

        vertVel += Physics.gravity.y * Time.deltaTime;

        if (cc.isGrounded && Input.GetButtonDown("Jump"))
        {
            vertVel = jumpSpeed;
        }

        Vector3 speed = new Vector3(sideSpeed, vertVel, forwardSpeed);


        speed = transform.rotation * speed;


        //cc.SimpleMove(speed);

        cc.Move(speed * Time.deltaTime);

    }
}
