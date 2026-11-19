using UnityEngine;

public class Quit : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (CNC.DigitalTwin.DigitalTwinHUD.Instance != null)
                CNC.DigitalTwin.DigitalTwinHUD.Instance.ShowQuitModal();
        }
    }
}
