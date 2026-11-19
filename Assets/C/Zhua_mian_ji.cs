using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zhua_mian_ji : MonoBehaviour
{
    public GameObject obj;
    public Transform v;
    public int Speed = 1;

    private float f = 40f;
    public float time = 0.0f;
    public int i = 0;
    private bool b = false;



    void Update()
    {
        if(b)
        {
            obj.transform.Translate(Vector3.up * Speed * Time.deltaTime);

            if (obj.transform.position.z <= v.position.z - 3.8f)
            {
                Speed = -1;
                i++;
            }

            if (i >= 1)
            {
                time += Time.deltaTime;
                i = 1;
            }

            if (time >= f)
            {
                Speed = 1;
                time = 0;
                i--;
            }
        }
    }
}

