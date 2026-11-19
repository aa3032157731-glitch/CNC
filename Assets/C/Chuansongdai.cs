using UnityEngine;

public class Chuansongdai : MonoBehaviour
{
    public float speed;

    private void OnCollisionStay(Collision collision)
    {
        // Only convey textile material items — NEVER push the player!
        string t = collision.transform.tag;
        if (t != "Juan" && t != "Qualified" && t != "Unqualified")
            return;

        var sim = CNC.DigitalTwin.SimulationState.Instance;
        float mult = (sim != null) ? (sim.isRunning ? sim.simSpeed : 0f) : 1f;
        if (mult <= 0f) return;

        collision.transform.Translate(Vector3.right * speed * mult * Time.deltaTime);
    }
}
