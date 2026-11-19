using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Zhuamian_UI : MonoBehaviour
{
    public GameObject obj;
    private bool b = true;
    private void Start()
    {
        if(true)
        {
            obj.SetActive(false);
            b = false;
        }         
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == "Player")
        {
            obj.SetActive(true);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            obj.SetActive(false);
        }
    }
}
