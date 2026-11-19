using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Juan_dong2 : MonoBehaviour
{

    private void OnTriggerStay(Collider other)
    {
        Juan_dong.b = false;
    }
    private void OnTriggerExit(Collider other)
    {
        Juan_dong.b = true;
    }
}
