using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chuansong : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.tag == "Player")
        {
            other.gameObject.transform.position = new Vector3(40, 8, 380);
        }       
    }


}
