using UnityEngine;

public class Xishadong : MonoBehaviour
{
    public float speed;
    public float time;

    void Update()
    {
        var sim = CNC.DigitalTwin.SimulationState.Instance;
        float mult = (sim != null) ? (sim.isRunning ? sim.simSpeed : 0f) : 1f;
        float dt = Time.deltaTime * mult;
        if (dt <= 0f) return;

        transform.Translate(Vector3.left * speed * dt);
        time += dt;

        if (time >= 8f)
        {
            speed = -speed;
            time = 0f;
        }
    }
}
