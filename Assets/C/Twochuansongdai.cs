using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Twochuansongdai : MonoBehaviour
{
    public float speed;
    private void OnCollisionStay(Collision collision)
    {
        collision.transform.Translate(Vector3.down * speed * Time.deltaTime);
    }

}
