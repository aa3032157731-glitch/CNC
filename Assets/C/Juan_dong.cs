using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Juan_dong : MonoBehaviour
{
    public float speed;
    public static bool b = true;
    private void OnTriggerStay(Collider other)
    {
        if(b)
        {
            if (other.transform.tag == "Juan")
            {
                other.transform.Translate(-transform.forward * speed * Time.deltaTime);
            }
        }


    }

}
