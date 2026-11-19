using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject obj;
    public float f = 2f;
    public float timef = 0.0f;

    public float Speed;
    private void OnCollisionStay(Collision collision)
    {
        collision.transform.Translate(Vector3.forward * Speed * Time.deltaTime);
    }


    private void Update()
    {
        timef += Time.deltaTime;
        if (timef >= f)
        {
            Spawn();
            timef = 0.0f;
        }

    }


    void Spawn()
    {
        int i = Random.Range(0, 101);
        Vector3 v = new Vector3(0, 1, -50);
        GameObject o = Instantiate(obj, v, transform.rotation);

        if(i <= 90)
        {
            o.tag = "Qualified";
            o.GetComponent<MeshRenderer>().material.color = Color.green;
        }
        else
        {
            o.tag = "Unqualified";
            o.GetComponent<MeshRenderer>().material.color = Color.red;
        }
       
    }
}
