using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Xishadong : MonoBehaviour
{
    public float speed;
    public float time;

    void Update()
    {
        transform.Translate(Vector3.left * speed * Time.deltaTime);
        time += Time.deltaTime;

        if (time >= 8 && time <9)
        {
            speed = -speed;
            time = 0;
        }
    }
}
