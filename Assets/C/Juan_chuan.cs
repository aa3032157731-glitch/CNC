using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Juan_chuan : MonoBehaviour
{
    public GameObject obj;
    public GameObject nulll;

    private void Start()
    {
        Sc();
    }

    private void OnTriggerExit(Collider other)
    {
        Sc();
    }


    void Sc()
    {
        var temp = Instantiate(obj);
        temp.transform.position = nulll.transform.position;
    }
}
