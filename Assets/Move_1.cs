using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move_1 : MonoBehaviour
{
    public GameObject obj1;
    public GameObject obj2;

    public int moveSpeed;

    void Start()
    {
        
    }


    void Update()
    {
        if(Vector3.Distance(this.transform.position,obj1.transform.position) <= 1 || Vector3.Distance(this.transform.position, obj2.transform.position) <= 1)
        {
            moveSpeed = -moveSpeed;
        }

        this.transform.Translate(Vector3.right * moveSpeed * Time.deltaTime, Space.World);
    }
}
