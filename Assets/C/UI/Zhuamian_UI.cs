using UnityEngine;

public class Zhuamian_UI : MonoBehaviour
{
    public GameObject obj;

    private void Start()
    {
        if (obj != null) obj.SetActive(false);
        if (CNC.DigitalTwin.DigitalTwinHUD.Instance != null)
            enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (obj != null && other.CompareTag("Player"))
            obj.SetActive(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (obj != null && other.CompareTag("Player"))
            obj.SetActive(false);
    }
}
