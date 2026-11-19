using UnityEngine;

public class UIMgr : MonoBehaviour
{
    public GameObject obj;

    private void Update()
    {
        // Superseded by CNC.DigitalTwin.MachineRaycaster & DigitalTwinHUD
        if (CNC.DigitalTwin.DigitalTwinHUD.Instance != null)
        {
            if (obj != null && obj.activeSelf) obj.SetActive(false);
            enabled = false;
            return;
        }
        if (obj == null) return;

        RaycastHit[] r = Physics.RaycastAll(transform.position, transform.forward, 1000f, 1 << LayerMask.NameToLayer("JiQi"));
        obj.SetActive(r.Length > 0);
    }
}
