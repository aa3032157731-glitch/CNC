using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class A : MonoBehaviour
{
    private void OnCollisionStay(Collision collision)
    {
        if(collision.transform.tag == "Qualified")
        {
            collision.transform.Translate(Vector3.right * 1 * Time.deltaTime);
        }
        else if (collision.transform.tag == "Unqualified")
        {
            collision.transform.Translate(Vector3.left * 1 * Time.deltaTime);
        }
    }
}
