using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UIMgr : MonoBehaviour
{
    public GameObject obj;

    private void Update()
    {
        RaycastHit[] r = Physics.RaycastAll(this.transform.position, this.transform.forward, 1000, 1 << LayerMask.NameToLayer("JiQi"));

        if(r.Length == 0)
            obj.gameObject.SetActive(false);

        for (int i = 0; i <r.Length ; i++)
        {
            obj.gameObject.SetActive(true);
        }
        
    }
}
