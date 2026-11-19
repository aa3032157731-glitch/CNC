using UnityEngine;

public class Chuansong : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (CNC.DigitalTwin.CameraRigManager.Instance != null)
            {
                CNC.DigitalTwin.CameraRigManager.Instance.FocusHall(CNC.DigitalTwin.WorkshopHall.InspectionWarehouse, false);
            }
            else
            {
                other.gameObject.transform.position = new Vector3(39.5f, 3.88f, 358f);
            }
        }
    }
}
