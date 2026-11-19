using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class juancun : MonoBehaviour
{
    public GameObject obj;
    private GameObject o;
    public int i = 0;
    private void OnCollisionEnter(Collision collision)
    {
        if (i < 17)
        {
            ++i;
            CNC.DigitalTwin.SimulationState.Instance?.SetWarehouseSlotCount(i);
            switch (i)
            {
                case 1:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(130.5f, 1.6f, 99.2f);
                    break;
                case 2:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(130.5f, 1.6f, 104.2f);
                    break;
                case 3:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(130.5f, 1.6f, 108.6f);
                    break;
                case 4:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(130.5f, 1.6f, 113.8f);
                    break;
                case 5:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(125.5f, 1.6f, 99.2f);
                    break;
                case 6:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(125.5f, 1.6f, 104.2f);
                    break;
                case 7:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(125.5f, 1.6f, 108.6f);
                    break;
                case 8:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(125.5f, 1.6f, 113.8f);
                    break;
                case 9:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(120.5f, 1.6f, 99.2f);
                    break;
                case 10:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(120.5f, 1.6f, 104.2f);
                    break;
                case 11:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(120.5f, 1.6f, 108.6f);
                    break;
                case 12:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(120.5f, 1.6f, 113.8f);
                    break;
                case 13:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(115.5f, 1.6f, 99.2f);
                    break;
                case 14:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(115.5f, 1.6f, 104.2f);
                    break;
                case 15:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(115.5f, 1.6f, 108.6f);
                    break;
                case 16:
                    o = Instantiate(obj);
                    o.transform.position = new Vector3(115.5f, 1.6f, 113.8f);
                    break;
            }
        }
    }
}
