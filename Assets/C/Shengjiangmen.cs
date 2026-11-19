using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shengjiangmen : MonoBehaviour
{
    public float speed = 1;
    private bool b = false;

    private void OnCollisionEnter(Collision collision)
    {
        b = true;
    }

    private void Update()
    {
        if (b)
        {
            if (this.transform.position.y >= 10)
            {
                speed = 0;
            }
            this.transform.Translate(Vector3.up * speed * Time.deltaTime);
        }
    }
}
