using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunchujuan : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.transform.tag == "Juan")
            Destroy(collision.gameObject);

    }

}
