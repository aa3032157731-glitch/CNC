using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    public GameObject obj;
    public float f = 2f;
    public float timef = 0.0f;
    public float Speed;

    private void OnCollisionStay(Collision collision)
    {
        var sim = CNC.DigitalTwin.SimulationState.Instance;
        float mult = (sim != null) ? (sim.isRunning ? sim.simSpeed : 0f) : 1f;
        collision.transform.Translate(Vector3.forward * Speed * mult * Time.deltaTime);
    }

    private void Update()
    {
        var sim = CNC.DigitalTwin.SimulationState.Instance;
        float mult = (sim != null) ? (sim.isRunning ? sim.simSpeed : 0f) : 1f;
        if (mult <= 0f || obj == null) return;

        timef += Time.deltaTime * mult;
        if (timef >= f)
        {
            Spawn();
            timef = 0.0f;
        }
    }

    void Spawn()
    {
        int i = Random.Range(0, 101);
        Vector3 v = new Vector3(0, 1, -50);
        GameObject o = Instantiate(obj, v, transform.rotation);
        bool qualified = i <= 90;

        o.tag = qualified ? "Qualified" : "Unqualified";
        var mr = o.GetComponent<MeshRenderer>();
        if (mr != null)
            mr.material.color = qualified ? Color.green : Color.red;

        CNC.DigitalTwin.SimulationState.Instance?.RecordInspectionResult(qualified);
        Destroy(o, 25f); // Prevent unbounded memory leak
    }
}
