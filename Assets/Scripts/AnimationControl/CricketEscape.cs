using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class CricketEscape : MonoBehaviour
{

    void OnTriggerEnter(Collider c)
    {
        //Debug.Log(c.tag);
        if (c.CompareTag("Player"))
        {
            gameObject.GetComponentInParent<WanderingAI_escape>().Escape();
        }
    }

}