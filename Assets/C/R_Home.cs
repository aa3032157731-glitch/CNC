using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R_Home : MonoBehaviour
{
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
        {
            gameObject.transform.position = new Vector3(40, 8, 380);
        }
    }

}
