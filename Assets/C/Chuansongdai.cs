using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chuansongdai : MonoBehaviour
{
    public float speed;
    private void OnCollisionStay(Collision collision)
    {
        collision.transform.Translate(Vector3.right * speed * Time.deltaTime);
    }

}
