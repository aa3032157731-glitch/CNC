using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Image : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        b = true;
    }

    public GameObject obj;
    public Transform v;
    public int Speed = 1;

    private float f = 25f;
    private float time = 0.0f;
    public int i = 0;
    public static bool b = false;



    void Update()
    {
        if (b)
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
